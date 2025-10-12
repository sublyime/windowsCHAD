import { app, BrowserWindow, ipcMain } from 'electron';
import * as path from 'path';
import { WeatherService } from './services/WeatherService';
import { DispersionModelingService } from './services/DispersionModelingService';
import { DatabaseService } from './services/DatabaseService';

class MainApplication {
  private mainWindow: BrowserWindow | null = null;
  private weatherService: WeatherService;
  private dispersionService: DispersionModelingService;
  private databaseService: DatabaseService;

  constructor() {
    this.weatherService = new WeatherService();
    this.dispersionService = new DispersionModelingService();
    this.databaseService = new DatabaseService();
  }

  public async initialize(): Promise<void> {
    await app.whenReady();
    await this.createMainWindow();
    await this.setupIpcHandlers();
    await this.initializeServices();
  }

  private async createMainWindow(): Promise<void> {
    this.mainWindow = new BrowserWindow({
      width: 1400,
      height: 900,
      webPreferences: {
        nodeIntegration: false,
        contextIsolation: true,
        preload: path.join(__dirname, 'preload.js'),
      },
      icon: path.join(__dirname, '../../assets/icon.png'),
      title: 'Chemical Dispersion Modeling',
      show: false,
    });

    // Load the React app
    const isDev = process.env.NODE_ENV === 'development';
    if (isDev) {
      await this.mainWindow.loadURL('http://localhost:3000');
      this.mainWindow.webContents.openDevTools();
    } else {
      await this.mainWindow.loadFile(path.join(__dirname, '../../renderer/index.html'));
    }

    this.mainWindow.once('ready-to-show', () => {
      this.mainWindow?.show();
    });

    this.mainWindow.on('closed', () => {
      this.mainWindow = null;
    });
  }

  private async setupIpcHandlers(): Promise<void> {
    // Weather API handlers
    ipcMain.handle('weather:getCurrentWeather', async (_, latitude: number, longitude: number) => {
      return await this.weatherService.getCurrentWeather(latitude, longitude);
    });

    ipcMain.handle('weather:getForecast', async (_, latitude: number, longitude: number) => {
      return await this.weatherService.getForecast(latitude, longitude);
    });

    // Dispersion modeling handlers
    ipcMain.handle('dispersion:calculateGaussianPlume', async (_, releaseData, weatherData, receptors) => {
      return await this.dispersionService.calculateGaussianPlume(releaseData, weatherData, receptors);
    });

    ipcMain.handle('dispersion:calculateDispersionGrid', async (_, releaseData, weatherData, gridSize, maxDistance) => {
      return await this.dispersionService.calculateDispersionGrid(releaseData, weatherData, gridSize, maxDistance);
    });

    // Database handlers
    ipcMain.handle('db:saveRelease', async (_, releaseData) => {
      return await this.databaseService.saveRelease(releaseData);
    });

    ipcMain.handle('db:getReleases', async () => {
      return await this.databaseService.getReleases();
    });

    ipcMain.handle('db:saveChemical', async (_, chemicalData) => {
      return await this.databaseService.saveChemical(chemicalData);
    });

    ipcMain.handle('db:getChemicals', async () => {
      return await this.databaseService.getChemicals();
    });

    // App control handlers
    ipcMain.handle('app:getVersion', () => {
      return app.getVersion();
    });

    ipcMain.handle('app:close', () => {
      app.quit();
    });
  }

  private async initializeServices(): Promise<void> {
    try {
      await this.databaseService.initialize();
      console.log('Services initialized successfully');
    } catch (error) {
      console.error('Failed to initialize services:', error);
    }
  }
}

// Application lifecycle
const mainApp = new MainApplication();

app.on('ready', async () => {
  await mainApp.initialize();
});

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') {
    app.quit();
  }
});

app.on('activate', async () => {
  if (BrowserWindow.getAllWindows().length === 0) {
    await mainApp.initialize();
  }
});

// Handle certificate errors for development
app.on('certificate-error', (event, webContents, url, error, certificate, callback) => {
  if (process.env.NODE_ENV === 'development') {
    event.preventDefault();
    callback(true);
  } else {
    callback(false);
  }
});