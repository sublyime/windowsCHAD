/**
 * Weather Service for fetching real-time atmospheric data
 * Ported from C# weather services to TypeScript
 */

import axios from 'axios';
import { WeatherData, WeatherApiResponse } from '../../shared/types';

export class WeatherService {
  private readonly nwsBaseUrl = 'https://api.weather.gov';
  private readonly openMeteoBaseUrl = 'https://api.open-meteo.com/v1';

  /**
   * Get current weather data for a location
   */
  public async getCurrentWeather(latitude: number, longitude: number): Promise<WeatherApiResponse> {
    try {
      // Try National Weather Service first
      const nwsData = await this.getNWSCurrentWeather(latitude, longitude);
      if (nwsData) {
        return { success: true, data: nwsData };
      }

      // Fallback to OpenMeteo
      const openMeteoData = await this.getOpenMeteoCurrentWeather(latitude, longitude);
      if (openMeteoData) {
        return { success: true, data: openMeteoData };
      }

      return { success: false, error: 'No weather data available from any source' };
    } catch (error) {
      console.error('Weather service error:', error);
      return { 
        success: false, 
        error: error instanceof Error ? error.message : 'Unknown weather service error' 
      };
    }
  }

  /**
   * Get weather forecast for a location
   */
  public async getForecast(latitude: number, longitude: number): Promise<WeatherData[]> {
    try {
      return await this.getOpenMeteoForecast(latitude, longitude);
    } catch (error) {
      console.error('Forecast service error:', error);
      return [];
    }
  }

  /**
   * Fetch current weather from National Weather Service
   */
  private async getNWSCurrentWeather(latitude: number, longitude: number): Promise<WeatherData | null> {
    try {
      // Get grid point for coordinates
      const pointResponse = await axios.get(`${this.nwsBaseUrl}/points/${latitude},${longitude}`, {
        timeout: 10000,
        headers: { 'User-Agent': 'ChemicalDispersionApp/1.0' }
      });

      if (!pointResponse.data?.properties?.forecastOffice || !pointResponse.data?.properties?.gridX) {
        return null;
      }

      const { forecastOffice, gridX, gridY } = pointResponse.data.properties;

      // Get current observations
      const obsResponse = await axios.get(
        `${this.nwsBaseUrl}/gridpoints/${forecastOffice}/${gridX},${gridY}/observations/latest`,
        {
          timeout: 10000,
          headers: { 'User-Agent': 'ChemicalDispersionApp/1.0' }
        }
      );

      const obs = obsResponse.data?.properties;
      if (!obs) return null;

      // Convert NWS data to our format
      const weatherData: WeatherData = {
        id: 0,
        latitude,
        longitude,
        timestamp: new Date(obs.timestamp || Date.now()),
        temperature: this.convertCelsiusToFahrenheit(obs.temperature?.value || 20),
        humidity: obs.relativeHumidity?.value || 50,
        pressure: obs.barometricPressure?.value ? obs.barometricPressure.value / 100 : 1013.25, // Convert Pa to hPa
        windSpeed: obs.windSpeed?.value ? obs.windSpeed.value * 0.277778 : 5, // Convert km/h to m/s
        windDirection: obs.windDirection?.value || 270,
        windGust: obs.windGust?.value ? obs.windGust.value * 0.277778 : undefined,
        visibility: obs.visibility?.value ? obs.visibility.value / 1000 : undefined, // Convert m to km
        cloudCover: obs.cloudLayers?.[0]?.amount ? this.parseCloudCover(obs.cloudLayers[0].amount) : undefined,
        stabilityClass: this.determineStabilityClass(obs.windSpeed?.value || 5, obs.temperature?.value || 20),
        source: 'National Weather Service',
        dataSource: 'NWS API',
        qualityFlag: 'Good',
        rawData: JSON.stringify(obs),
        createdAt: new Date()
      };

      return weatherData;
    } catch (error) {
      console.error('NWS API error:', error);
      return null;
    }
  }

  /**
   * Fetch current weather from OpenMeteo
   */
  private async getOpenMeteoCurrentWeather(latitude: number, longitude: number): Promise<WeatherData | null> {
    try {
      const response = await axios.get(`${this.openMeteoBaseUrl}/forecast`, {
        params: {
          latitude,
          longitude,
          current_weather: true,
          hourly: 'temperature_2m,relativehumidity_2m,surface_pressure,windspeed_10m,winddirection_10m,windgusts_10m',
          timezone: 'auto'
        },
        timeout: 10000
      });

      const data = response.data;
      if (!data?.current_weather) return null;

      const current = data.current_weather;
      const hourly = data.hourly;

      // Get the current hour index
      const currentTime = new Date();
      const currentHourIndex = 0; // Use first hour for current conditions

      const weatherData: WeatherData = {
        id: 0,
        latitude,
        longitude,
        timestamp: new Date(current.time || Date.now()),
        temperature: current.temperature || 20,
        humidity: hourly?.relativehumidity_2m?.[currentHourIndex] || 50,
        pressure: hourly?.surface_pressure?.[currentHourIndex] || 1013.25,
        windSpeed: current.windspeed || 5,
        windDirection: current.winddirection || 270,
        windGust: hourly?.windgusts_10m?.[currentHourIndex],
        stabilityClass: this.determineStabilityClass(current.windspeed || 5, current.temperature || 20),
        source: 'Open-Meteo',
        dataSource: 'OpenMeteo API',
        qualityFlag: 'Good',
        rawData: JSON.stringify(data),
        createdAt: new Date()
      };

      return weatherData;
    } catch (error) {
      console.error('OpenMeteo API error:', error);
      return null;
    }
  }

  /**
   * Get forecast data from OpenMeteo
   */
  private async getOpenMeteoForecast(latitude: number, longitude: number): Promise<WeatherData[]> {
    try {
      const response = await axios.get(`${this.openMeteoBaseUrl}/forecast`, {
        params: {
          latitude,
          longitude,
          hourly: 'temperature_2m,relativehumidity_2m,surface_pressure,windspeed_10m,winddirection_10m,windgusts_10m',
          forecast_days: 3,
          timezone: 'auto'
        },
        timeout: 15000
      });

      const data = response.data;
      if (!data?.hourly) return [];

      const hourly = data.hourly;
      const forecasts: WeatherData[] = [];

      // Process next 24 hours
      for (let i = 0; i < Math.min(24, hourly.time?.length || 0); i++) {
        const forecast: WeatherData = {
          id: 0,
          latitude,
          longitude,
          timestamp: new Date(hourly.time[i]),
          temperature: hourly.temperature_2m?.[i] || 20,
          humidity: hourly.relativehumidity_2m?.[i] || 50,
          pressure: hourly.surface_pressure?.[i] || 1013.25,
          windSpeed: hourly.windspeed_10m?.[i] || 5,
          windDirection: hourly.winddirection_10m?.[i] || 270,
          windGust: hourly.windgusts_10m?.[i],
          stabilityClass: this.determineStabilityClass(
            hourly.windspeed_10m?.[i] || 5,
            hourly.temperature_2m?.[i] || 20
          ),
          source: 'Open-Meteo Forecast',
          dataSource: 'OpenMeteo API',
          qualityFlag: 'Good',
          createdAt: new Date()
        };

        forecasts.push(forecast);
      }

      return forecasts;
    } catch (error) {
      console.error('OpenMeteo forecast error:', error);
      return [];
    }
  }

  /**
   * Determine atmospheric stability class based on wind speed and temperature
   */
  private determineStabilityClass(windSpeed: number, temperature: number): string {
    // Simplified Pasquill-Gifford stability classification
    // In reality, this would consider solar radiation, cloud cover, time of day, etc.
    
    if (windSpeed < 2) {
      return 'F'; // Very stable
    } else if (windSpeed < 3) {
      return 'E'; // Stable
    } else if (windSpeed < 5) {
      return 'D'; // Neutral
    } else if (windSpeed < 6) {
      return 'C'; // Slightly unstable
    } else {
      return 'B'; // Unstable
    }
  }

  /**
   * Convert Celsius to Fahrenheit (if needed)
   */
  private convertCelsiusToFahrenheit(celsius: number): number {
    return celsius; // Keep in Celsius for calculations
  }

  /**
   * Parse cloud cover amount from NWS data
   */
  private parseCloudCover(amount: string): number {
    const cloudMap: Record<string, number> = {
      'CLR': 0,
      'FEW': 25,
      'SCT': 50,
      'BKN': 75,
      'OVC': 100
    };
    
    return cloudMap[amount] || 50;
  }
}