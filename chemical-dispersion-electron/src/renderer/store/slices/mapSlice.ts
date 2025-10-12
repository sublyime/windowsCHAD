import { createSlice, PayloadAction } from '@reduxjs/toolkit';

interface MapState {
  center: [number, number]; // [latitude, longitude]
  zoom: number;
  mapType: 'satellite' | 'street' | 'terrain';
  showWeatherOverlay: boolean;
  showTerrain: boolean;
  selectedLocation: [number, number] | null;
  plumeBounds: {
    north: number;
    south: number;
    east: number;
    west: number;
  } | null;
  markers: Array<{
    id: string;
    position: [number, number];
    type: 'release' | 'receptor' | 'weather_station';
    label: string;
    data?: any;
  }>;
}

const initialState: MapState = {
  center: [39.8283, -98.5795], // Geographic center of United States
  zoom: 10,
  mapType: 'street',
  showWeatherOverlay: false,
  showTerrain: false,
  selectedLocation: null,
  plumeBounds: null,
  markers: [],
};

export const mapSlice = createSlice({
  name: 'map',
  initialState,
  reducers: {
    setMapCenter: (state, action: PayloadAction<[number, number]>) => {
      state.center = action.payload;
    },
    setMapZoom: (state, action: PayloadAction<number>) => {
      state.zoom = action.payload;
    },
    setMapType: (state, action: PayloadAction<'satellite' | 'street' | 'terrain'>) => {
      state.mapType = action.payload;
    },
    toggleWeatherOverlay: (state) => {
      state.showWeatherOverlay = !state.showWeatherOverlay;
    },
    setWeatherOverlay: (state, action: PayloadAction<boolean>) => {
      state.showWeatherOverlay = action.payload;
    },
    toggleTerrain: (state) => {
      state.showTerrain = !state.showTerrain;
    },
    setTerrain: (state, action: PayloadAction<boolean>) => {
      state.showTerrain = action.payload;
    },
    setSelectedLocation: (state, action: PayloadAction<[number, number] | null>) => {
      state.selectedLocation = action.payload;
    },
    setPlumeBounds: (state, action: PayloadAction<{
      north: number;
      south: number;
      east: number;
      west: number;
    } | null>) => {
      state.plumeBounds = action.payload;
    },
    addMarker: (state, action: PayloadAction<{
      id: string;
      position: [number, number];
      type: 'release' | 'receptor' | 'weather_station';
      label: string;
      data?: any;
    }>) => {
      const existingIndex = state.markers.findIndex(m => m.id === action.payload.id);
      if (existingIndex >= 0) {
        state.markers[existingIndex] = action.payload;
      } else {
        state.markers.push(action.payload);
      }
    },
    removeMarker: (state, action: PayloadAction<string>) => {
      state.markers = state.markers.filter(m => m.id !== action.payload);
    },
    clearMarkers: (state) => {
      state.markers = [];
    },
    clearMarkersOfType: (state, action: PayloadAction<'release' | 'receptor' | 'weather_station'>) => {
      state.markers = state.markers.filter(m => m.type !== action.payload);
    },
    updateMarker: (state, action: PayloadAction<{
      id: string;
      updates: Partial<{
        position: [number, number];
        label: string;
        data: any;
      }>;
    }>) => {
      const marker = state.markers.find(m => m.id === action.payload.id);
      if (marker) {
        Object.assign(marker, action.payload.updates);
      }
    },
    centerOnMarker: (state, action: PayloadAction<string>) => {
      const marker = state.markers.find(m => m.id === action.payload);
      if (marker) {
        state.center = marker.position;
        state.selectedLocation = marker.position;
      }
    },
    fitBoundsToMarkers: (state) => {
      if (state.markers.length === 0) return;
      
      const lats = state.markers.map(m => m.position[0]);
      const lngs = state.markers.map(m => m.position[1]);
      
      const minLat = Math.min(...lats);
      const maxLat = Math.max(...lats);
      const minLng = Math.min(...lngs);
      const maxLng = Math.max(...lngs);
      
      // Center on the bounds
      state.center = [(minLat + maxLat) / 2, (minLng + maxLng) / 2];
      
      // Adjust zoom based on bounds (simplified calculation)
      const latDiff = maxLat - minLat;
      const lngDiff = maxLng - minLng;
      const maxDiff = Math.max(latDiff, lngDiff);
      
      if (maxDiff > 1) state.zoom = 8;
      else if (maxDiff > 0.1) state.zoom = 10;
      else if (maxDiff > 0.01) state.zoom = 12;
      else state.zoom = 14;
    },
  },
});

export const {
  setMapCenter,
  setMapZoom,
  setMapType,
  toggleWeatherOverlay,
  setWeatherOverlay,
  toggleTerrain,
  setTerrain,
  setSelectedLocation,
  setPlumeBounds,
  addMarker,
  removeMarker,
  clearMarkers,
  clearMarkersOfType,
  updateMarker,
  centerOnMarker,
  fitBoundsToMarkers,
} = mapSlice.actions;

export default mapSlice.reducer;