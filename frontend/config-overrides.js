// config-overrides.js
const webpack = require('webpack');

module.exports = {
  webpack: (config) => {
    config.resolve.fallback = {
      fs: false,
      path: require.resolve('path-browserify'),
      stream: require.resolve('stream-browserify'),
      process: require.resolve('process/browser'), // Retain this line if process is needed
    };

    return config;
  },
};