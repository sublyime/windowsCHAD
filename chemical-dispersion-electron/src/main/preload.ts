import { contextBridge, ipcRenderer } from 'electron';

// Expose protected methods that allow the renderer process to use
// the ipcRenderer without exposing the entire object
contextBridge.exposeInMainWorld('electronAPI', {
  // Weather API
  weather: {
    getCurrentWeather: (latitude: number, longitude: number) =>
      ipcRenderer.invoke('weather:getCurrentWeather', latitude, longitude),
    getForecast: (latitude: number, longitude: number) =>
      ipcRenderer.invoke('weather:getForecast', latitude, longitude),
  },

  // Dispersion modeling API
  dispersion: {
    calculateGaussianPlume: (releaseData: any, weatherData: any, receptors: any) =>
      ipcRenderer.invoke('dispersion:calculateGaussianPlume', releaseData, weatherData, receptors),
    calculateDispersionGrid: (releaseData: any, weatherData: any, gridSize: number, maxDistance: number) =>
      ipcRenderer.invoke('dispersion:calculateDispersionGrid', releaseData, weatherData, gridSize, maxDistance),
  },

  // Database API
  database: {
    saveRelease: (releaseData: any) =>
      ipcRenderer.invoke('db:saveRelease', releaseData),
    getReleases: () =>
      ipcRenderer.invoke('db:getReleases'),
    saveChemical: (chemicalData: any) =>
      ipcRenderer.invoke('db:saveChemical', chemicalData),
    getChemicals: () =>
      ipcRenderer.invoke('db:getChemicals'),
  },

  // App control API
  app: {
    getVersion: () =>
      ipcRenderer.invoke('app:getVersion'),
    close: () =>
      ipcRenderer.invoke('app:close'),
  },
});