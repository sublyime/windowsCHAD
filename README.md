# Chemical Dispersion Modeling Application

A comprehensive **cross-platform desktop application** for modeling chemical dispersions in urban areas using fluid dynamics and real-time physics simulation. Built with modern web technologies for enhanced portability and maintainability.

## 🎯 Features

- **Real-time Weather Integration**: Connects to National Weather Service API and OpenMeteo for live weather data
- **SQLite Database**: Lightweight, embedded database for chemicals, weather data, releases, and results
- **Interactive Mapping**: Leaflet-based mapping with click-to-select release points and visual feedback
- **Chemical Database**: Comprehensive chemical properties and toxicity information
- **Atmospheric Physics**: Gaussian plume model with Pasquill-Gifford stability calculations
- **Receptor Analysis**: Downwind impact assessment with risk classification
- **3D Terrain Support**: Integration with topographical and building data visualization
- **Real-time Updates**: Automatic refresh with configurable intervals
- **Cross-platform**: Runs on Windows, macOS, and Linux

## 🛠️ Technology Stack

- **Framework**: Electron + TypeScript for cross-platform desktop deployment
- **Frontend**: React + Redux Toolkit for modern UI state management
- **Database**: SQLite with Node.js integration for lightweight data storage
- **Mapping**: Leaflet with React-Leaflet for interactive geographical visualization
- **Physics Engine**: TypeScript implementation of atmospheric dispersion models
- **Weather APIs**: National Weather Service API, OpenMeteo API
- **Build System**: Vite for fast development and optimized production builds

## 🚀 Getting Started

### Prerequisites

1. **Node.js** (v16 or later)
2. **npm** or **yarn**
3. **Git** for version control

### Installation

1. **Clone the repository**:
   ```bash
   git clone https://github.com/your-username/windowsCHAD.git
   cd windowsCHAD/chemical-dispersion-electron
   ```

2. **Install dependencies**:
   ```bash
   npm install
   ```

3. **Development mode**:
   ```bash
   npm run dev
   ```

4. **Build for production**:
   ```bash
   npm run build
   ```

5. **Package executable**:
   ```bash
   npm run dist        # All platforms
   npm run dist:win    # Windows only
   npm run dist:mac    # macOS only
   npm run dist:linux  # Linux only
   ```

## 📁 Project Structure

```
chemical-dispersion-electron/
├── src/
│   ├── main/                      # Electron main process
│   │   ├── main.ts               # Application entry point
│   │   ├── preload.ts            # Secure API bridge
│   │   └── services/             # Backend services
│   │       ├── WeatherService.ts
│   │       ├── DispersionModelingService.ts
│   │       └── DatabaseService.ts
│   ├── renderer/                  # React frontend
│   │   ├── App.tsx               # Main application component
│   │   ├── store/                # Redux state management
│   │   └── components/           # UI components
│   └── shared/                    # Shared TypeScript types
│       ├── types.ts              # Domain models and interfaces
│       └── physics/              # Atmospheric physics calculations
├── dist/                          # Compiled output
├── package.json                   # Dependencies and scripts
└── README.md                      # This file
```

## 🧪 Core Components

### Domain Models (TypeScript)
- **Chemical**: Chemical properties and characteristics
- **WeatherData**: Meteorological observations  
- **Release**: Chemical release event configuration
- **Receptor**: Downwind monitoring points
- **DispersionResult**: Model calculation results
- **TerrainData**: Topographical and building information

### Services (Node.js Backend)
- **WeatherService**: Weather data acquisition from multiple APIs
- **DispersionModelingService**: Gaussian plume dispersion calculations
- **DatabaseService**: SQLite database operations with async support

### User Interface (React Components)
- **Interactive Map**: Leaflet-powered map with click-to-select functionality
- **Release Configuration**: Chemical selection and release parameters
- **Weather Dashboard**: Real-time meteorological conditions
- **Results Visualization**: Concentration contours and receptor data
- **Status Monitor**: System health and connectivity indicators

## 🌍 Cross-Platform Deployment

The application packages into native executables for all major platforms:

- **Windows**: `.exe` installer with auto-updater support
- **macOS**: `.dmg` disk image for drag-and-drop installation  
- **Linux**: `.AppImage` for universal Linux distribution compatibility

## 🔬 Atmospheric Physics

Advanced implementation of dispersion modeling:

- **Gaussian Plume Model**: Industry-standard atmospheric dispersion calculations
- **Pasquill-Gifford Stability**: Automatic classification (A-F) based on weather conditions
- **Real-time Calculations**: Dynamic coefficient updates with meteorological changes
- **Concentration Mapping**: Grid-based concentration field generation
- **Risk Assessment**: Toxicity threshold analysis with safety classifications

## 🌤️ Weather Integration

Multi-source weather data acquisition:

- **National Weather Service (NWS)**: Official US government meteorological data
- **OpenMeteo**: Free global weather API with historical data support
- **Automatic Fallback**: Seamless switching between data sources for reliability
- **Caching**: Intelligent data caching to minimize API calls

## 🗂️ Database Schema

SQLite tables for efficient data management:

```sql
-- Chemicals with physical and toxicity properties
CREATE TABLE chemicals (
  id INTEGER PRIMARY KEY,
  name TEXT NOT NULL,
  cas_number TEXT,
  molecular_weight REAL,
  toxicity_threshold REAL,
  -- ... additional properties
);

-- Weather observations with spatial-temporal indexing  
CREATE TABLE weather_data (
  id INTEGER PRIMARY KEY,
  latitude REAL,
  longitude REAL,
  timestamp DATETIME,
  temperature REAL,
  wind_speed REAL,
  wind_direction REAL,
  -- ... meteorological parameters
);

-- Release scenarios and model results
CREATE TABLE releases (
  id INTEGER PRIMARY KEY,
  chemical_id INTEGER,
  latitude REAL,
  longitude REAL,
  release_rate REAL,
  duration REAL,
  -- ... release parameters
);
```

## 🧑‍💻 Development

### Available Scripts

- `npm run dev` - Start development server with hot reload
- `npm run build` - Build for production
- `npm run build:main` - Build Electron main process only
- `npm run build:renderer` - Build React frontend only
- `npm start` - Start built application
- `npm run dist` - Package executables for distribution

### Architecture Patterns

- **MVVM**: Model-View-ViewModel pattern with Redux state management
- **Clean Architecture**: Separation of concerns between UI, business logic, and data
- **Async/Await**: Promise-based asynchronous programming throughout
- **Type Safety**: Comprehensive TypeScript coverage for maintainability

## 🔮 Future Enhancements

- [ ] **Advanced 3D Visualization**: Three.js integration for immersive plume visualization
- [ ] **Additional Models**: AERMOD, CALPUFF, and CFD model integration
- [ ] **Machine Learning**: AI-powered atmospheric stability prediction
- [ ] **Real-time Sensors**: IoT weather station and chemical sensor integration
- [ ] **Cloud Deployment**: Web-based version with distributed computing
- [ ] **Mobile Companion**: React Native mobile application
- [ ] **API Framework**: RESTful API for third-party integrations

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/amazing-feature`
3. Commit changes: `git commit -m 'Add amazing feature'`
4. Push to branch: `git push origin feature/amazing-feature`  
5. Open a pull request

## 📄 License

This project is developed for chemical dispersion modeling and emergency response applications.

## 🆘 Support

For technical support, dispersion modeling questions, or feature requests:
- Open an issue on GitHub
- Consult the [User Guide](docs/user-guide.md)
- Contact the development team

---

**Built with ❤️ using modern web technologies for scientific computing**