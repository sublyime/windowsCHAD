"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const electron_1 = require("electron");
// Expose protected methods that allow the renderer process to use
// the ipcRenderer without exposing the entire object
electron_1.contextBridge.exposeInMainWorld('electronAPI', {
    // Weather API
    weather: {
        getCurrentWeather: (latitude, longitude) => electron_1.ipcRenderer.invoke('weather:getCurrentWeather', latitude, longitude),
        getForecast: (latitude, longitude) => electron_1.ipcRenderer.invoke('weather:getForecast', latitude, longitude),
    },
    // Dispersion modeling API
    dispersion: {
        calculateGaussianPlume: (releaseData, weatherData, receptors) => electron_1.ipcRenderer.invoke('dispersion:calculateGaussianPlume', releaseData, weatherData, receptors),
        calculateDispersionGrid: (releaseData, weatherData, gridSize, maxDistance) => electron_1.ipcRenderer.invoke('dispersion:calculateDispersionGrid', releaseData, weatherData, gridSize, maxDistance),
    },
    // Database API
    database: {
        saveRelease: (releaseData) => electron_1.ipcRenderer.invoke('db:saveRelease', releaseData),
        getReleases: () => electron_1.ipcRenderer.invoke('db:getReleases'),
        saveChemical: (chemicalData) => electron_1.ipcRenderer.invoke('db:saveChemical', chemicalData),
        getChemicals: () => electron_1.ipcRenderer.invoke('db:getChemicals'),
    },
    // App control API
    app: {
        getVersion: () => electron_1.ipcRenderer.invoke('app:getVersion'),
        close: () => electron_1.ipcRenderer.invoke('app:close'),
    },
});
