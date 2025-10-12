import { createSlice, createAsyncThunk, PayloadAction } from '@reduxjs/toolkit';
import { Release, ReleaseFormData } from '../../../shared/types';

interface ReleaseState {
  releases: Release[];
  currentRelease: Release | null;
  loading: boolean;
  error: string | null;
}

const initialState: ReleaseState = {
  releases: [],
  currentRelease: null,
  loading: false,
  error: null,
};

// Async thunks for API calls
export const fetchReleases = createAsyncThunk(
  'release/fetchReleases',
  async () => {
    const result = await window.electronAPI.releases.getAll();
    return result;
  }
);

export const saveRelease = createAsyncThunk(
  'release/saveRelease',
  async (releaseData: Omit<Release, 'id' | 'createdAt' | 'updatedAt'>) => {
    const result = await window.electronAPI.releases.create(releaseData);
    return result;
  }
);

export const releaseSlice = createSlice({
  name: 'releases',
  initialState,
  reducers: {
    setCurrentRelease: (state, action: PayloadAction<Release | null>) => {
      state.currentRelease = action.payload;
    },
    updateReleaseLocation: (state, action: PayloadAction<{ latitude: number; longitude: number }>) => {
      if (state.currentRelease) {
        state.currentRelease.latitude = action.payload.latitude;
        state.currentRelease.longitude = action.payload.longitude;
      }
    },
    updateReleaseProperty: (state, action: PayloadAction<{ property: keyof Release; value: any }>) => {
      if (state.currentRelease) {
        (state.currentRelease as any)[action.payload.property] = action.payload.value;
      }
    },
    clearError: (state) => {
      state.error = null;
    },
    resetReleases: (state) => {
      state.releases = [];
      state.currentRelease = null;
      state.error = null;
    },
  },
  extraReducers: (builder) => {
    builder
      // Fetch releases
      .addCase(fetchReleases.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(fetchReleases.fulfilled, (state, action) => {
        state.loading = false;
        state.releases = action.payload;
      })
      .addCase(fetchReleases.rejected, (state, action) => {
        state.loading = false;
        state.error = action.error.message || 'Failed to fetch releases';
      })
      // Save release
      .addCase(saveRelease.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(saveRelease.fulfilled, (state, action) => {
        state.loading = false;
        state.releases.push(action.payload);
        state.currentRelease = action.payload;
      })
      .addCase(saveRelease.rejected, (state, action) => {
        state.loading = false;
        state.error = action.error.message || 'Failed to save release';
      });
  },
});

export const { 
  setCurrentRelease, 
  updateReleaseLocation, 
  updateReleaseProperty,
  clearError, 
  resetReleases 
} = releaseSlice.actions;

export default releaseSlice.reducer;