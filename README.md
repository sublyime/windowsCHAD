# Chemical Dispersion Modeling Application

A comprehensive Windows desktop application for modeling chemical dispersions in urban areas using fluid dynamics and real-time physics simulation.

## Features

- **Real-time Weather Integration**: Connects to National Weather Service API and OpenMeteo for live weather data
- **PostgreSQL Database**: Stores chemicals, weather data, releases, and results
- **Interactive Mapping**: Click-to-select release points with visual feedback
- **Chemical Database**: Comprehensive chemical properties and toxicity information
- **Dispersion Modeling**: Gaussian plume model with atmospheric stability calculations
- **Receptor Analysis**: Downwind impact assessment with risk classification
- **3D Terrain Support**: Integration with topographical and building data
- **Real-time Updates**: Automatic refresh every 30 seconds
- **GIS Import**: Support for importing GIS imagery and geo file types

## Technology Stack

- **Framework**: .NET 8 WPF (Windows Presentation Foundation)
- **Database**: PostgreSQL with Entity Framework Core
- **Architecture**: MVVM pattern with dependency injection
- **Weather APIs**: National Weather Service API, OpenMeteo API
- **Physics**: Gaussian plume dispersion model with Pasquill-Gifford stability classes

## Database Configuration

The application connects to a PostgreSQL database with the following default settings:
- **Host**: localhost
- **Database**: javadisp
- **Username**: postgres
- **Password**: ala1nna
- **Port**: 5432

## Getting Started

### Prerequisites

1. .NET 8 SDK
2. PostgreSQL server running locally
3. Visual Studio 2022 or VS Code

### Setup

1. Clone the repository
2. Ensure PostgreSQL is running with the database `javadisp` created
3. Restore NuGet packages:
   ```
   dotnet restore
   ```
4. Build the solution:
   ```
   dotnet build
   ```
5. Run the application:
   ```
   dotnet run --project ChemicalDispersionModeling.Desktop
   ```

## Project Structure

```
ChemicalDispersionModeling/
├── ChemicalDispersionModeling.Core/       # Domain models and services
│   ├── Models/                             # Domain entities
│   └── Services/                           # Business logic interfaces
├── ChemicalDispersionModeling.Data/       # Data access layer
│   └── Context/                            # Entity Framework DbContext
└── ChemicalDispersionModeling.Desktop/    # WPF UI application
    └── ViewModels/                         # MVVM view models
```

## Key Components

### Domain Models
- **Chemical**: Chemical properties and characteristics
- **WeatherData**: Meteorological observations
- **Release**: Chemical release event configuration
- **Receptor**: Downwind monitoring points
- **DispersionResult**: Model calculation results
- **TerrainData**: Topographical and building information

### Services
- **IWeatherService**: Weather data acquisition from multiple sources
- **IDispersionModelingService**: Gaussian plume dispersion calculations
- **ITerrainService**: Terrain and building data management

### User Interface
- **Interactive Map**: Click-to-select release locations
- **Release Configuration**: Chemical selection and release parameters
- **Weather Display**: Real-time meteorological conditions
- **Results Grid**: Receptor concentrations and risk levels
- **Status Monitoring**: Database, weather, and model status

## Usage

1. **Set Release Point**: Click on the map to select the chemical release location
2. **Select Chemical**: Choose from the database of available chemicals
3. **Configure Release**: Set release type, rate, duration, and scenario
4. **Weather Update**: System automatically fetches current weather conditions
5. **Add Receptors**: Manually add or auto-generate downwind receptor points
6. **Run Model**: Execute dispersion calculations
7. **View Results**: Analyze concentrations, distances, and risk levels
8. **Export Data**: Save results for further analysis

## Weather Data Sources

The application supports multiple weather data sources:
- **National Weather Service (NWS)**: Official US government weather data
- **OpenMeteo**: Free weather API with global coverage
- **Local Station**: Direct connection to physical weather stations

## Dispersion Modeling

The application implements the Gaussian plume model for dispersion calculations:
- Pasquill-Gifford atmospheric stability classifications (A-F)
- Distance-dependent dispersion coefficients
- Ground-level reflection and plume rise calculations
- Concentration and dosage calculations at receptor points
- Risk assessment based on toxicity thresholds

## Development Guidelines

- Follow MVVM pattern for UI separation
- Use dependency injection for service management
- Implement async/await for all API calls and database operations
- Include comprehensive error handling and logging
- Write unit tests for business logic
- Follow clean architecture principles

## Future Enhancements

- [ ] Advanced 3D visualization with Helix Toolkit
- [ ] Integration with ArcGIS Runtime for professional mapping
- [ ] Support for additional dispersion models (AERMOD, CALPUFF)
- [ ] Machine learning for atmospheric stability prediction
- [ ] Real-time sensor data integration
- [ ] Advanced statistical analysis and uncertainty quantification
- [ ] Mobile application companion
- [ ] Multi-language support

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes following the coding guidelines
4. Add tests for new functionality
5. Submit a pull request

## License

This project is developed for chemical dispersion modeling and emergency response applications.

## Support

For technical support or questions about the dispersion modeling algorithms, please refer to the user guide or contact the development team.