//+------------------------------------------------------------------+
//|                                                  MA_Rounding.mq5 | 
//|                                      Copyright � 2009, BACKSPACE | 
//|                                                                  | 
//+------------------------------------------------------------------+
#property copyright "Copyright � 2009, BACKSPACE"
#property link ""
//---- ����� ������ ����������
#property version   "1.01"
//---- ��������� ���������� � ������� ����
#property indicator_chart_window 
//---- ���������� ������������ �������
#property indicator_buffers 1 
//---- ������������ ����� ���� ����������� ����������
#property indicator_plots   1
//+-----------------------------------+
//| ��������� ��������� ����������    |
//+-----------------------------------+
//---- ��������� ���������� � ���� �����
#property indicator_type1   DRAW_LINE
//---- � �������� ����� ����� ���������� ����������� DarkViolet ����
#property indicator_color1 clrDarkViolet
//---- ����� ���������� - ����������� ������
#property indicator_style1  STYLE_SOLID
//---- ������� ����� ���������� ����� 1
#property indicator_width1  1
//---- ����������� ����� ����������
#property indicator_label1  "MA Rounding"
//+-----------------------------------+
//| �������� ������ CXMA              |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+
//---- ���������� ���������� ������ CXMA �� ����� SmoothAlgorithms.mqh
CXMA XMA1;
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
input Smooth_Method XMA_Method=MODE_SMA; // ����� ����������
input int XLength=12; // ������� �����������
input int XPhase=15;  // �������� �����������
//--- ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������;
//--- ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input Applied_price_ IPC=PRICE_CLOSE;//������� ���������
input uint MaRound=50; // ����������� ����������
input int Shift=0;     // ����� ���������� �� ����������� � �����
//+-----------------------------------+
//---- ���������� ������������� �������, ������� ����� � 
//---- ���������� ����������� � �������� ������������� ������
double IndBuffer[];
//---- ���������� ���������� �������� ������������� ������
double MaRo;
//---- ���������� ������������� ���������� ������ ������� ������
int min_rates_total;
//+------------------------------------------------------------------+   
//| XMA indicator initialization function                            | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_total=XMA1.GetStartBars(XMA_Method,XLength,XPhase)+2;
//---- ��������� ������� �� ������������ �������� ������� ����������
   XMA1.XMALengthCheck("XLength", XLength);
   XMA1.XMAPhaseCheck("XPhase", XPhase, XMA_Method);
//---- ������������� ������ �� ���������
   MaRo=_Point*MaRound;
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- ������������� ������ ���������� 1 �� �����������
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0);
//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   string Smooth1=XMA1.GetString_MA_Method(XMA_Method);
   StringConcatenate(shortname,"MA Rounding(",XLength,", ",Smooth1,")");
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
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
   double price_,MovAve0,MovAle0,res0,res1;
//---- ���������� ������������� ����������
   int first,bar;
//---- ���������� ����������� ����������  
   static double MovAle1,MovAve1;
//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
     {
      first=1; // ��������� ����� ��� ������� ���� �����
      MovAve1=PriceSeries(IPC,first,open,low,high,close);
      MovAle1=0;
     }
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����
//---- �������� ���� ������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      //---- ����� ������� PriceSeries ��� ��������� ������� ���� price_
      price_=PriceSeries(IPC,bar,open,low,high,close);
      //----
      MovAve0=XMA1.XMASeries(1,prev_calculated,rates_total,XMA_Method,XPhase,XLength,price_,bar,false);
      //----
      res1=IndBuffer[bar-1];
      //----
      if(MovAve0>MovAve1+MaRo
         || MovAve0<MovAve1-MaRo
         || MovAve0>res1+MaRo
         || MovAve0<res1-MaRo
         || (MovAve0>res1 && MovAle1==+1)
         || (MovAve0<res1 && MovAle1==-1))
         IndBuffer[bar]=MovAve0;
      else IndBuffer[bar]=res1;
      //----
      MovAle0=0;
      res0=IndBuffer[bar];
      if(res0<res1) MovAle0 =-1;
      if(res0>res1) MovAle0 =+1;
      if(res0==res1) MovAle0=MovAle1;
      //--- �������� ������� � ��������� �������  
      if(bar<rates_total-1)
        {
         MovAle1=MovAle0;
         MovAve1=MovAve0;
        }
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
