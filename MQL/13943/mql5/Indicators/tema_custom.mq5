//+---------------------------------------------------------------------+
//|                                                     TEMA_CUSTOM.mq5 | 
//|                                      Copyright � 2015, Sergey Vrady | 
//|                                  https://www.mql5.com/ru/code/13263 | 
//+---------------------------------------------------------------------+ 
//| ��� ������  ����������  �������  �������� ���� SmoothAlgorithms.mqh |
//| � ����� (����������): �������_������_���������\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright � 2015, Sergey Vrady"
#property link "https://www.mql5.com/ru/code/13263"
//---- ����� ������ ����������
#property version   "1.00"
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
//---- � �������� ����� ����� ���������� ����������� MediumOrchid ����
#property indicator_color1 clrMediumOrchid
//---- ����� ���������� - ����������� ������
#property indicator_style1  STYLE_SOLID
//---- ������� ����� ���������� ����� 2
#property indicator_width1  2
//---- ����������� ����� ����������
#property indicator_label1  "TEMA_CUSTOM"
//+-----------------------------------+
//| �������� ������ CXMA              |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+
//---- ���������� ���������� ������ CXMA �� ����� SmoothAlgorithms.mqh
CXMA XMA1,XMA2,XMA3;
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
   PRICE_SIMPL_,         //Simple Price (OC/2)
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
//| ������� ��������� ����������     |
//+-----------------------------------+
input Smooth_Method MA_Method1=MODE_EMA_; // ����� ���������� ������� �����������
input int Length1=12; // �������  ������� �����������
input int Phase1=15;  // �������� ������� �����������
//---- ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������;
//---- ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input Smooth_Method MA_Method2=MODE_EMA; // ����� ���������� ������� �����������
input int Length2 = 5; // �������  ������� �����������
input int Phase2=15;   // �������� ������� �����������
//---- ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������;
//---- ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input Smooth_Method MA_Method3=MODE_EMA; // ����� ���������� �������� �����������
input int Length3 = 5; // ������� �������� �����������
input int Phase3=15;   // �������� �������� �����������
//---- ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������;
//---- ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input Applied_price_ IPC=PRICE_MEDIAN; // ������� ���������, �� ������� ������������ ������ ����������
input int Shift=0; // ����� ���������� �� ����������� � �����
input int PriceShift=0; // ����� ���������� �� ��������� � �������
//+-----------------------------------+
//---- ���������� ������������� �������, ������� ����� � 
//---- ���������� ����������� � �������� ������������� ������
double IndBuffer[];
//---- ���������� ���������� �������� ������������� ������ ���������� �������
double dPriceShift;
//---- ���������� ������������� ���������� ������ ������� ������
int min_rates_total,StartBars1,StartBars2,StartBars3;
//+------------------------------------------------------------------+   
//| Custom indicator initialization function                         | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   StartBars1=GetStartBars(MA_Method1, Length1, Phase1);
   StartBars2=GetStartBars(MA_Method2, Length2, Phase2);
   StartBars2=GetStartBars(MA_Method3, Length3, Phase3);
   min_rates_total=StartBars1+StartBars2+StartBars3;
//---- ��������� ������� �� ������������ �������� ������� ����������
   XMA1.XMALengthCheck("Length1", Length1);
   XMA2.XMALengthCheck("Length2", Length2);
   XMA3.XMALengthCheck("Length3", Length3);
//---- ��������� ������� �� ������������ �������� ������� ����������
   XMA1.XMAPhaseCheck("Phase1", Phase1, MA_Method1);
   XMA2.XMAPhaseCheck("Phase2", Phase2, MA_Method2);
   XMA3.XMAPhaseCheck("Phase3", Phase3, MA_Method3);
//---- ������������� ������ �� ���������
   dPriceShift=_Point*PriceShift;
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
   string Smooth1=XMA1.GetString_MA_Method(MA_Method1);
   string Smooth2=XMA1.GetString_MA_Method(MA_Method2);
   string Smooth3=XMA1.GetString_MA_Method(MA_Method3);
   StringConcatenate(shortname,"TEMA_CUSTOM(",Length1,", ",Length2,", ",Length3,", ",Smooth1,", ",Smooth2,", ",Smooth3,")");
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
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
//---- ���������� ���������� � ��������� ������  
   double price,x1xma,x2xma,x3xma;
//---- ���������� ������������� ���������� � ��������� ��� ����������� �����
   int first,bar;
//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
      first=0; // ��������� ����� ��� ������� ���� �����
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����
//---- �������� ���� ������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      price=PriceSeries(IPC,bar,open,low,high,close);
      x1xma=XMA1.XMASeries(0,prev_calculated,rates_total,MA_Method1,Phase1,Length1,price,bar,false);
      x2xma=XMA2.XMASeries(StartBars1,prev_calculated,rates_total,MA_Method2,Phase2,Length2,x1xma,bar,false);
      x3xma=XMA3.XMASeries(StartBars2,prev_calculated,rates_total,MA_Method3,Phase3,Length3,x2xma,bar,false);
      IndBuffer[bar]=3*x1xma-3*x2xma+x3xma;
      IndBuffer[bar]+=dPriceShift;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
