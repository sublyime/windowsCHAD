import { contextBridge, ipcRenderer } from 'electron';
import { Chemical, WeatherData, Release, Receptor, DispersionResult, ElectronAPI } from '../shared/types';

// API implementation
const electronAPI: ElectronAPI = {
  chemicals: {
    getAll: () => ipcRenderer.invoke('chemicals:getAll'),
    getById: (id: number) => ipcRenderer.invoke('chemicals:getById', id),
    create: (chemical) => ipcRenderer.invoke('chemicals:create', chemical),
    update: (id: number, chemical) => ipcRenderer.invoke('chemicals:update', id, chemical),
    delete: (id: number) => ipcRenderer.invoke('chemicals:delete', id),
  },

  releases: {
    getAll: () => ipcRenderer.invoke('releases:getAll'),
    getById: (id: number) => ipcRenderer.invoke('releases:getById', id),
    create: (release) => ipcRenderer.invoke('releases:create', release),
    update: (id: number, release) => ipcRenderer.invoke('releases:update', id, release),
    delete: (id: number) => ipcRenderer.invoke('releases:delete', id),
  },

  weather: {
    getCurrentWeather: (latitude: number, longitude: number) => 
      ipcRenderer.invoke('weather:getCurrentWeather', latitude, longitude),
    getForecast: (latitude: number, longitude: number) => 
      ipcRenderer.invoke('weather:getForecast', latitude, longitude),
    getHistoricalWeather: (latitude: number, longitude: number, startDate: Date, endDate: Date) => 
      ipcRenderer.invoke('weather:getHistoricalWeather', latitude, longitude, startDate, endDate),
  },

  dispersion: {
    calculateGaussianPlume: (release: Release, weather: WeatherData, receptors: Receptor[]) => 
      ipcRenderer.invoke('dispersion:calculateGaussianPlume', release, weather, receptors),
    calculateDispersionGrid: (release: Release, weather: WeatherData, gridSize: number, maxDistance: number) => 
      ipcRenderer.invoke('dispersion:calculateDispersionGrid', release, weather, gridSize, maxDistance),
    runModel: (release: Release, weather: WeatherData, receptors: Receptor[], gridBounds: any) => 
      ipcRenderer.invoke('dispersion:runModel', release, weather, receptors, gridBounds),
    saveResult: (result: DispersionResult) => 
      ipcRenderer.invoke('dispersion:saveResult', result),
    getResults: () => 
      ipcRenderer.invoke('dispersion:getResults'),
    deleteResult: (id: number) => 
      ipcRenderer.invoke('dispersion:deleteResult', id),
  },

  // Legacy database operations (for compatibility)
  database: {
    saveRelease: (release) => ipcRenderer.invoke('database:saveRelease', release),
    getReleases: () => ipcRenderer.invoke('database:getReleases'),
    saveChemical: (chemical) => ipcRenderer.invoke('database:saveChemical', chemical),
    getChemicals: () => ipcRenderer.invoke('database:getChemicals'),
  },

  utils: {
    selectDirectory: () => ipcRenderer.invoke('utils:selectDirectory'),
    selectFile: (filters) => ipcRenderer.invoke('utils:selectFile', filters),
    saveFile: (defaultPath, filters) => ipcRenderer.invoke('utils:saveFile', defaultPath, filters),
    showNotification: (title: string, body: string) => ipcRenderer.send('utils:showNotification', title, body),
    openExternal: (url: string) => ipcRenderer.invoke('utils:openExternal', url),
  },

  // App operations
  app: {
    getVersion: () => ipcRenderer.invoke('app:getVersion'),
    close: () => ipcRenderer.invoke('app:close'),
  },
};

// Expose the API to the renderer process
contextBridge.exposeInMainWorld('electronAPI', electronAPI);