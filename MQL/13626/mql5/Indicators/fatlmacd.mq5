//+---------------------------------------------------------------------+ 
//|                                                        FatlMacd.mq5 | 
//|                                  Copyright � 2014, Nikolay Kositsin | 
//|                                 Khabarovsk,   farria@mail.redcom.ru | 
//+---------------------------------------------------------------------+ 
//| ��� ������  ����������  �������  �������� ���� SmoothAlgorithms.mqh |
//| � ����� (����������): �������_������_���������\\MQL5\Include        |
//+---------------------------------------------------------------------+ 
#property copyright "Copyright � 2014, Nikolay Kositsin"
#property link "farria@mail.redcom.ru" 
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window 
//---- ���������� ������������ ������� 2
#property indicator_buffers 2 
//---- ������������ ����� ���� ����������� ����������
#property indicator_plots   1
//+----------------------------------------------+
//| ��������� ��������� ����������               |
//+----------------------------------------------+
//---- ��������� ���������� � ���� ������� �������
#property indicator_type1 DRAW_COLOR_ARROW
//---- � �������� ������ �������������� ����������� ������������
#property indicator_color1 clrMagenta,clrDeepPink,clrGray,clrDodgerBlue,clrOliveDrab
//---- ����� ���������� - ��������
#property indicator_style1 STYLE_SOLID
//---- ������� ����� ���������� ����� 3
#property indicator_width1 3
//---- ����������� ����� ����������
#property indicator_label1 "FatlMacd"
//+----------------------------------------------+
//| ��������� ����������� �������������� ������� |
//+----------------------------------------------+
#property indicator_level1 0
#property indicator_levelcolor clrBlue
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//| �������� ������� ����������                  |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+
//---- ���������� ���������� ������� CFATL, CJJMA � CMomentum �� ����� JJMASeries_Cls.mqh
CXMA XMA1;
CFATL FATL1;
//+----------------------------------------------+
//| ���������� ������������                      |
//+----------------------------------------------+
enum Applied_price_ //��� ���������
  {
   PRICE_CLOSE_ = 1,     //Close
   PRICE_OPEN_,          //Open
   PRICE_HIGH_,          //High
   PRICE_LOW_,           //Low
   PRICE_MEDIAN_,        //Median Price (HL/2)
   PRICE_TYPICAL_,       //Typical Price (HLC/3)
   PRICE_WEIGHTED_,      //Weighted Close (HLCC/4)
   PRICE_SIMPL_,         //Simpl Price (OC/2)
   PRICE_QUARTER_,       //Quarted Price (HLOC/4) 
   PRICE_TRENDFOLLOW0_,  //TrendFollow_1 Price 
   PRICE_TRENDFOLLOW1_,  //TrendFollow_2 Price 
   PRICE_DEMARK_         //Demark Price
  };
//+----------------------------------------------+
/*enum Smooth_Method - ������������ ��������� � ����� SmoothAlgorithms.mqh
  {
   MODE_SMA_,  //SMA
   MODE_EMA_,  //EMA
   MODE_SMMA_, //SMMA
   MODE_LWMA_, //LWMA
   MODE_JJMA,  //JJMA
   MODE_JurX,  //JurX
   MODE_ParMA, //ParMA
   MODE_T3,    //T3
   MODE_VIDYA, //VIDYA
   MODE_AMA,   //AMA
  }; */
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input Smooth_Method XMA_Method=MODE_T3; // ����� ���������� �����������
input int XMA_Lengh = 20; // ������ ����������
input int XMA_Phase= 100; // �������� ���������� ���������� �������
                          // ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������
                          // ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input Applied_price_ AppliedPrice=PRICE_CLOSE_;// ������� ���������
//+----------------------------------------------+
//---- ���������� ����� ���������� ������ ������� ������
int min_rates_total,min_rates_1;
//---- ���������� ������������ ��������, ������� ����� � 
//---- ���������� ������������ � �������� ������������ �������
double IndBuffer[],ColorIndBuffer[];
//+------------------------------------------------------------------+    
//| Custom indicator initialization function                         | 
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_1=39;
   min_rates_total=min_rates_1+XMA1.GetStartBars(XMA_Method,XMA_Lengh,XMA_Phase);
//---- ����������� ������������� ������� IndBuffer � ������������ �����
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(1,ColorIndBuffer,INDICATOR_COLOR_INDEX);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- ��������� ������� �� ������������ �������� ������� ����������
   XMA1.XMALengthCheck("Fast_XMA",XMA_Phase);
//---- ��������� ������� �� ������������ �������� ������� ����������
   XMA1.XMAPhaseCheck("Phase",XMA_Phase,XMA_Method);
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,"FatlMacd");
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//---- ���������� �������������
  }
//+------------------------------------------------------------------+  
//| Custom indicator iteration function                              | 
//+------------------------------------------------------------------+  
int OnCalculate(const int rates_total,    // ���������� ������� � ����� �� ������� ����
                const int prev_calculated,// ���������� ������� � ����� �� ���������� ����
                const datetime &time[],
                const double &open[],
                const double &high[],
                const double &low[],
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//---- �������� ���������� ����� �� ������������� ��� �������
   if(rates_total<min_rates_total) return(0);
//---- ���������� ������������� ����������
   int first,bar;
//---- ���������� ���������� � ��������� ������  
   double price,fatl,macd;
//---- ������������� ���������� � ����� OnCalculate()
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      first=0; // ��������� ����� ��� ������� ���� ����� ������� �����
     }
   else // ��������� ����� ��� ������� ����� �����
     {
      first=prev_calculated-1;
     }
//---- �������� ���� ������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      //---- �������� ������� ���� price
      price=PriceSeries(AppliedPrice,bar,open,low,high,close);
      //---- ������ ������� ���� price � FATLSeries() � �������� fatl
      fatl=FATL1.FATLSeries(0,prev_calculated,rates_total,price,bar,false);
      //---- ������� MACD
      macd=price-fatl;
      //---- ������ macd � XMASeries() ��� ����������
      IndBuffer[bar]=XMA1.XMASeries(min_rates_1,prev_calculated,rates_total,XMA_Method,XMA_Phase,XMA_Lengh,macd,bar,false);
      //---- ������ ����������� ���������� �� ����� ��������
      IndBuffer[bar]/=_Point;
     }
//----
   if(prev_calculated>rates_total || prev_calculated<=0) first++;
//---- �������� ���� ��������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      int clr=2;
      //----
      if(IndBuffer[bar]>0)
        {
         if(IndBuffer[bar]>IndBuffer[bar-1]) clr=4;
         if(IndBuffer[bar]<IndBuffer[bar-1]) clr=3;
        }
      //----
      if(IndBuffer[bar]<0)
        {
         if(IndBuffer[bar]<IndBuffer[bar-1]) clr=0;
         if(IndBuffer[bar]>IndBuffer[bar-1]) clr=1;
        }
      //----
      ColorIndBuffer[bar]=clr;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
