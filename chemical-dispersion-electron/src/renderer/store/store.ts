import { configureStore } from '@reduxjs/toolkit';
import chemicalReducer from './slices/chemicalSlice';
import releaseReducer from './slices/releaseSlice';
import weatherReducer from './slices/weatherSlice';
import mapReducer from './slices/mapSlice';
import dispersionReducer from './slices/dispersionSlice';

export const store = configureStore({
  reducer: {
    chemicals: chemicalReducer,
    releases: releaseReducer,
    weather: weatherReducer,
    map: mapReducer,
    dispersion: dispersionReducer,
  },
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware({
      serializableCheck: {
        ignoredActions: ['persist/PERSIST'],
      },
    }),
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;