#!/bin/bash

echo "================================="
echo "Chemical Dispersion Modeling Setup"
echo "================================="
echo

echo "Installing dependencies..."
npm install
if [ $? -ne 0 ]; then
    echo "Error: Failed to install dependencies"
    exit 1
fi

echo
echo "Building application..."
npm run build
if [ $? -ne 0 ]; then
    echo "Error: Failed to build application"
    exit 1
fi

echo
echo "================================="
echo "Setup completed successfully!"
echo "================================="
echo
echo "Available commands:"
echo "  npm run dev    - Development mode with hot reload"
echo "  npm start      - Run the built application"
echo "  npm run dist   - Create distribution packages"
echo
echo "Starting development mode..."
npm run dev