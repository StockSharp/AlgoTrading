//+------------------------------------------------------------------+
//|                                              BounceNumber_V0.mq4 |
//|                                  Copyright 2023, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2023, MetaQuotes Ltd."
#property link      "https://www.mql5.com"
#property version   "1.00"
#property strict

input int maxbar = 10000;                             // Max number of lookback candles
input ENUM_TIMEFRAMES TF = PERIOD_M1;                 // Time-Frame
input int BPoints = 300;                              // Bouncing Channel Half Height (Points)
input color ChartBG = clrBlack;                       // Chart Background Color
input color color1 = clrRed;                          // Bar Chart Color 1
input color color2 = clrBlue;                         // Bar Chart Color 2
input color countcolor = clrBlue;                     // Count Cells Color
input color BounceColor = clrDarkBlue;                // Bounce Numbers Cell color
input color barcolor = clrGold;                       // Load bar color

datetime newtime;
int freq[];
string OBJName="";
color BGColor;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
  {
   // Prepare Chart for Illustration
   ChartSetInteger(ChartID(), CHART_FOREGROUND, false);
   ChartSetInteger(0, CHART_COLOR_CANDLE_BEAR, ChartBG);
   ChartSetInteger(0, CHART_COLOR_CANDLE_BULL, ChartBG);
   ChartSetInteger(0, CHART_COLOR_CHART_LINE, ChartBG);
   ChartSetInteger(0, CHART_COLOR_CHART_DOWN, ChartBG);
   ChartSetInteger(0, CHART_COLOR_CHART_UP, ChartBG);
   ChartSetInteger(0, CHART_COLOR_ASK, ChartBG);
   ChartSetInteger(0, CHART_COLOR_BID, ChartBG);
   ChartSetInteger(0, CHART_COLOR_GRID, ChartBG);
   ChartSetInteger(0, CHART_COLOR_BACKGROUND, ChartBG);

   OBJName = "Percentage";
   ObjectCreate(0, OBJName, OBJ_EDIT, 0, 0, 0);
   OBJSet(OBJName, "0.0 %", 20, 50, 50, 20, 10, barcolor, ChartBG, ChartBG, ALIGN_LEFT, "Calibri");
   newtime=0;
   BGColor = ChartBG;

   
   OBJName = "_Symbol";
   if (ObjectFind(ChartID(), OBJName)<0) 
      ObjectCreate(ChartID(), OBJName, OBJ_EDIT, 0, 0, 0);
      OBJSet(OBJName, Symbol(), 15, 15, 150, 25, 12, ChartBG, ChartBG, clrGold, ALIGN_LEFT, "Arial");


   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   ObjectDelete(0, "Percentage");
   ObjectsDeleteAll(0, "Header_", -1, -1);
   ObjectsDeleteAll(0, "Bar_", -1, -1);
   ObjectsDeleteAll(0, "Text_", -1, -1);
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
   if(iTime(Symbol(), Period(), 0) == newtime)
      return;

   int mybars = iBars(Symbol(), TF);
   Print("mybars: ", mybars);
   ArrayResize(freq, 1); ArrayInitialize(freq, 0);

   int i=mybars, p=0;
   if(i>maxbar) i=maxbar;
   mybars=i;

   while(i>0)
     {
      int index = -1;
      for(int z=i-1; z>=0; z--)                                                 // Whhich candle Price goes beyond Bounce Channel?
         if(iHigh(Symbol(), TF, z)>iClose(Symbol(), TF, i)+2*BPoints*Point())   // Price breacks channel up limit
           {
            index = z;
            break;
           }
         else
            if(iLow(Symbol(), TF, z)<iClose(Symbol(), TF, i)-2*BPoints*Point()) // Price breaks channel low limit
              {
               index = z;
               break;
              }

      if(index!=-1)        // Check how many times Price bounce between Channel High/Low before breaking it
        {
         int count=0, con=0;
         double LowBand = iClose(Symbol(), TF, i)-BPoints*Point();
         double HighBand = iClose(Symbol(), TF, i)+BPoints*Point();

         for(int z=i-1; z>=index+1; z--)
            if(con>=0 && iHigh(Symbol(), TF, z)>LowBand && iLow(Symbol(), TF, z)<LowBand)
              {
               con=-1;
               count++;
              }
            else
               if(con<=0 && iLow(Symbol(), TF, z)<HighBand && iHigh(Symbol(), TF, z)>HighBand)
                 {
                  con=1;
                  count++;
                 }
         if (ArraySize(freq)<=count) ArrayResize(freq, count+1);
         freq[count]++;
        }
      i--;

      if(MathMod(i, 100) !=0)             // each 100 candle update graphical things
         continue;

      ObjectSetInteger(0, "Percentage", OBJPROP_XSIZE, (int)(50 + (1-((double) i/ (double)mybars)) * 300));
      ObjectSetString(0, "Percentage", OBJPROP_TEXT, DoubleToString((1-((double) i/ (double)mybars)) * 100, 1)+" %");
      UpdateStat();
      ChartRedraw();
      Sleep(1);
     }

   newtime = iTime(Symbol(), Period(),0);
  }
//+------------------------------------------------------------------+
void UpdateStat()
  {
   int x = 20;
   int y = (int)(ChartGetInteger(ChartID(), CHART_HEIGHT_IN_PIXELS)/2);
   int tickness = 50;

   int maxcount = freq[ArrayMaximum(freq, WHOLE_ARRAY, 0)];
   bool first = false;
   int step = 0, spoint = (int) (y/3);

   for(int i=ArraySize(freq)-1+x; i>=x+1; i--)
     {
      if(freq[i-x]!=0 && !first)
        {
         first=true;
         step=i-x;
        }

      if(!first) continue;
      OBJName = "Text_" + IntegerToString(i-x);

      if(maxcount!=0)
         if(ObjectFind(ChartID(), OBJName)<0)
           {
            ObjectCreate(ChartID(), OBJName, OBJ_EDIT, 0, 0, 0);
            TrendSet(OBJName, (i-x) *(tickness+2) +i, spoint+22, tickness,20, BounceColor, 1, STYLE_SOLID, true, IntegerToString(i-x));
            ObjectSetInteger(0, OBJName, OBJPROP_COLOR, clrWhite);
            ObjectSetInteger(0, OBJName, OBJPROP_BORDER_COLOR, clrDarkBlue);
            ObjectSetInteger(0, OBJName, OBJPROP_ALIGN, ALIGN_CENTER);
            ObjectSetInteger(0, OBJName, OBJPROP_CORNER, CORNER_LEFT_LOWER);
           }
         else
           {
            TrendSet(OBJName, (i-x) *(tickness+2) +i, spoint+22, tickness,20, BounceColor, 1, STYLE_SOLID, true, IntegerToString(i-x));
            ObjectSetInteger(0, OBJName, OBJPROP_COLOR, clrWhite);
            ObjectSetInteger(0, OBJName, OBJPROP_BORDER_COLOR, clrDarkBlue);
            ObjectSetInteger(0, OBJName, OBJPROP_ALIGN, ALIGN_CENTER);
            ObjectSetInteger(0, OBJName, OBJPROP_CORNER, CORNER_LEFT_LOWER);
           }

      if(freq[i-x]!=0 && !first) first=true;
      if(!first) continue;
      
      OBJName = "Header_" + IntegerToString(i-x);
      if(maxcount!=0)
         if(ObjectFind(ChartID(), OBJName)<0)
           {
            ObjectCreate(ChartID(), OBJName, OBJ_EDIT, 0, 0, 0);
            TrendSet(OBJName, (i-x) *(tickness+2) +i, spoint+45, tickness,20, countcolor, 1, STYLE_SOLID, true, IntegerToString(freq[i-x]));
            ObjectSetInteger(0, OBJName, OBJPROP_COLOR, clrWhite);
            ObjectSetInteger(0, OBJName, OBJPROP_BORDER_COLOR, clrBlue);
            ObjectSetInteger(0, OBJName, OBJPROP_ALIGN, ALIGN_CENTER);
            ObjectSetInteger(0, OBJName, OBJPROP_CORNER, CORNER_LEFT_LOWER);
           }
         else
           {
            TrendSet(OBJName, (i-x) *(tickness+2) +i, spoint+45, tickness,20, countcolor, 1, STYLE_SOLID, true, IntegerToString(freq[i-x]));
            ObjectSetInteger(0, OBJName, OBJPROP_COLOR, clrWhite);
            ObjectSetInteger(0, OBJName, OBJPROP_BORDER_COLOR, clrBlue);
            ObjectSetInteger(0, OBJName, OBJPROP_ALIGN, ALIGN_CENTER);
            ObjectSetInteger(0, OBJName, OBJPROP_CORNER, CORNER_LEFT_LOWER);
           }

      OBJName = "Bar_" + IntegerToString(i-x);
      color mycolor = ss2rgb(ColorToString(color1, false), ColorToString(color2, false), step, i-x);

      if(freq[i-x]!=0)
        {
         if(ObjectFind(ChartID(), OBJName)<0)
           {
            ObjectCreate(ChartID(), OBJName, OBJ_EDIT, 0, 0, 0);
            if(maxcount!=0)
               TrendSet(OBJName, (i-x) *(tickness+2) +i, spoint+50, tickness,-y*freq[i-x]/maxcount-1, mycolor, 1, STYLE_SOLID, true, "");
            ObjectSetInteger(0, OBJName, OBJPROP_CORNER, CORNER_LEFT_LOWER);
           }
         else
           {
            if(maxcount!=0)
               TrendSet(OBJName, (i-x) *(tickness+2) +i, spoint+50, tickness,-y*freq[i-x]/maxcount-1, mycolor, 1, STYLE_SOLID, true, "");
            ObjectSetInteger(0, OBJName, OBJPROP_CORNER, CORNER_LEFT_LOWER);
           }
        }
     }
  }
//+------------------------------------------------------------------+
void TrendSet(string TrendName, int x0, int y0, int xsize, int ysize, color Tcolor, int Twidth, ENUM_LINE_STYLE Tstyle, int b, string text)
  {
   ObjectSetInteger(ChartID(), TrendName, OBJPROP_XDISTANCE, x0);
   ObjectSetInteger(ChartID(), TrendName, OBJPROP_YDISTANCE, y0);
   ObjectSetInteger(ChartID(), TrendName, OBJPROP_XSIZE, xsize);
   ObjectSetInteger(ChartID(), TrendName, OBJPROP_YSIZE, ysize);
   ObjectSetInteger(ChartID(), TrendName, OBJPROP_COLOR, Tcolor);
   ObjectSetInteger(ChartID(), TrendName, OBJPROP_BGCOLOR, Tcolor);
   ObjectSetInteger(ChartID(), TrendName, OBJPROP_WIDTH, Twidth);
   ObjectSetInteger(ChartID(), TrendName, OBJPROP_STYLE, Tstyle);
   ObjectSetInteger(ChartID(), TrendName, OBJPROP_BACK, false);
   ObjectSetInteger(ChartID(), TrendName, OBJPROP_SELECTABLE, false);
   ObjectSetInteger(ChartID(), TrendName, OBJPROP_READONLY, true);
   ObjectSetString(ChartID(), TrendName, OBJPROP_TEXT, text);
   ObjectSetInteger(ChartID(), TrendName, OBJPROP_TIMEFRAMES, b?OBJ_ALL_PERIODS:OBJ_NO_PERIODS);
  }
//+------------------------------------------------------------------+
//| ChartEvent function                                              |
//+------------------------------------------------------------------+
void OnChartEvent(const int id,
                  const long &lparam,
                  const double &dparam,
                  const string &sparam)
  {
   int PH = (int) ChartGetInteger(ChartID(), CHART_HEIGHT_IN_PIXELS);
   int PW = (int) ChartGetInteger(ChartID(), CHART_WIDTH_IN_PIXELS);

   OBJName = "Bounce" + "_Frame";
   if(ObjectFind(ChartID(), OBJName)<0)
      ObjectCreate(ChartID(), OBJName, OBJ_EDIT, 0, 0, 0);
   OBJSet(OBJName, "", 0, 0, PW, PW, 1, BGColor, BGColor, BGColor, ALIGN_CENTER, "Arial");
   ObjectSetInteger(ChartID(), OBJName, OBJPROP_BACK, true);
  }
//+------------------------------------------------------------------+
void OBJSet(string OBJNameSet, string txt, int XDis, int YDis, int XSize, int YSize, int FSize, color Fill, color Border, color TColor, ENUM_ALIGN_MODE myAlign, string myFont)
  {
   ObjectSetInteger(ChartID(), OBJNameSet, OBJPROP_CORNER, CORNER_LEFT_UPPER);
   ObjectSetInteger(ChartID(), OBJNameSet, OBJPROP_ANCHOR, ANCHOR_LEFT_UPPER);
   ObjectSetInteger(ChartID(), OBJNameSet, OBJPROP_XDISTANCE, XDis);
   ObjectSetInteger(ChartID(), OBJNameSet, OBJPROP_YDISTANCE, YDis);
   ObjectSetInteger(ChartID(), OBJNameSet, OBJPROP_XSIZE, XSize);
   ObjectSetInteger(ChartID(), OBJNameSet, OBJPROP_YSIZE, YSize);
   ObjectSetInteger(ChartID(), OBJNameSet, OBJPROP_FONTSIZE, FSize);
   ObjectSetString(ChartID(),  OBJNameSet, OBJPROP_TEXT,txt);
   ObjectSetString(ChartID(),  OBJNameSet, OBJPROP_FONT,myFont);

   ObjectSetInteger(ChartID(), OBJNameSet, OBJPROP_BGCOLOR, Fill);
   ObjectSetInteger(ChartID(), OBJNameSet, OBJPROP_BORDER_COLOR, Border);
   ObjectSetInteger(ChartID(), OBJNameSet, OBJPROP_COLOR, TColor);
   ObjectSetInteger(ChartID(), OBJNameSet, OBJPROP_ALIGN, myAlign);

   ObjectSetInteger(ChartID(), OBJNameSet, OBJPROP_BACK, false);
   ObjectSetInteger(ChartID(), OBJNameSet, OBJPROP_ZORDER,5);
   ObjectSetInteger(ChartID(), OBJNameSet, OBJPROP_HIDDEN, true);
   ObjectSetInteger(ChartID(), OBJNameSet, OBJPROP_READONLY, true);
   ObjectSetInteger(ChartID(), OBJNameSet, OBJPROP_SELECTABLE, false);
  }
//+------------------------------------------------------------------+
color ss2rgb(string colour1, string colour2, int step, int index)
  {
   int r1 = StrToInteger(StringSubstr(colour1, 0,3));
   int g1 = StrToInteger(StringSubstr(colour1, 4,3));
   int b1 = StrToInteger(StringSubstr(colour1, 8,3));

   int r2 = StrToInteger(StringSubstr(colour2, 0,3));
   int g2 = StrToInteger(StringSubstr(colour2, 4,3));
   int b2 = StrToInteger(StringSubstr(colour2, 8,3));

   int r=0, g=0, b=0;

   if(r1>r2)
     {
      r = r1+((r2-r1)/step*index);
     }
   if(r1<r2)
     {
      r = r1-((r1-r2)/step*index);
     }

   if(g1>g2)
     {
      g = g1+((g2-g1)/step*index);
     }
   if(g1<g2)
     {
      g = g1-((g1-g2)/step*index);
     }

   if(b1>b2)
     {
      b = b1+((b2-b1)/step*index);
     }
   if(b1<b2)
     {
      b = b1-((b1-b2)/step*index);
     }

   g<<=8;
   b<<=16;
   return((color) (r+g+b));

  }
//+------------------------------------------------------------------+
