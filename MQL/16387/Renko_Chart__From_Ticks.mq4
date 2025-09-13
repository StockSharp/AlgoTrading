//+------------------------------------------------------------------+
//|                                      Renko_Chart__From_Ticks.mq4 |
//|                                                    Binoy Raphael |
//+------------------------------------------------------------------+
// Rev0: Initial revision

#property copyright "Binoy Raphael"
#property link      ""
#property version   "1.00"
#property strict
input int RenkoSize        = 10; // Renko BoxSize
input int RenkoTimeFrame   = 2;  // Renko New Timeframe
#include <stdlib.mqh>

// Newbar Variable
datetime stored_bartime;

//Renko calculation variables
double points=Point;
double BoxPoints;
double prevRenkoOpen,prevRenkoClose;
double prevRenkoHi,prevRenkoLo;
static double volume;

// Variables for History write
int HstHandle;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
// 1. Renko initialization
   if(Digits==3 || Digits==5){points*=10;}
   BoxPoints      = NormalizeDouble(RenkoSize * points,Digits);
   prevRenkoOpen  = NormalizeDouble(MathFloor(Close[1]/BoxPoints)*BoxPoints,Digits);
   prevRenkoClose = prevRenkoOpen + BoxPoints;
   prevRenkoHi    = High[1];
   prevRenkoLo    = Low[1];
   volume         =(double)Volume[1];
// Not used in the history file

// 2. History file initialization
// Create History file : Will be located in "Tester/Files/". Copy it to the "History" directory
   string filename=Symbol()+IntegerToString(RenkoTimeFrame)+".hst";
   HstHandle=FileOpen(filename,FILE_BIN|FILE_WRITE|FILE_SHARE_WRITE|FILE_SHARE_READ|FILE_ANSI);
   if(HstHandle<0)
     {
      Print("Error: can\'t create history file: "+ErrorDescription(GetLastError())+": "+filename);
      return(0);
     }

// 3. Write the history header
   int HstUnused[13];
   FileWriteInteger(HstHandle, 400, LONG_VALUE);            // Version
   FileWriteString(HstHandle, "", 64);                      // Copyright
   FileWriteString(HstHandle, Symbol(), 12);                // Symbol
   FileWriteInteger(HstHandle, RenkoTimeFrame, LONG_VALUE); // Period
   FileWriteInteger(HstHandle, Digits, LONG_VALUE);         // Digits
   FileWriteInteger(HstHandle, 0, LONG_VALUE);              // Time Sign
   FileWriteInteger(HstHandle, 0, LONG_VALUE);              // Last Sync
   FileWriteArray(HstHandle, HstUnused, 0, 13);             // Unused

   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   if(HstHandle>=0)
     {
      FileClose(HstHandle);
      HstHandle=-1;
     }
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
   double bidPrice=Bid;

   datetime bar_time=(datetime)SeriesInfoInteger(Symbol(),Period(),SERIES_LASTBAR_DATE);
/// NEW BAR GENERATED ?
   if(stored_bartime!=bar_time)
     {
      // A new bar is created...
      // Reset to defaults
      stored_bartime=bar_time;
      volume+=(double)Volume[1];
     }

// Calculation of renko arrays
   prevRenkoHi = MathMax(prevRenkoHi, Bid);
   prevRenkoLo = MathMin(prevRenkoLo, Bid);
/// 1. Check for the LOW renko
   while(bidPrice<(prevRenkoOpen-BoxPoints) || CompareDoubles(bidPrice,prevRenkoOpen-BoxPoints))
     {
      prevRenkoOpen    -= BoxPoints;
      prevRenkoClose   -= BoxPoints;
      FileRenkoWrite(Time[0],prevRenkoClose,prevRenkoLo,prevRenkoHi,prevRenkoOpen,0);
      // time, open, low, high, close, vol

      volume=(double)Volume[1]; // Volume not used for file-writing
      prevRenkoHi = prevRenkoOpen;
      prevRenkoLo = prevRenkoOpen;
     }
/// 2. Check for the HIGH renko
   while(bidPrice>(prevRenkoClose+BoxPoints) || CompareDoubles(bidPrice,prevRenkoClose+BoxPoints))
     {
      prevRenkoOpen    += BoxPoints;
      prevRenkoClose   += BoxPoints;
      FileRenkoWrite(Time[0],prevRenkoOpen,prevRenkoLo,prevRenkoHi,prevRenkoClose,0);
      // time, open, low, high, close, vol

      volume=(double)Volume[1]; // Volume not used for file-writing
      prevRenkoHi = prevRenkoClose;
      prevRenkoLo = prevRenkoClose;
     }
  }
//+------------------------------------------------------------------+

void FileRenkoWrite(datetime time,double open,double low,double high,double close,double vol)
  {
   FileWriteInteger(HstHandle,(int)time,LONG_VALUE);
   FileWriteDouble(HstHandle,open,DOUBLE_VALUE);
   FileWriteDouble(HstHandle,low,DOUBLE_VALUE);
   FileWriteDouble(HstHandle,high,DOUBLE_VALUE);
   FileWriteDouble(HstHandle,close,DOUBLE_VALUE);
   FileWriteDouble(HstHandle,vol,DOUBLE_VALUE);
  }
//+------------------------------------------------------------------+
