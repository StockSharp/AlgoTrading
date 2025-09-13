/*------------------------------------------------------------------+
 |                                              Nik_PSAR_2B.mq4.mq4 |
 |                                                 Copyright © 2010 |
 |                                             basisforex@gmail.com |
 +------------------------------------------------------------------*/
#property copyright "Copyright © 2010, basisforex@gmail.com"
#property link      "basisforex@gmail.com"
//-----
#property indicator_chart_window
#property indicator_buffers 6
#property indicator_color1 White
#property indicator_color2 Yellow
#property indicator_color3 Blue
#property indicator_color4 Black
#property indicator_color5 Green
#property indicator_color6 Red
//-----
extern bool       AlertsSignal   = true;
extern bool       AlertsEnabled  = true;
extern bool       TF4            = true;
extern bool       TF3            = true;
extern bool       TF2            = true;
//-----
extern double     Step           = 0.02;
extern double     Maximum        = 0.2;
//-----
double s1[];
double s2[];
double s3[];
double s4[];
double bullish[];
double bearish[];
double sarUp[];
double sarDn[];
double alertBar;
//+------------------------------------------------------------------+
int init()
 {
   SetIndexBuffer(0, s1);
   SetIndexBuffer(1, s2);
   SetIndexBuffer(2, s3);
   SetIndexBuffer(3, s4);
   //-----
   SetIndexStyle(0, DRAW_ARROW);
   SetIndexArrow(0, 159);
   SetIndexStyle(1, DRAW_ARROW);
   SetIndexArrow(1, 159);
   SetIndexStyle(2, DRAW_ARROW);
   SetIndexArrow(2, 159);
   SetIndexStyle(3, DRAW_ARROW);
   SetIndexArrow(3, 159);
   //------
   SetIndexStyle(4, DRAW_ARROW);// UP___UP___UP 
   SetIndexArrow(4, 233);
   SetIndexBuffer(4, bullish);
   //-----
   SetIndexStyle(5, DRAW_ARROW);// DOWN____DOWN
   SetIndexArrow(5, 234);       
   SetIndexBuffer(5, bearish);
   //-----
   return(0);
 }
//+------------------------------------------------------------------+
string GetNextTF(int curTF)
 {
   switch(curTF)
    {
      case 1:
        return("5=15#30");
        break;
      case 5:
        return("15=30#60");
        break; 
      case 15:
        return("30=60#240");
        break;
      case 30:
        return("60=240#1440");
        break;
      case 60:
        return("240=1440#10080");
        break;
      case 240:
        return("1440=10080#43200");
        break;        
    }
 }
//+------------------------------------------------------------------+
void AlertDn(double sar)
 {
   int limit;
   int counted_bars=IndicatorCounted();
   if(counted_bars < 0) counted_bars = 0;
   if(counted_bars > 0) counted_bars--;
   limit = Bars - counted_bars;
   //---- 
   for(int i = 0; i < limit ;i++)
    {
      if(sar >= iHigh(Symbol(),0,i))
       {
         if(AlertsEnabled == true && sarUp[i] == 0 && Bars > alertBar)
          {
            Alert("PSAR Going Down on ", Symbol(), " - ", Period(), " min");
            if(AlertsSignal == true) PlaySound("alert.wav");
            alertBar = Bars;
          }
         sarUp[i] = sar;  
         sarDn[i] = 0;
       }
    }
 }
//+------------------------------------------------------------------+ 
void AlertUp(double sar)
 {
   int limit;
   int counted_bars = IndicatorCounted();
   if(counted_bars < 0) counted_bars = 0;
   if(counted_bars > 0) counted_bars--;
   limit = Bars - counted_bars;
   //---- 
   for(int i = 0; i<limit ;i++)
    {
      if(sar <= iLow(Symbol(), 0, i))
       {
         if(AlertsEnabled == true && sarDn[i] == 0 && Bars > alertBar)
          {
            Alert("PSAR Going Up on ",Symbol(), " - ", Period(), " min");
            if(AlertsSignal == true) PlaySound("alert.wav");
            alertBar = Bars;
          }
         sarUp[i] = 0;
         sarDn[i] = sar;
       }
    }
 }
//+------------------------------------------------------------------+ 
int start()
 {
   int limit;
   int counted_bars = IndicatorCounted();
   if(counted_bars < 0) return(-1);
   if(counted_bars > 0) counted_bars--;
   limit = Bars - counted_bars;
   //-----
   string T = GetNextTF(Period());
   int tf1 = StrToDouble(StringSubstr(T, 0, StringFind(T, "=", 0)));
   int tf2 = StrToDouble(StringSubstr(T, StringFind(T, "=", 0) + 1, StringFind(T, "#", 0)));
   int tf3 = StrToDouble(StringSubstr(T, StringFind(T, "#", 0) + 1, StringLen(T)));
   //-----
   for(int i = limit - 1; i >= 0; i--)
    {
         //===============================================         __________________________________________________   sar1  &  TF2  &  TF3  & TF4
         if(TF2 == true && TF3 == true && TF4 == true)
          {
            Comment(Period(), " White", "\n", tf1, " Yellow", "\n", tf2, " Blue", "\n", tf3, " Black");
            s1[i]  = iSAR(NULL, Period(), Step, Maximum, i);
            s2[i]  = iSAR(NULL, tf1, Step, Maximum, i / (tf1 / Period()));
            s3[i]  = iSAR(NULL, tf2, Step, Maximum, i / (tf2 / Period()));
            s4[i]  = iSAR(NULL, tf3, Step, Maximum, i / (tf3 / Period()));
            //============================================================
            if((s1[i] > High[i] && s2[i] > High[i] && s3[i] > High[i] && s4[i + 1] < Low[i + 1] && s4[i] > High[i]) ||
               (s1[i] > High[i] && s2[i] > High[i] && s3[i + 1] < Low[i + 1] && s3[i] > High[i] && s4[i] > High[i]) ||
               (s1[i] > High[i] && s2[i + 1] < Low[i + 1] && s2[i] > High[i] && s3[i] > High[i] && s4[i] > High[i]) || 
               (s1[i + 1] < Low[i + 1] && s1[i] > High[i] && s2[i] > High[i] && s3[i] > High[i] && s4[i] > High[i]))
             {
                bearish[i] = s1[i] + 5 * Point;//       SELL__SELL__SELL
                AlertDn(s1[i]);
             }
            //-----
            if((s1[i] < Low[i] && s2[i] < Low[i] && s3[i] < Low[i] && s4[i + 1] > High[i + 1] && s4[i] < Low[i]) ||
               (s1[i] < Low[i] && s2[i] < Low[i] && s3[i + 1] > High[i + 1] && s3[i] < Low[i] && s4[i] < Low[i]) ||
               (s1[i] < Low[i] && s2[i + 1] > High[i + 1] && s2[i] < Low[i] && s3[i] < Low[i] && s4[i] < Low[i]) ||
               (s1[i + 1] > High[i + 1] && s1[i] < Low[i] && s2[i] < Low[i] && s3[i] < Low[i] && s4[i] < Low[i]))
             {
               bullish[i] =  s1[i] - 5 * Point;//      BUY___BUY___BUY
               AlertUp(s1[i]);
             } 
          }
         //===============================================         __________________________________________________   sar1  &  TF2  &  TF3 
         else if(TF2 == true && TF3 == true && TF4 == false)
          {     
            Comment(Period(), " White", "\n", tf1, " Yellow  ", "\n", tf2, " Blue");
            s1[i]  = iSAR(NULL, Period(), Step, Maximum, i);
            s2[i]  = iSAR(NULL, tf1, Step, Maximum, i / (tf1 / Period()));
            s3[i]  = iSAR(NULL, tf2, Step, Maximum, i / (tf2 / Period()));
            //============================================================
            if((s1[i] > High[i] && s2[i] > High[i] && s3[i + 1] < Low[i + 1] && s3[i] > High[i]) ||
               (s1[i] > High[i] && s2[i + 1] < Low[i + 1] && s2[i] > High[i] && s3[i] > High[i]) || 
               (s1[i + 1] < Low[i + 1] && s1[i] > High[i] && s2[i] > High[i] && s3[i] > High[i]))
             {
               bearish[i] = s1[i] + 5 * Point;//       SELL__SELL__SELL
               AlertDn(s1[i]);
             }
            //-----
            if((s1[i] < Low[i] && s2[i] < Low[i] && s3[i + 1] > High[i + 1] && s3[i] < Low[i]) ||
               (s1[i] < Low[i] && s2[i + 1] > High[i + 1] && s2[i] < Low[i] && s3[i] < Low[i]) ||
               (s1[i + 1] > High[i + 1] && s1[i] < Low[i] && s2[i] < Low[i] && s3[i] < Low[i]))
             {
               bullish[i] =  s1[i] - 5 * Point;//      BUY___BUY___BUY
               AlertUp(s1[i]);
             } 
          }
         //===============================================          __________________________________________________   sar1  &  TF2
         else if(TF2 == true && TF3 == false && TF4 == false)
          {     
            Comment(Period(), " White", "\n", tf1, " Yellow");
            s1[i]  = iSAR(NULL, Period(), Step, Maximum, i);
            s2[i]  = iSAR(NULL, tf1, Step, Maximum, i / (tf1 / Period()));
            //============================================================
            if((s1[i] > High[i] && s2[i + 1] < Low[i + 1] && s2[i] > High[i]) || 
               (s1[i + 1] < Low[i + 1] && s1[i] > High[i] && s2[i] > High[i]))
             {
               bearish[i] = s1[i] + 5 * Point;//       SELL__SELL__SELL
               AlertDn(s1[i]);
             }
            //-----
            if((s1[i] < Low[i] && s2[i + 1] > High[i + 1] && s2[i] < Low[i]) ||
               (s1[i + 1] > High[i + 1] && s1[i] < Low[i] && s2[i] < Low[i]))
             {
               bullish[i] =  s1[i] - 5 * Point;//      BUY___BUY___BUY
               AlertUp(s1[i]);
             }
          }
         //===============================================          __________________________________________________   sar1
         else if(TF2 == false && TF3 == false && TF4 == false)
          {
            Comment(Period(), " White");
            s1[i]  = iSAR(NULL, Period(), Step, Maximum, i);
            //============================================================
            if(s1[i + 1] < Low[i + 1] && s1[i] > High[i])
             {
               bearish[i] = s1[i] + 5 * Point;//       SELL__SELL__SELL
               AlertDn(s1[i]);
             }
            //----- 
            if(s1[i + 1] > High[i + 1] && s1[i] < Low[i])
             {
               bullish[i] =  s1[i] - 5 * Point;//      BUY___BUY___BUY
               AlertUp(s1[i]);
             }
          } 
         ////////////////////////////////////////////////////////////////////////////////////////////////////////////        
    }  
   //==============================================================================================================================================
   return(0);
 }

