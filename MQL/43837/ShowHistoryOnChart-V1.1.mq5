//+------------------------------------------------------------------+
//|                                      ShowHistoryOnChart-V1.1.mq5 |
//|                                  Copyright 2023, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2023, MetaQuotes Ltd."
#property link      "https://www.mql5.com"
#property version   "1.00"

input string fileName ="";                              //FileName(xxx.csv)


color clr;                                              // Color for buy signals and sell signals
ENUM_OBJECT objectArrowOpen,objectArrowClose;           // Assign arrows for open and close trade.
string openArrow,closeArrow,trendline;                  // assign open arrow, close arrow and trend line for each trades.



//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- reading files from MQL5\File\xxx.csv

   int handle = FileOpen(fileName,FILE_CSV | FILE_READ | FILE_ANSI, ";");
   if(handle != INVALID_HANDLE)
     {


      while(!FileIsEnding(handle))
        {

         datetime openTime = StringToTime(FileReadString(handle));
         string type = FileReadString(handle);
         double volume1 = StringToDouble(FileReadString(handle));
         string symbol = FileReadString(handle);
         double openPrice = StringToDouble(StringSubstr(FileReadString(handle),0,7));
         double volume2 = StringToDouble(FileReadString(handle));
         datetime closeTime = StringToTime(FileReadString(handle));
         double closePrice = StringToDouble(StringSubstr(FileReadString(handle),0,7));
         double commission = StringToDouble(FileReadString(handle,4));
         double swap = StringToDouble(FileReadString(handle,4));
         double profit = StringToDouble(FileReadString(handle));


         // Assigning arrows, colors, trendlines for each trade.



         openArrow= "openArrow"+ TimeToString(openTime);
         closeArrow = "closeArrow" + TimeToString(closeTime);
         trendline = "trendline " + TimeToString(openTime) + " volume"+ DoubleToString(volume1,2);

         //All buy signals have blue color for arrows and trendlines.

         if(type =="Buy")
           {
            clr = clrBlue;
            objectArrowOpen = OBJ_ARROW_BUY;
            objectArrowClose = OBJ_ARROW_SELL;

           }
         else
            //All sell signals have red color for arrows and trendlines.

            if(type=="Sell")
              {
               clr = clrRed;
               objectArrowOpen = OBJ_ARROW_SELL;
               objectArrowClose = OBJ_ARROW_BUY;
              }

         if(Symbol()== symbol)
           {

            ObjectCreate(ChartID(),openArrow,objectArrowOpen,0,openTime,openPrice);
            ObjectSetInteger(ChartID(),openArrow,OBJPROP_COLOR,clr);
            ObjectSetInteger(ChartID(),openArrow,OBJPROP_WIDTH,1);

            ObjectCreate(ChartID(),closeArrow,objectArrowClose,0,closeTime,closePrice);
            ObjectSetInteger(ChartID(),closeArrow,OBJPROP_COLOR,clr);
            ObjectSetInteger(ChartID(),closeArrow,OBJPROP_WIDTH,1);

            ObjectCreate(ChartID(),trendline,OBJ_TREND,0,openTime,openPrice,closeTime,closePrice);
            ObjectSetInteger(ChartID(),trendline,OBJPROP_COLOR,clr);
            ObjectSetInteger(ChartID(),trendline,OBJPROP_STYLE,STYLE_DOT);

           }


        }


     }

   FileClose(handle);



//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---
   ObjectsDeleteAll(ChartID(),0,-1);

  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---

  }
//+------------------------------------------------------------------+
