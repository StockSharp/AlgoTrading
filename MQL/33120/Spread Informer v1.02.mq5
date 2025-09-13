
//+------------------------------------------------------------------+
//|                                              Spread Informer.mq5 |
//|                                           Copyright 2021, Progid |
//|                             https://www.mql5.com/ru/users/progid |
//+------------------------------------------------------------------+
#property copyright "Copyright 2021, Progid"
#property link      "https://www.mql5.com/ru/users/progid"
#property version   "1.02"

//+------------------------------------------------------------------+
//|
//| This EA collects information about the spread and shows the statistics on the chart.
//| 
//| When the EA ends its working, it prints all statistics to the Journal, which can be useful for the Strategy Tester. 
//| For example, if you want to know all information about the spread of historical data in the tester for a certain period.
//| 
//| The 'Max. Spread' parameter.
//| The parameter value in points, for example: 1.12001 - 1.12000 = 1 point.
//| When the current spread exceeds this value there will be an Alert.
//| If the parameter value = 0, this function does not work.
//|
//| v.1.01
//| - New input parameter is 'Minimum Alert Interval (in seconds)'.
//|   After the Alert, the next Alert will not be earlier than specified in this parameter.
//|
//| v.1.02
//| - Added information about the current spread.
//|
//+------------------------------------------------------------------+

//---inputs
input int      MaxSpread_           = 0;//Max. Spread(in points,0-OFF)
input int      SpreadAlertInterval_ = 0;//Minimum Alert Interval (in seconds)

//---gvars
bool     _IsVisual = !MQLInfoInteger(MQL_TESTER) || MQLInfoInteger(MQL_VISUAL_MODE);
int      _PrevSpread = 0;
//Max&Min spread:
int      _MaxSpread = 0;
datetime _MaxSpreadTime = 0;
int      _MinSpread = 0;
datetime _MinSpreadTime = 0;
//Average spread:
double   _AveSpread = 0;
long     _AveSpreadV = 0;
//working dates
datetime _StartTime = 0;
datetime _EndTime = 0;

datetime _LastAlertTime = 0;

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
{
   _PrevSpread = 0;
   
   _MaxSpread = 0;
   _MaxSpreadTime = 0;
   _MinSpread = 0;
   _MinSpreadTime = 0;
   
   _AveSpread = 0;
   _AveSpreadV = 0;
   
   _StartTime = _EndTime = TimeCurrent();
   _LastAlertTime = 0;
   //---
   OnTick();
   //---
   return(INIT_SUCCEEDED);
}
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
{
   Print("SPREAD INFO from "+(string)_StartTime+" to "+(string)_EndTime);
   Print("MAXIMUM SPREAD is "+(string)_MaxSpread+" points ("+(string)_MaxSpreadTime+")!");
   Print("MINIMUM SPREAD is "+(string)_MinSpread+" points ("+(string)_MinSpreadTime+")!");
   Print("AVERAGE SPREAD is "+DoubleToString(GetAveSpread(), 1)+" points!");
}
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
{
   const int      ActSpread = (int)SymbolInfoInteger(_Symbol, SYMBOL_SPREAD);
   const datetime CurrentTime = TimeCurrent();
   
//---max&min spread
   if (_MaxSpread < ActSpread || _MaxSpreadTime <= 0){
      _MaxSpread = ActSpread;
      _MaxSpreadTime = CurrentTime;
   }
   
   if (_MinSpread > ActSpread || _MinSpreadTime <= 0){
      _MinSpread = ActSpread;
      _MinSpreadTime = CurrentTime;
   }
   
//---average spread
   _AveSpread += ActSpread;
   ++_AveSpreadV;
   
//---time info
   _EndTime = CurrentTime;
   
//---chart comments
   if (_IsVisual){
      string Info = "SPREAD INFO"+"\n";
      Info = Info + "From: "+(string)_StartTime+"\n";
      Info = Info + "To: "+(string)_EndTime+"\n";
      Info = Info + ""+"\n";
      Info = Info + "Current: "+(string)ActSpread+" points"+"\n";
      Info = Info + "Maximum: "+(string)_MaxSpread+" points ("+(string)_MaxSpreadTime+")"+"\n";
      Info = Info + "Minimum: "+(string)_MinSpread+" points ("+(string)_MinSpreadTime+")"+"\n";
      Info = Info + "Average: "+DoubleToString(GetAveSpread(), 1)+" points"+"\n";
      
      Comment(Info);
   }
//---alert
   if (_IsVisual){
      if (MaxSpread_ > 0 && ActSpread > MaxSpread_ && _PrevSpread <= MaxSpread_){
         if (CurrentTime - _LastAlertTime >= SpreadAlertInterval_){
            string Info = "CurrentSpread is "+(string)ActSpread+" > "+(string)MaxSpread_+", "+(string)CurrentTime;
            Alert(Info);
            _LastAlertTime = CurrentTime;
         }
      }
   }
//---
   _PrevSpread = ActSpread;
}

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double GetAveSpread()
{
   if (_AveSpreadV <= 0)
      return 0;
   
   return _AveSpread / (double)_AveSpreadV;
}
