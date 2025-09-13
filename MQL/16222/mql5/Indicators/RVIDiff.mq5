//+---------------------------------------------------------------------+ 
//|                                                         RVIDiff.mq5 | 
//|                                        Copyright � 2009, DesO'Regan | 
//|                                              oregan_des@hotmail.com | 
//+---------------------------------------------------------------------+ 
//| ��� ������  ����������  �������  �������� ���� SmoothAlgorithms.mqh |
//| � ����� (����������): �������_������_���������\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright � 2009, DesO'Regan"
#property link "oregan_des@hotmail.com"
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window 
//---- ���������� ������������ ������� 2
#property indicator_buffers 2
//---- ������������ ��� ����������� ����������
#property indicator_plots  1
//+----------------------------------------------+
//|  ���������� ��������                         |
//+----------------------------------------------+
#define RESET  0 // ��������� ��� �������� ��������� ������� �� �������� ����������
//+----------------------------------------------+
//|  ��������� ��������� ���������� 1            |
//+----------------------------------------------+
//---- ��������� ���������� � ���� �����������
#property indicator_type1   DRAW_COLOR_HISTOGRAM
//---- � �������� ������ ���������� ������������
#property indicator_color1 clrLime,clrTeal,clrGray,clrDarkOrange,clrGold
//---- ����� ���������� - ��������
#property indicator_style1 STYLE_SOLID
//---- ������� ����� ���������� ����� 3
#property indicator_width1 3
//---- ����������� ����� ����������
#property indicator_label1  "RVIDiff"
//+----------------------------------------------+
//|  �������� ������ CXMA                        |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+
//---- ���������� ���������� ������ CXMA �� ����� SmoothAlgorithms.mqh
CXMA XMA1;
//+----------------------------------------------+
//|  ���������� ������������                     |
//+----------------------------------------------+
/*enum SmoothMethod - ������������ ��������� � ����� SmoothAlgorithms.mqh
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
//|  ������� ��������� ����������                |
//+----------------------------------------------+
input uint RVIPeriod=12;
input Smooth_Method XMA_Method=MODE_T3;        //����� ����������� ����������
input uint XLength=13;                         //������� �����������                    
input int XPhase=15;                           //�������� �����������,
//---- ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������;
//---- ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input int Shift=0;                             //����� ���������� �� ����������� � �����
//+----------------------------------------------+

//---- ���������� ����� ���������� ������ ������� ������
int  min_rates_1,min_rates_total;
//---- ���������� ������������ ��������, ������� ����� � 
// ���������� ������������ � �������� ������������ �������
double IndBuffer[],ColorBuffer[];
//---- ���������� ����� ���������� ��� ������� �����������
int RVI_Handle;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_1=int(RVIPeriod)+1+3+1;
   min_rates_total=min_rates_1+GetStartBars(XMA_Method,XLength,XPhase);
//---- ��������� ������ ���������� iRVI
   RVI_Handle=iRVI(NULL,0,RVIPeriod);
   if(RVI_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iRVI");
      return(INIT_FAILED);
     }

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- ������������� ������ ���������� �� ����������� �� InpKijun
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(IndBuffer,true);

//---- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(1,ColorBuffer,INDICATOR_COLOR_INDEX);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(ColorBuffer,true);

//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,"RVIDiff("+string(RVIPeriod)+")");
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,6);
//---- ���������� �������������
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+  
//| Custom indicator iteration function                              | 
//+------------------------------------------------------------------+  
int OnCalculate(
                const int rates_total,    // ���������� ������� � ����� �� ������� ����
                const int prev_calculated,// ���������� ������� � ����� �� ���������� ����
                const datetime &Time[],
                const double &Open[],
                const double &High[],
                const double &Low[],
                const double &Close[],
                const long &Tick_Volume[],
                const long &Volume[],
                const int &Spread[]
                )
  {
//---- �������� ���������� ����� �� ������������� ��� �������
   if(BarsCalculated(RVI_Handle)<rates_total || rates_total<min_rates_total) return(RESET);

//---- ���������� ��������� ����������
   int to_copy,limit,bar,maxbar;
   double RVI[],Sign[],diff;

//---- ������ ���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      limit=rates_total-min_rates_1-1; // ��������� ����� ��� ������� ���� �����
     }
   else limit=rates_total-prev_calculated; // ��������� ����� ��� ������� ����� �����

   to_copy=limit+1;
   maxbar=rates_total-1-min_rates_1;

//---- �������� ����� ����������� ������ � �������
   if(CopyBuffer(RVI_Handle,MAIN_LINE,0,to_copy,RVI)<=0) return(RESET);
   if(CopyBuffer(RVI_Handle,SIGNAL_LINE,0,to_copy,Sign)<=0) return(RESET);
   
//---- ���������� ��������� � �������� ��� � ����������  
   ArraySetAsSeries(RVI,true);
   ArraySetAsSeries(Sign,true);

//---- �������� ���� ��������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      diff=RVI[bar]-Sign[bar];
      IndBuffer[bar]=XMA1.XMASeries(maxbar,prev_calculated,rates_total,XMA_Method,XPhase,XLength,diff,bar,true);

      int clr=2;
      if(IndBuffer[bar]>=0)
        {
         if(IndBuffer[bar]>=IndBuffer[bar+1]) clr=0;
         else clr=1;
        }
      else
        {
         if(IndBuffer[bar]<=IndBuffer[bar+1]) clr=4;
         else clr=3;
        }
      ColorBuffer[bar]=clr;
     }
//----    
   return(rates_total);
  }
//+------------------------------------------------------------------+