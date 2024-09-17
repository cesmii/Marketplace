import React from 'react';

const CurrentYear = () => {
  const currentYear = new Date().getFullYear();
  
  return (
    <div>
      <p>The current year is: {currentYear}</p>
    </div>
  );
};

export default CurrentYear;