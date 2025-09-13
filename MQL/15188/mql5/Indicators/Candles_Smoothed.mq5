//+---------------------------------------------------------------------+
//|                                                Candles_Smoothed.mq5 |
//|                                Copyright � 2011,   Nikolay Kositsin | 
//|                                 Khabarovsk,   farria@mail.redcom.ru | 
//+---------------------------------------------------------------------+
//| ��� ������  ����������  �������  �������� ���� SmoothAlgorithms.mqh |
//| � ����� (����������): terminal_data_folder\MQL5\Include             |
//+---------------------------------------------------------------------+
#property copyright "Copyright � 2011, Nikolay Kositsin"
#property link "farria@mail.redcom.ru"
#property description "Smoothed Candles"
//---- ����� ������ ����������
#property version   "1.00"
//+----------------------------------------------+
//|  ��������� ��������� ����������              |
//+----------------------------------------------+
//---- ��������� ���������� � ������� ����
#property indicator_chart_window 
//---- ��� ������� � ��������� ���������� ������������ ���� �������
#property indicator_buffers 5
//---- ������������ ����� ���� ����������� ����������
#property indicator_plots   1
//---- � �������� ������� ����������� ������������ ������� �����
#property indicator_type1   DRAW_COLOR_CANDLES
#property indicator_color1  SlateBlue, Magenta
//---- ����������� ����� ����������
#property indicator_label1  "Smoothed Candles Open; Smoothed Candles High; Smoothed Candles Low; Smoothed Candles Close"
//+-----------------------------------+
//|  �������� ������� ����������      |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+
//---- ���������� ���������� ������ CXMA �� ����� SmoothAlgorithms.mqh
CXMA XMAO,XMAL,XMAH,XMAC;
//+-----------------------------------+
//|  ���������� ������������          |
//+-----------------------------------+
/*enum Smooth_Method - ������������ ��������� � ����� SmoothAlgorithms.mqh
  {
   MODE_SMA_,  // SMA
   MODE_EMA_,  // EMA
   MODE_SMMA_, // SMMA
   MODE_LWMA_, // LWMA
   MODE_JJMA,  // JJMA
   MODE_JurX,  // JurX
   MODE_ParMA, // ParMA
   MODE_T3,    // T3
   MODE_VIDYA, // VIDYA
   MODE_AMA,   // AMA
  }; */
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input Smooth_Method MA_SMethod=MODE_LWMA_; // ����� ����������
input int MA_Length=30;                    // ������� ����������                    
input int MA_Phase=100;                    // �������� ����������
                                           // ��� JJMA ������������ � �������� -100 ... +100,
                                           // ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
//+----------------------------------------------+
//---- ���������� ������������ ��������, ������� � ���������� 
//---- ����� ������������ � �������� ������������ �������
double ExtOpenBuffer[];
double ExtHighBuffer[];
double ExtLowBuffer[];
double ExtCloseBuffer[];
double ExtColorBuffer[];
//---
int StartBars;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
  {
//---- ������������� ���������� ���������� 
   StartBars=XMAO.GetStartBars(MA_SMethod,MA_Length,MA_Phase)+1;

//---- ��������� ������� �� ������������ �������� ������� ����������
   XMAO.XMALengthCheck("Length", MA_Length);
   XMAO.XMAPhaseCheck("Phase", MA_Phase, MA_SMethod);

//---- ����������� ������������ �������� � ������������ ������
   SetIndexBuffer(0,ExtOpenBuffer,INDICATOR_DATA);
   SetIndexBuffer(1,ExtHighBuffer,INDICATOR_DATA);
   SetIndexBuffer(2,ExtLowBuffer,INDICATOR_DATA);
   SetIndexBuffer(3,ExtCloseBuffer,INDICATOR_DATA);
//---- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(4,ExtColorBuffer,INDICATOR_COLOR_INDEX);
//---- ������������� ������ ������ ������� ��������� ���������� 1
   PlotIndexSetInteger(4,PLOT_DRAW_BEGIN,StartBars);

//---- ��������� ������� �������� ����������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- ��� ��� ���� ������ � ����� ��� ������� 
   string short_name="Smoothed Candles";
   IndicatorSetString(INDICATOR_SHORTNAME,short_name);
//----   
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
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
//---- �������� ���������� ����� �� ������������� ��� �������
   if(rates_total<StartBars) return(0);

//---- ���������� ��������� ���������� 
   int first,bar;

//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
     {
      first=0; // ��������� ����� ��� ������� ���� �����
     }
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����

//---- �������� ���� ������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      //---- ������ ������ ������� XMASeries.
      ExtOpenBuffer [bar]=XMAO.XMASeries(0, prev_calculated, rates_total, MA_SMethod, MA_Phase, MA_Length, open [bar], bar, false);
      ExtCloseBuffer[bar]=XMAC.XMASeries(0, prev_calculated, rates_total, MA_SMethod, MA_Phase, MA_Length, close[bar], bar, false);
      ExtHighBuffer [bar]=XMAH.XMASeries(0, prev_calculated, rates_total, MA_SMethod, MA_Phase, MA_Length, high [bar], bar, false);
      ExtLowBuffer  [bar]=XMAL.XMASeries(0, prev_calculated, rates_total, MA_SMethod, MA_Phase, MA_Length, low  [bar], bar, false);

      if(bar<=StartBars) continue;

      //--- ������������� ������
      if(ExtOpenBuffer[bar]<ExtCloseBuffer[bar]) ExtColorBuffer[bar]=0.0;
      else                                       ExtColorBuffer[bar]=1.0;

     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+