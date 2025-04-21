import React, { useEffect } from 'react';

/**
 * HubSpotTracking - A component that embeds HubSpot tracking code into a React application
 * 
 * This component handles loading the HubSpot tracking script only once,
 * even if the component is mounted multiple times or remounted.
 * 
 * @returns {null} This component doesn't render anything visible
 */
const HubSpotTracking = () => {
  useEffect(() => {
    // Check if the script is already loaded
    if (!document.getElementById('hs-script-loader')) {
      // Create script element
      const script = document.createElement('script');
      script.id = 'hs-script-loader';
      script.type = 'text/javascript';
      script.async = true;
      script.defer = true;
      script.src = '//js.hs-scripts.com/43818189.js';
      
      // Append to document head
      document.head.appendChild(script);
    }
    
    // No cleanup needed as we want the script to persist
  }, []); // Empty dependency array ensures this runs once on mount

  // Component doesn't render anything visible
  return null;
};

export default HubSpotTracking;