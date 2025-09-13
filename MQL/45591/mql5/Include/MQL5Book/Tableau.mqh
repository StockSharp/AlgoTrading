//+------------------------------------------------------------------+
//|                                                      Tableau.mqh |
//|                               Copyright (c) 2021-2022, Marketeer |
//|                          https://www.mql5.com/en/users/marketeer |
//| On-chart table with given number of columns and rows             |
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Defines                                                          |
//+------------------------------------------------------------------+
#define TBL_FLAG_ROW_0_HEADER 1      // bold text in top-most row
#define TBL_FLAG_COL_0_HEADER 2      // bold text in left-most column
#define TBL_CELL_HEIGHT_AUTO -1
#define TBL_CELL_WIDTH_AUTO 100      // always auto width!

//+------------------------------------------------------------------+
//| UI table                                                         |
//+------------------------------------------------------------------+
class Tableau
{
private:
   const string prefix;
   const ENUM_BASE_CORNER corner;
   const int nrows, ncols;
   int cellHeight, cellWidth;
   const int gap;
   const int fontSize;
   const int flags;
   const string res;
    
   const string defaultFont;
   const string boldFont;
   const color bgColor;
   const uchar bgTrans;

public:
   static ENUM_ANCHOR_POINT corner2anchor(const ENUM_BASE_CORNER corner)
   {
      return (ENUM_ANCHOR_POINT)(corner * 2);
   };
    
   static bool isBottomSide(const ENUM_BASE_CORNER corner)
   {
      return corner == CORNER_LEFT_LOWER || corner == CORNER_RIGHT_LOWER;
   }

   static bool isRightSide(const ENUM_BASE_CORNER corner)
   {
      return corner == CORNER_RIGHT_UPPER || corner == CORNER_RIGHT_LOWER;
   }
    
   Tableau(const string pref, const int rows, const int cols,
      const int height = TBL_CELL_HEIGHT_AUTO, const int width = TBL_CELL_WIDTH_AUTO,
      const ENUM_BASE_CORNER c = CORNER_RIGHT_LOWER, const int g = 8,
      const int fsize = 8, const string font = "Consolas", const string bold = "Arial Black",
      const int mask = TBL_FLAG_COL_0_HEADER,
      const color bgc = 0x808080, const uchar bgt = 0xC0):
      prefix(pref + (string)rows + "x" + (string)cols), corner(c), nrows(rows), ncols(cols),
      cellHeight(height), cellWidth(width), gap(g),
      fontSize(fabs(fsize)), flags(mask), defaultFont(font), boldFont(bold),
      bgColor(bgc), bgTrans(bgt),
      res("::LLET" + (string)ChartID())
   {
      if(height == TBL_CELL_HEIGHT_AUTO) cellHeight = fontSize * 2;
      
      const ENUM_ANCHOR_POINT anchor = corner2anchor(corner);
      const bool invertY = isBottomSide(corner);
      const bool invertX = isRightSide(corner);

      const int w = ncols * (width + gap);
      const int h = nrows * (cellHeight + gap / 2) + gap / 2;

      uint img[];
      ArrayResize(img, w * h);
      ArrayInitialize(img, (uint)ColorToARGB(bgColor, bgTrans));
      ResourceCreate(res, img, w, h, 0, 0, w, COLOR_FORMAT_ARGB_NORMALIZE);
      
      string name = prefix + "_";
      ObjectCreate(0, name, OBJ_BITMAP_LABEL, 0, 0, 0);
      ObjectSetString(0, name, OBJPROP_BMPFILE, 0, res);
      ObjectSetString(0, name, OBJPROP_BMPFILE, 1, res);
      ObjectSetInteger(0, name, OBJPROP_CORNER, corner);
      ObjectSetInteger(0, name, OBJPROP_ANCHOR, anchor);
      ObjectSetInteger(0, name, OBJPROP_XSIZE, w);
      ObjectSetInteger(0, name, OBJPROP_YSIZE, h);
      ObjectSetInteger(0, name, OBJPROP_XDISTANCE, gap / 2);
      ObjectSetInteger(0, name, OBJPROP_YDISTANCE, gap / 2);
      ObjectSetInteger(0, name, OBJPROP_BGCOLOR, ColorToARGB((color)ChartGetInteger(0, CHART_COLOR_GRID), 192));
      ObjectSetInteger(0, name, OBJPROP_BORDER_TYPE, BORDER_FLAT);

      for(int i = 0; i < nrows; i++)
      {
         int k = invertY ? nrows - i - 1 : i;
        
         for(int j = 0; j < ncols; j++)
         {
            int m = invertX ? ncols - j - 1 : j;
          
            name = prefix + "_" +(string)i + "_" + (string)j;
            ObjectCreate(0, name, OBJ_LABEL, 0, 0, 0);
            ObjectSetString(0, name, OBJPROP_TEXT, "...");
            ObjectSetInteger(0, name, OBJPROP_CORNER, corner);
            ObjectSetInteger(0, name, OBJPROP_ANCHOR, invertY ? ANCHOR_RIGHT_LOWER : ANCHOR_RIGHT_UPPER /*anchor*/);
            ObjectSetInteger(0, name, OBJPROP_XSIZE, width);
            ObjectSetInteger(0, name, OBJPROP_YSIZE, cellHeight);
            ObjectSetString(0, name, OBJPROP_TEXT, name);
            ObjectSetInteger(0, name, OBJPROP_XDISTANCE, m * width + m * gap + gap);
            ObjectSetInteger(0, name, OBJPROP_YDISTANCE, k * (cellHeight + gap / 2) + gap);
            ObjectSetInteger(0, name, OBJPROP_COLOR, ChartGetInteger(0, CHART_COLOR_FOREGROUND));
            ObjectSetString(0, name, OBJPROP_FONT, defaultFont);
            ObjectSetInteger(0, name, OBJPROP_FONTSIZE, fontSize);
          
            if((flags & TBL_FLAG_COL_0_HEADER) != 0 && j == 0)
            {
               //if(!((flags & TBL_FLAG_ROW_0_HEADER) == 0 && i == 0))
               {
                  ObjectSetString(0, name, OBJPROP_FONT, boldFont);
               }
            }
            if((flags & TBL_FLAG_ROW_0_HEADER) != 0 && i == 0)
            {
               //if(!((flags & TBL_FLAG_COL_0_HEADER) == 0 && j == 0))
               {
                  ObjectSetString(0, name, OBJPROP_FONT, boldFont);
               }
            }
         }
      }
   }
    
   ~Tableau()
   {
      ObjectsDeleteAll(0, prefix, 0);
   }
   
   int getRows() const
   {
      return nrows;
   }
   
   int getColumns() const
   {
      return ncols;
   }
    
   bool fill(const string &data[], const string &hint[]) const
   {
      if(!TextSetFont(defaultFont, fontSize * -10))
      {
         Print("TextSetFont failed: ", _LastError);
      }

      const bool invertY = isBottomSide(corner);
      const bool invertX = isRightSide(corner);
      
      const int n = ArraySize(data);
      int cols[];
      int offsets[];
      ArrayResize(cols, ncols);
      ArrayInitialize(cols, 0);
      ArrayResize(offsets, ncols + 1);
      ArrayInitialize(offsets, 0);
      
      for(int i = 0; i < n; i++)
      {
         const int y = i / ncols; // row
         const int x = i % ncols; // column
         int w, h;
         
         if(x < ncols && TextGetSize(data[i], w, h))
         {
            //if((int)(w * 1.5) > cols[x]) cols[x] = (int)(w * 1.5);
            if(w > cols[x]) cols[x] = w;
         }
         else
         {
            Print(__FUNCSIG__, " failed: ", _LastError);
            return false;
         }
      }

      if(invertX)
      {
         ArrayReverse(cols);
      }
      
      int width = 0;
      for(int i = 0; i < ncols; i++)
      {
         offsets[i] = (i > 0 ? offsets[i - 1] + cols[i - 1] : 0);
         width += cols[i] + gap;
      }
      offsets[ncols] = offsets[ncols - 1] + cols[ncols - 1];
      
      if(!invertX)
      {
         for(int k = 0; k < ncols; k++)
         {
            offsets[k] = offsets[k + 1] + gap;
         }
      }

      const int h = ArraySize(hint);
      
      for(int i = 0; i < n; i++)
      {
         const int y = i / ncols;
         const int x = i % ncols;
         if(x < ncols)
         {
            int k = invertY ? nrows - y - 1 : y;
            int m = invertX ? ncols - x - 1 : x;
          
            string name = prefix + "_" +(string)y + "_" + (string)x;
            // NB: setting dimensions for OBJ_LABEL does nothing,
            // cause it's automatically sized to text content
            // this is a problem, but has minor impact
            ObjectSetInteger(0, name, OBJPROP_XSIZE, cols[x]);
            ObjectSetInteger(0, name, OBJPROP_YSIZE, cellHeight);
            ObjectSetString(0, name, OBJPROP_TEXT, data[i]);
            if(i < h && StringLen(hint[i]) > 0)
            {
               ObjectSetString(0, name, OBJPROP_TOOLTIP, hint[i]);
            }
            ObjectSetInteger(0, name, OBJPROP_XDISTANCE, offsets[m] + gap * m + gap);
            ObjectSetInteger(0, name, OBJPROP_YDISTANCE, k * (cellHeight + gap / 2) + gap);
         }
      }

      string name = prefix + "_";
      ObjectSetInteger(0, name, OBJPROP_XSIZE, width + gap);

      const int w = width + gap;
      const int h1 = nrows * (cellHeight + gap / 2) + gap / 2;

      uint img[];
      ArrayResize(img, w * h1);
      ArrayInitialize(img, (uint)ColorToARGB(bgColor, bgTrans));
      ResourceCreate(res, img, w, h1, 0, 0, w, COLOR_FORMAT_ARGB_NORMALIZE);

      ChartRedraw();
      return true;
   }
};
//+------------------------------------------------------------------+
