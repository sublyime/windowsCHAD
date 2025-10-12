"use strict";
/**
 * Dispersion Modeling Service for Electron main process
 * Implements Gaussian plume calculations and atmospheric dispersion modeling
 */
Object.defineProperty(exports, "__esModule", { value: true });
exports.DispersionModelingService = void 0;
const GaussianPlumeModel_1 = require("../../shared/physics/GaussianPlumeModel");
class DispersionModelingService {
    /**
     * Calculate Gaussian plume dispersion for specific receptors
     */
    async calculateGaussianPlume(release, weather, receptors) {
        const startTime = Date.now();
        try {
            const results = [];
            // Calculate effective parameters
            const effectiveHeight = release.releaseHeight + this.calculatePlumeRise(release, weather);
            const effectiveRate = this.calculateEffectiveReleaseRate(release, weather);
            const stabilityClass = weather.stabilityClass || this.determineStabilityClass(weather);
            // Calculate concentration at each receptor
            for (const receptor of receptors) {
                const distance = this.calculateDistance(release.latitude, release.longitude, receptor.latitude, receptor.longitude);
                const bearing = this.calculateBearing(release.latitude, release.longitude, receptor.latitude, receptor.longitude);
                // Convert to downwind/crosswind coordinates
                const windRad = (weather.windDirection - 90) * Math.PI / 180;
                const receptorBearingRad = bearing * Math.PI / 180;
                const relativeBearing = receptorBearingRad - windRad;
                const x = distance * Math.cos(relativeBearing); // downwind
                const y = distance * Math.sin(relativeBearing); // crosswind
                if (x > 0) { // Only calculate for downwind receptors
                    const concentration = this.calculateGaussianConcentration(x, y, effectiveHeight, effectiveRate, weather.windSpeed, stabilityClass);
                    const result = {
                        id: 0,
                        calculationTime: new Date(),
                        latitude: receptor.latitude,
                        longitude: receptor.longitude,
                        height: 1.5, // Breathing height
                        concentration: Math.max(0, concentration),
                        concentrationUnit: "mg/m³",
                        distanceFromSource: distance,
                        directionFromSource: bearing,
                        windSpeed: weather.windSpeed,
                        windDirection: weather.windDirection,
                        stabilityClass: stabilityClass,
                        temperature: weather.temperature,
                        exceedsToxicityThreshold: false, // Would need chemical toxicity data
                        riskLevel: this.determineRiskLevel(concentration),
                        modelUsed: "Gaussian Plume",
                        createdAt: new Date(),
                        releaseId: release.id,
                        receptorId: receptor.id
                    };
                    results.push(result);
                }
            }
            const calculationTime = Date.now() - startTime;
            return {
                success: true,
                results,
                calculationTime
            };
        }
        catch (error) {
            return {
                success: false,
                error: error instanceof Error ? error.message : 'Calculation failed',
                calculationTime: Date.now() - startTime
            };
        }
    }
    /**
     * Calculate dispersion grid for visualization
     */
    async calculateDispersionGrid(release, weather, gridSize = 100, maxDistance = 10000) {
        const startTime = Date.now();
        try {
            const results = GaussianPlumeModel_1.GaussianPlumeModel.calculateDispersionGrid(release, weather, gridSize, maxDistance);
            const calculationTime = Date.now() - startTime;
            return {
                success: true,
                results,
                calculationTime
            };
        }
        catch (error) {
            return {
                success: false,
                error: error instanceof Error ? error.message : 'Grid calculation failed',
                calculationTime: Date.now() - startTime
            };
        }
    }
    /**
     * Calculate Gaussian concentration at a point
     */
    calculateGaussianConcentration(x, y, h, Q, u, stabilityClass) {
        if (x <= 0 || u <= 0)
            return 0;
        // Get dispersion parameters
        const dispParams = this.getDispersionParameters(stabilityClass);
        const sigmaY = this.calculateSigmaY(x, dispParams);
        const sigmaZ = this.calculateSigmaZ(x, dispParams);
        if (sigmaY <= 0 || sigmaZ <= 0)
            return 0;
        // Gaussian plume equation
        const lateralTerm = Math.exp(-0.5 * Math.pow(y / sigmaY, 2));
        const verticalTerm = Math.exp(-0.5 * Math.pow(h / sigmaZ, 2)) +
            Math.exp(-0.5 * Math.pow(-h / sigmaZ, 2)); // Ground reflection
        const concentration = (Q / (2 * Math.PI * u * sigmaY * sigmaZ)) * lateralTerm * verticalTerm;
        return concentration * 1000; // Convert to mg/m³
    }
    /**
     * Get Pasquill-Gifford dispersion parameters
     */
    getDispersionParameters(stabilityClass) {
        const params = {
            'A': { ay: 0.22, by: 0.0001, az: 0.20, bz: 0.0 },
            'B': { ay: 0.16, by: 0.0001, az: 0.12, bz: 0.0 },
            'C': { ay: 0.11, by: 0.0001, az: 0.08, bz: 0.0002 },
            'D': { ay: 0.08, by: 0.0001, az: 0.06, bz: 0.0015 },
            'E': { ay: 0.06, by: 0.0001, az: 0.03, bz: 0.0003 },
            'F': { ay: 0.04, by: 0.0001, az: 0.016, bz: 0.0003 }
        };
        return params[stabilityClass.toUpperCase()] || params['D'];
    }
    /**
     * Calculate sigma Y (horizontal dispersion coefficient)
     */
    calculateSigmaY(x, dispParams) {
        return dispParams.ay * x * Math.pow(1 + dispParams.by * x, -0.5);
    }
    /**
     * Calculate sigma Z (vertical dispersion coefficient)
     */
    calculateSigmaZ(x, dispParams) {
        return dispParams.az * x * Math.pow(1 + dispParams.bz * x, -0.5);
    }
    /**
     * Calculate effective release rate
     */
    calculateEffectiveReleaseRate(release, weather) {
        // Basic calculation - could be enhanced with evaporation models, etc.
        if (release.releaseRate) {
            return release.releaseRate; // kg/s
        }
        if (release.totalMass) {
            // For instantaneous releases, assume 1-hour duration
            return release.totalMass / 3600; // kg/s
        }
        return 1.0; // Default 1 kg/s
    }
    /**
     * Calculate plume rise due to buoyancy and momentum
     */
    calculatePlumeRise(release, weather) {
        // Simplified plume rise calculation
        // In reality would consider stack parameters, exit velocity, temperature, etc.
        if (release.initialTemperature && release.initialTemperature > weather.temperature + 10) {
            // Hot release - some buoyant rise
            const deltaT = release.initialTemperature - weather.temperature;
            return Math.min(50, deltaT * 0.5); // Max 50m rise
        }
        return 0; // Assume ground-level release
    }
    /**
     * Determine atmospheric stability class from weather data
     */
    determineStabilityClass(weather) {
        if (weather.stabilityClass) {
            return weather.stabilityClass;
        }
        // Simplified classification based on wind speed
        // In reality would consider solar radiation, cloud cover, time of day
        const windSpeed = weather.windSpeed;
        if (windSpeed < 2)
            return 'F'; // Very stable
        if (windSpeed < 3)
            return 'E'; // Stable
        if (windSpeed < 5)
            return 'D'; // Neutral
        if (windSpeed < 6)
            return 'C'; // Slightly unstable
        return 'B'; // Unstable
    }
    /**
     * Determine risk level based on concentration
     */
    determineRiskLevel(concentration) {
        if (concentration > 100)
            return 'CRITICAL';
        if (concentration > 10)
            return 'HIGH';
        if (concentration > 1)
            return 'MODERATE';
        if (concentration > 0.1)
            return 'LOW';
        return 'MINIMAL';
    }
    /**
     * Calculate distance between two lat/lng points
     */
    calculateDistance(lat1, lng1, lat2, lng2) {
        const R = 6371000; // Earth's radius in meters
        const dLat = (lat2 - lat1) * Math.PI / 180;
        const dLng = (lng2 - lng1) * Math.PI / 180;
        const a = Math.sin(dLat / 2) * Math.sin(dLat / 2) +
            Math.cos(lat1 * Math.PI / 180) * Math.cos(lat2 * Math.PI / 180) *
                Math.sin(dLng / 2) * Math.sin(dLng / 2);
        const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
        return R * c;
    }
    /**
     * Calculate bearing from one point to another
     */
    calculateBearing(lat1, lng1, lat2, lng2) {
        const dLng = (lng2 - lng1) * Math.PI / 180;
        const lat1Rad = lat1 * Math.PI / 180;
        const lat2Rad = lat2 * Math.PI / 180;
        const y = Math.sin(dLng) * Math.cos(lat2Rad);
        const x = Math.cos(lat1Rad) * Math.sin(lat2Rad) -
            Math.sin(lat1Rad) * Math.cos(lat2Rad) * Math.cos(dLng);
        const bearing = Math.atan2(y, x) * 180 / Math.PI;
        return (bearing + 360) % 360;
    }
}
exports.DispersionModelingService = DispersionModelingService;
