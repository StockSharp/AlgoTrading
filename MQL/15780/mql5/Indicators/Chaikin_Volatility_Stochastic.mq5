//+---------------------------------------------------------------------+
//|                                   Chaikin_Volatility_Stochastic.mq5 |
//|                                            Copyright � 2007, Giaras |
//|                                       giampiero.raschetti@gmail.com |
//+---------------------------------------------------------------------+ 
//| ��� ������  ����������  �������  �������� ���� SmoothAlgorithms.mqh |
//| � ����� (����������): �������_������_���������\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright � 2007, Giaras"
#property link      "giampiero.raschetti@gmail.com"
//---- ����� ������ ����������
#property version   "1.11"
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window 
//---- ���������� ������������ �������
#property indicator_buffers 2 
//---- ������������ ����� ���� ����������� ����������
#property indicator_plots   1
//+-----------------------------------+
//|  ��������� ��������� ����������   |
//+-----------------------------------+
//---- ��������� ���������� � ���� �������� ������
#property indicator_type1   DRAW_FILLING
//---- � �������� ����jd ���������� ������������
#property indicator_color1  clrBlue,clrRed
//---- ����������� ����� ����������
#property indicator_label1  "Chaikin_Volatility_Stochastic"
//+-----------------------------------+
//|  �������� ������ CXMA             |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+
//---- ���������� ���������� ������ CXMA �� ����� xrangeAlgorithms.mqh
CXMA XMA1,XMA2;
//+-----------------------------------+
//|  ���������� ������������          |
//+-----------------------------------+
/*enum Smooth_Method - ������������ ��������� � ����� xrangeAlgorithms.mqh
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
   MODE_AMA    //AMA
  }; */
//+-----------------------------------+
//|  ������� ��������� ����������     |
//+-----------------------------------+
input Smooth_Method XMA_Method=MODE_EMA_; //����� ����������
input int XLength=10; //������� �����������                    
input int XPhase=15; //�������� �����������,
//---- ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������;
//---- ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input uint StocLength= 5;
input uint WMALength = 5;
input int Shift=0; // ����� ���������� �� ����������� � �����
//+-----------------------------------+
//---- ���������� ������������ ��������, ������� � ����������
//---- ����� ������������ � �������� ������������ �������
double IndBuffer[],TriggerBuffer[];
//---- ���������� ����� ���������� ������ ������� ������
int min_rates_total,min_rates_1,min_rates_2;
//---- ���������� ������������ ��������, ������� ����� � 
// ���������� ������������ � �������� ��������� �������
int Count[];
double xrange[];
//+------------------------------------------------------------------+
//|  �������� ������� ������ ������ �������� � �������               |
//+------------------------------------------------------------------+   
void Recount_ArrayZeroPos(int &CoArr[],// ������� �� ������ ������ �������� �������� �������� ����
                          int Size)
  {
//----
   int numb,Max1,Max2;
   static int count=1;

   Max2=Size;
   Max1=Max2-1;

   count--;
   if(count<0) count=Max1;

   for(int iii=0; iii<Max2; iii++)
     {
      numb=iii+count;
      if(numb>Max1) numb-=Max2;
      CoArr[iii]=numb;
     }
//----
  }
//+------------------------------------------------------------------+   
//| Chaikin_Volatility indicator initialization function             | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_1=XMA1.GetStartBars(XMA_Method,XLength,XPhase);
   min_rates_2=min_rates_1+int(StocLength);
   min_rates_total=min_rates_2+int(WMALength+1);
//---- ��������� ������� �� ������������ �������� ������� ����������
   XMA1.XMAPhaseCheck("XPhase",XPhase,XMA_Method);

//---- ������������� ������ ��� ������� ����������  
   ArrayResize(Count,StocLength);
   ArrayResize(xrange,StocLength);

//---- ������������� �������� ����������
   ArrayInitialize(Count,0.0);
   ArrayInitialize(xrange,0.0);

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- ������������� ������ ���������� 1 �� �����������
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������

//---- ����������� ������������� ������� TriggerBuffer[] � ������������ �����
   SetIndexBuffer(1,TriggerBuffer,INDICATOR_DATA);

//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,"Chaikin_Volatility_Stochastic");

//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//---- ���������� �������������
  }
//+------------------------------------------------------------------+ 
//| Chaikin_Volatility iteration function                            | 
//+------------------------------------------------------------------+ 
int OnCalculate(
                const int rates_total,    // ���������� ������� � ����� �� ������� ����
                const int prev_calculated,// ���������� ������� � ����� �� ���������� ����
                const datetime &time[],
                const double &open[],
                const double &high[],
                const double &low[],
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[]
                )
  {
//---- �������� ���������� ����� �� ������������� ��� �������
   if(rates_total<min_rates_total) return(0);

//---- ���������� ����� ���������� � ��������� ��� ����������� �����
   int first,bar;

//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
     {
      first=0; // ��������� ����� ��� ������� ���� �����
      ArrayInitialize(Count,0.0);
     }
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����

//---- �������� ���� ������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      double range=high[bar]-low[bar];
      xrange[Count[0]]=XMA1.XMASeries(0,prev_calculated,rates_total,XMA_Method,XPhase,XLength,range,bar,false);
      
      double _chakin=xrange[Count[0]];
      double hh=_chakin,ll=_chakin;
      for(int iii=0; iii<int(StocLength); iii++)
        {
         double tmp=xrange[Count[iii]];
         hh=MathMax(hh,tmp);
         ll=MathMin(ll,tmp);
        }
      double Value1=_chakin-ll;
      double Value2=hh-ll;
      double Value3=NULL;
      if(Value2) Value3=Value1/Value2;
      IndBuffer[bar]=100*XMA2.XMASeries(min_rates_2,prev_calculated,rates_total,MODE_LWMA_,0,WMALength,Value3,bar,false);
      TriggerBuffer[bar]=IndBuffer[MathMax(bar-1,0)];
      if(bar<rates_total-1) Recount_ArrayZeroPos(Count,StocLength);
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
