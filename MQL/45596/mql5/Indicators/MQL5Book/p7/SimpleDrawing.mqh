//+------------------------------------------------------------------+
//|                                                SimpleDrawing.mqh |
//|                              Copyright (c) 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#include "ShapesDrawing.mqh"
#include <MQL5Book/ColorMix.mqh>

namespace DRAW
{

enum ORIENTATION
{
   VERTICAL,
   HORIZONTAL
};

const ENUM_ANCHOR_POINT Anchors[] =
{
   ANCHOR_LEFT_UPPER,
   ANCHOR_LEFT_LOWER,
   ANCHOR_RIGHT_LOWER,
   ANCHOR_RIGHT_UPPER
};

//+------------------------------------------------------------------+
//| Simple Graphic Editor - place basic shapes on bitmap resource    |
//+------------------------------------------------------------------+
class SimpleDrawing
{
   const string prefix;           // common prefix for objects
   const int gap;                 // space between buttons
   const int size;                // toolbar and pallette size
   const ENUM_BASE_CORNER corner; // dock corner
   const ORIENTATION orientation; // dock side

   AutoPtr<MyDrawing> raster;     // shape drawing manager
   int toolCount;                 // number of shape classes (buttons)
   int shapeType;                 // selected shape type
   color activeColor;             // selectec color to draw
   color bgColor;                 // inative button background color
   
   const static string palette;
   const static string canvas;
   
   class ToolButton
   {
      const string name;
      const string icon;
      const uint offset;
      uint data[];
      uint w, h;
      const SimpleDrawing *owner;
   public:
      ToolButton(const SimpleDrawing *ptr, const string n, const string text,
         uint &array[], const uint width, const uint height, const int position):
         owner(ptr), name(n), icon(text), w(width), h(height), offset(position)
      {
         ArraySwap(data, array);
         
         ObjectCreate(0, name, OBJ_BITMAP_LABEL, 0, 0, 0);
         
         const string res = "::" + name + (string)ChartID();
         
         ArrayInitialize(data, ColorToARGB(owner.getColor()));
         TextOut(icon, w, h, owner.getOrientation() ? TA_RIGHT | TA_BOTTOM : TA_LEFT | TA_BOTTOM,
            data, w, h, ColorToARGB(owner.getBgColor()), COLOR_FORMAT_ARGB_RAW);
         ResourceCreate(res + "on", data, w, h, 0, 0, w, COLOR_FORMAT_ARGB_RAW);
      
         ArrayInitialize(data, ColorToARGB(owner.getBgColor()));
         TextOut(icon, w, h, owner.getOrientation() ? TA_RIGHT | TA_BOTTOM : TA_LEFT | TA_BOTTOM,
            data, w, h, ColorToARGB(owner.getColor()), COLOR_FORMAT_ARGB_RAW);
         ResourceCreate(res + "off", data, w, h, 0, 0, w, COLOR_FORMAT_ARGB_RAW);
         
         ObjectSetString(0, name, OBJPROP_BMPFILE, 0, res + "on");
         ObjectSetString(0, name, OBJPROP_BMPFILE, 1, res + "off");
         ObjectSetInteger(0, name, OBJPROP_XDISTANCE, owner.getSize() + offset * owner.getOrientation());
         ObjectSetInteger(0, name, OBJPROP_YDISTANCE, owner.getSize() + offset * !owner.getOrientation());
         ObjectSetInteger(0, name, OBJPROP_XSIZE, w);
         ObjectSetInteger(0, name, OBJPROP_YSIZE, h);
         ObjectSetInteger(0, name, OBJPROP_CORNER, owner.getCorner());
         ObjectSetInteger(0, name, OBJPROP_ANCHOR, Anchors[(int)owner.getCorner()]);
      }
      
      void colorize(const color clr)
      {
         ArrayInitialize(data, ColorToARGB(clr));
         TextOut(icon, w, h, owner.getOrientation() ? TA_RIGHT | TA_BOTTOM : TA_LEFT | TA_BOTTOM,
            data, w, h, ColorToARGB(clrBlack), COLOR_FORMAT_ARGB_RAW);
         ResourceCreate(name  + (string)ChartID() + "on", data, w, h, 0, 0, w, COLOR_FORMAT_ARGB_RAW);
      }
   };
   
   AutoPtr<ToolButton> buttons[];
   
   void onCanvasClick(int x, int y, const bool drag = false, const int flags = 0)
   {
      if(shapeType != -1 && !drag)
      {
         if(Anchors[(int)corner] == ANCHOR_LEFT_UPPER || Anchors[(int)corner] == ANCHOR_RIGHT_UPPER)
         {
            y -= 2 * size;
         }
         if(Anchors[(int)corner] == ANCHOR_LEFT_LOWER || Anchors[(int)corner] == ANCHOR_LEFT_UPPER)
         {
            x -= 2 * size;
         }
         ObjectSetInteger(0, prefix + (string)shapeType, OBJPROP_STATE, false);
         
         const int w = (int)ChartGetInteger(0, CHART_WIDTH_IN_PIXELS);
         const int h = (int)ChartGetInteger(0, CHART_HEIGHT_IN_PIXELS);
         
         // TODO: should request shape class for default params
         const int parameters[] = {50, 100, 150};
         raster[].push(Shape::Registrator::get(shapeType).create(x, y, activeColor, parameters));
         raster[].draw();
         shapeType = -1;
      }
      else if(drag)
      {
         static int prevx = -1, prevy = -1;
         static Shape *prevp = NULL;
         Shape *p = raster[].findAt(x, y, prevp);
         if(p != prevp)
         {
            prevx = -1;
            prevy = -1;
         }
         
         if(p != NULL)
         {
            if(prevx != -1 && prevy != -1)
            {
               if((flags & 8) != 0)
               {
                  p.resize(x - prevx, y - prevy);
               }
               else
               {
                  p.move(x - prevx, y - prevy);
               }
               raster[].draw(true);
            }
            prevx = x;
            prevy = y;
            prevp = p;
         }
      }
   }

   void selectColor(const int p, const int total)
   {
      activeColor = ColorMix::HSVtoRGB(p * 255.0 / total);
      if(shapeType != -1)
      {
         for(int i = 0; i < ArraySize(buttons); ++i)
         {
            buttons[i][].colorize(activeColor);
         }
         ChartRedraw();
      }
   }
   
   void selectTool(const int index)
   {
      shapeType = index;
   }
   
   void onButtonClick(int x, int y, const string &name)
   {
      ResetLastError();
      const int tool = (int)StringToInteger(StringSubstr(name, StringLen(prefix)));
      if(_LastError == ERR_WRONG_STRING_PARAMETER)
      {
         const int total = orientation ? (int)ChartGetInteger(0, CHART_WIDTH_IN_PIXELS) : (int)ChartGetInteger(0, CHART_HEIGHT_IN_PIXELS);
         const int p = orientation ? x : y;
         selectColor(p, total);
      }
      else
      {
         const bool state = (bool)ObjectGetInteger(0, name, OBJPROP_STATE);
         // NB: sometimes a button is not pressed at 1-st attempt
         for(int i = 0; i < toolCount; ++i)
         {
            if(i == tool)
            {
               selectTool(state ? tool : -1);
               continue;
            }
            ObjectSetInteger(0, prefix + (string)i, OBJPROP_STATE, false);
         }
      }
      ChartRedraw();
   }

   uint createToolButton(const int i, const string icon, const uint offset)
   {
      uint w, h;
      TextGetSize(icon, h, w);
      if(orientation)
      {
         uint t = w;
         w = h;
         h = t;
      }
      
      const string name = prefix + (string)i;
      uint data[];
      ArrayResize(data, w * h);

      PUSH(buttons, new ToolButton(&this, name, icon, data, w, h, offset));
      
      return (orientation ? w : h) + gap;
   }
   
   void createToolbox()
   {
      TextSetFont("Consolas", -120, 0, !orientation * 900);
      toolCount = Shape::Registrator::getTypeCount();
      
      uint offset = 0;
      for(int i = 0; i < toolCount; ++i)
      {
         offset += createToolButton(i, Shape::Registrator::get(i).type, offset);
      }
   }
   
   void createPaletteResource(const int w, const int h)
   {
      uint data[];
      const int width = orientation ? w : size;
      const int height = orientation ? size : h;
      ArrayResize(data, width * height);
      ArrayInitialize(data, 0);
      for(int i = 0; i < fmax(width, height); ++i)
      {
         const color clr = ColorMix::HSVtoRGB(i * 255.0 / fmax(width, height));
      
         for(int j = 0; j < size; ++j)
         {
            data[orientation ? width * j + i : size * i + j] = ColorToARGB(clr);
         }
      }
      ResourceCreate("::" + palette + (string)ChartID(), data, width, height, 0, 0, width, COLOR_FORMAT_ARGB_NORMALIZE);
   }
   
   void createPalette(const int w, const int h)
   {
      const string name = prefix + palette;
      ObjectCreate(0, name, OBJ_BITMAP_LABEL, 0, 0, 0);
      
      ObjectSetInteger(0, name, OBJPROP_XDISTANCE, 0);
      ObjectSetInteger(0, name, OBJPROP_YDISTANCE, 0);
      ObjectSetInteger(0, name, OBJPROP_XSIZE, orientation ? w : size);
      ObjectSetInteger(0, name, OBJPROP_YSIZE, orientation ? size : h);
      ObjectSetInteger(0, name, OBJPROP_CORNER, corner);
      ObjectSetInteger(0, name, OBJPROP_ANCHOR, Anchors[(int)corner]);
      createPaletteResource(w, h);
      ObjectSetString(0, name, OBJPROP_BMPFILE, "::" + palette + (string)ChartID());
   }
   
   void OnResizePalette(const int w, const int h)
   {
      const string name = prefix + palette;
      ObjectSetInteger(0, name, OBJPROP_XSIZE, orientation ? w : size);
      ObjectSetInteger(0, name, OBJPROP_YSIZE, orientation ? size : h);
      createPaletteResource(w, h);
      ChartRedraw();
   }
   
   void OnResizeCanvas(const int w, const int h)
   {
      createCanvas(w, h, true);
   }
   
   void createCanvas(const int w, const int h, const bool resize = false)
   {
      const string name = prefix + canvas;
      const color backgroundColor = 0x80808080;
      const int width = orientation ? w : w - 2 * size;
      const int height = orientation ? h - 2 * size : h;

      if(!resize)
      {
         ObjectCreate(0, name, OBJ_BITMAP_LABEL, 0, 0, 0);
         ObjectSetInteger(0, name, OBJPROP_XDISTANCE, orientation ? 0 : 2 * size);
         ObjectSetInteger(0, name, OBJPROP_YDISTANCE, !orientation ? 0 : 2 * size);
         ObjectSetInteger(0, name, OBJPROP_CORNER, corner);
         ObjectSetInteger(0, name, OBJPROP_ANCHOR, Anchors[(int)corner]);
      }
      
      ObjectSetInteger(0, name, OBJPROP_XSIZE, width);
      ObjectSetInteger(0, name, OBJPROP_YSIZE, height);
      
      if(!resize)
      {
         uint data[];
         ArrayResize(data, width * height);
         ArrayInitialize(data, backgroundColor);
         const string res = "::" + canvas + (string)ChartID();
         ResourceCreate(res, data, width, height, 0, 0, width, COLOR_FORMAT_ARGB_NORMALIZE);
         ObjectSetString(0, name, OBJPROP_BMPFILE, res);
         raster = new MyDrawing(res, data, width, height, backgroundColor);
      }
      else if(raster[] != NULL)
      {
         raster[].resize(width, height);
         raster[].draw(true);
      }
   }

public:
   SimpleDrawing(const string _prefix, const int _size, ENUM_BASE_CORNER _corner,
      ORIENTATION direction, const int _gap = 3): prefix(_prefix),
      toolCount(0), shapeType(-1), size(_size), corner(_corner),
      orientation(direction), gap(_gap)
   {
      const int w = (int)ChartGetInteger(0, CHART_WIDTH_IN_PIXELS);
      const int h = (int)ChartGetInteger(0, CHART_HEIGHT_IN_PIXELS);
      const color bg = (color)ChartGetInteger(0, CHART_COLOR_BACKGROUND);
      // detect bg color of the chart to select button colors
      const bool light = ((bg & 0xFF) + (bg >> 8 & 0xFF) + (bg >> 16 & 0xFF)) / 3 > 0x80;
      bgColor = light ? clrBlack : clrWhite; // all deselected
      activeColor = light ? clrWhite : clrBlack;   // one selected

      createToolbox();
      createPalette(w, h);
      createCanvas(w, h);
   }
   
   ~SimpleDrawing()
   {
      ObjectsDeleteAll(0, prefix);   
   }
   
   color getColor() const
   {
      return activeColor;
   }
   
   color getBgColor() const
   {
      return bgColor;
   }
   
   int getSize() const
   {
      return size;
   }
   
   ENUM_BASE_CORNER getCorner() const
   {
      return corner;
   }
   
   ORIENTATION getOrientation() const
   {
      return orientation;
   }
   
   bool onObjectClick(const int id, const long &lparam, const double &dparam, const string &sparam)
   {
      if(StringFind(sparam, prefix) == 0)
      {
         if(StringFind(sparam, "canvas") > 0)
         {
            onCanvasClick((int)lparam, (int)dparam);
         }
         else
         {
            onButtonClick((int)lparam, (int)dparam, sparam);
         }
      }
      return true; // processed
   }
   
   bool onMouseMove(const int id, const long &lparam, const double &dparam, const string &sparam)
   {
      const int flags = (int)sparam;
      if((flags & 1) != 0)
      {
         onCanvasClick((int)lparam, (int)dparam, true, flags);
      }
      return true;
   }

   bool onChartChange()
   {   
      const int w = (int)ChartGetInteger(0, CHART_WIDTH_IN_PIXELS);
      const int h = (int)ChartGetInteger(0, CHART_HEIGHT_IN_PIXELS);
      OnResizePalette(w, h);
      OnResizeCanvas(w, h);
      return true;
   }
};

const static string SimpleDrawing::palette = "palette";
const static string SimpleDrawing::canvas = "canvas";

}
