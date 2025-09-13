//+---------------------------------------------------------------------+
//|                                                            BvsB.mq5 | 
//|                                           Copyright � 2012, BECEMAL | 
//|                                           http://www.becemal.ru/mql | 
//+---------------------------------------------------------------------+ 
//| ��� ������  ����������  �������  �������� ���� SmoothAlgorithms.mqh |
//| � ����� (����������): �������_������_���������\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright � 2012, BECEMAL"
#property link "http://www.becemal.ru/mql"
//--- ����� ������ ����������
#property version   "1.01"
//--- ��������� ���������� � ��������� ����
#property indicator_separate_window
//--- ���������� ������������ ������� 2
#property indicator_buffers 2 
//--- ������������ ����� ���� ����������� ����������
#property indicator_plots   1
//+----------------------------------------------+
//| ��������� ��������� ����������               |
//+----------------------------------------------+
//--- ��������� ���������� � ���� �������� ������
#property indicator_type1   DRAW_FILLING
//--- � �������� ������ ���������� ������������
#property indicator_color1  clrPaleGreen,clrDeepPink
//--- ����������� ����� ����������
#property indicator_label1  "Buy;Sell"
//+----------------------------------------------+
//| ��������� ����������� �������������� ������� |
//+----------------------------------------------+
#property indicator_level1 0.0
#property indicator_levelcolor clrBlue
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//|  �������� ������ CXMA                        |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+
//--- ���������� ���������� ������ CBvsB �� ����� SmoothAlgorithms.mqh
CXMA XMA1;
//+----------------------------------------------+
//|  ���������� ������������                     |
//+----------------------------------------------+
enum Applied_price_      //��� ���������
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
//|  ���������� ������������                     |
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
input Smooth_Method BvsB_Method=MODE_SMA;  // ����� ����������
input int XLength=12;                      // ������� �����������                  
input int XPhase=15;                       // �������� �����������
//--- XPhase: ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������;
//--- XPhase: ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input Applied_price_ IPC=PRICE_CLOSE;      // ������� ���������
input int Shift=0;                         // ����� ���������� �� ����������� � �����
//+----------------------------------------------+
//--- ���������� ������������ ��������, ������� �����
//--- � ���������� ������������ � �������� ������������ �������
double ExtABuffer[];
double ExtBBuffer[];
//--- ���������� ������������� ���������� ������ ������� ������
int min_rates_total;
//+------------------------------------------------------------------+   
//| BvsB indicator initialization function                           | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//--- ������������� ���������� ������ ������� ������
   min_rates_total=XMA1.GetStartBars(BvsB_Method,XLength,XPhase);
//--- ��������� ������� �� ������������ �������� ������� ����������
   XMA1.XMALengthCheck("XLength",XLength);
   XMA1.XMAPhaseCheck("XPhase",XPhase,BvsB_Method);
//--- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,ExtABuffer,INDICATOR_DATA);
//--- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- ������������� ������ ���������� �� ����������� �� InpKijun
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//--- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(1,ExtBBuffer,INDICATOR_DATA);
//--- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//--- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- ������������� ������ ���������� �� ����������� �� -InpKijun
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//--- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   string Smooth1=XMA1.GetString_MA_Method(BvsB_Method);
   StringConcatenate(shortname,"BvsB(",XLength,", ",Smooth1,")");
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//--- ���������� �������������
  }
//+------------------------------------------------------------------+ 
//| BvsB iteration function                                          | 
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
//--- �������� ���������� ����� �� ������������� ��� �������
   if(rates_total<min_rates_total) return(0);
//--- ���������� ���������� � ��������� ������  
   double price,x1xma;
//--- ���������� ������������� ���������� � ��������� ��� ������������ �����
   int first,bar;
//--- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
      first=0; // ��������� ����� ��� ������� ���� �����
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����
//--- �������� ���� ������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      price=PriceSeries(IPC,bar,open,low,high,close);
      x1xma=XMA1.XMASeries(0,prev_calculated,rates_total,BvsB_Method,XPhase,XLength,price,bar,false);        
      ExtABuffer[bar]=(high[bar]-x1xma)/_Point;
      ExtBBuffer[bar]=(x1xma-low[bar])/_Point;
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+