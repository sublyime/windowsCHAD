import { createSlice, createAsyncThunk, PayloadAction } from '@reduxjs/toolkit';
import { Chemical, ChemicalFormData } from '../../../shared/types';

interface ChemicalState {
  chemicals: Chemical[];
  selectedChemical: Chemical | null;
  loading: boolean;
  error: string | null;
}

const initialState: ChemicalState = {
  chemicals: [],
  selectedChemical: null,
  loading: false,
  error: null,
};

// Async thunks for API calls
export const fetchChemicals = createAsyncThunk(
  'chemical/fetchChemicals',
  async () => {
    const result = await window.electronAPI.chemicals.getAll();
    return result;
  }
);

export const saveChemical = createAsyncThunk(
  'chemical/saveChemical',
  async (chemicalData: Omit<Chemical, 'id' | 'createdAt' | 'updatedAt'>) => {
    const result = await window.electronAPI.chemicals.create(chemicalData);
    return result;
  }
);

export const chemicalSlice = createSlice({
  name: 'chemicals',
  initialState,
  reducers: {
    selectChemical: (state, action: PayloadAction<Chemical | null>) => {
      state.selectedChemical = action.payload;
    },
    clearError: (state) => {
      state.error = null;
    },
    resetChemicals: (state) => {
      state.chemicals = [];
      state.selectedChemical = null;
      state.error = null;
    },
  },
  extraReducers: (builder) => {
    builder
      // Fetch chemicals
      .addCase(fetchChemicals.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(fetchChemicals.fulfilled, (state, action) => {
        state.loading = false;
        state.chemicals = action.payload;
      })
      .addCase(fetchChemicals.rejected, (state, action) => {
        state.loading = false;
        state.error = action.error.message || 'Failed to fetch chemicals';
      })
      // Save chemical
      .addCase(saveChemical.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(saveChemical.fulfilled, (state, action) => {
        state.loading = false;
        state.chemicals.push(action.payload);
      })
      .addCase(saveChemical.rejected, (state, action) => {
        state.loading = false;
        state.error = action.error.message || 'Failed to save chemical';
      });
  },
});

export const { selectChemical, clearError, resetChemicals } = chemicalSlice.actions;
export default chemicalSlice.reducer;