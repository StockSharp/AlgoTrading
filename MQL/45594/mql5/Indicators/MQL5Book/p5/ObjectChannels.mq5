//+------------------------------------------------------------------+
//|                                               ObjectChannels.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2021, MetaQuotes Ltd."
#property link      "https://www.mql5.com"
#property version   "1.0"
#property description "Create and update object channels applied on specified number of latest bars.\n"
                      "Get estimation of future prices, extrapolated by channel lines (upper and lower)."

#property indicator_chart_window
#property indicator_buffers 0
#property indicator_plots   0

input int WorkPeriod = 10;

const string Prefix = "ObjChnl-";
const string ObjStdDev = Prefix + "StdDev";
const string ObjRegr = Prefix + "Regr";

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
{
   CreateObjects();
   UpdateObjects();
}

//+------------------------------------------------------------------+
//| Prepare 2 objects with channels                                  |
//+------------------------------------------------------------------+
void CreateObjects()
{
   ObjectCreate(0, ObjStdDev, OBJ_STDDEVCHANNEL, 0, 0, 0);
   ObjectCreate(0, ObjRegr, OBJ_REGRESSION, 0, 0, 0);
   ObjectSetInteger(0, ObjStdDev, OBJPROP_COLOR, clrBlue);
   ObjectSetInteger(0, ObjStdDev, OBJPROP_RAY_RIGHT, true);
   ObjectSetInteger(0, ObjRegr, OBJPROP_COLOR, clrRed);
   // NB: ray is not enabled for regression channel
   // this will prevent extrapolation of future price levels for it
}

//+------------------------------------------------------------------+
//| Per bar object processing (keep time position on latest bars)    |
//+------------------------------------------------------------------+
void UpdateObjects()
{
   const datetime t0 = iTime(NULL, 0, WorkPeriod);
   const datetime t1 = iTime(NULL, 0, 0);

   // we don't use ObjectMove because the channels require time coordinates only
   ObjectSetInteger(0, ObjStdDev, OBJPROP_TIME, 0, t0);
   ObjectSetInteger(0, ObjStdDev, OBJPROP_TIME, 1, t1);
   ObjectSetInteger(0, ObjRegr, OBJPROP_TIME, 0, t0);
   ObjectSetInteger(0, ObjRegr, OBJPROP_TIME, 1, t1);
}

//+------------------------------------------------------------------+
//| Per tick object processing (show prices in anchor points)        |
//+------------------------------------------------------------------+
void DisplayObjectData()
{
   const double p0 = ObjectGetDouble(0, ObjStdDev, OBJPROP_PRICE, 0);
   const double p1 = ObjectGetDouble(0, ObjStdDev, OBJPROP_PRICE, 1);

   // the following conditions are always true due to the channels algorithms:
   // - middle lines of both channels coincide,
   // - binding points are always on the middle line
   // const double d0 = ObjectGetValueByTime(0, ObjStdDev, iTime(NULL, 0, 0), 0); == p1
   // const double r0 = ObjectGetValueByTime(0, ObjRegr, iTime(NULL, 0, 0), 0); == p1

   // trying to get extrapolation of future prices for WorkPeriod bars
   const double d1 = ObjectGetValueByTime(0, ObjStdDev, iTime(NULL, 0, 0) + WorkPeriod * PeriodSeconds(), 1);
   const double d2 = ObjectGetValueByTime(0, ObjStdDev, iTime(NULL, 0, 0) + WorkPeriod * PeriodSeconds(), 2);

   const double r1 = ObjectGetValueByTime(0, ObjRegr, iTime(NULL, 0, 0) + WorkPeriod * PeriodSeconds(), 1);
   const double r2 = ObjectGetValueByTime(0, ObjRegr, iTime(NULL, 0, 0) + WorkPeriod * PeriodSeconds(), 2);
   
   // display current prices on binding points of the middle line,
   // and future prices along the upper and lower lines of the channels
   Comment(StringFormat("%.*f %.*f\ndev: up=%.*f dn=%.*f\nreg: up=%.*f dn=%.*f",
      _Digits, p0, _Digits, p1,
      _Digits, d1, _Digits, d2,
      _Digits, r1, _Digits, r2));
   // NB: because of intentionally omitted RAY setting for regression channel,
   // it will always produce zeros!
}

//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,
                const int prev_calculated,
                const int begin,
                const double &price[])
{
   static datetime now = 0;
   if(now != iTime(NULL, 0, 0))
   {
      UpdateObjects();
      now = iTime(NULL, 0, 0);
   }
   
   DisplayObjectData();
   
   return rates_total;
}

//+------------------------------------------------------------------+
//| Finalization handler                                             |
//+------------------------------------------------------------------+
void OnDeinit(const int)
{
   ObjectsDeleteAll(0, Prefix);
   Comment("");
}
//+------------------------------------------------------------------+
