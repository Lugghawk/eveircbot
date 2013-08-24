CREATE TABLE dbo.mapRegions
(
  regionID    int,
  regionName  nvarchar(100)  COLLATE Latin1_General_CI_AI,
  x           float,
  y           float,
  z           float,
  xMin        float,
  xMax        float,
  yMin        float,
  yMax        float,
  zMin        float,
  zMax        float,
  factionID   int,
  radius      float,
)
GO
CREATE TABLE dbo.mapConstellations
(
  regionID             int,
  constellationID      int,
  constellationName    nvarchar(100)  COLLATE Latin1_General_CI_AI,
  x                    float,
  y                    float,
  z                    float,
  xMin                 float,
  xMax                 float,
  yMin                 float,
  yMax                 float,
  zMin                 float,
  zMax                 float,
  factionID            int,
  radius               float,
)
GO
CREATE TABLE dbo.mapSolarSystems
(
  regionID             int,
  constellationID      int,
  solarSystemID        int,
  solarSystemName      nvarchar(100)  COLLATE Latin1_General_CI_AI,
  x                    float,
  y                    float,
  z                    float,
  xMin                 float,
  xMax                 float,
  yMin                 float,
  yMax                 float,
  zMin                 float,
  zMax                 float,
  luminosity           float,
  --
  border               bit,
  fringe               bit,
  corridor             bit,
  hub                  bit,
  international        bit,
  regional             bit,
  constellation        bit,
  security             float,
  factionID            int,
  radius               float,
  sunTypeID            int,
  securityClass        varchar(2)
)