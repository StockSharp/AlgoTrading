//+---------------------------------------------------------------------+
//|                                                    Ichimoku_Osc.mq5 | 
//|                                               Copyright � 2010, MDM | 
//|                                                                     | 
//+---------------------------------------------------------------------+
//| ��� ������  ����������  �������  �������� ���� SmoothAlgorithms.mqh |
//| � ����� (����������): terminal_data_folder\MQL5\Include             |
//+---------------------------------------------------------------------+
#property copyright "Copyright � 2010, MDM"
#property link ""
#property description "Ichimoku_Osc"
//---- ����� ������ ����������
#property version   "1.01"
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window
//---- ���������� ������������ �������
#property indicator_buffers 4 
//---- ������������ ����� ��� ����������� ����������
#property indicator_plots   2
//+-----------------------------------+
//| ���������� ��������               |
//+-----------------------------------+
#define RESET 0 // ��������� ��� �������� ��������� ������� �� �������� ����������
//+-----------------------------------+
//| ��������� ��������� ���������� 1  |
//+-----------------------------------+
//---- ��������� ���������� � ���� ����������� �����
#property indicator_type1   DRAW_COLOR_LINE
//---- � �������� ����� ����� ���������� ������������
#property indicator_color1 clrGray,clrDeepSkyBlue,clrDeepPink
//---- ����� ���������� - ��������
#property indicator_style1  STYLE_SOLID
//---- ������� ����� ���������� ����� 4
#property indicator_width1  4
//---- ����������� ����� ����������
#property indicator_label1  "Signal"
//+-----------------------------------+
//| ��������� ��������� ���������� 2  |
//+-----------------------------------+
//---- ��������� ���������� � ���� �����
//#property indicator_type2   DRAW_LINE
//---- � �������� ������ ����������� ������������
#property indicator_color2 clrGray
//---- ����� ���������� - ����������� ������
#property indicator_style2  STYLE_SOLID
//---- ������� ����� ���������� ����� 2
#property indicator_width2  2
//---- ����������� ����� ����������
#property indicator_label2  "Ichimoku Oscillator"
//+-----------------------------------+
//| �������� ������ CXMA              |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+
//---- ���������� ���������� ������ CXMA �� ����� SmoothAlgorithms.mqh
CXMA XSIGN;
//+-----------------------------------+
//| ���������� ������������           |
//+-----------------------------------+
enum IndStyle //����� ����������� ����������
  {
   LINE = DRAW_LINE,          //�����
   ARROW=DRAW_ARROW,          //������
   HISTOGRAM=DRAW_HISTOGRAM   //�����������
  };
//+-----------------------------------+
/*enum Smooth_Method - ��������� � ����� SmoothAlgorithms.mqh
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
input int Tenkan=9;      // Tenkan-sen
input int Kijun=26;      // Kijun-sen
input int Senkou=52;     // Senkou Span B
//---
input Smooth_Method SSmoothMethod=MODE_JJMA; // ����� ���������� ���������� �����
input int SPeriod=7;  // ������ ���������� �����
input int SPhase=100; // �������� ���������� �����
                      // ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������
// ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input int Shift=0; // ����� ���������� �� ����������� � �����
input IndStyle Style=DRAW_ARROW; // ����� ����������� �����������
//+-----------------------------------+
//---- ���������� ������������� ��������, ������� ����� � 
//---- ���������� ������������ � �������� ������������ �������
double Osc[],XOsc[];
double ColorXOsc[];
//---- ���������� ������������� ���������� ��� ������� �����������
int Ich_Handle;
//---- ���������� ������������� ���������� ������ ������� ������
int min_rates_total,min_rates_;
//+------------------------------------------------------------------+   
//| Osc indicator initialization function                            | 
//+------------------------------------------------------------------+ 
int OnInit()
  {
//---- ������������� ���������� ������ ������� ������   
   min_rates_=int(MathMax(MathMax(Tenkan,Kijun),Senkou));
   min_rates_total=min_rates_+GetStartBars(SSmoothMethod,SPeriod,SPhase);
//---- ��������� ������ ���������� Ichimoku_Calc
   Ich_Handle=iCustom(NULL,PERIOD_CURRENT,"Ichimoku_Calc",Tenkan,Kijun,Senkou,0);
   if(Ich_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� Ichimoku_Calc");
      return(INIT_FAILED);
     }
//---- ��������� ������� �� ������������ �������� ������� ����������
   XSIGN.XMALengthCheck("SPeriod",SPeriod);
//---- ��������� ������� �� ������������ �������� ������� ����������
   XSIGN.XMAPhaseCheck("SPhase",SPhase,SSmoothMethod);
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,XOsc,INDICATOR_DATA);
//---- ������������� ������ ���������� 2 �� �����������
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(XOsc,true);
//---- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(1,ColorXOsc,INDICATOR_COLOR_INDEX);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(ColorXOsc,true);
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(2,Osc,INDICATOR_DATA);
//---- ������������� ������ ���������� 1 �� �����������
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- ��������� ����� ����������� ����������   
   PlotIndexSetInteger(1,PLOT_DRAW_TYPE,Style);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(Osc,true);
//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname,Smooth;
   Smooth=XSIGN.GetString_MA_Method(SSmoothMethod);
   StringConcatenate(shortname,"Ichimoku Oscillator(",string(Tenkan),",",Smooth,")");
//---- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//---- ���������� �������������
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+ 
//| Osc iteration function                                           | 
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
   if(BarsCalculated(Ich_Handle)<rates_total || rates_total<min_rates_total) return(RESET);
//---- ���������� ���������� � ��������� ������  
   double markt,trend,TS[],KS[],SA[],CS[];
//---- ���������� ������������� ����������
   int bar,limit,maxbar,to_copy;
//----
   maxbar=rates_total-1-min_rates_total;
//---- ������ ���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
      limit=rates_total-min_rates_-1; // ��������� ����� ��� ������� ���� �����
   else limit=rates_total-prev_calculated; // ��������� ����� ��� ������� ����� ����� 
//----
   to_copy=limit+1;
//---- �������� ����� ����������� ������ � �������
   if(CopyBuffer(Ich_Handle,TENKANSEN_LINE,0,to_copy,TS)<=0) return(RESET);
   if(CopyBuffer(Ich_Handle,KIJUNSEN_LINE,0,to_copy,KS)<=0) return(RESET);
   if(CopyBuffer(Ich_Handle,SENKOUSPANB_LINE,0,to_copy,SA)<=0) return(RESET);
   if(CopyBuffer(Ich_Handle,CHINKOUSPAN_LINE,0,to_copy,CS)<=0) return(RESET);
//---- ���������� ��������� � �������� ��� � ����������  
   ArraySetAsSeries(TS,true);
   ArraySetAsSeries(KS,true);
   ArraySetAsSeries(SA,true);
   ArraySetAsSeries(CS,true);
//---- �������� ���� ������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      markt=(CS[bar]-SA[bar]);
      trend=(TS[bar]-KS[bar]);
      //----
      Osc[bar]=(markt-trend)/_Point;
      //---- �������� ����������� �������� � ������������ �����
      XOsc[bar]=XSIGN.XMASeries(maxbar,prev_calculated,rates_total,SSmoothMethod,SPhase,SPeriod,Osc[bar],bar,true);
     }
//---- �������� ���� ��������� ���������� �����
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      ColorXOsc[bar]=0;
      if(XOsc[bar]>XOsc[bar+1]) ColorXOsc[bar]=1;
      if(XOsc[bar]<XOsc[bar+1]) ColorXOsc[bar]=2;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
