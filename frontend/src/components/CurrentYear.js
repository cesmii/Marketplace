import React from 'react';

const CurrentYear = () => {
  const currentYear = new Date().getFullYear();
  
  return (
    <span className="d-inline">{currentYear}</span>
  );
};

export default CurrentYear;