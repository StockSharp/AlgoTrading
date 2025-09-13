//+------------------------------------------------------------------+
//|                                            XMA_Range_Channel.mq5 |
//|                               Copyright � 2015, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
//| ��� ������  ���������� ���� SmoothAlgorithms.mqh                 |
//| ������� �������� � �����: �������_������_���������\MQL5\Include  |
//+------------------------------------------------------------------+
#property copyright "Copyright � 2010, ellizii"
#property link ""
#property description "XMA Ichimoku Channel"
//---- ����� ������ ����������
#property version   "1.02"
//---- ��������� ���������� � ������� ����
#property indicator_chart_window 
//---- ���������� ������������ ������� 9
#property indicator_buffers 9 
//---- ������������ 4 ����������� ����������
#property indicator_plots   4
//+--------------------------------------------+
//| ��������� ��������� ������                 |
//+--------------------------------------------+
//---- ��������� ���������� � ���� ������
#property indicator_type1   DRAW_FILLING
//---- � �������� ����� ����� ���������� ����������� ���� Lavender
#property indicator_color1 clrLavender
//---- ����������� ����� ����������
#property indicator_label1  "XMA Range Channel"
//+--------------------------------------------+
//| ��������� ��������� �������                |
//+--------------------------------------------+
//---- ��������� ������� � ���� �����
#property indicator_type2   DRAW_LINE
#property indicator_type3   DRAW_LINE
//---- ������ ������ �������
#property indicator_color2  clrMediumSeaGreen
#property indicator_color3  clrRed
//---- ������ - ��������������� ������
#property indicator_style2 STYLE_SOLID
#property indicator_style3 STYLE_SOLID
//---- ������� ������� ����� 1
#property indicator_width2  1
#property indicator_width3  1
//---- ����������� ����� �������
#property indicator_label2  "Up Line"
#property indicator_label3  "Down Line"
//+--------------------------------------------+
//| ��������� ��������� ������                 |
//+--------------------------------------------+
//---- � �������� ���������� ������������ ������� �����
#property indicator_type4   DRAW_COLOR_CANDLES
#property indicator_color4   clrMagenta,clrPurple,clrGreen,clrLime
//---- ����������� ����� ����������
#property indicator_label4  "Open;High;Low;Close"
//+--------------------------------------------+
//| �������� ������� ����������                |
//+--------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+--------------------------------------------+
//---- ���������� ���������� ������� CXMA �� ����� SmoothAlgorithms.mqh
CXMA XMA1,XMA2;
//+--------------------------------------------+
//| ���������� ������������                    |
//+--------------------------------------------+
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
   PRICE_TRENDFOLLOW1_,  // TrendFollow_2 Price 
   PRICE_DEMARK_         // Demark Price  
  };
//+--------------------------------------------+
//| ���������� ������������                    |
//+--------------------------------------------+
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
//+--------------------------------------------+
//| ������� ��������� ����������               |
//+--------------------------------------------+ 
input Smooth_Method XMA_Method=MODE_SMA; // ����� ����������
input int XLength=7;  // ������� �����������
input int XPhase=100; // �������� ����������
//---- ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������;
//---- ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ���������� 
input int Shift=0; // ����� ���������� �� ����������� � �����
input int PriceShift=0; // ����� ���������� �� ��������� � �������
//+--------------------------------------------+
//---- ���������� ������������ ��������, ������� ����� � 
//---- ���������� ������������ � �������� ������������ �������
double ExtOpenBuffer[],ExtHighBuffer[],ExtLowBuffer[],ExtCloseBuffer[],ExtColorBuffer[];
double UpIndBuffer[],DnIndBuffer[],UpLineBuffer[],DnLineBuffer[];
//---- ���������� ���������� �������� ������������� ������ �������
double dPriceShift;
//---- ���������� ������������� ���������� ������ ������� ������
int min_rates_total;
//+------------------------------------------------------------------+   
//| XMA_Range_Channel indicator initialization function              | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_total=GetStartBars(XMA_Method,XLength,XPhase);
//---- ��������� ������� �� ������������ �������� ������� ����������
   XMA1.XMALengthCheck("XLength",XLength);
   XMA1.XMAPhaseCheck("XPhase",XPhase,XMA_Method);
//---- ������������� ������ �� ���������
   dPriceShift=_Point*PriceShift;
//---- ����������� ������������ �������� � ������������ ������
   SetIndexBuffer(0,UpIndBuffer,INDICATOR_DATA);
   SetIndexBuffer(1,DnIndBuffer,INDICATOR_DATA);
//---- ������������� ������ ���������� 1 �� �����������
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- ����������� ������������ �������� � ������������ ������
   SetIndexBuffer(2,UpLineBuffer,INDICATOR_DATA);
   SetIndexBuffer(3,DnLineBuffer,INDICATOR_DATA);
//---- ��������� �������, � ������� ���������� ����� ������
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total);
//---- ������ �� ��������� ����������� ������ ��������
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- ������������� ������ ���������� �� �����������
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
   PlotIndexSetInteger(2,PLOT_SHIFT,Shift);
//---- ����������� ������������ �������� � ������������ ������
   SetIndexBuffer(4,ExtOpenBuffer,INDICATOR_DATA);
   SetIndexBuffer(5,ExtHighBuffer,INDICATOR_DATA);
   SetIndexBuffer(6,ExtLowBuffer,INDICATOR_DATA);
   SetIndexBuffer(7,ExtCloseBuffer,INDICATOR_DATA);
//---- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(8,ExtColorBuffer,INDICATOR_COLOR_INDEX);
//---- ��������� �������, � ������� ���������� ����� ������
   PlotIndexSetInteger(3,PLOT_DRAW_BEGIN,min_rates_total);
//---- ������ �� ��������� ����������� ������ ��������
   PlotIndexSetDouble(3,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- ������������� ������ ���������� �� �����������
//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   string Smooth=XMA1.GetString_MA_Method(XMA_Method);
   StringConcatenate(shortname,"XMA_Range_Channel",XLength,", ",XPhase,", ",Smooth,")");
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- ���������� �������������
  }
//+------------------------------------------------------------------+ 
//| XMA_Range_Channel iteration function                             | 
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
//---- ���������� ������������� ���������� � ��������� ��� ����������� �����
   int first,bar;
//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
      first=0; // ��������� ����� ��� ������� ���� �����
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����
//---- �������� ���� ������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      UpLineBuffer[bar]=UpIndBuffer[bar]=XMA1.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,XLength,high[bar],bar,false)+dPriceShift;
      DnLineBuffer[bar]=DnIndBuffer[bar]=XMA2.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,XLength,low[bar],bar,false)+dPriceShift;
     }
//---- �������� ���� ����������� � ����������� ������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      ExtOpenBuffer[bar]=ExtCloseBuffer[bar]=ExtHighBuffer[bar]=ExtLowBuffer[bar]=ExtColorBuffer[bar]=EMPTY_VALUE;
      //----
      if(close[bar]>UpLineBuffer[bar])
        {
         ExtOpenBuffer[bar]=open[bar];
         ExtCloseBuffer[bar]=close[bar];
         ExtHighBuffer[bar]=high[bar];
         ExtLowBuffer[bar]=low[bar];
         if(close[bar]>=open[bar]) ExtColorBuffer[bar]=3;
         else ExtColorBuffer[bar]=2;
        }
      //----
      if(close[bar]<DnLineBuffer[bar])
        {
         ExtOpenBuffer[bar]=open[bar];
         ExtCloseBuffer[bar]=close[bar];
         ExtHighBuffer[bar]=high[bar];
         ExtLowBuffer[bar]=low[bar];
         if(close[bar]<=open[bar]) ExtColorBuffer[bar]=0;
         else ExtColorBuffer[bar]=1;
        }
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
