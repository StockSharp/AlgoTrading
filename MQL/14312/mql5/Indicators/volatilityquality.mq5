//+---------------------------------------------------------------------+
//|                                               VolatilityQuality.mq5 | 
//|                                    Copyright � 2011, raff1410@o2.pl | 
//|                                                      raff1410@o2.pl | 
//+---------------------------------------------------------------------+ 
//| ��� ������  ����������  �������  �������� ���� SmoothAlgorithms.mqh |
//| � ����� (����������): �������_������_���������\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright � 2011, raff1410@o2.pl"
#property link "raff1410@o2.pl"
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window
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
#property indicator_color1  clrBlue,clrMagenta
//---- ����� ���������� - ����������� ������
#property indicator_style1  STYLE_SOLID
//---- ������� ����� ���������� ����� 3
#property indicator_width1  3
//---- ����������� ����� ����������
#property indicator_label1  "VolatilityQuality"
//+-----------------------------------+
//| �������� ������ CXMA              |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+
//---- ���������� ���������� ������ CXMA �� ����� SmoothAlgorithms.mqh
CXMA XmaMH,XmaML,XmaMO,XmaMC,XmaMC1;
//+-----------------------------------+
//| ���������� ������������           |
//+-----------------------------------+
enum App_price //��� ���������
  {
   PRICE_CLOSE_=1,     //Close
   PRICE_MEDIAN_=5     //Median Price (HL/2)
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
input Smooth_Method XMA_Method=MODE_LWMA; // ����� ����������
input int XLength=5; // �������  �����������
input int XPhase=15; // �������� �����������
//---- ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������;
//---- ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input uint Smoothing=1; // �������  ��������� 
input uint Filter=5; // ���������� � ������� �������� �������
input App_price Price=PRICE_MEDIAN; // ����
input int Shift=0; // ����� ���������� �� ����������� � �����
input int PriceShift=0; // ����� ���������� �� ��������� � �������
//+-----------------------------------+
//---- ���������� ������������ ��������, ������� ����� � 
//---- ���������� ������������ � �������� ������������ �������
double IndBuffer[];
double ColorIndBuffer[];
//---- ���������� ���������� �������� ������������� ������ �������
double dPriceShift,dFilter;
//---- ���������� ������������� ���������� ������ ������� ������
int min_rates_total,min_rates,Len;
//---- ���������� ���������� ����������
int Count[];
double Mc[];
//+------------------------------------------------------------------+
//|  �������� ������� ������ ������ �������� � �������               |
//+------------------------------------------------------------------+   
void Recount_ArrayZeroPos(int &CoArr[],// ������� �� ������ ������ �������� �������� �������� ����
                          int Size)
  {
//----
   int numb,Max1,Max2;
   static int count=1;
//----
   Max2=Size;
   Max1=Max2-1;
//----
   count--;
   if(count<0) count=Max1;
//----
   for(int iii=0; iii<Max2; iii++)
     {
      numb=iii+count;
      if(numb>Max1) numb-=Max2;
      CoArr[iii]=numb;
     }
  }
//+------------------------------------------------------------------+   
//| Custom indicator initialization function                         | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates=GetStartBars(XMA_Method,XLength,XPhase);
   min_rates_total=int(min_rates+Smoothing+1);
   Len=int(Smoothing+1);
//---- ������������� ������ �� ���������
   dPriceShift=_Point*PriceShift;
   dFilter=_Point*Filter;
//---- ������������� ������ ��� ������� ����������  
   ArrayResize(Count,Len);
   ArrayResize(Mc,Len);
   ArrayInitialize(Count,0);
   ArrayInitialize(Mc,0.0);
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
//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   string Smooth=XmaMC.GetString_MA_Method(XMA_Method);
   StringConcatenate(shortname,"VolatilityQuality(",XLength,", ",Smooth,")");
//---- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//---- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
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
   double price,MH,ML,MC,MC1,MO,VQ,SumVQ,res1,res2;
   static double SumVQ_prev;
//---- ���������� ������������� ���������� � ��������� ��� ����������� �����
   int first,bar,clr;
//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
     {
      first=0; // ��������� ����� ��� ������� ���� �����
      SumVQ_prev=PriceSeries(Price,0,open,low,high,close);
      ArrayInitialize(Count,0);
      ArrayInitialize(Mc,SumVQ_prev);
     }
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����
//---- �������� ���� ������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      price=PriceSeries(Price,bar,open,low,high,close);
      Mc[Count[0]]=XmaMC.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,XLength,price,bar,false);
      MC=Mc[Count[0]];
      MC1=Mc[Count[Smoothing]];
      if(bar==min_rates_total) SumVQ_prev=PriceSeries(Price,bar-1,open,low,high,close);
      MH=XmaMH.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,XLength,high[bar],bar,false);
      ML=XmaML.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,XLength,low[bar],bar,false);
      MO=XmaMO.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,XLength,open[bar],bar,false);
      res1=MathMax(MH-ML,MathMax(MH-MC1,MC1-ML));
      res2=MH-ML;
      if(res1 && res2) VQ=MathAbs(((MC-MC1)/res1+(MC-MO)/res2)*0.5)*((MC-MC1+(MC-MO))*0.5);
      else VQ=price;
      SumVQ=SumVQ_prev+VQ;
      if(Filter && MathAbs(SumVQ-SumVQ_prev)<dFilter) SumVQ=SumVQ_prev;
      IndBuffer[bar]=SumVQ+dPriceShift;
      //----
      if(bar<rates_total-1)
        {
         Recount_ArrayZeroPos(Count,Len);
         SumVQ_prev=SumVQ;
        }
     }
//---- ������������� �������� ���������� first
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
      first=min_rates_total; // ��������� ����� ��� ������� ���� �����
//---- �������� ���� ��������� ���������� �����
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      if(IndBuffer[bar-1]<IndBuffer[bar]) clr=0;
      else if(IndBuffer[bar-1]>IndBuffer[bar]) clr=1;
      else clr=int(ColorIndBuffer[bar-1]);
      ColorIndBuffer[bar]=clr;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
