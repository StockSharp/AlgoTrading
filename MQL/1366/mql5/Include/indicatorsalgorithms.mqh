//MQL5 Version  June 21, 2010 Final
//+X================================================================X+
//|                                         IndicatorsAlgorithms.mqh |
//|                               Copyright © 2010, Nikolay Kositsin |
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+X================================================================X+
#property copyright "2010,   Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.00"

//+X----------------------------------------------------------------X+
// Declaration of classes of averaging                               | 
//+X----------------------------------------------------------------X+ 
#include <SmoothAlgorithms.mqh> 
//+X================================================================X+
//|  Functional utilities for the classes of indicators              |
//+X================================================================X+
class CIndicatorsTools
  {
public:
   int               iBarShift(string symbol,ENUM_TIMEFRAMES timeframe,datetime time);

   bool              bGetLimitX4(
                                 int Number,// Number of a call in the IsNewBar function in the program code of the Expert Advisor 
                                 //(Minimum value - 0, Maximum value - 3)
                                 string symbol,// Symbol name NULL means current symbol.
                                 ENUM_TIMEFRAMES timeframe,// Period. Can be one of the chart periods. 0 means the current chart period.
                                 bool recount,// Repeated return of the previous value on the next GetLimit() call
                                 int &limit // Returns the start number of the new bars calculation by reference
                                 );

   bool              IsNewBarX4(int Number,string symbol,ENUM_TIMEFRAMES timeframe);

   int               iHighest(
                              const double &array[],// array for searching for maximum element index
                              int count,// the number of the array elements (from a current bar to the index increasing),
                              // along which the searching must be performed(the access as in time series!).
                              int startPos //the initial bar index (shift relative to a current bar),
                              // the search for the greatest value begins from
                              );

   int               iLowest(
                             const double &array[],// array for searching for minimum element index
                             int count,// the number of the array elements (from a current bar to the index increasing),
                             // along which the searching must be performed(the access as in time series!).
                             int startPos //the initial bar index (shift relative to a current bar),
                             // the search for the lowest value begins from
                             );

   int               iHighest_(
                               const double &array[],// array for searching for maximum element index
                               int count,// the number of the array elements (from a current bar to the index increasing),
                               // along which the searching must be performed(the access as in time series!).
                               int startPos //the initial bar index (shift relative to a current bar),
                               // the search for the greatest value begins from
                               );

   int               iLowest_(
                              const double &array[],// array for searching for minimum element index
                              int count,// the number of the array elements (from a current bar to the index increasing),
                              // along which the searching must be performed(the access as in time series!).
                              int startPos //the initial bar index (shift relative to a current bar),
                              // the search for the lowest value begins from
                              );

   void              Recount_ArrayZeroPos(int &count,
                                          int Length,
                                          int prev_calculated,
                                          double series,
                                          int bar,
                                          double &Array[]
                                          );

   int               Recount_ArrayNumber(int count,int Length,int Number);

   bool              SeriesArrayResize(string FunctionsName,
                                       int Length,
                                       double &Array[],
                                       int &Size_
                                       );

protected:

   bool              ArrayResizeErrorPrint(
                                           string FunctionsName,
                                           int &Size_
                                           );
                                           
   //---- declaration of variables of the class ÑMoving_Average
   datetime          m_LastTime[4];
   datetime          m_OldLastTime[4];
   datetime          m_Told[4];
  };
//+X================================================================X+
//|  Linear regression averaging of price series                     |
//+X================================================================X+
class CLRMA
  {
public:
   double LRMASeries(uint begin,// number of beginning of bars for reliable calculation
                     uint prev_calculated,// amount of history in bars at the previous tick
                     uint rates_total,// amount of history in bars at the current tick
                     int Length,// Averaging period
                     double series,// Price series value calculated for the bar with number 'bar'
                     uint bar,// Bar index
                     bool set // direction of indexing arrays
                     )
     {
      //----+
      //---- Declaration of local variables
      double sma,lwma,lrma;

      //---- declaration of variables of the class Moving_Average from the file MASeries_Cls.mqh
      // CMoving_Average SMA, LWMA;

      //---- Getting values of moving averages  
      sma=m_SMA.SMASeries(begin,prev_calculated,rates_total,Length,series,bar,set);
      lwma=m_LWMA.LWMASeries(begin,prev_calculated,rates_total,Length,series,bar,set);

      //---- Calculation of LRMA
      lrma=3.0*lwma-2.0*sma;
      //----+
      return(lrma);
     };

protected:
   //---- declaration of variables of the class ÑMoving_Average
   CMoving_Average   m_SMA,m_LWMA;
  };
//+X================================================================X+
//|  The algorithm of getting the Bollinger channel calculated from  |
//|  VIDYA                                                           |
//+X================================================================X+
class CVidyaBands
  {
public:
   double VidyaBandsSeries(uint begin,// number of beginning of bars for reliable calculation
                           uint prev_calculated,// amount of history in bars at the previous tick
                           uint rates_total,// amount of history in bars at the current tick
                           int CMO_period,// Period of averaging of the oscillator CMO
                           double EMA_period,// period of averaging of EMA
                           int BBLength,// period of averaging of the Bollinger Bands
                           double deviation,// Deviation
                           double series,// Price series value calculated for the bar with number 'bar'
                           uint bar,// Bar index
                           bool set,// Direction of indexing arrays
                           double& DnMovSeries,// Value of the lower border of the channel for the current bar 
                           double& MovSeries,  // Value of the middle line of the channel for the current bar 
                           double &UpMovSeries  // Value of the upper border of the channel for the current bar 
                           )
     {
      //----+
      //----+ Calculation of the middle line    
      MovSeries=m_VIDYA.VIDYASeries(begin,prev_calculated,rates_total,CMO_period,EMA_period,series,bar,set);

      //----+ Calculation of the Bollinger channel
      double StdDev=m_STD.StdDevSeries(begin+CMO_period+1,prev_calculated,rates_total,BBLength,deviation,series,MovSeries,bar,set);
      DnMovSeries = MovSeries - StdDev;
      UpMovSeries = MovSeries + StdDev;
      //----+
      return(StdDev);
     }

protected:
   //---- declaration of variables of the classes CCMO and CStdDeviation
   CCMO              m_VIDYA;
   CStdDeviation     m_STD;
  };
//+X================================================================X+
//|  Algorithm of getting the Bollinger channel                      |
//+X================================================================X+
class CBBands
  {
public:
   double            BBandsSeries(uint begin,// number of beginning of bars for reliable calculation
                                  uint prev_calculated,// amount of history in bars at the previous tick
                                  uint rates_total,// amount of history in bars at the current tick
                                  int Length,// Averaging period
                                  double deviation,// Deviation
                                  ENUM_MA_METHOD MA_Method,// method of averaging
                                  double series,// Price series value calculated for the bar with number 'bar'
                                  uint bar,// Bar index
                                  bool set,// Direction of indexing arrays
                                  double& DnMovSeries,// Value of the lower border of the channel for the current bar 
                                  double& MovSeries,  // Value of the middle line of the channel for the current bar 
                                  double &UpMovSeries  // Value of the upper border of the channel for the current bar 
                                  );

   double            BBandsSeries_(uint begin,// number of beginning of bars for reliable calculation
                                   uint prev_calculated,// amount of history in bars at the previous tick
                                   uint rates_total,// amount of history in bars at the current tick
                                   int MALength,// Period of moving average
                                   ENUM_MA_METHOD MA_Method,// method of averaging
                                   int BBLength,// period of averaging of the Bollinger Bands
                                   double deviation,// Deviation
                                   double series,// Price series value calculated for the bar with number 'bar'
                                   uint bar,// Bar index
                                   bool set,// Direction of indexing arrays
                                   double& DnMovSeries,// Value of the lower border of the channel for the current bar 
                                   double& MovSeries,  // Value of the middle line of the channel for the current bar 
                                   double &UpMovSeries  // Value of the upper border of the channel for the current bar 
                                   );
protected:
   //---- declaration of variables of the classes ÑMoving_Average and CStdDeviation
   CStdDeviation     m_STD;
   CMoving_Average   m_MA;
  };
//+X================================================================X+
//|  Calculation of the Bollinger channel                            |
//+X================================================================X+    
double CBBands::BBandsSeries
(
 uint begin,// number of beginning of bars for reliable calculation
 uint prev_calculated,// amount of history in bars at the previous tick
 uint rates_total,// amount of history in bars at the current tick
 int Length,// Averaging period
 double deviation,// Deviation
 ENUM_MA_METHOD MA_Method,//method of averaging
 double series,// Price series value calculated for the bar with number 'bar'
 uint bar,// Bar index
 bool set,// Direction of indexing arrays
 double &DnMovSeries,// Value of the lower border of the channel for the current bar 
 double &MovSeries,// Value of the middle line of the channel for the current bar
 double &UpMovSeries // Value of the upper border of the channel for the current bar
 )
// BBandsMASeries(begin, prev_calculated, rates_total, period, deviation,
// MA_Method, Series, bar, set, DnMovSeries, MovSeries, UpMovSeries) 
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
  {
//----+
//----+ Calculation of the middle line
   MovSeries=m_MA.MASeries(begin,prev_calculated,rates_total,Length,MA_Method,series,bar,set);

//----+ Calculation of the Bollinger channel
   double StdDev=m_STD.StdDevSeries(begin,prev_calculated,rates_total,Length,deviation,series,MovSeries,bar,set);
   DnMovSeries = MovSeries - StdDev;
   UpMovSeries = MovSeries + StdDev;
//----+
   return(StdDev);
  }
//+X================================================================X+
//|  Calculation of the Bollinger channel                            |
//+X================================================================X+    
double CBBands::BBandsSeries_
(
 uint begin,// number of beginning of bars for reliable calculation
 uint prev_calculated,// amount of history in bars at the previous tick
 uint rates_total,// amount of history in bars at the current tick
 int MALength,// Period of moving average
 ENUM_MA_METHOD MA_Method,//method of averaging
 int BBLength,// period of averaging of the Bollinger Bands
 double deviation,// Deviation
 double series,// Price series value calculated for the bar with number 'bar'
 uint bar,// Bar index
 bool set,// Direction of indexing arrays
 double &DnMovSeries,// Value of the lower border of the channel for the current bar 
 double &MovSeries,// Value of the middle line of the channel for the current bar
 double &UpMovSeries // Value of the upper border of the channel for the current bar
 )
// BBandsMASeries_(begin, prev_calculated, rates_total, MALength, MA_Method,
// deviation, BBLength, Series, bar, set, DnMovSeries, MovSeries, UpMovSeries) 
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
  {
//----+
//----+ Calculation of the middle line
   MovSeries=m_MA.MASeries(begin,prev_calculated,rates_total,MALength,MA_Method,series,bar,set);

//----+ Calculation of the Bollinger channel
   double StdDev=m_STD.StdDevSeries(begin+MALength+1,prev_calculated,rates_total,BBLength,deviation,series,MovSeries,bar,set);
   DnMovSeries = MovSeries - StdDev;
   UpMovSeries = MovSeries + StdDev;
//----+
   return(StdDev);
  }
//+X================================================================X+   
//| iBarShift() function                                             |
//+X================================================================X+  
int CIndicatorsTools :: iBarShift(string symbol,ENUM_TIMEFRAMES timeframe,datetime time)

// iBarShift(symbol, timeframe, time)
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
  {
//----+
   if(time<0) return(-1);
   datetime Arr[],time1;

   time1=(datetime)SeriesInfoInteger(symbol,timeframe,SERIES_LASTBAR_DATE);

   if(CopyTime(symbol,timeframe,time,time1,Arr)>0)
     {
      int size=ArraySize(Arr);
      return(size-1);
     }
   else return(-1);
//----+
  }
//+X================================================================X+
//| bGetLimit_X4() function                                          |
//+X================================================================X+
bool CIndicatorsTools :: bGetLimitX4
(
 int Number,// Number of a call in the IsNewBar function in the program code of the Expert Advisor
 string symbol,// Symbol name NULL means current symbol.
 ENUM_TIMEFRAMES timeframe,// Period. Can be one of the chart periods. 0 means the current chart period.
 bool recount,// Repeated return of the previous value on the next GetLimit() call
 int &limit //
 )
// bGetLimitX4(Number, symbol, timeframe, recount, limit)
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
  {
//----+
   datetime LastTime_;
//----
   if(recount)
     {
      m_LastTime[Number]=m_OldLastTime[Number];
      return(true);
     }

   datetime NewTime=(datetime)SeriesInfoInteger(symbol,timeframe,SERIES_LASTBAR_DATE);

   LastTime_=m_LastTime[Number];

   if(NewTime!=LastTime_)
     {
      if(LastTime_!=0)
         limit=iBarShift(symbol,timeframe,LastTime_);
      else limit=Bars(symbol,timeframe)-1;
      //----
      if(limit<=0) return(false);
      //----
      m_LastTime[Number]=NewTime;
      m_OldLastTime[Number]=LastTime_;
     }
   else limit=0;
//----+
   return(true);
  }
//+X================================================================X+
//| IsNewBar() function                                              |
//+X================================================================X+
bool CIndicatorsTools :: IsNewBarX4
(int Number,string symbol,ENUM_TIMEFRAMES timeframe)
  {
//----+
   datetime Tnew[1];
   CopyTime(symbol,timeframe,0,1,Tnew);
   if(Tnew[0]!=m_Told[Number] && Tnew[0]>0)
     {
      m_Told[Number]=Tnew[0];
      return(true);
     }
//----+
   return(false);
  }
//+------------------------------------------------------------------+
//|  Search index of the maximal element in the array                |
//+------------------------------------------------------------------+
int CIndicatorsTools :: iHighest
(
 const double &array[],// array for searching for maximum element index
 int count,// the number of the array elements (from a current bar to the index increasing),
 // along which the searching must be performed(the access as in time series!).
 int startPos //the initial bar index (shift relative to a current bar),
 // the search for the greatest value begins from
 )
  {
//----
   int index=startPos;

//---- checking correctness of the initial index
   if(startPos<0)
     {
      Print("Bad value in the iHighest function, startPos = ",startPos);
      return(0);
     }

//---- checking correctness of startPos value
   if(count<0) count=startPos;

   double max=array[startPos];

//---- searching for an index
   for(int i=startPos; i<startPos+count; i++)
     {
      if(array[i]>max)
        {
         index=i;
         max=array[i];
        }
     }
//---- returning of the greatest bar index
   return(index);
  }
//+------------------------------------------------------------------+
//|  Search index of the minimal element in the array                |
//+------------------------------------------------------------------+
int CIndicatorsTools :: iLowest
(
 const double &array[],// array for searching for minimum element index
 int count,// the number of the array elements (from a current bar to the index increasing),
 // along which the searching must be performed(the access as in time series!).
 int startPos //the initial bar index (shift relative to a current bar),
 // the search for the lowest value begins from
 )
  {
//----
   int index=startPos;

//---- checking correctness of the initial index
   if(startPos<0)
     {
      Print("Bad value in the iLowest function, startPos = ",startPos);
      return(0);
     }

//---- checking correctness of startPos value
   if(count<0) count=startPos;

   double min=array[startPos];

//---- searching for an index
   for(int i=startPos; i<startPos+count; i++)
     {
      if(array[i]<min)
        {
         index=i;
         min=array[i];
        }
     }
//---- returning of the lowest bar index
   return(index);
  }
//+------------------------------------------------------------------+
//|  Search index of the maximal element in the array                |
//+------------------------------------------------------------------+
int CIndicatorsTools :: iHighest_
(
 const double &array[],// array for searching for maximum element index
 int count,// the number of the array elements (from a current bar to the index descending),
 // along which the searching must be performed(the access as in time series!).
 int startPos //the initial bar index (shift relative to a current bar),
 // the search for the greatest value begins from
 )
  {
//----
   int index=startPos;

//---- checking correctness of the initial index
   if(startPos<0)
     {
      Print("Bad value in the iHighest function, startPos = ",startPos);
      return(0);
     }

//---- checking correctness of startPos value
   if(startPos-count<0) count=startPos;

   double max=array[startPos];

//---- searching for an index
   for(int i=startPos; i>startPos-count; i--)
     {
      if(array[i]>max)
        {
         index=i;
         max=array[i];
        }
     }
//---- returning of the greatest bar index
   return(index);
  }
//+------------------------------------------------------------------+
//|  Search index of the minimal element in the array                |
//+------------------------------------------------------------------+
int CIndicatorsTools :: iLowest_
(
 const double &array[],// array for searching for minimum element index
 int count,// the number of the array elements (from a current bar to the index descending),
 // along which the searching must be performed(the access as in time series!).
 int startPos //the initial bar index (shift relative to a current bar),
 // the search for the lowest value begins from
 )
  {
//----
   int index=startPos;

//---- checking correctness of the initial index
   if(startPos<0)
     {
      Print("Bad value in the iLowest function, startPos = ",startPos);
      return(0);
     }

//---- checking correctness of startPos value
   if(startPos-count<0)
      count=startPos;

   double min=array[startPos];

//---- searching for an index
   for(int i=startPos; i>startPos-count; i--)
     {
      if(array[i]<min)
        {
         index=i;
         min=array[i];
        }
     }
//---- returning of the lowest bar index
   return(index);
  }
//+X================================================================X+
//|  recalculation of position of a newest element in the array      |
//+X================================================================X+    
void CIndicatorsTools :: Recount_ArrayZeroPos
(
 int &count,// Returns the number of the current value of the price series by reference
 int Size,// array size
 int prev_calculated,
 double series,// Price series value calculated for the bar with number 'bar'
 int bar,
 double &Array[]
 )
// Recount_ArrayZeroPos(count, Size, prev_calculated, series, bar, Array[])
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
  {
//----+
   if(bar!=prev_calculated-1)
     {
      count--;
      if(count<0) count=Size-1;
     }

   Array[count]=series;
//----+
  }
//+X================================================================X+
//|  Transformation of a timeseries number into an array position    |
//+X================================================================X+    
int CIndicatorsTools :: Recount_ArrayNumber
(
 int count,// Number of the current value of the price series
 int Size,// array size
 int Number // Position of the requested value relatively to the current bar 'bar'
 )
// Recount_ArrayNumber(count, Size, Number)
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
  {
//----+
   int ArrNumber=Number+count;

   if(ArrNumber>Size-1) ArrNumber-=Size;
//----+
   return(ArrNumber);
  }
//+X================================================================X+
//|  Changing size of the array Array[]                              |
//+X================================================================X+    
bool CIndicatorsTools :: SeriesArrayResize
(
 string FunctionsName,// Name of the function in which the size is changed
 int Size,// new size of the array
 double &Array[],// Array that is being changed
 int &Size_ // Resulting size of the array
 )
// SeriesArrayResize(FunctionsName, Size, Array, Size_) 
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
  {
//----+    

//----+ Changing the size of the array of variables
   if(Size>Size_)
     {
      int size=Size+1;

      if(ArrayResize(Array,size)==-1)
        {
         ArrayResizeErrorPrint(FunctionsName,Size_);
         return(false);
        }

      Size_=size;
     }
//----+
   return(true);
  }
//+X================================================================X+
//|  Writing the error of changing size of the array into log file   |
//+X================================================================X+    
bool CIndicatorsTools :: ArrayResizeErrorPrint
(
 string FunctionsName,
 int &Size_
 )
// ArrayResizeErrorPrint(FunctionsName, Size_) 
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
  {
//----+    
   string lable,word;
   StringConcatenate(lable,FunctionsName,"():");
   StringConcatenate(word,lable," Error!!! Failed to change",
                     " the size of the array of variables of the function ",FunctionsName,"()!");
   Print(word);
//----             
   int error=GetLastError();
   ResetLastError();
//----
   if(error>4000)
     {
      StringConcatenate(word,lable,"(): Error code ",error);
      Print(word);
     }

   Size_=-2;
   return(false);
//----+
   return(true);
  }
//+X----------------------+ <<< The End >>> +-----------------------X+
