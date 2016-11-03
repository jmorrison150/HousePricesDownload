#region copyright

//###############################################################################
//# $Id$
//#
//# Project:  GDAL2Tiles, Google Summer of Code 2007 & 2008
//#           Global Map Tiles Classes
//# Purpose:  Convert a raster into TMS tiles, create KML SuperOverlay EPSG:4326,
//#			generate a simple HTML viewers based on Google Maps and OpenLayers
//# Author:	Klokan Petr Pridal, klokan at klokan dot cz
//# Web:		http://www.klokan.cz/projects/gdal2tiles/
//#
//###############################################################################
//# Copyright (c) 2008 Klokan Petr Pridal. All rights reserved.
//#
//# Permission is hereby granted, free of charge, to any person obtaining a
//# copy of this software and associated documentation files (the "Software"),
//# to deal in the Software without restriction, including without limitation
//# the rights to use, copy, modify, merge, publish, distribute, sublicense,
//# and/or sell copies of the Software, and to permit persons to whom the
//# Software is furnished to do so, subject to the following conditions:
//#
//# The above copyright notice and this permission notice shall be included
//# in all copies or substantial portions of the Software.
//#
//# THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
//# OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//# FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
//# THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//# LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
//# FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
//# DEALINGS IN THE SOFTWARE.
//###############################################################################

//"""
//globalmaptiles.py

//Global Map Tiles as defined in Tile Map Service (TMS) Profiles
//==============================================================

//Functions necessary for generation of global tiles used on the web.
//It contains classes implementing coordinate conversions for:

//  - GlobalMercator (based on EPSG:900913 = EPSG:3785)
//       for Google Maps, Yahoo Maps, Microsoft Maps compatible tiles
//  - GlobalGeodetic (based on EPSG:4326)
//       for OpenLayers Base Map and Google Earth compatible tiles

//More info at:

//http://wiki.osgeo.org/wiki/Tile_Map_Service_Specification
//http://wiki.osgeo.org/wiki/WMS_Tiling_Client_Recommendation
//http://msdn.microsoft.com/en-us/library/bb259689.aspx
//http://code.google.com/apis/maps/documentation/overlays.html#Google_Maps_Coordinates

//Created by Klokan Petr Pridal on 2008-07-03.
//Google Summer of Code 2008, project GDAL2Tiles for OSGEO.

//In case you use this class in your product, translate it to another language
//or find it usefull for your project please let me know.
//My email: klokan at klokan dot cz.
//I would like to know where it was used.

//Class is available under the open-source GDAL license (www.gdal.org).
#endregion
#region comments
//TMS Global Mercator Profile
//---------------------------

//Functions necessary for generation of tiles in Spherical Mercator projection,
//EPSG:900913 (EPSG:gOOglE, Google Maps Global Mercator), EPSG:3785, OSGEO:41001.

//Such tiles are compatible with Google Maps, Microsoft Virtual Earth, Yahoo Maps,
//UK Ordnance Survey OpenSpace API, ...
//and you can overlay them on top of base maps of those web mapping applications.

//Pixel and tile coordinates are in TMS notation (origin [0,0] in bottom-left).

//What coordinate conversions do we need for TMS Global Mercator tiles::

//     LatLon      <->       Meters      <->     Pixels    <->       Tile     

// WGS84 coordinates   Spherical Mercator  Pixels in pyramid  Tiles in pyramid
//     lat/lon            XY in metres     XY pixels Z zoom      XYZ from TMS 
//    EPSG:4326           EPSG:900913                                         
//     .----.              ---------               --                TMS      
//    /      \     <->     |       |     <->     /----/    <->      Google    
//    \      /             |       |           /--------/          QuadTree   
//     -----               ---------         /------------/                   
//   KML, public         WebMapService         Web Clients      TileMapService

//What is the coordinate extent of Earth in EPSG:900913?

//  [-20037508.342789244, -20037508.342789244, 20037508.342789244, 20037508.342789244]
//  Constant 20037508.342789244 comes from the circumference of the Earth in meters,
//  which is 40 thousand kilometers, the coordinate origin is in the middle of extent.
//  In fact you can calculate the constant as: 2 * math.pi * 6378137 / 2.0
//  $ echo 180 85 | gdaltransform -s_srs EPSG:4326 -t_srs EPSG:900913
//  Polar areas with abs(latitude) bigger then 85.05112878 are clipped off.

//What are zoom level constants (pixels/meter) for pyramid with EPSG:900913?

//  whole region is on top of pyramid (zoom=0) covered by 256x256 pixels tile,
//  every lower zoom level resolution is always divided by two
//  initialResolution = 20037508.342789244 * 2 / 256 = 156543.03392804062

//What is the difference between TMS and Google Maps/QuadTree tile name convention?

//  The tile raster itself is the same (equal extent, projection, pixel size),
//  there is just different identification of the same raster tile.
//  Tiles in TMS are counted from [0,0] in the bottom-left corner, id is XYZ.
//  Google placed the origin [0,0] to the top-left corner, reference is XYZ.
//  Microsoft is referencing tiles by a QuadTree name, defined on the website:
//  http://msdn2.microsoft.com/en-us/library/bb259689.aspx

//The lat/lon coordinates are using WGS84 datum, yeh?

//  Yes, all lat/lon we are mentioning should use WGS84 Geodetic Datum.
//  Well, the web clients like Google Maps are projecting those coordinates by
//  Spherical Mercator, so in fact lat/lon coordinates on sphere are treated as if
//  the were on the WGS84 ellipsoid.

//  From MSDN documentation:
//  To simplify the calculations, we use the spherical form of projection, not
//  the ellipsoidal form. Since the projection is used only for map display,
//  and not for displaying numeric coordinates, we don't need the extra precision
//  of an ellipsoidal projection. The spherical projection causes approximately
//  0.33 percent scale distortion in the Y direction, which is not visually noticable.

//How do I create a raster in EPSG:900913 and convert coordinates with PROJ.4?

//  You can use standard GIS tools like gdalwarp, cs2cs or gdaltransform.
//  All of the tools supports -t_srs 'epsg:900913'.

//  For other GIS programs check the exact definition of the projection:
//  More info at http://spatialreference.org/ref/user/google-projection/
//  The same projection is degined as EPSG:3785. WKT definition is in the official
//  EPSG database.

//  Proj4 Text:
//    +proj=merc +a=6378137 +b=6378137 +lat_ts=0.0 +lon_0=0.0 +x_0=0.0 +y_0=0
//    +k=1.0 +units=m +nadgrids=@null +no_defs

//  Human readable WKT format of EPGS:900913:
//     PROJCS["Google Maps Global Mercator",
//         GEOGCS["WGS 84",
//             DATUM["WGS_1984",
//                 SPHEROID["WGS 84",6378137,298.2572235630016,
//                     AUTHORITY["EPSG","7030"]],
//                 AUTHORITY["EPSG","6326"]],
//             PRIMEM["Greenwich",0],
//             UNIT["degree",0.0174532925199433],
//             AUTHORITY["EPSG","4326"]],
//         PROJECTION["Mercator_1SP"],
//         PARAMETER["central_meridian",0],
//         PARAMETER["scale_factor",1],
//         PARAMETER["false_easting",0],
//         PARAMETER["false_northing",0],
//         UNIT["metre",1,
//             AUTHORITY["EPSG","9001"]]]

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlobalMapTiles {


    public class GlobalMercator {
        int tileSize = 256;
        double initialResolution = 156543.03392804062;
        double originShift = 20037508.342789244;
        //void GlobalMercator() {
        //    tileSize = 256;
        //    initialResolution = 2 * Math.PI * 6378137 / this.tileSize;
        //    originShift = 2 * Math.PI * 6378137 / 2.0;
        //}

        double[] latLngToMeters(double lat, double lng) {
            //"Converts given lat/lon in WGS84 Datum to XY in Spherical Mercator EPSG:900913"
            double mx = lng * this.originShift / 180.0;
            double my = Math.Log(Math.Tan(( 90 + lat ) * Math.PI / 360.0)) / ( Math.PI / 180.0 );
            my = my * this.originShift / 180.0;

            double[] m = new double[2];
            m[0] = mx;
            m[1] = my;
            return m;
        }
        public string latlngToQuadKey(double lat, double lng, int zoom){

            //whole world as pixels
            double sinLatitude = Math.Sin(lat * Math.PI/180.0);
            int pixelX = (int) ( ( ( lng + 180.0 ) / 360.0 ) * 256.0 * Math.Pow(2.0, zoom) );
            int pixelY = (int) ( ( 0.5 - Math.Log(( 1.0 + sinLatitude ) / ( 1.0 - sinLatitude )) / ( 4.0 * Math.PI ) ) * 256.0 * Math.Pow(2.0, zoom) );

            //get whole tiles
            int tileX = (int) ( Math.Ceiling(pixelX / (double) ( this.tileSize )) - 1 );
            int tileY = (int) ( Math.Ceiling(pixelY / (double) ( this.tileSize )) - 1 );

            string quad = tileXYToQuadKey(tileX,tileY,zoom);
            return quad;
            
        }
        public int[] latLngToTile(double lat, double lng, int zoom) {
            double level = zoom;
            double latitude = lat;
            double longitude = lng;
            double sinLatitude = Math.Sin(latitude * Math.PI/180.0);

            int pixelX = (int) ( ( ( longitude + 180.0 ) / 360.0 ) * 256.0 * Math.Pow(2.0, level) );
            int pixelY = (int) ( ( 0.5 - Math.Log(( 1.0 + sinLatitude ) / ( 1.0 - sinLatitude )) / ( 4.0 * Math.PI ) ) * 256.0 * Math.Pow(2.0, level) );

            int tileX = (pixelX / 256);
            int tileY = (pixelY / 256);

            int deltaX = pixelX % 256;
            int deltaY = (pixelY) % 256;

            int[] p = new int[3];
            p[0] = deltaX;
            p[1] = deltaY;
            p[2] = zoom;
            return p;
        
        }
        double[] metersToLatLon(double mx, double my) {
            //"Converts XY point from Spherical Mercator EPSG:900913 to lat/lon in WGS84 Datum"
            double lng = ( mx / this.originShift ) * 180.0;
            double lat = ( my / this.originShift ) * 180.0;
            lat = 180 / Math.PI * ( 2 * Math.Atan(Math.Exp(lat * Math.PI / 180.0)) - Math.PI / 2.0 );

            double[] latLng = new double[2];
            latLng[0] = lat;
            latLng[1] = lng;
            return latLng;


        }
        double[] pixelsToMeters(int px, int py, int zoom) {
            //"Converts pixel coordinates in given zoom level of pyramid to EPSG:900913"

            double res = resolution(zoom);
            double mx = px * res - this.originShift;
            double my = py * res - this.originShift;

            double[] m = new double[2];
            m[0] = mx;
            m[1] = my;
            return m;
        }
        int[] metersToPixels(double mx, double my, int zoom) {
            //"Converts EPSG:900913 to pyramid pixel coordinates in given zoom level"

            double res = resolution(zoom);
            int px = (int) ( ( mx + this.originShift ) / res );
            int py = (int) ( ( my + this.originShift ) / res );

            int[] p = new int[2];
            p[0] = px;
            p[1] = py;
            return p;
        }
        int[] pixelsToTile(int px, int py) {
            //"Returns a tile covering region in given pixel coordinates"

            int tx = (int) ( Math.Ceiling(px / (double) ( this.tileSize )) - 1 );
            int ty = (int) ( Math.Ceiling(py / (double) ( this.tileSize )) - 1 );


            int[] t = new int[2];
            t[0] = tx;
            t[1] = ty;
            return t;
        }
        int[] pixelsToRaster(int px, int py, int zoom) {
            //"Move the origin of pixel coordinates to top-left corner"

            int mapSize = this.tileSize << zoom;
            py = mapSize - py;


            int[] p = new int[2];
            p[0] = px;
            p[1] = py;
            return p;
        }
        int[] metersToTile(double mx, double my, int zoom) {
            //"Returns tile for given mercator coordinates"
            int[] p = metersToPixels(mx, my, zoom);
            return pixelsToTile(p[0], p[1]);
        }
        double[] tileBounds(int tx, int ty, int zoom) {
            //"Returns bounds of the given tile in EPSG:900913 coordinates"
            double[] min, max;
            min = pixelsToMeters(tx*this.tileSize, ty*this.tileSize, zoom);
            max = pixelsToMeters(( tx+1 )*this.tileSize, ( ty+1 )*this.tileSize, zoom);

            double[] bounds = new double[4];
            bounds[0] = min[0];
            bounds[1] = min[1];
            bounds[2] = max[0];
            bounds[3] = max[1];

            return bounds;
        }
        public double[] tileLatLngBounds(int tx, int ty, int zoom) {
            //"Returns bounds of the given tile in latutude/longitude using WGS84 datum"

            int[] tile = googleTile(tx, ty, zoom);
            //int[] tile = new int[3];
            //tile[0] = tx; tile[1] = ty; tile[2] = zoom;


            double[] min, max;
            double[] bounds;
            bounds = tileBounds(tile[0], tile[1], tile[2]);
            min = metersToLatLon(bounds[0], bounds[1]);
            max = metersToLatLon(bounds[2], bounds[3]);

            double[] latLngBounds = new double[4];
            latLngBounds[0] = min[0];
            latLngBounds[1] = max[0];
            latLngBounds[2] = min[1];
            latLngBounds[3] = max[1];

            //Console.WriteLine(tx+","+ty+","+zoom);
            //Console.WriteLine(latLngBounds[0]);
            //Console.WriteLine(latLngBounds[1]);
            //Console.WriteLine(latLngBounds[2]);
            //Console.WriteLine(latLngBounds[3]);
            //Console.WriteLine("---");

            return latLngBounds;
        }
        double resolution(int zoom) {
            //"Resolution (meters/pixel) for given zoom level (measured at Equator)"
            //# return (2 * math.pi * 6378137) / (self.tileSize * 2**zoom)
            return ( this.initialResolution / ( Math.Pow(2, zoom) ) );
        }
        int zoomForPixelSize(double pixelSize) {
            //"Maximal scaledown zoom of the pyramid closest to the pixelSize."

            for(int i = 0; i <30; i++) {
                if(pixelSize > resolution(i)) {
                    if(i==0) {
                        return 0;
                    } else {
                        return i-1;
                    }
                }
            }
            return 30;
        }
        int[] googleTile(int tx, int ty, int zoom) {
            //"Converts TMS tile coordinates to Google Tile coordinates"

            //# coordinate origin is moved from bottom-left to top-left corner of the extent
            int[] t = new int[3];
            t[0] = tx;
            t[1] = (int) ( ( Math.Pow(2, zoom) - 1 ) - ty );
            t[2] = zoom;
            return t;
        }
        public string quadTree(int tx, int ty, int zoom) {
            //"Converts TMS tile coordinates to Microsoft QuadTree"

            string quadKey = "";
            ty = (int) ( ( Math.Pow(2, zoom) - 1 ) - ty );
            for(int i = zoom; i > 0; i--) {
                int digit = 0;
                int mask = 1 << ( i-1 );
                if(( tx & mask ) != 0) {
                    digit += 1;
                }
                if(( ty & mask ) != 0) {
                    digit += 2;
                }
                quadKey += digit.ToString();


            }

            return quadKey;
        }
        /// <summary>
        /// Converts tile XY coordinates into a QuadKey at a specified level of detail.
        /// </summary>
        /// <param name="tileX">Tile X coordinate.</param>
        /// <param name="tileY">Tile Y coordinate.</param>
        /// <param name="levelOfDetail">Level of detail, from 1 (lowest detail)
        /// to 23 (highest detail).</param>
        /// <returns>A string containing the QuadKey.</returns>
        public string tileXYToQuadKey(int tileX, int tileY, int levelOfDetail) {
            StringBuilder quadKey = new StringBuilder();
            int[] tile = googleTile(tileX, tileY, levelOfDetail);
            
            for(int i = tile[2]; i > 0; i--) {
                char digit = '0';
                int mask = 1 << ( i - 1 );
                if(( tile[0] & mask ) != 0) {
                    digit++;
                }
                if(( tile[1] & mask ) != 0) {
                    digit++;
                    digit++;
                }
                quadKey.Append(digit);
            }
            return quadKey.ToString();
        }
        public int[] quadKeyToTileXY(String quadKey) {
            int tileX;
            int tileY;
            int levelOfDetail;

            tileX = tileY = 0;
            levelOfDetail = quadKey.Length;
            for(int i = levelOfDetail; i > 0; i--) {
                int mask = 1 << ( i - 1 );
                switch(quadKey[(levelOfDetail - i)]) {
                    case '0':
                        break;

                    case '1':
                        tileX |= mask;
                        break;

                    case '2':
                        tileY |= mask;
                        break;

                    case '3':
                        tileX |= mask;
                        tileY |= mask;
                        break;

                }
            }

            int[] tileXYZ = googleTile(tileX, tileY, levelOfDetail);
            return tileXYZ;
        }
    }

}




//------------------------------------------------------------------------------
// <copyright company="Microsoft">
//     Copyright (c) 2006-2009 Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.MapPoint
{
    static class TileSystem
    {
        private const double EarthRadius = 6378137;
        private const double MinLatitude = -85.05112878;
        private const double MaxLatitude = 85.05112878;
        private const double MinLongitude = -180;
        private const double MaxLongitude = 180;


        /// <summary>
        /// Clips a number to the specified minimum and maximum values.
        /// </summary>
        /// <param name="n">The number to clip.</param>
        /// <param name="minValue">Minimum allowable value.</param>
        /// <param name="maxValue">Maximum allowable value.</param>
        /// <returns>The clipped value.</returns>
        private static double Clip(double n, double minValue, double maxValue)
        {
            return Math.Min(Math.Max(n, minValue), maxValue);
        }
        
        

        /// <summary>
        /// Determines the map width and height (in pixels) at a specified level
        /// of detail.
        /// </summary>
        /// <param name="levelOfDetail">Level of detail, from 1 (lowest detail)
        /// to 23 (highest detail).</param>
        /// <returns>The map width and height in pixels.</returns>
        public static uint MapSize(int levelOfDetail)
        {
            return (uint) 256 << levelOfDetail;
        }



        /// <summary>
        /// Determines the ground resolution (in meters per pixel) at a specified
        /// latitude and level of detail.
        /// </summary>
        /// <param name="latitude">Latitude (in degrees) at which to measure the
        /// ground resolution.</param>
        /// <param name="levelOfDetail">Level of detail, from 1 (lowest detail)
        /// to 23 (highest detail).</param>
        /// <returns>The ground resolution, in meters per pixel.</returns>
        public static double GroundResolution(double latitude, int levelOfDetail)
        {
            latitude = Clip(latitude, MinLatitude, MaxLatitude);
            return Math.Cos(latitude * Math.PI / 180) * 2 * Math.PI * EarthRadius / MapSize(levelOfDetail);
        }



        /// <summary>
        /// Determines the map scale at a specified latitude, level of detail,
        /// and screen resolution.
        /// </summary>
        /// <param name="latitude">Latitude (in degrees) at which to measure the
        /// map scale.</param>
        /// <param name="levelOfDetail">Level of detail, from 1 (lowest detail)
        /// to 23 (highest detail).</param>
        /// <param name="screenDpi">Resolution of the screen, in dots per inch.</param>
        /// <returns>The map scale, expressed as the denominator N of the ratio 1 : N.</returns>
        public static double MapScale(double latitude, int levelOfDetail, int screenDpi)
        {
            return GroundResolution(latitude, levelOfDetail) * screenDpi / 0.0254;
        }



        /// <summary>
        /// Converts a point from latitude/longitude WGS-84 coordinates (in degrees)
        /// into pixel XY coordinates at a specified level of detail.
        /// </summary>
        /// <param name="latitude">Latitude of the point, in degrees.</param>
        /// <param name="longitude">Longitude of the point, in degrees.</param>
        /// <param name="levelOfDetail">Level of detail, from 1 (lowest detail)
        /// to 23 (highest detail).</param>
        /// <param name="pixelX">Output parameter receiving the X coordinate in pixels.</param>
        /// <param name="pixelY">Output parameter receiving the Y coordinate in pixels.</param>
        public static void LatLongToPixelXY(double latitude, double longitude, int levelOfDetail, out int pixelX, out int pixelY)
        {
            latitude = Clip(latitude, MinLatitude, MaxLatitude);
            longitude = Clip(longitude, MinLongitude, MaxLongitude);

            double x = (longitude + 180) / 360; 
            double sinLatitude = Math.Sin(latitude * Math.PI / 180);
            double y = 0.5 - Math.Log((1 + sinLatitude) / (1 - sinLatitude)) / (4 * Math.PI);

            uint mapSize = MapSize(levelOfDetail);
            pixelX = (int) Clip(x * mapSize + 0.5, 0, mapSize - 1);
            pixelY = (int) Clip(y * mapSize + 0.5, 0, mapSize - 1);
        }



        /// <summary>
        /// Converts a pixel from pixel XY coordinates at a specified level of detail
        /// into latitude/longitude WGS-84 coordinates (in degrees).
        /// </summary>
        /// <param name="pixelX">X coordinate of the point, in pixels.</param>
        /// <param name="pixelY">Y coordinates of the point, in pixels.</param>
        /// <param name="levelOfDetail">Level of detail, from 1 (lowest detail)
        /// to 23 (highest detail).</param>
        /// <param name="latitude">Output parameter receiving the latitude in degrees.</param>
        /// <param name="longitude">Output parameter receiving the longitude in degrees.</param>
        public static void PixelXYToLatLong(int pixelX, int pixelY, int levelOfDetail, out double latitude, out double longitude)
        {
            double mapSize = MapSize(levelOfDetail);
            double x = (Clip(pixelX, 0, mapSize - 1) / mapSize) - 0.5;
            double y = 0.5 - (Clip(pixelY, 0, mapSize - 1) / mapSize);

            latitude = 90 - 360 * Math.Atan(Math.Exp(-y * 2 * Math.PI)) / Math.PI;
            longitude = 360 * x;
        }



        /// <summary>
        /// Converts pixel XY coordinates into tile XY coordinates of the tile containing
        /// the specified pixel.
        /// </summary>
        /// <param name="pixelX">Pixel X coordinate.</param>
        /// <param name="pixelY">Pixel Y coordinate.</param>
        /// <param name="tileX">Output parameter receiving the tile X coordinate.</param>
        /// <param name="tileY">Output parameter receiving the tile Y coordinate.</param>
        public static void PixelXYToTileXY(int pixelX, int pixelY, out int tileX, out int tileY)
        {
            tileX = pixelX / 256;
            tileY = pixelY / 256;
        }



        /// <summary>
        /// Converts tile XY coordinates into pixel XY coordinates of the upper-left pixel
        /// of the specified tile.
        /// </summary>
        /// <param name="tileX">Tile X coordinate.</param>
        /// <param name="tileY">Tile Y coordinate.</param>
        /// <param name="pixelX">Output parameter receiving the pixel X coordinate.</param>
        /// <param name="pixelY">Output parameter receiving the pixel Y coordinate.</param>
        public static void TileXYToPixelXY(int tileX, int tileY, out int pixelX, out int pixelY)
        {
            pixelX = tileX * 256;
            pixelY = tileY * 256;
        }



        /// <summary>
        /// Converts tile XY coordinates into a QuadKey at a specified level of detail.
        /// </summary>
        /// <param name="tileX">Tile X coordinate.</param>
        /// <param name="tileY">Tile Y coordinate.</param>
        /// <param name="levelOfDetail">Level of detail, from 1 (lowest detail)
        /// to 23 (highest detail).</param>
        /// <returns>A string containing the QuadKey.</returns>
        public static string TileXYToQuadKey(int tileX, int tileY, int levelOfDetail)
        {
            StringBuilder quadKey = new StringBuilder();
            for (int i = levelOfDetail; i > 0; i--)
            {
                char digit = '0';
                int mask = 1 << (i - 1);
                if ((tileX & mask) != 0)
                {
                    digit++;
                }
                if ((tileY & mask) != 0)
                {
                    digit++;
                    digit++;
                }
                quadKey.Append(digit);
            }
            return quadKey.ToString();
        }



        /// <summary>
        /// Converts a QuadKey into tile XY coordinates.
        /// </summary>
        /// <param name="quadKey">QuadKey of the tile.</param>
        /// <param name="tileX">Output parameter receiving the tile X coordinate.</param>


        /// <param name="tileY">Output parameter receiving the tile Y coordinate.</param>
        /// <param name="levelOfDetail">Output parameter receiving the level of detail.</param>
        public static void QuadKeyToTileXY(string quadKey, out int tileX, out int tileY, out int levelOfDetail)
        {
            tileX = tileY = 0;
            levelOfDetail = quadKey.Length;
            for (int i = levelOfDetail; i > 0; i--)
            {
                int mask = 1 << (i - 1);
                switch (quadKey[levelOfDetail - i])
                {
                    case '0':
                        break;

                    case '1':
                        tileX |= mask;
                        break;

                    case '2':
                        tileY |= mask;
                        break;

                    case '3':
                        tileX |= mask;
                        tileY |= mask;
                        break;

                    default:
                        throw new ArgumentException("Invalid QuadKey digit sequence.");
                }
            }
        }
    }
}
