# Chemical Dispersion Modeling Application - Copilot Instructions

## Project Overview
This is a comprehensive Windows desktop application for modeling chemical dispersions in urban areas using fluid dynamics and real-time physics simulation.

## Key Features
- [x] ✅ **Project Requirements Clarified** - Windows desktop app for chemical dispersion modeling
- [x] ✅ **Project Scaffolded** - .NET 8 WPF application structure complete
- [x] ✅ **Database Integration** - PostgreSQL connectivity configured (postgres/ala1nna@javadisp)
- [x] ✅ **Domain Models** - Chemical, WeatherData, Release, Receptor, DispersionResult, TerrainData
- [x] ✅ **Service Interfaces** - Weather, Dispersion Modeling, and Terrain services defined
- [x] ✅ **MVVM Architecture** - View models and dependency injection configured
- [x] ✅ **Interactive UI** - Comprehensive WPF interface with map interaction
- [ ] **GIS Mapping** - Professional mapping integration (Microsoft Maps/ArcGIS)
- [ ] **Weather Implementation** - Full API integration for NWS and OpenMeteo
- [ ] **Physics Engine** - Complete fluid dynamics and dispersion modeling
- [ ] **3D Visualization** - Topographical data and building visualization
- [ ] **File Import** - GIS imagery and geo file support
- [ ] **Real-time Updates** - 30-second refresh implementation
- [ ] **Database Migrations** - Entity Framework migrations setup

## Technology Stack
- **Framework**: .NET 8 WPF (Windows Presentation Foundation)
- **Database**: PostgreSQL with Entity Framework Core
- **Mapping**: Microsoft Maps SDK or ArcGIS Runtime
- **Weather APIs**: National Weather Service API, OpenMeteo API
- **Physics**: Custom fluid dynamics engine
- **3D Visualization**: Helix Toolkit 3D

## Database Configuration
- **Host**: localhost
- **Username**: postgres
- **Password**: ala1nna
- **Database**: javadisp

## Current Status
The basic application structure is complete with:
- ✅ Solution with 3 projects (Core, Data, Desktop)
- ✅ Domain models and entity relationships
- ✅ Service interfaces and dependency injection
- ✅ Comprehensive WPF UI with MVVM pattern
- ✅ Mock implementations for development/testing
- ✅ Configuration system with appsettings.json
- ✅ Build system working correctly

## Development Guidelines
- Follow MVVM pattern for WPF
- Implement dependency injection
- Use async/await for all API calls
- Implement proper error handling and logging
- Follow clean architecture principles
- Include comprehensive unit tests

## Next Steps
1. Set up Entity Framework migrations
2. Implement actual weather service APIs
3. Complete dispersion modeling calculations
4. Add professional mapping control
5. Implement 3D terrain visualization
6. Add GIS file import capabilities