//+------------------------------------------------------------------+
//|                                         Heiken_Ashi_Smoothed.mq5 |
//|                             Copyright � 2011,   Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright � 2011, Nikolay Kositsin"
#property link "farria@mail.redcom.ru"
#property description "Heiken Ashi Smoothed"
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
//---- � �������� ���������� ������������ ������� �����
#property indicator_type1   DRAW_COLOR_CANDLES
#property indicator_color1  clrDodgerBlue,clrRed
//---- ����������� ����� ����������
#property indicator_label1  "Heiken Ashi Open;Heiken Ashi High;Heiken Ashi Low;Heiken Ashi Close"
//+----------------------------------------------+
//|  �������� ������� ����������                 |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+
//---- ���������� ���������� ������ CXMA �� ����� SmoothAlgorithms.mqh
CXMA XMAO,XMAL,XMAH,XMAC;
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
input Smooth_Method HMA_Method=MODE_JJMA; //����� ����������
input int HLength=30;                     //�������  ����������                    
input int HPhase=100;                     //�������� ����������,
//---- ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������;
//---- VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
//+----------------------------------------------+

//---- ���������� ������������ ��������, ������� ����� � 
// ���������� ������������ � �������� ������������ �������
double ExtOpenBuffer[];
double ExtHighBuffer[];
double ExtLowBuffer[];
double ExtCloseBuffer[];
double ExtColorBuffer[];
//----
int min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
  {
//---- ������������� ���������� ���������� 
   min_rates_total=XMAO.GetStartBars(HMA_Method,HLength,HPhase)+1;

//---- ��������� ������� �� ������������ �������� ������� ����������
   XMAO.XMALengthCheck("HLength", HLength);
   XMAO.XMAPhaseCheck("HPhase", HPhase, HMA_Method);

//---- ����������� ������������ �������� � ������������ ������
   SetIndexBuffer(0,ExtOpenBuffer,INDICATOR_DATA);
   SetIndexBuffer(1,ExtHighBuffer,INDICATOR_DATA);
   SetIndexBuffer(2,ExtLowBuffer,INDICATOR_DATA);
   SetIndexBuffer(3,ExtCloseBuffer,INDICATOR_DATA);
//---- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(4,ExtColorBuffer,INDICATOR_COLOR_INDEX);
//---- ������������� ������ ������ ������� ��������� ���������� 1
   PlotIndexSetInteger(4,PLOT_DRAW_BEGIN,min_rates_total);

//---- ��������� ������� �������� ����������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- ��� ��� ���� ������ � ����� ��� �������� 
   string short_name="Heiken Ashi Smoothed";
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
   if(rates_total<min_rates_total) return(0);

//---- ���������� ��������� ���������� 
   int first,bar;
   double XmaOpen,XmaHigh,XmaLow,XmaClose;

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
      XmaOpen  = XMAO.XMASeries(0, prev_calculated, rates_total, HMA_Method, HPhase, HLength, open [bar], bar, false);
      XmaClose = XMAC.XMASeries(0, prev_calculated, rates_total, HMA_Method, HPhase, HLength, close[bar], bar, false);
      XmaHigh  = XMAH.XMASeries(0, prev_calculated, rates_total, HMA_Method, HPhase, HLength, high [bar], bar, false);
      XmaLow   = XMAL.XMASeries(0, prev_calculated, rates_total, HMA_Method, HPhase, HLength, low  [bar], bar, false);

      if(bar<=min_rates_total)
        {
         ExtOpenBuffer [bar]=XmaOpen;
         ExtCloseBuffer[bar]=XmaClose;
         ExtHighBuffer [bar]=XmaHigh;
         ExtLowBuffer  [bar]=XmaLow;

         continue;
        }

      ExtOpenBuffer [bar]=(ExtOpenBuffer[bar-1]+ExtCloseBuffer[bar-1])/2;
      ExtCloseBuffer[bar]=(XmaOpen+XmaHigh+XmaLow+XmaClose)/4;
      ExtHighBuffer [bar]=MathMax(XmaHigh,MathMax(ExtOpenBuffer[bar],ExtCloseBuffer[bar]));
      ExtLowBuffer  [bar]=MathMin(XmaLow,MathMin(ExtOpenBuffer[bar],ExtCloseBuffer[bar]));

      //--- ������������� ������
      if(ExtOpenBuffer[bar]<ExtCloseBuffer[bar]) ExtColorBuffer[bar]=0.0;
      else                                       ExtColorBuffer[bar]=1.0;

     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
