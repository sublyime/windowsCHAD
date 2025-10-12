/**
 * Database Service using SQLite for cross-platform compatibility
 * Replaces PostgreSQL Entity Framework from C# version
 */

import * as sqlite3 from 'sqlite3';
import * as path from 'path';
import { app } from 'electron';
import { 
  Chemical, 
  Release, 
  WeatherData, 
  DispersionResult,
  ChemicalFormData,
  ReleaseFormData
} from '../../shared/types';

export class DatabaseService {
  private db: sqlite3.Database | null = null;
  private readonly dbPath: string;

  constructor() {
    // Store database in user data directory
    const userDataPath = app.getPath('userData');
    this.dbPath = path.join(userDataPath, 'chemical_dispersion.db');
  }

  /**
   * Initialize database connection and create tables
   */
  public async initialize(): Promise<void> {
    return new Promise((resolve, reject) => {
      this.db = new sqlite3.Database(this.dbPath, (err) => {
        if (err) {
          reject(err);
          return;
        }
        
        console.log('Connected to SQLite database');
        this.createTables()
          .then(resolve)
          .catch(reject);
      });
    });
  }

  /**
   * Create database tables
   */
  private async createTables(): Promise<void> {
    if (!this.db) throw new Error('Database not initialized');

    const tables = [
      // Chemicals table
      `CREATE TABLE IF NOT EXISTS chemicals (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        name TEXT NOT NULL,
        cas_number TEXT,
        physical_state TEXT DEFAULT 'Gas',
        molecular_weight REAL NOT NULL,
        vapor_pressure REAL,
        boiling_point REAL,
        melting_point REAL,
        density REAL NOT NULL,
        henry_constant REAL,
        diffusion_coefficient REAL,
        toxicity_threshold REAL,
        toxicity_unit TEXT,
        is_flammable BOOLEAN DEFAULT 0,
        lower_explosive_limit REAL,
        upper_explosive_limit REAL,
        description TEXT,
        created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
        updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
      )`,

      // Weather data table
      `CREATE TABLE IF NOT EXISTS weather_data (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        latitude REAL NOT NULL,
        longitude REAL NOT NULL,
        timestamp DATETIME NOT NULL,
        temperature REAL NOT NULL,
        humidity REAL NOT NULL,
        pressure REAL NOT NULL,
        wind_speed REAL NOT NULL,
        wind_direction REAL NOT NULL,
        wind_gust REAL,
        visibility REAL,
        cloud_cover REAL,
        precipitation REAL,
        stability_class TEXT,
        mixing_height REAL,
        surface_roughness REAL,
        monin_obukhov_length REAL,
        source TEXT NOT NULL,
        data_source TEXT,
        quality_flag TEXT,
        raw_data TEXT,
        created_at DATETIME DEFAULT CURRENT_TIMESTAMP
      )`,

      // Releases table
      `CREATE TABLE IF NOT EXISTS releases (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        name TEXT NOT NULL,
        latitude REAL NOT NULL,
        longitude REAL NOT NULL,
        elevation REAL DEFAULT 0,
        release_height REAL NOT NULL,
        start_time DATETIME NOT NULL,
        end_time DATETIME,
        release_rate REAL,
        total_mass REAL,
        volume REAL,
        release_type TEXT DEFAULT 'Instantaneous',
        scenario TEXT NOT NULL,
        temperature REAL,
        pressure REAL,
        diameter_or_area REAL,
        initial_temperature REAL,
        is_liquid_release BOOLEAN DEFAULT 0,
        is_heavy_gas BOOLEAN DEFAULT 0,
        is_hot_release BOOLEAN DEFAULT 0,
        modeling_wind_speed REAL NOT NULL,
        modeling_wind_direction REAL NOT NULL,
        modeling_stability_class TEXT DEFAULT 'D',
        modeling_temperature REAL NOT NULL,
        modeling_humidity REAL NOT NULL,
        status TEXT DEFAULT 'Draft',
        description TEXT,
        created_by TEXT,
        created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
        updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
        chemical_id INTEGER NOT NULL,
        weather_data_id INTEGER,
        FOREIGN KEY (chemical_id) REFERENCES chemicals (id),
        FOREIGN KEY (weather_data_id) REFERENCES weather_data (id)
      )`,

      // Dispersion results table
      `CREATE TABLE IF NOT EXISTS dispersion_results (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        calculation_time DATETIME NOT NULL,
        latitude REAL NOT NULL,
        longitude REAL NOT NULL,
        height REAL NOT NULL,
        concentration REAL NOT NULL,
        concentration_unit TEXT DEFAULT 'mg/m³',
        dosage REAL,
        distance_from_source REAL NOT NULL,
        direction_from_source REAL NOT NULL,
        plume_width REAL,
        plume_height REAL,
        wind_speed REAL NOT NULL,
        wind_direction REAL NOT NULL,
        stability_class TEXT NOT NULL,
        temperature REAL NOT NULL,
        exceeds_toxicity_threshold BOOLEAN DEFAULT 0,
        risk_level TEXT DEFAULT 'Low',
        model_used TEXT DEFAULT 'Gaussian Plume',
        calculation_parameters TEXT,
        created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
        release_id INTEGER NOT NULL,
        receptor_id INTEGER,
        FOREIGN KEY (release_id) REFERENCES releases (id)
      )`
    ];

    return new Promise((resolve, reject) => {
      if (!this.db) {
        reject(new Error('Database not initialized'));
        return;
      }

      let completed = 0;
      const total = tables.length;

      tables.forEach((sql) => {
        this.db!.run(sql, (err) => {
          if (err) {
            reject(err);
            return;
          }
          
          completed++;
          if (completed === total) {
            console.log('Database tables created successfully');
            resolve();
          }
        });
      });
    });
  }

  /**
   * Save a chemical to the database
   */
  public async saveChemical(chemicalData: ChemicalFormData): Promise<Chemical> {
    return new Promise((resolve, reject) => {
      if (!this.db) {
        reject(new Error('Database not initialized'));
        return;
      }

      const sql = `
        INSERT INTO chemicals (
          name, cas_number, physical_state, molecular_weight, density,
          toxicity_threshold, is_flammable, description
        ) VALUES (?, ?, ?, ?, ?, ?, ?, ?)
      `;

      const params = [
        chemicalData.name,
        chemicalData.casNumber,
        chemicalData.physicalState,
        chemicalData.molecularWeight,
        chemicalData.density,
        chemicalData.toxicityThreshold,
        chemicalData.isFlammable ? 1 : 0,
        chemicalData.description
      ];

      this.db.run(sql, params, function(err) {
        if (err) {
          reject(err);
          return;
        }

        // Return the created chemical
        const chemical: Chemical = {
          id: this.lastID,
          name: chemicalData.name,
          casNumber: chemicalData.casNumber,
          physicalState: chemicalData.physicalState as 'Gas' | 'Liquid' | 'Solid',
          molecularWeight: chemicalData.molecularWeight,
          density: chemicalData.density,
          toxicityThreshold: chemicalData.toxicityThreshold,
          isFlammable: chemicalData.isFlammable,
          description: chemicalData.description,
          createdAt: new Date(),
          updatedAt: new Date()
        };

        resolve(chemical);
      });
    });
  }

  /**
   * Get all chemicals from the database
   */
  public async getChemicals(): Promise<Chemical[]> {
    return new Promise((resolve, reject) => {
      if (!this.db) {
        reject(new Error('Database not initialized'));
        return;
      }

      const sql = 'SELECT * FROM chemicals ORDER BY name';

      this.db.all(sql, [], (err, rows: any[]) => {
        if (err) {
          reject(err);
          return;
        }

        const chemicals: Chemical[] = rows.map(row => ({
          id: row.id,
          name: row.name,
          casNumber: row.cas_number,
          physicalState: row.physical_state,
          molecularWeight: row.molecular_weight,
          vaporPressure: row.vapor_pressure,
          boilingPoint: row.boiling_point,
          meltingPoint: row.melting_point,
          density: row.density,
          henryConstant: row.henry_constant,
          diffusionCoefficient: row.diffusion_coefficient,
          toxicityThreshold: row.toxicity_threshold,
          toxicityUnit: row.toxicity_unit,
          isFlammable: row.is_flammable === 1,
          lowerExplosiveLimit: row.lower_explosive_limit,
          upperExplosiveLimit: row.upper_explosive_limit,
          description: row.description,
          createdAt: new Date(row.created_at),
          updatedAt: new Date(row.updated_at)
        }));

        resolve(chemicals);
      });
    });
  }

  /**
   * Save a release to the database
   */
  public async saveRelease(releaseData: ReleaseFormData): Promise<Release> {
    return new Promise((resolve, reject) => {
      if (!this.db) {
        reject(new Error('Database not initialized'));
        return;
      }

      const sql = `
        INSERT INTO releases (
          name, latitude, longitude, release_height, chemical_id,
          release_type, release_rate, total_mass, scenario, description,
          modeling_wind_speed, modeling_wind_direction, modeling_temperature,
          modeling_humidity, start_time
        ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
      `;

      const params = [
        releaseData.name,
        releaseData.latitude,
        releaseData.longitude,
        releaseData.releaseHeight,
        releaseData.chemicalId,
        releaseData.releaseType,
        releaseData.releaseRate,
        releaseData.totalMass,
        releaseData.scenario,
        releaseData.description,
        5.0, // Default wind speed
        270.0, // Default wind direction
        20.0, // Default temperature
        50.0, // Default humidity
        new Date().toISOString()
      ];

      this.db.run(sql, params, function(err) {
        if (err) {
          reject(err);
          return;
        }

        // Return the created release
        const release: Release = {
          id: this.lastID,
          name: releaseData.name,
          latitude: releaseData.latitude,
          longitude: releaseData.longitude,
          elevation: 0,
          releaseHeight: releaseData.releaseHeight,
          startTime: new Date(),
          releaseRate: releaseData.releaseRate,
          totalMass: releaseData.totalMass,
          releaseType: releaseData.releaseType as 'Instantaneous' | 'Continuous' | 'Variable',
          scenario: releaseData.scenario,
          isLiquidRelease: false,
          isHeavyGas: false,
          isHotRelease: false,
          modelingWindSpeed: 5.0,
          modelingWindDirection: 270.0,
          modelingStabilityClass: 'D',
          modelingTemperature: 20.0,
          modelingHumidity: 50.0,
          status: 'Draft',
          description: releaseData.description,
          createdAt: new Date(),
          updatedAt: new Date(),
          chemicalId: releaseData.chemicalId
        };

        resolve(release);
      });
    });
  }

  /**
   * Get all releases from the database
   */
  public async getReleases(): Promise<Release[]> {
    return new Promise((resolve, reject) => {
      if (!this.db) {
        reject(new Error('Database not initialized'));
        return;
      }

      const sql = `
        SELECT r.*, c.name as chemical_name 
        FROM releases r 
        LEFT JOIN chemicals c ON r.chemical_id = c.id 
        ORDER BY r.created_at DESC
      `;

      this.db.all(sql, [], (err, rows: any[]) => {
        if (err) {
          reject(err);
          return;
        }

        const releases: Release[] = rows.map(row => ({
          id: row.id,
          name: row.name,
          latitude: row.latitude,
          longitude: row.longitude,
          elevation: row.elevation || 0,
          releaseHeight: row.release_height,
          startTime: new Date(row.start_time),
          endTime: row.end_time ? new Date(row.end_time) : undefined,
          releaseRate: row.release_rate,
          totalMass: row.total_mass,
          volume: row.volume,
          releaseType: row.release_type,
          scenario: row.scenario,
          temperature: row.temperature,
          pressure: row.pressure,
          diameterOrArea: row.diameter_or_area,
          initialTemperature: row.initial_temperature,
          isLiquidRelease: row.is_liquid_release === 1,
          isHeavyGas: row.is_heavy_gas === 1,
          isHotRelease: row.is_hot_release === 1,
          modelingWindSpeed: row.modeling_wind_speed,
          modelingWindDirection: row.modeling_wind_direction,
          modelingStabilityClass: row.modeling_stability_class,
          modelingTemperature: row.modeling_temperature,
          modelingHumidity: row.modeling_humidity,
          status: row.status,
          description: row.description,
          createdBy: row.created_by,
          createdAt: new Date(row.created_at),
          updatedAt: new Date(row.updated_at),
          chemicalId: row.chemical_id,
          weatherDataId: row.weather_data_id
        }));

        resolve(releases);
      });
    });
  }

  /**
   * Close database connection
   */
  public close(): void {
    if (this.db) {
      this.db.close((err) => {
        if (err) {
          console.error('Error closing database:', err);
        } else {
          console.log('Database connection closed');
        }
      });
      this.db = null;
    }
  }
}