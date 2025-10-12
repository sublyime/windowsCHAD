"use strict";
var __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    var desc = Object.getOwnPropertyDescriptor(m, k);
    if (!desc || ("get" in desc ? !m.__esModule : desc.writable || desc.configurable)) {
      desc = { enumerable: true, get: function() { return m[k]; } };
    }
    Object.defineProperty(o, k2, desc);
}) : (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    o[k2] = m[k];
}));
var __setModuleDefault = (this && this.__setModuleDefault) || (Object.create ? (function(o, v) {
    Object.defineProperty(o, "default", { enumerable: true, value: v });
}) : function(o, v) {
    o["default"] = v;
});
var __importStar = (this && this.__importStar) || (function () {
    var ownKeys = function(o) {
        ownKeys = Object.getOwnPropertyNames || function (o) {
            var ar = [];
            for (var k in o) if (Object.prototype.hasOwnProperty.call(o, k)) ar[ar.length] = k;
            return ar;
        };
        return ownKeys(o);
    };
    return function (mod) {
        if (mod && mod.__esModule) return mod;
        var result = {};
        if (mod != null) for (var k = ownKeys(mod), i = 0; i < k.length; i++) if (k[i] !== "default") __createBinding(result, mod, k[i]);
        __setModuleDefault(result, mod);
        return result;
    };
})();
Object.defineProperty(exports, "__esModule", { value: true });
const electron_1 = require("electron");
const path = __importStar(require("path"));
const WeatherService_1 = require("./services/WeatherService");
const DispersionModelingService_1 = require("./services/DispersionModelingService");
const DatabaseService_1 = require("./services/DatabaseService");
class MainApplication {
    mainWindow = null;
    weatherService;
    dispersionService;
    databaseService;
    constructor() {
        this.weatherService = new WeatherService_1.WeatherService();
        this.dispersionService = new DispersionModelingService_1.DispersionModelingService();
        this.databaseService = new DatabaseService_1.DatabaseService();
    }
    async initialize() {
        await electron_1.app.whenReady();
        await this.createMainWindow();
        await this.setupIpcHandlers();
        await this.initializeServices();
    }
    async createMainWindow() {
        this.mainWindow = new electron_1.BrowserWindow({
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
        }
        else {
            await this.mainWindow.loadFile(path.join(__dirname, '../../renderer/index.html'));
        }
        this.mainWindow.once('ready-to-show', () => {
            this.mainWindow?.show();
        });
        this.mainWindow.on('closed', () => {
            this.mainWindow = null;
        });
    }
    async setupIpcHandlers() {
        // Weather API handlers
        electron_1.ipcMain.handle('weather:getCurrentWeather', async (_, latitude, longitude) => {
            return await this.weatherService.getCurrentWeather(latitude, longitude);
        });
        electron_1.ipcMain.handle('weather:getForecast', async (_, latitude, longitude) => {
            return await this.weatherService.getForecast(latitude, longitude);
        });
        // Dispersion modeling handlers
        electron_1.ipcMain.handle('dispersion:calculateGaussianPlume', async (_, releaseData, weatherData, receptors) => {
            return await this.dispersionService.calculateGaussianPlume(releaseData, weatherData, receptors);
        });
        electron_1.ipcMain.handle('dispersion:calculateDispersionGrid', async (_, releaseData, weatherData, gridSize, maxDistance) => {
            return await this.dispersionService.calculateDispersionGrid(releaseData, weatherData, gridSize, maxDistance);
        });
        // Database handlers
        electron_1.ipcMain.handle('db:saveRelease', async (_, releaseData) => {
            return await this.databaseService.saveRelease(releaseData);
        });
        electron_1.ipcMain.handle('db:getReleases', async () => {
            return await this.databaseService.getReleases();
        });
        electron_1.ipcMain.handle('db:saveChemical', async (_, chemicalData) => {
            return await this.databaseService.saveChemical(chemicalData);
        });
        electron_1.ipcMain.handle('db:getChemicals', async () => {
            return await this.databaseService.getChemicals();
        });
        // App control handlers
        electron_1.ipcMain.handle('app:getVersion', () => {
            return electron_1.app.getVersion();
        });
        electron_1.ipcMain.handle('app:close', () => {
            electron_1.app.quit();
        });
    }
    async initializeServices() {
        try {
            await this.databaseService.initialize();
            console.log('Services initialized successfully');
        }
        catch (error) {
            console.error('Failed to initialize services:', error);
        }
    }
}
// Application lifecycle
const mainApp = new MainApplication();
electron_1.app.on('ready', async () => {
    await mainApp.initialize();
});
electron_1.app.on('window-all-closed', () => {
    if (process.platform !== 'darwin') {
        electron_1.app.quit();
    }
});
electron_1.app.on('activate', async () => {
    if (electron_1.BrowserWindow.getAllWindows().length === 0) {
        await mainApp.initialize();
    }
});
// Handle certificate errors for development
electron_1.app.on('certificate-error', (event, webContents, url, error, certificate, callback) => {
    if (process.env.NODE_ENV === 'development') {
        event.preventDefault();
        callback(true);
    }
    else {
        callback(false);
    }
});
