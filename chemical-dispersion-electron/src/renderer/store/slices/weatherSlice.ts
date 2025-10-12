import { createSlice, createAsyncThunk, PayloadAction } from '@reduxjs/toolkit';
import { WeatherData } from '../../../shared/types';

interface WeatherState {
  currentWeather: WeatherData | null;
  forecast: WeatherData[];
  loading: boolean;
  error: string | null;
  lastUpdate: Date | null;
}

const initialState: WeatherState = {
  currentWeather: null,
  forecast: [],
  loading: false,
  error: null,
  lastUpdate: null,
};

// Async thunks for API calls
export const fetchCurrentWeather = createAsyncThunk(
  'weather/fetchCurrentWeather',
  async ({ latitude, longitude }: { latitude: number; longitude: number }) => {
    const result = await window.electronAPI.weather.getCurrentWeather(latitude, longitude);
    if (!result.success) {
      throw new Error(result.error || 'Failed to fetch weather data');
    }
    return result.data!;
  }
);

export const fetchWeatherForecast = createAsyncThunk(
  'weather/fetchWeatherForecast',
  async ({ latitude, longitude }: { latitude: number; longitude: number }) => {
    const result = await window.electronAPI.weather.getForecast(latitude, longitude);
    return result;
  }
);

export const weatherSlice = createSlice({
  name: 'weather',
  initialState,
  reducers: {
    clearWeather: (state) => {
      state.currentWeather = null;
      state.forecast = [];
      state.error = null;
      state.lastUpdate = null;
    },
    clearError: (state) => {
      state.error = null;
    },
    setWeatherData: (state, action: PayloadAction<WeatherData>) => {
      state.currentWeather = action.payload;
      state.lastUpdate = new Date();
    },
  },
  extraReducers: (builder) => {
    builder
      // Fetch current weather
      .addCase(fetchCurrentWeather.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(fetchCurrentWeather.fulfilled, (state, action) => {
        state.loading = false;
        state.currentWeather = action.payload;
        state.lastUpdate = new Date();
      })
      .addCase(fetchCurrentWeather.rejected, (state, action) => {
        state.loading = false;
        state.error = action.error.message || 'Failed to fetch weather data';
      })
      // Fetch weather forecast
      .addCase(fetchWeatherForecast.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(fetchWeatherForecast.fulfilled, (state, action) => {
        state.loading = false;
        state.forecast = action.payload;
      })
      .addCase(fetchWeatherForecast.rejected, (state, action) => {
        state.loading = false;
        state.error = action.error.message || 'Failed to fetch weather forecast';
      });
  },
});

export const { clearWeather, clearError, setWeatherData } = weatherSlice.actions;
export default weatherSlice.reducer;