/**
 * Shared type definitions for Chemical Dispersion Modeling
 * Ported from C# domain models to TypeScript interfaces
 */

// Core domain interfaces
export interface Chemical {
  id: number;
  name: string;
  casNumber?: string;
  physicalState: 'Gas' | 'Liquid' | 'Solid';
  molecularWeight: number;
  vaporPressure?: number;
  boilingPoint?: number;
  meltingPoint?: number;
  density: number;
  henryConstant?: number;
  diffusionCoefficient?: number;
  toxicityThreshold?: number;
  toxicityUnit?: string;
  isFlammable: boolean;
  lowerExplosiveLimit?: number;
  upperExplosiveLimit?: number;
  description?: string;
  createdAt: Date;
  updatedAt: Date;
}

export interface WeatherData {
  id: number;
  latitude: number;
  longitude: number;
  timestamp: Date;
  temperature: number;
  humidity: number;
  pressure: number;
  windSpeed: number;
  windDirection: number;
  windGust?: number;
  visibility?: number;
  cloudCover?: number;
  precipitation?: number;
  stabilityClass?: string;
  mixingHeight?: number;
  surfaceRoughness?: number;
  monin_ObukhovLength?: number;
  source: string;
  dataSource?: string;
  qualityFlag?: string;
  rawData?: string;
  createdAt: Date;
}

export interface Release {
  id: number;
  name: string;
  latitude: number;
  longitude: number;
  elevation: number;
  releaseHeight: number;
  startTime: Date;
  endTime?: Date;
  releaseRate?: number;
  totalMass?: number;
  volume?: number;
  releaseType: 'Instantaneous' | 'Continuous' | 'Variable';
  scenario: string;
  temperature?: number;
  pressure?: number;
  diameterOrArea?: number;
  initialTemperature?: number;
  isLiquidRelease: boolean;
  isHeavyGas: boolean;
  isHotRelease: boolean;
  modelingWindSpeed: number;
  modelingWindDirection: number;
  modelingStabilityClass: string;
  modelingTemperature: number;
  modelingHumidity: number;
  status: 'Draft' | 'Active' | 'Completed' | 'Archived';
  description?: string;
  createdBy?: string;
  createdAt: Date;
  updatedAt: Date;
  chemicalId: number;
  weatherDataId?: number;
  chemical?: Chemical;
  weatherData?: WeatherData;
}

export interface Receptor {
  id: number;
  name: string;
  latitude: number;
  longitude: number;
  elevation: number;
  receptorType: string;
  population?: number;
  isActive: boolean;
  description?: string;
  createdAt: Date;
  updatedAt: Date;
  releaseId: number;
}

export interface DispersionResult {
  id: number;
  calculationTime: Date;
  latitude: number;
  longitude: number;
  height: number;
  concentration: number;
  concentrationUnit: string;
  dosage?: number;
  distanceFromSource: number;
  directionFromSource: number;
  plumeWidth?: number;
  plumeHeight?: number;
  windSpeed: number;
  windDirection: number;
  stabilityClass: string;
  temperature: number;
  exceedsToxicityThreshold: boolean;
  riskLevel: string;
  modelUsed: string;
  calculationParameters?: string;
  createdAt: Date;
  releaseId: number;
  receptorId?: number;
}

export interface TerrainData {
  id: number;
  latitude: number;
  longitude: number;
  elevation: number;
  surfaceRoughness: number;
  landUseType: string;
  buildingHeight?: number;
  buildingDensity?: number;
  vegetationHeight?: number;
  waterBodyDistance?: number;
  urbanCanopyHeight?: number;
  description?: string;
  createdAt: Date;
  updatedAt: Date;
}

// Atmospheric dispersion calculation types
export interface DispersionParameters {
  ay: number;
  by: number;
  az: number;
  bz: number;
}

export interface PlumeContour {
  coordinates: number[][][]; // Array of polygon arrays
  concentration: number;
  color: string;
  fillColor: string;
  opacity: number;
  isDownwindCorridor?: boolean;
}

export interface DispersionVisualizationOptions {
  concentrationLevels: number[];
  contourColors: string[];
  opacity: number;
  showConcentrationLabels: boolean;
  showFootprint: boolean;
  showCenterline: boolean;
}

export interface GaussianPlumeParameters {
  stabilityClass: string;
  windSpeed: number;
  windDirection: number;
  temperature: number;
  humidity: number;
  releaseHeight: number;
  effectiveHeight: number;
  sourceStrength: number;
}

// Map and visualization types
export interface MapClickEvent {
  latitude: number;
  longitude: number;
  screenX: number;
  screenY: number;
}

export interface MapViewState {
  center: [number, number];
  zoom: number;
  bounds?: [[number, number], [number, number]];
}

// API response types
export interface WeatherApiResponse {
  success: boolean;
  data?: WeatherData;
  error?: string;
}

export interface DispersionCalculationResponse {
  success: boolean;
  results?: DispersionResult[];
  error?: string;
  calculationTime: number;
}

// Application state types
export interface ApplicationState {
  currentRelease?: Release;
  currentWeather?: WeatherData;
  selectedChemical?: Chemical;
  dispersionResults: DispersionResult[];
  receptors: Receptor[];
  mapState: MapViewState;
  isCalculating: boolean;
  statusMessage: string;
  debugMode: boolean;
}

// Form and UI types
export interface ReleaseFormData {
  name: string;
  latitude: number;
  longitude: number;
  releaseHeight: number;
  chemicalId: number;
  releaseType: string;
  releaseRate?: number;
  totalMass?: number;
  scenario: string;
  description?: string;
}

export interface ChemicalFormData {
  name: string;
  casNumber?: string;
  physicalState: string;
  molecularWeight: number;
  density: number;
  toxicityThreshold?: number;
  isFlammable: boolean;
  description?: string;
}

// Error handling types
export interface ApiError {
  code: string;
  message: string;
  details?: string;
}

export interface ValidationError {
  field: string;
  message: string;
}

// Electron API types
export interface ElectronAPI {
  // Chemical operations
  chemicals: {
    getAll: () => Promise<Chemical[]>;
    getById: (id: number) => Promise<Chemical | null>;
    create: (chemical: Omit<Chemical, 'id' | 'createdAt' | 'updatedAt'>) => Promise<Chemical>;
    update: (id: number, chemical: Partial<Chemical>) => Promise<Chemical>;
    delete: (id: number) => Promise<{ success: boolean; error?: string }>;
  };

  // Release operations
  releases: {
    getAll: () => Promise<Release[]>;
    getById: (id: number) => Promise<Release | null>;
    create: (release: Omit<Release, 'id' | 'createdAt' | 'updatedAt'>) => Promise<Release>;
    update: (id: number, release: Partial<Release>) => Promise<Release>;
    delete: (id: number) => Promise<{ success: boolean; error?: string }>;
  };

  // Weather operations
  weather: {
    getCurrentWeather: (latitude: number, longitude: number) => Promise<{
      success: boolean;
      data?: WeatherData;
      error?: string;
    }>;
    getForecast: (latitude: number, longitude: number) => Promise<WeatherData[]>;
    getHistoricalWeather: (latitude: number, longitude: number, startDate: Date, endDate: Date) => Promise<WeatherData[]>;
  };

  // Dispersion modeling operations
  dispersion: {
    calculateGaussianPlume: (release: Release, weather: WeatherData, receptors: Receptor[]) => Promise<{
      success: boolean;
      data?: DispersionResult;
      error?: string;
    }>;
    calculateDispersionGrid: (
      release: Release,
      weather: WeatherData,
      gridSize: number,
      maxDistance: number
    ) => Promise<{
      success: boolean;
      data?: DispersionResult;
      error?: string;
    }>;
    runModel: (
      release: Release,
      weather: WeatherData,
      receptors: Receptor[],
      gridBounds: { north: number; south: number; east: number; west: number }
    ) => Promise<{
      success: boolean;
      data?: DispersionResult;
      error?: string;
    }>;
    saveResult: (result: DispersionResult) => Promise<{
      success: boolean;
      data?: DispersionResult;
      error?: string;
    }>;
    getResults: () => Promise<DispersionResult[]>;
    deleteResult: (id: number) => Promise<{
      success: boolean;
      error?: string;
    }>;
  };

  // Legacy database operations (for compatibility)
  database: {
    saveRelease: (release: ReleaseFormData) => Promise<Release>;
    getReleases: () => Promise<Release[]>;
    saveChemical: (chemical: ChemicalFormData) => Promise<Chemical>;
    getChemicals: () => Promise<Chemical[]>;
  };

  // Utility operations
  utils: {
    selectDirectory: () => Promise<string | null>;
    selectFile: (filters?: { name: string; extensions: string[] }[]) => Promise<string | null>;
    saveFile: (defaultPath?: string, filters?: { name: string; extensions: string[] }[]) => Promise<string | null>;
    showNotification: (title: string, body: string) => void;
    openExternal: (url: string) => Promise<void>;
  };

  // App operations
  app: {
    getVersion: () => Promise<string>;
    close: () => Promise<void>;
  };
}

// Global window interface extension
declare global {
  interface Window {
    electronAPI: ElectronAPI;
  }
}