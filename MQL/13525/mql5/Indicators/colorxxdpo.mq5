//+---------------------------------------------------------------------+
//|                                                      ColorXXDPO.mq5 | 
//|                                Copyright � 2011,   Nikolay Kositsin | 
//|                                 Khabarovsk,   farria@mail.redcom.ru | 
//+---------------------------------------------------------------------+ 
//| ��� ������  ����������  �������  �������� ���� SmoothAlgorithms.mqh |
//| � ����� (����������): �������_������_���������\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright � 2011, Nikolay Kositsin"
#property link "farria@mail.redcom.ru"
//---- ����� ������ ����������
#property version   "1.01"
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window
//---- ���������� ������������ �������
#property indicator_buffers 2 
//---- ������������ ����� ���� ����������� ����������
#property indicator_plots   1
//+----------------------------------------------+
//| ��������� ��������� ����������               |
//+----------------------------------------------+
//---- ��������� ���������� � ���� �������������� �����������
#property indicator_type1 DRAW_COLOR_HISTOGRAM
//---- � �������� ������ �������������� ����������� ������������
#property indicator_color1 clrGray,clrDodgerBlue,clrBlue,clrMagenta,clrMediumOrchid
//---- ����� ���������� - ����������� ������
#property indicator_style1  STYLE_SOLID
//---- ������� ����� ���������� ����� 3
#property indicator_width1  3
//---- ����������� ����� ����������
#property indicator_label1  "XXDPO"
//+----------------------------------------------+
//| ��������� ����������� �������������� ������� |
//+----------------------------------------------+
#property indicator_level1 0.0
#property indicator_levelcolor clrMagenta
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//| �������� ������ CXMA                         |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+
//---- ���������� ���������� ������ CXMA �� ����� SmoothAlgorithms.mqh
CXMA XMA1,XMA2;
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
   PRICE_TRENDFOLLOW1_   //TrendFollow_2 Price 
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
input Smooth_Method MA_Method1=MODE_T3; // ����� ���������� DPO
input int Length1=21; // �������  ���������� DPO                  
input int Phase1=15;  // �������� ���������� DPO
                      // ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������
                      // ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input Smooth_Method MA_Method2=MODE_JJMA; // ����� ����������� ����������
input int Length2= 5; // ������� ����������� ����������
input int Phase2=100; // �������� ����������� ����������
                      // ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������
                      // ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input Applied_price_ IPC=PRICE_CLOSE; // ������� ���������
/* , �� ������� ������������ ������ ���������� ( 1-CLOSE, 2-OPEN, 3-HIGH, 4-LOW, 
  5-MEDIAN, 6-TYPICAL, 7-WEIGHTED, 8-SIMPL, 9-QUARTER, 10-TRENDFOLLOW, 11-0.5 * TRENDFOLLOW.) */
input int Shift=0; // ����� ���������� �� ����������� � �����
//+----------------------------------------------+
//---- ���������� ������������ ��������, ������� ����� � 
//---- ���������� ������������ � �������� ������������ �������
double XXDPO[],ColorXXDPO[];
//---- ���������� ������������� ���������� ������ ������� ������
int StartBars,StartBars1,StartBars2;
//+------------------------------------------------------------------+   
//| XXDPO indicator initialization function                          | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   StartBars1=XMA1.GetStartBars(MA_Method1, Length1, Phase1);
   StartBars2=XMA2.GetStartBars(MA_Method2, Length2, Phase2);
   StartBars=StartBars1+StartBars2;
//---- ��������� ������� �� ������������ �������� ������� ����������
   XMA1.XMALengthCheck("Length1", Length1);
   XMA2.XMALengthCheck("Length2", Length2);
//---- ��������� ������� �� ������������ �������� ������� ����������
   XMA1.XMAPhaseCheck("Phase1", Phase1, MA_Method1);
   XMA2.XMAPhaseCheck("Phase2", Phase2, MA_Method2);
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,XXDPO,INDICATOR_DATA);
//---- ������������� ������ ���������� 1 �� �����������
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,StartBars);
//--- �������� ����� ��� ����������� � DataWindow
   PlotIndexSetString(0,PLOT_LABEL,"XXDPO");
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0);
//---- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(1,ColorXXDPO,INDICATOR_COLOR_INDEX);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,StartBars+1);
//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   string Smooth1=XMA1.GetString_MA_Method(MA_Method1);
   string Smooth2=XMA1.GetString_MA_Method(MA_Method2);
   StringConcatenate(shortname,"XXDPO(",Length1,", ",Length2,", ",Smooth1,", ",Smooth2,")");
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- ���������� �������������
  }
//+------------------------------------------------------------------+ 
//| XXDPO iteration function                                         | 
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
   if(rates_total<StartBars) return(0);
//---- ���������� ���������� � ��������� ������  
   double price_,x1xma,xdpo,xxdpo;
//---- ���������� ������������� ���������� � ��������� ��� ����������� �����
   int first,bar;
//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
      first=0; // ��������� ����� ��� ������� ���� �����
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����
//---- �������� ���� ������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      //---- ����� ������� PriceSeries ��� ��������� ������� ���� price_
      price_=PriceSeries(IPC,bar,open,low,high,close);
      //---- ��� ������ ������� XMASeries
      //---- �� ������ ������ �������� begin �������� �� StartBars1 �. �. ��� ��������� XMA �����������
      x1xma= XMA1.XMASeries(0,prev_calculated,rates_total,MA_Method1,Phase1,Length1,price_,bar,false);
      xdpo = price_-x1xma;
      xxdpo= XMA2.XMASeries(StartBars1,prev_calculated,rates_total,MA_Method2,Phase2,Length2,xdpo,bar,false);
      //----       
      XXDPO[bar]=xxdpo;
     }
//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
      first=StartBars+1;
//---- �������� ���� ��������� ���������� XXDPO
   for(bar=first; bar<rates_total; bar++)
     {
      ColorXXDPO[bar]=0;

      if(XXDPO[bar]>0)
        {
         if(XXDPO[bar]>XXDPO[bar-1]) ColorXXDPO[bar]=1;
         if(XXDPO[bar]<XXDPO[bar-1]) ColorXXDPO[bar]=2;
        }
      //----
      if(XXDPO[bar]<0)
        {
         if(XXDPO[bar]<XXDPO[bar-1]) ColorXXDPO[bar]=3;
         if(XXDPO[bar]>XXDPO[bar-1]) ColorXXDPO[bar]=4;
        }
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
