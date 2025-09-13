//+---------------------------------------------------------------------+
//|                                               ColorZerolagX10MA.mq5 | 
//|                                  Copyright � 2015, Nikolay Kositsin | 
//|                                 Khabarovsk,   farria@mail.redcom.ru | 
//+---------------------------------------------------------------------+ 
//| ��� ������  ����������  �������  �������� ���� SmoothAlgorithms.mqh |
//| � ����� (����������): �������_������_���������\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright � 2015, Nikolay Kositsin"
#property link "farria@mail.redcom.ru"
//---- ����� ������ ����������
#property version   "1.02"
//---- ��������� ���������� � ������� ����
#property indicator_chart_window 
//---- ���������� ������������ �������
#property indicator_buffers 2 
//---- ������������ ����� ���� ����������� ����������
#property indicator_plots   1
//+-----------------------------------+
//| ��������� ��������� ����������    |
//+-----------------------------------+
//---- ��������� ���������� � ���� ������������ �����
#property indicator_type1   DRAW_COLOR_LINE
//---- � �������� ������ ����������� ����� ������������
#property indicator_color1  clrRed,clrGray,clrTeal
//---- ����� ���������� - ����������� ������
#property indicator_style1  STYLE_SOLID
//---- ������� ����� ���������� ����� 2
#property indicator_width1  2
//---- ����������� ����� ����������
#property indicator_label1  "ColorZerolagX10MA"
//+-----------------------------------+
//| �������� ������ CXMA              |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+
//---- ���������� ���������� ������ CXMA �� ����� SmoothAlgorithms.mqh
CXMA XMA[11];
//+-----------------------------------+
//| ���������� ������������           |
//+-----------------------------------+
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
//+-----------------------------------+
//| ���������� ������������           |
//+-----------------------------------+
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
//+-----------------------------------+
//| ������� ��������� ����������      |
//+-----------------------------------+
input Smooth_Method XMA_Method=MODE_JJMA; // ����� ���������� 
input uint Length1=3;   // ������� ���������� 1
input double Factor1=0.1;
input uint Length2=5;   // ������� ���������� 2
input double Factor2=0.1;
input uint Length3=7;   // ������� ���������� 3
input double Factor3=0.1;
input uint Length4=9;   // ������� ���������� 4 
input double Factor4=0.1;
input uint Length5=11;  // ������� ���������� 5 
input double Factor5=0.1;
input uint Length6=13;  // ������� ���������� 6
input double Factor6=0.1;
input uint Length7=15;  // ������� ���������� 7 
input double Factor7=0.1;
input uint Length8=17;  // ������� ���������� 8
input double Factor8=0.1;
input uint Length9=21;  // ������� ���������� 9
input double Factor9=0.1;
input uint Length10=23; // ������� ���������� 10 
input double Factor10=0.1;
input int XPhase=15;    // �������� ����������
                        // ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������
                        // ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input Smooth_Method SmoothMethod=MODE_SMA; // ����� ����������� 
input uint Smooth=3;      // ������� �����������
input int SmoothPhase=15; // �������� �����������
                          // ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������
                          // ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input Applied_price_ IPC=PRICE_CLOSE; // ������� ���������
input int Shift=0;      // ����� ���������� �� ����������� � �����
input int PriceShift=0; // ����� ���������� �� ��������� � �������
//+-----------------------------------+
//---- ���������� ������������ ��������, ������� ����� � 
//---- ���������� ������������ � �������� ������������ �������
double IndBuffer[];
double ColorIndBuffer[];
//---- ���������� ���������� �������� ������������� ������ ���������� �������
double dPriceShift;
//---- ���������� ������������� ���������� ������ ������� ������
int min_rates_total,min_rates_;
//+------------------------------------------------------------------+   
//| XMA indicator initialization function                            | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   int min_rates[10];
   min_rates[0]=GetStartBars(XMA_Method,Length1,XPhase);
   min_rates[1]=GetStartBars(XMA_Method,Length2,XPhase);
   min_rates[2]=GetStartBars(XMA_Method,Length3,XPhase);
   min_rates[3]=GetStartBars(XMA_Method,Length4,XPhase);
   min_rates[4]=GetStartBars(XMA_Method,Length5,XPhase);
   min_rates[5]=GetStartBars(XMA_Method,Length6,XPhase);
   min_rates[6]=GetStartBars(XMA_Method,Length7,XPhase);
   min_rates[7]=GetStartBars(XMA_Method,Length8,XPhase);
   min_rates[8]=GetStartBars(XMA_Method,Length9,XPhase);
   min_rates[9]=GetStartBars(XMA_Method,Length10,XPhase);
   min_rates_=min_rates[ArrayMaximum(min_rates)];
   min_rates_total=min_rates_+GetStartBars(SmoothMethod,Smooth,SmoothPhase);
//---- ������������� ������ �� ���������
   dPriceShift=_Point*PriceShift;
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(1,ColorIndBuffer,INDICATOR_COLOR_INDEX);
//---- ������������� ������ ���������� 1 �� �����������
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);
//---- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,"ColorZerolagX10MA");
//---- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- ���������� �������������
  }
//+------------------------------------------------------------------+ 
//| XMA iteration function                                           | 
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
//---- ���������� ���������� � ��������� ������  
   double price,xma[10],zlagxma;
//---- ���������� ������������� ���������� � ��������� ��� ����������� �����
   int first,bar;
//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
      first=0; // ��������� ����� ��� ������� ���� �����
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����
//---- �������� ���� ������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      //---- ����� ������� PriceSeries ��� ��������� ������� ���� price
      price=PriceSeries(IPC,bar,open,low,high,close);
      xma[0]=XMA[0].XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,Length1,price,bar,false);
      xma[1]=XMA[1].XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,Length2,price,bar,false);
      xma[2]=XMA[2].XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,Length3,price,bar,false);
      xma[3]=XMA[3].XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,Length4,price,bar,false);
      xma[4]=XMA[4].XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,Length5,price,bar,false);
      xma[5]=XMA[5].XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,Length6,price,bar,false);
      xma[6]=XMA[6].XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,Length7,price,bar,false);
      xma[7]=XMA[7].XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,Length8,price,bar,false);
      xma[8]=XMA[8].XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,Length9,price,bar,false);
      xma[9]=XMA[9].XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,Length10,price,bar,false);
      xma[9]=XMA[9].XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,Length10,price,bar,false);
      zlagxma=Factor1*xma[0]+Factor2*xma[1]+Factor3*xma[2]+Factor4*xma[3]+Factor5*xma[4]
              +Factor6*xma[5]+Factor7*xma[6]+Factor8*xma[7]+Factor9*xma[8]+Factor10*xma[9];
      IndBuffer[bar]=XMA[10].XMASeries(min_rates_,prev_calculated,rates_total,SmoothMethod,SmoothPhase,Smooth,zlagxma,bar,false);
      IndBuffer[bar]+=dPriceShift;
     }
//---- ������������� �������� ���������� first
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
      first=min_rates_total; // ��������� ����� ��� ������� ���� �����
//---- �������� ���� ��������� ���������� �����
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      ColorIndBuffer[bar]=1;
      if(IndBuffer[bar-1]<IndBuffer[bar]) ColorIndBuffer[bar]=2;
      if(IndBuffer[bar-1]>IndBuffer[bar]) ColorIndBuffer[bar]=0;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
