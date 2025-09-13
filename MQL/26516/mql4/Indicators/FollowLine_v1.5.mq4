//===================================================================================================================================================//
#property copyright   "Copyright 2014-2019, Nikolaos Pantzos"
#property link        "https://www.mql5.com/en/users/pannik"
#property version     "1.5"
#property description "Follow Line Indicator"
#property strict
//===================================================================================================================================================//
enum Arrows{Hide_Arrows,Simple_Arrows,Open_Cose_Median,High_Low_Open_Close};
//===================================================================================================================================================//
#property indicator_chart_window
#property indicator_buffers 4
#property indicator_color1 clrDeepSkyBlue
#property indicator_color2 clrCrimson
#property indicator_color3 clrSpringGreen
#property indicator_color4 clrYellow
#property indicator_width1 2
#property indicator_width2 2
#property indicator_width3 2
#property indicator_width4 2
//===================================================================================================================================================//
input int    BarsCount     = 10000;
input int    BBperiod      = 21;
input double BBdeviations  = 1;
input int    MAperiod      = 21;
input int    ATRperiod     = 5;
input bool   UseATRfilter  = FALSE;
input bool   AlertON       = TRUE;
input Arrows TypeOfArrows  = Simple_Arrows;
//===================================================================================================================================================//
double TrendLine[];
double iTrend[];
double TrendUp[];
double TrendDown[];
double UpArrows[];
double DownArrows[];
double BB_Upper=0;
double BB_Lower=0;
double MA_High=0;
double MA_Open=0;
double MA_Close=0;
double MA_Low=0;
double MA_Median=0;
double Diff_LOC=0;
double Diff_HOC=0;
bool AlertBuy=TRUE;
bool AlertSell=TRUE;
bool ShowUpArrow=TRUE;
bool ShowDnArrow=TRUE;
int AlertSignal=0;
//===================================================================================================================================================//
int OnInit(void)
  {
//-----------------------------------------------------------------------------------
   IndicatorShortName(WindowExpertName());
//-----------------------------------------------------------------------------------
   IndicatorDigits(Digits);
   IndicatorBuffers(7);
//---
   SetIndexStyle(0,DRAW_LINE);
   SetIndexBuffer(0,TrendUp);
   SetIndexLabel(0,"Trend Up");
//---
   SetIndexStyle(1,DRAW_LINE);
   SetIndexBuffer(1,TrendDown);
   SetIndexLabel(1,"Trend Down");
//---
   SetIndexStyle(2,DRAW_ARROW);
   SetIndexArrow(2,233);
   SetIndexBuffer(2,UpArrows);
   SetIndexLabel(2,"Arrow Up");
//---
   SetIndexStyle(3,DRAW_ARROW);
   SetIndexArrow(3,234);
   SetIndexBuffer(3,DownArrows);
   SetIndexLabel(3,"Arrow Down");
//---
   SetIndexBuffer(4,TrendLine);
   SetIndexBuffer(5,iTrend);
//-----------------------------------------------------------------------------------
   return(INIT_SUCCEEDED);
//-----------------------------------------------------------------------------------
  }
//====================================================================================================================================================//
void OnDeinit(const int reason)
  {
//--------------------------------------------------------------------------------
   Comment("");
//--------------------------------------------------------------------------------
  }
//===================================================================================================================================================//
int OnCalculate(const int rates_total,
                const int prev_calculated,
                const datetime &time[],
                const double &open[],
                const double &high[],
                const double &low[],
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//-----------------------------------------------------------------------------------
   int i=0;
   int Limit=0;
   int BB_Signal=0;
   int MA_Signal=0;
   int CountedBars=0;
//-----------------------------------------------------------------------------------
   CountedBars=BarsCount;//IndicatorCounted();
   if(CountedBars>Bars-1) CountedBars=Bars-1;
   if(CountedBars<0) return(-1);
   if(CountedBars>0) CountedBars--;
   Limit=CountedBars;
//-----------------------------------------------------------------------------------
   for(i=Limit-1; i>=0; i--)
     {
      BB_Upper=iBands(NULL,0,BBperiod,BBdeviations,0,PRICE_CLOSE,MODE_UPPER,i+1);
      BB_Lower=iBands(NULL,0,BBperiod,BBdeviations,0,PRICE_CLOSE,MODE_LOWER,i+1);
      //-----------------------------------------------------------------------------------
      if(close[i]>BB_Upper) BB_Signal=1;
      if(close[i]<BB_Lower) BB_Signal=-1;
      //-----------------------------------------------------------------------------------
      if(BB_Signal>0)
        {
         if(UseATRfilter==true) TrendLine[i]=NormalizeDouble(low[i]-iATR(NULL,0,ATRperiod,i),Digits);
         if(UseATRfilter==false) TrendLine[i]=NormalizeDouble(low[i],Digits);
         if(TrendLine[i]<TrendLine[i+1]) TrendLine[i]=TrendLine[i+1];
        }
      //---
      if(BB_Signal<0)
        {
         if(UseATRfilter==true) TrendLine[i]=NormalizeDouble(high[i]+iATR(NULL,0,ATRperiod,i),Digits);
         if(UseATRfilter==false) TrendLine[i]=NormalizeDouble(high[i],Digits);
         if(TrendLine[i]>TrendLine[i+1]) TrendLine[i]=TrendLine[i+1];
        }
      //---
     }
//-----------------------------------------------------------------------------------
   for(i=Limit-2; i>=0; i--)
     {
      iTrend[i]=iTrend[i+1];
      if(TrendLine[i]>TrendLine[i+1]) iTrend[i]=1;
      if(TrendLine[i]<TrendLine[i+1]) iTrend[i]=-1;
      //-------------------------------------------------
      if(TypeOfArrows>1)
        {
         MA_High=iMA(NULL,0,MAperiod,0,MODE_SMA,PRICE_HIGH,i);
         MA_Open=iMA(NULL,0,MAperiod,0,MODE_SMA,PRICE_OPEN,i);
         MA_Close=iMA(NULL,0,MAperiod,0,MODE_SMA,PRICE_CLOSE,i);
         MA_Low=iMA(NULL,0,MAperiod,0,MODE_SMA,PRICE_LOW,i);
         MA_Median=iMA(NULL,0,MAperiod,0,MODE_SMA,PRICE_MEDIAN,i);
         //-------------------------------------------------
         Diff_HOC=NormalizeDouble((MA_High-MA_Open)+(MA_High-MA_Close),Digits);
         Diff_LOC=NormalizeDouble((MA_Open-MA_Low)+(MA_Close-MA_Low),Digits);
         //-------------------------------------------------
         if(TypeOfArrows==2)
           {
            if((MA_Close>MA_Open)&&(MA_Median<MA_Close)&&(MA_Median>MA_Open)) MA_Signal=1;
            if((MA_Close<MA_Open)&&(MA_Median>MA_Close)&&(MA_Median<MA_Open)) MA_Signal=-1;
           }
         if(TypeOfArrows==3)
           {
            if((MA_Close>MA_Open)&&(Diff_HOC>Diff_LOC)) MA_Signal=1;
            if((MA_Close<MA_Open)&&(Diff_HOC<Diff_LOC)) MA_Signal=-1;
           }
         //-------------------------------------------------
         if(iTrend[i]<0) ShowUpArrow=true;
         if(iTrend[i]>0) ShowDnArrow=true;
        }
      //-------------------------------------------------
      if(iTrend[i]>0)
        {
         TrendUp[i]=TrendLine[i];
         if(((iTrend[i+1]<0) && (TypeOfArrows==1)) || (TypeOfArrows>1))
           {
            TrendUp[i+1]=TrendLine[i+1];
            //---Arrows UP
            if((ShowUpArrow==true) && (TypeOfArrows!=0) && ((MA_Signal>0) || (TypeOfArrows==1)))
              {
               UpArrows[i+1]=NormalizeDouble(TrendLine[i+1]-100*Point,Digits);
               ShowUpArrow=false;
               ShowDnArrow=true;
              }
            //---
           }
         TrendDown[i]=EMPTY_VALUE;
         DownArrows[i]=EMPTY_VALUE;
        }
      //-------------------------------------------------
      if(iTrend[i]<0)
        {
         TrendDown[i]=TrendLine[i];
         if(((iTrend[i+1]>0) && (TypeOfArrows==1)) || (TypeOfArrows>1))
           {
            TrendDown[i+1]=TrendLine[i+1];
            //---Arrows DN
            if((ShowDnArrow==true) && (TypeOfArrows!=0) && ((MA_Signal<0) || (TypeOfArrows==1)))
              {
               DownArrows[i+1]=NormalizeDouble(TrendLine[i+1]+100*Point,Digits);
               ShowUpArrow=true;
               ShowDnArrow=false;
              }
           }
         TrendUp[i]=EMPTY_VALUE;
         UpArrows[i]=EMPTY_VALUE;
        }
      //---
     }
//-----------------------------------------------------------------------------------
//Pop up alerts
   if(AlertON==TRUE)
     {
      if((AlertBuy==true) && ((iTrend[1]>0)) && ((iTrend[2]<0)))
        {
         AlertSignal=1;
         AlertBuy=FALSE;
         AlertSell=TRUE;
        }
      //---
      if((AlertSell==true) && ((iTrend[1]<0)) && ((iTrend[2]>0)))
        {
         AlertSignal=-1;
         AlertBuy=TRUE;
         AlertSell=FALSE;
        }
      //-----------------------------------------------------------------------------------
      if(AlertSignal>0)
        {
         Alert(WindowExpertName()+" => Buy signal @ "+DoubleToStr(close[0],Digits)+" "+Symbol()+", Time: "+TimeToString(TimeCurrent())+", "+DoubleToStr(Period(),0)+" Minutes Chart");
         AlertSignal=0;
        }
      if(AlertSignal<0)
        {
         Alert(WindowExpertName()+" => Sell signal @ "+DoubleToStr(close[0],Digits)+" "+Symbol()+", Time: "+TimeToString(TimeCurrent())+", "+DoubleToStr(Period(),0)+" Minutes Chart");
         AlertSignal=0;
        }
     }
//-----------------------------------------------------------------------------------
   WindowRedraw();
//-----------------------------------------------------------------------------------
   return(rates_total);
//-----------------------------------------------------------------------------------
  }
//===================================================================================================================================================//
