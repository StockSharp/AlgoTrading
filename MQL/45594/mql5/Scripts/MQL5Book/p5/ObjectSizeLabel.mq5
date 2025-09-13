//+------------------------------------------------------------------+
//|                                              ObjectSizeLabel.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//|                                                                  |
//| The script slowly moves a label around the chart and             |
//| changes anchor point on the label.                               |
//| Label dimensions are taking into account.                        |
//+------------------------------------------------------------------+
#property script_show_inputs

input ENUM_BASE_CORNER Corner = CORNER_LEFT_UPPER;

//+------------------------------------------------------------------+
//| Helper function to concatenate strings from given array          |
//+------------------------------------------------------------------+
string StringImplode(const string &lines[], const string glue,
   const int start = 0, const int stop = -1)
{
   const int n = stop == -1 ? ArraySize(lines) : fmin(stop, ArraySize(lines));
   string result = "";
   for(int i = start; i < n; i++)
   {
      result += (i > start ? glue : "") + lines[i];
   }
   return result;
}

//+------------------------------------------------------------------+
//| Neat stringifier function for enumeration element                |
//+------------------------------------------------------------------+
template<typename E>
string GetEnumString(const E e)
{
   string words[];
   StringSplit(EnumToString(e), '_', words);
   for(int i = 0; i < ArraySize(words); ++i) StringToLower(words[i]);
   return StringImplode(words, " ", 1);
}

//+------------------------------------------------------------------+
//| Coordinates of bounding rectangle relative to anchor point       |
//+------------------------------------------------------------------+
struct Margins
{
   int nearX;  // padding between label sides and window edges
   int nearY;  //    adjacent to the selected corner
   int farX;   // padding between label sides and window edges
   int farY;   //    opposite to the selected corner
};
  
//+------------------------------------------------------------------+
//| Get bounding rectangle around anchor point according to axes     |
//| directions specific to selected corner.                          |
//| For example X may increase from left to right, or vice versa,    |
//| and Y may run from up to down or in reverse.                     |
//+------------------------------------------------------------------+
Margins GetMargins(const ENUM_BASE_CORNER corner, const ENUM_ANCHOR_POINT anchor,
   int dx, int dy)
{
   Margins margins = {}; // zero margins by default

   #define LEFT 0x1
   #define LOWER 0x2
   #define RIGHT 0x4
   #define UPPER 0x8
   #define CENTER 0x16
   
   const int corner_flags[] = // flags of ENUM_BASE_CORNER elements
   {
      LEFT | UPPER,
      LEFT | LOWER,
      RIGHT | LOWER,
      RIGHT | UPPER
   };

   const int anchor_flags[] = // flags of ENUM_ANCHOR_POINT elements
   {
      LEFT | UPPER,
      LEFT,
      LEFT | LOWER,
      LOWER,
      RIGHT | LOWER,
      RIGHT,
      RIGHT | UPPER,
      UPPER,
      CENTER
   };

   if(anchor == ANCHOR_CENTER)
   {
      margins.nearX = margins.farX = dx / 2;
      margins.nearY = margins.farY = dy / 2;
   }
   else
   {
      const int mask = corner_flags[corner] & anchor_flags[anchor];
      
      if((mask & (LEFT | RIGHT)) != 0) // both corner and anchor are same-side
      {
         margins.farX = dx;
      }
      else // corner is left/right-side but anchor is right/left, or vice versa
      {
         if((anchor_flags[anchor] & (LEFT | RIGHT)) == 0)
         {
            margins.nearX = dx / 2;
            margins.farX = dx / 2;
         }
         else
         {
            margins.nearX = dx;
         }
      }

      if((mask & (UPPER | LOWER)) != 0) // both corner and anchor are same-side
      {
         margins.farY = dy;
      }
      else // corner is up/down-side but anchor is down/up, or vice versa
      {
         if((anchor_flags[anchor] & (UPPER | LOWER)) == 0)
         {
            margins.farY = dy / 2;
            margins.nearY = dy / 2;
         }
         else
         {
            margins.nearY = dy;
         }
      }
   }

   #undef LEFT
   #undef LOWER
   #undef RIGHT
   #undef UPPER
   #undef CENTER
   
   return margins;
}

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   // characteristic colors for different anchor points
   const color colors[] =
   {
      clrRed,
      clrMagenta,
      clrBlue,
      clrLimeGreen,
      clrOrange,
      clrDarkTurquoise,
      clrSienna,
      clrLightSalmon,
      clrGray,
   };
   
   // init
   const int t = ChartWindowOnDropped();
   Comment(EnumToString(Corner));

   const string name = "ObjSizeLabel";
   int h = (int)ChartGetInteger(0, CHART_HEIGHT_IN_PIXELS, t) - 1;
   int w = (int)ChartGetInteger(0, CHART_WIDTH_IN_PIXELS) - 1;
   int x = w / 2;
   int y = h / 2;
   
   // create and setup the label
   ObjectCreate(0, name, OBJ_LABEL, t, 0, 0);
   ObjectSetInteger(0, name, OBJPROP_SELECTABLE, true);
   ObjectSetInteger(0, name, OBJPROP_SELECTED, true);
   ObjectSetInteger(0, name, OBJPROP_CORNER, Corner);
   
   // choose monospace font to get stable length of string
   // while changing coordinates
   ObjectSetString(0, name, OBJPROP_FONT, "Consolas");
   
   // variables   
   int px = 5, py = 5;           // diagonal movement
   int dx = 0, dy = 0;           // label size will be stored here
   int pass = 0;                 // animation cycles
   ENUM_ANCHOR_POINT anchor = 0; // random anchor point
   Margins m = {}; // will hold indents from window borders respecting label size
   
   const int key = TerminalInfoInteger(TERMINAL_KEYSTATE_SCRLOCK);

   for( ;!IsStopped(); ++pass)
   {
      if(TerminalInfoInteger(TERMINAL_KEYSTATE_SCRLOCK) != key)
      {
         Sleep(1000);
         continue;
      }
      
      // once in a while change anchor point
      if(pass % 75 == 0)
      {
         // ENUM_ANCHOR_POINT consists of 9 elements: get a random one
         const int r = rand() * 8 / 32768 + 1;
         anchor = (ENUM_ANCHOR_POINT)((anchor + r) % 9);
         ObjectSetInteger(0, name, OBJPROP_ANCHOR, anchor);
         ObjectSetInteger(0, name, OBJPROP_COLOR, colors[anchor]);
         const string message = " " + GetEnumString(anchor)
            + StringFormat("[%3d,%3d] ", x, y);
         ResetLastError();
         ObjectSetString(0, name, OBJPROP_TEXT, message);
         // wait for the changes to take effect
         do
         {
            ChartRedraw();
            Sleep(1);
         }
         while (ObjectGetString(0, name, OBJPROP_TEXT) != message
         && !IsStopped());
         
         // This is not always working as expected, that is
         // OBJPROP_TEXT is changed but OBJPROP_XSIZE/OBJPROP_YSIZE
         // are still 0,0 or something smaller than should be,
         // according to what we get in the next line
         dx = (int)ObjectGetInteger(0, name, OBJPROP_XSIZE);
         dy = (int)ObjectGetInteger(0, name, OBJPROP_YSIZE);
         // This is why we read OBJPROP_XSIZE/OBJPROP_YSIZE
         // once again at the end of the loop

         m = GetMargins(Corner, anchor, dx, dy);
         
         // keep label in current point during anchor change
         x -= px;
         y -= py;
      }

      // bouncing from window endges, prevent clipping by x
      if(x + px >= w - m.farX)
      {
         x = w - m.farX + px - 1;
         px = -px;
      }
      else if(x + px < m.nearX)
      {
         x = m.nearX + px;
         px = -px;
      }
      
      // bouncing from window endges, prevent clipping by y
      if(y + py >= h - m.farY)
      {
         y = h - m.farY + py - 1;
         py = -py;
      }
      else if(y + py < m.nearY)
      {
         y = m.nearY + py;
         py = -py;
      }

      // calculate label's new position
      x += px;
      y += py;
      
      // update the label content
      ObjectSetString(0, name, OBJPROP_TEXT, " " + GetEnumString(anchor)
         + StringFormat("[%3d,%3d] ", x, y));

      // update the label position
      ObjectSetInteger(0, name, OBJPROP_XDISTANCE, x);
      ObjectSetInteger(0, name, OBJPROP_YDISTANCE, y);

      // animation timeout
      ChartRedraw();
      Sleep(100);
      
      // get actual window size for the case it's changed by user
      h = (int)ChartGetInteger(0, CHART_HEIGHT_IN_PIXELS, t) - 1;
      w = (int)ChartGetInteger(0, CHART_WIDTH_IN_PIXELS) - 1;

      // get new dimesions
      dx = (int)ObjectGetInteger(0, name, OBJPROP_XSIZE);
      dy = (int)ObjectGetInteger(0, name, OBJPROP_YSIZE);
      m = GetMargins(Corner, anchor, dx, dy);

      if(_LastError != 0) // object may be deleted by user
      {
         Print("Terminated: ", _LastError);
         break;
      }
   }
   
   ObjectDelete(0, name);
   Comment("");
}
//+------------------------------------------------------------------+
