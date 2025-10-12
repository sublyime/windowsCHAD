import { createSlice, createAsyncThunk, PayloadAction } from '@reduxjs/toolkit';
import { DispersionResult, WeatherData, Release, Receptor } from '../../../shared/types';

interface DispersionState {
  results: DispersionResult[];
  currentSimulation: DispersionResult | null;
  loading: boolean;
  error: string | null;
  simulationProgress: number;
  isSimulationRunning: boolean;
  parameters: {
    timeStep: number; // seconds
    totalTime: number; // seconds
    gridResolution: number; // meters
    contourLevels: number[];
    showRealtimeUpdate: boolean;
  };
  visualSettings: {
    showContours: boolean;
    showParticles: boolean;
    showConcentrationGradient: boolean;
    opacity: number;
    colorScheme: 'rainbow' | 'heat' | 'blue_red' | 'grayscale';
    contourSmoothing: boolean;
  };
}

const initialState: DispersionState = {
  results: [],
  currentSimulation: null,
  loading: false,
  error: null,
  simulationProgress: 0,
  isSimulationRunning: false,
  parameters: {
    timeStep: 60, // 1 minute
    totalTime: 3600, // 1 hour
    gridResolution: 100, // 100 meters
    contourLevels: [0.001, 0.01, 0.1, 1.0, 10.0], // concentration levels
    showRealtimeUpdate: true,
  },
  visualSettings: {
    showContours: true,
    showParticles: false,
    showConcentrationGradient: true,
    opacity: 0.7,
    colorScheme: 'heat',
    contourSmoothing: true,
  },
};

// Async thunks for dispersion modeling
export const runDispersionModel = createAsyncThunk(
  'dispersion/runModel',
  async (params: {
    release: Release;
    weather: WeatherData;
    receptors: Receptor[];
    gridBounds: { north: number; south: number; east: number; west: number };
  }) => {
    const result = await window.electronAPI.dispersion.runModel(
      params.release,
      params.weather,
      params.receptors,
      params.gridBounds
    );
    if (!result.success) {
      throw new Error(result.error || 'Dispersion modeling failed');
    }
    return result.data!;
  }
);

export const saveDispersionResult = createAsyncThunk(
  'dispersion/saveResult',
  async (result: DispersionResult) => {
    const saved = await window.electronAPI.dispersion.saveResult(result);
    if (!saved.success) {
      throw new Error(saved.error || 'Failed to save dispersion result');
    }
    return saved.data!;
  }
);

export const loadDispersionResults = createAsyncThunk(
  'dispersion/loadResults',
  async () => {
    const results = await window.electronAPI.dispersion.getResults();
    return results;
  }
);

export const deleteDispersionResult = createAsyncThunk(
  'dispersion/deleteResult',
  async (id: number) => {
    const result = await window.electronAPI.dispersion.deleteResult(id);
    if (!result.success) {
      throw new Error(result.error || 'Failed to delete dispersion result');
    }
    return id;
  }
);

export const dispersionSlice = createSlice({
  name: 'dispersion',
  initialState,
  reducers: {
    setCurrentSimulation: (state, action: PayloadAction<DispersionResult | null>) => {
      state.currentSimulation = action.payload;
    },
    clearResults: (state) => {
      state.results = [];
      state.currentSimulation = null;
    },
    clearError: (state) => {
      state.error = null;
    },
    updateSimulationProgress: (state, action: PayloadAction<number>) => {
      state.simulationProgress = action.payload;
    },
    setSimulationRunning: (state, action: PayloadAction<boolean>) => {
      state.isSimulationRunning = action.payload;
      if (!action.payload) {
        state.simulationProgress = 0;
      }
    },
    updateParameters: (state, action: PayloadAction<Partial<DispersionState['parameters']>>) => {
      state.parameters = { ...state.parameters, ...action.payload };
    },
    updateVisualSettings: (state, action: PayloadAction<Partial<DispersionState['visualSettings']>>) => {
      state.visualSettings = { ...state.visualSettings, ...action.payload };
    },
    setTimeStep: (state, action: PayloadAction<number>) => {
      state.parameters.timeStep = action.payload;
    },
    setTotalTime: (state, action: PayloadAction<number>) => {
      state.parameters.totalTime = action.payload;
    },
    setGridResolution: (state, action: PayloadAction<number>) => {
      state.parameters.gridResolution = action.payload;
    },
    setContourLevels: (state, action: PayloadAction<number[]>) => {
      state.parameters.contourLevels = action.payload;
    },
    toggleRealtimeUpdate: (state) => {
      state.parameters.showRealtimeUpdate = !state.parameters.showRealtimeUpdate;
    },
    toggleContours: (state) => {
      state.visualSettings.showContours = !state.visualSettings.showContours;
    },
    toggleParticles: (state) => {
      state.visualSettings.showParticles = !state.visualSettings.showParticles;
    },
    toggleConcentrationGradient: (state) => {
      state.visualSettings.showConcentrationGradient = !state.visualSettings.showConcentrationGradient;
    },
    setOpacity: (state, action: PayloadAction<number>) => {
      state.visualSettings.opacity = Math.max(0, Math.min(1, action.payload));
    },
    setColorScheme: (state, action: PayloadAction<'rainbow' | 'heat' | 'blue_red' | 'grayscale'>) => {
      state.visualSettings.colorScheme = action.payload;
    },
    toggleContourSmoothing: (state) => {
      state.visualSettings.contourSmoothing = !state.visualSettings.contourSmoothing;
    },
  },
  extraReducers: (builder) => {
    builder
      // Run dispersion model
      .addCase(runDispersionModel.pending, (state) => {
        state.loading = true;
        state.error = null;
        state.isSimulationRunning = true;
        state.simulationProgress = 0;
      })
      .addCase(runDispersionModel.fulfilled, (state, action) => {
        state.loading = false;
        state.isSimulationRunning = false;
        state.simulationProgress = 100;
        state.currentSimulation = action.payload;
        
        // Add to results if not already present
        const existingIndex = state.results.findIndex(r => r.id === action.payload.id);
        if (existingIndex >= 0) {
          state.results[existingIndex] = action.payload;
        } else {
          state.results.push(action.payload);
        }
      })
      .addCase(runDispersionModel.rejected, (state, action) => {
        state.loading = false;
        state.isSimulationRunning = false;
        state.simulationProgress = 0;
        state.error = action.error.message || 'Dispersion modeling failed';
      })
      // Save dispersion result
      .addCase(saveDispersionResult.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(saveDispersionResult.fulfilled, (state, action) => {
        state.loading = false;
        const existingIndex = state.results.findIndex(r => r.id === action.payload.id);
        if (existingIndex >= 0) {
          state.results[existingIndex] = action.payload;
        } else {
          state.results.push(action.payload);
        }
      })
      .addCase(saveDispersionResult.rejected, (state, action) => {
        state.loading = false;
        state.error = action.error.message || 'Failed to save dispersion result';
      })
      // Load dispersion results
      .addCase(loadDispersionResults.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(loadDispersionResults.fulfilled, (state, action) => {
        state.loading = false;
        state.results = action.payload;
      })
      .addCase(loadDispersionResults.rejected, (state, action) => {
        state.loading = false;
        state.error = action.error.message || 'Failed to load dispersion results';
      })
      // Delete dispersion result
      .addCase(deleteDispersionResult.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(deleteDispersionResult.fulfilled, (state, action) => {
        state.loading = false;
        state.results = state.results.filter(r => r.id !== action.payload);
        if (state.currentSimulation?.id === action.payload) {
          state.currentSimulation = null;
        }
      })
      .addCase(deleteDispersionResult.rejected, (state, action) => {
        state.loading = false;
        state.error = action.error.message || 'Failed to delete dispersion result';
      });
  },
});

export const {
  setCurrentSimulation,
  clearResults,
  clearError,
  updateSimulationProgress,
  setSimulationRunning,
  updateParameters,
  updateVisualSettings,
  setTimeStep,
  setTotalTime,
  setGridResolution,
  setContourLevels,
  toggleRealtimeUpdate,
  toggleContours,
  toggleParticles,
  toggleConcentrationGradient,
  setOpacity,
  setColorScheme,
  toggleContourSmoothing,
} = dispersionSlice.actions;

export default dispersionSlice.reducer;