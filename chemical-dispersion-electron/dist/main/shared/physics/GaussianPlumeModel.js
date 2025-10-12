"use strict";
/**
 * Gaussian Plume Atmospheric Dispersion Model
 * Ported from C# RealMappingService to TypeScript
 */
Object.defineProperty(exports, "__esModule", { value: true });
exports.GaussianPlumeModel = void 0;
class GaussianPlumeModel {
    /**
     * Generate realistic contour data for atmospheric dispersion visualization
     */
    static generateContourData(result, concentrationLevels = [10.0, 1.0, 0.1, 0.01], colors = ["#FF0000", "#FF8800", "#FFAA00", "#FFDD00"], opacities = [0.8, 0.6, 0.4, 0.3]) {
        const centerLat = result.latitude;
        const centerLng = result.longitude;
        // Get weather data for realistic dispersion
        const windSpeed = result.windSpeed > 0 ? result.windSpeed : 5.0; // m/s
        const windDirection = result.windDirection; // degrees from north
        const stabilityClass = result.stabilityClass || "D"; // Pasquill stability
        const contours = [];
        // Generate multiple concentration contours for realistic visualization
        for (let i = 0; i < concentrationLevels.length; i++) {
            const contour = this.generateGaussianPlumeContour(centerLat, centerLng, windSpeed, windDirection, stabilityClass, concentrationLevels[i]);
            contours.push({
                coordinates: [contour],
                concentration: concentrationLevels[i],
                color: colors[i] || colors[colors.length - 1],
                fillColor: colors[i] || colors[colors.length - 1],
                opacity: opacities[i] || opacities[opacities.length - 1]
            });
        }
        // Add downwind corridor visualization
        const corridorContour = this.generateDownwindCorridor(centerLat, centerLng, windSpeed, windDirection);
        contours.push({
            coordinates: [corridorContour],
            concentration: -1.0, // Special marker for corridor
            color: "#0066CC",
            fillColor: "#0066CC",
            opacity: 0.2,
            isDownwindCorridor: true
        });
        return {
            contours,
            windDirection,
            windSpeed,
            stabilityClass
        };
    }
    /**
     * Generate Gaussian plume contour for specific concentration level
     */
    static generateGaussianPlumeContour(sourceLat, sourceLng, windSpeed, windDirection, stabilityClass, concentrationLevel) {
        const contour = [];
        // Pasquill-Gifford dispersion parameters
        const dispersionParams = this.getDispersionParameters(stabilityClass);
        // Convert wind direction to radians (meteorological to mathematical)
        const windRad = (windDirection - 90) * Math.PI / 180;
        // Calculate maximum downwind distance for this concentration
        const maxDistance = this.calculateMaxDistance(windSpeed, concentrationLevel, dispersionParams);
        // Generate points along the plume boundary
        const numPoints = 50;
        // Right side of plume
        for (let i = 0; i <= numPoints; i++) {
            const x = (i / numPoints) * maxDistance; // downwind distance
            const sigmaY = this.calculateSigmaY(x, dispersionParams);
            const sigmaZ = this.calculateSigmaZ(x, dispersionParams);
            // Calculate crosswind distance for this concentration level
            const y = this.calculateCrosswindDistance(concentrationLevel, sigmaY, sigmaZ);
            // Transform to lat/lng
            const point = this.transformToLatLng(sourceLat, sourceLng, x, y, windRad);
            contour.push([point.lat, point.lng]);
        }
        // Left side of plume (reverse order)
        for (let i = numPoints; i >= 0; i--) {
            const x = (i / numPoints) * maxDistance;
            const sigmaY = this.calculateSigmaY(x, dispersionParams);
            const sigmaZ = this.calculateSigmaZ(x, dispersionParams);
            const y = -this.calculateCrosswindDistance(concentrationLevel, sigmaY, sigmaZ);
            const point = this.transformToLatLng(sourceLat, sourceLng, x, y, windRad);
            contour.push([point.lat, point.lng]);
        }
        // Close the polygon
        if (contour.length > 0) {
            contour.push(contour[0]);
        }
        return contour;
    }
    /**
     * Generate downwind corridor visualization
     */
    static generateDownwindCorridor(sourceLat, sourceLng, windSpeed, windDirection) {
        const corridor = [];
        const windRad = (windDirection - 90) * Math.PI / 180;
        // Create a corridor showing the general wind direction
        const corridorWidth = 0.02; // degrees
        const corridorLength = 0.05; // degrees
        // Corridor extends downwind
        const endPoint = this.transformToLatLng(sourceLat, sourceLng, corridorLength * 111000, 0, windRad);
        // Create corridor polygon
        const leftStart = this.transformToLatLng(sourceLat, sourceLng, 0, corridorWidth * 111000 / 2, windRad);
        const rightStart = this.transformToLatLng(sourceLat, sourceLng, 0, -corridorWidth * 111000 / 2, windRad);
        const leftEnd = this.transformToLatLng(endPoint.lat, endPoint.lng, 0, corridorWidth * 111000 / 2, windRad);
        const rightEnd = this.transformToLatLng(endPoint.lat, endPoint.lng, 0, -corridorWidth * 111000 / 2, windRad);
        corridor.push([leftStart.lat, leftStart.lng]);
        corridor.push([leftEnd.lat, leftEnd.lng]);
        corridor.push([rightEnd.lat, rightEnd.lng]);
        corridor.push([rightStart.lat, rightStart.lng]);
        corridor.push([leftStart.lat, leftStart.lng]); // Close polygon
        return corridor;
    }
    /**
     * Get Pasquill-Gifford dispersion parameters
     */
    static getDispersionParameters(stabilityClass) {
        const params = {
            'A': { ay: 0.22, by: 0.0001, az: 0.20, bz: 0.0 }, // Very unstable
            'B': { ay: 0.16, by: 0.0001, az: 0.12, bz: 0.0 }, // Unstable
            'C': { ay: 0.11, by: 0.0001, az: 0.08, bz: 0.0002 }, // Slightly unstable
            'D': { ay: 0.08, by: 0.0001, az: 0.06, bz: 0.0015 }, // Neutral
            'E': { ay: 0.06, by: 0.0001, az: 0.03, bz: 0.0003 }, // Slightly stable
            'F': { ay: 0.04, by: 0.0001, az: 0.016, bz: 0.0003 } // Stable
        };
        return params[stabilityClass.toUpperCase()] || params['D']; // Default to neutral
    }
    /**
     * Calculate sigma Y (horizontal dispersion coefficient)
     */
    static calculateSigmaY(x, dispParams) {
        return dispParams.ay * x * Math.pow(1 + dispParams.by * x, -0.5);
    }
    /**
     * Calculate sigma Z (vertical dispersion coefficient)
     */
    static calculateSigmaZ(x, dispParams) {
        return dispParams.az * x * Math.pow(1 + dispParams.bz * x, -0.5);
    }
    /**
     * Calculate maximum distance for concentration level
     */
    static calculateMaxDistance(windSpeed, concentrationLevel, dispParams) {
        // Simplified calculation - in reality this would be more complex
        const baseDistance = Math.log(100.0 / concentrationLevel) * windSpeed * 100;
        return Math.max(500, Math.min(10000, baseDistance)); // Clamp between 500m and 10km
    }
    /**
     * Calculate crosswind distance for concentration level
     */
    static calculateCrosswindDistance(concentrationLevel, sigmaY, sigmaZ) {
        // Simplified Gaussian plume calculation
        const factor = Math.sqrt(-2 * Math.log(concentrationLevel / 100.0));
        return factor * sigmaY;
    }
    /**
     * Transform local coordinates to lat/lng
     */
    static transformToLatLng(originLat, originLng, x, y, windRad) {
        // Convert meters to degrees (approximate)
        const latOffset = (x * Math.cos(windRad) - y * Math.sin(windRad)) / 111000.0;
        const lngOffset = (x * Math.sin(windRad) + y * Math.cos(windRad)) / (111000.0 * Math.cos(originLat * Math.PI / 180));
        return {
            lat: originLat + latOffset,
            lng: originLng + lngOffset
        };
    }
    /**
     * Calculate full dispersion grid for detailed modeling
     */
    static calculateDispersionGrid(release, weather, gridSize = 100, maxDistance = 10000) {
        const results = [];
        const effectiveReleaseRate = this.calculateEffectiveReleaseRate(release, weather);
        const plumeRise = this.calculatePlumeRise(release, weather);
        const effectiveHeight = release.releaseHeight + plumeRise;
        const stabilityClass = this.determineStabilityClass(weather);
        // Convert wind direction to radians for calculations
        const windRad = (weather.windDirection - 90) * Math.PI / 180;
        // Calculate grid points
        const numPoints = Math.ceil(maxDistance / gridSize);
        const crosswindPoints = Math.ceil(maxDistance / gridSize / 2);
        for (let i = 1; i <= numPoints; i++) {
            const x = i * gridSize; // downwind distance
            for (let j = -crosswindPoints; j <= crosswindPoints; j++) {
                const y = j * gridSize; // crosswind distance
                // Calculate concentration at this point
                const concentration = this.calculateGaussianConcentration(x, y, effectiveHeight, effectiveReleaseRate, weather.windSpeed, stabilityClass);
                if (concentration > 0.001) { // Only include significant concentrations
                    // Transform to geographic coordinates
                    const point = this.transformToLatLng(release.latitude, release.longitude, x, y, windRad);
                    const result = {
                        id: 0,
                        calculationTime: new Date(),
                        latitude: point.lat,
                        longitude: point.lng,
                        height: 1.5, // Breathing height
                        concentration: concentration,
                        concentrationUnit: "mg/m³",
                        distanceFromSource: Math.sqrt(x * x + y * y),
                        directionFromSource: Math.atan2(y, x) * 180 / Math.PI,
                        windSpeed: weather.windSpeed,
                        windDirection: weather.windDirection,
                        stabilityClass: stabilityClass,
                        temperature: weather.temperature,
                        exceedsToxicityThreshold: false, // Would need chemical data
                        riskLevel: concentration > 10 ? "HIGH" : concentration > 1 ? "MODERATE" : "LOW",
                        modelUsed: "Gaussian Plume",
                        createdAt: new Date(),
                        releaseId: release.id
                    };
                    results.push(result);
                }
            }
        }
        return results;
    }
    /**
     * Calculate Gaussian concentration at a point
     */
    static calculateGaussianConcentration(x, y, h, Q, u, stabilityClass) {
        if (x <= 0)
            return 0;
        const dispParams = this.getDispersionParameters(stabilityClass);
        const sigmaY = this.calculateSigmaY(x, dispParams);
        const sigmaZ = this.calculateSigmaZ(x, dispParams);
        // Gaussian plume equation
        const lateralTerm = Math.exp(-0.5 * Math.pow(y / sigmaY, 2));
        const verticalTerm = Math.exp(-0.5 * Math.pow(h / sigmaZ, 2)) +
            Math.exp(-0.5 * Math.pow(-h / sigmaZ, 2)); // Ground reflection
        const concentration = (Q / (2 * Math.PI * u * sigmaY * sigmaZ)) * lateralTerm * verticalTerm;
        return concentration * 1000; // Convert to mg/m³
    }
    /**
     * Calculate effective release rate
     */
    static calculateEffectiveReleaseRate(release, weather) {
        // Simplified - would be more complex in reality
        return release.releaseRate || (release.totalMass || 0) / 3600; // kg/s
    }
    /**
     * Calculate plume rise
     */
    static calculatePlumeRise(release, weather) {
        // Simplified plume rise calculation
        // In reality would consider buoyancy, momentum, atmospheric stability
        return 0; // Assume ground-level release for now
    }
    /**
     * Determine atmospheric stability class
     */
    static determineStabilityClass(weather) {
        // Simplified stability determination
        if (weather.stabilityClass) {
            return weather.stabilityClass;
        }
        // Basic classification based on wind speed
        if (weather.windSpeed < 2)
            return "F"; // Stable
        if (weather.windSpeed < 4)
            return "E"; // Slightly stable  
        if (weather.windSpeed < 6)
            return "D"; // Neutral
        return "C"; // Slightly unstable
    }
}
exports.GaussianPlumeModel = GaussianPlumeModel;
