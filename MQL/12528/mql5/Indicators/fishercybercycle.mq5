//+------------------------------------------------------------------+
//|                                             FisherCyberCycle.mq5 |
//|                                                                  |
//| Fisher Cyber Cycle                                               |
//|                                                                  |
//| Algorithm taken from book                                        |
//|     "Cybernetics Analysis for Stock and Futures"                 |
//| by John F. Ehlers                                                |
//|                                                                  |
//|                                              contact@mqlsoft.com |
//|                                          http://www.mqlsoft.com/ |
//+------------------------------------------------------------------+
//--- ��������� ����������
#property copyright "Coded by Witold Wozniak"
//--- ��������� ����������
#property link      "www.mqlsoft.com"
//--- ����� ������ ����������
#property version   "1.10"
//--- ��������� ���������� � ��������� ����
#property indicator_separate_window
//--- ��� ������� � ��������� ���������� ������������ ��� ������
#property indicator_buffers 2
//--- ������������ ��� ����������� ����������
#property indicator_plots   2
//+----------------------------------------------+
//| ��������� ��������� ���������� Cyber Cycle   |
//+----------------------------------------------+
//--- ��������� ���������� 1 � ���� �����
#property indicator_type1   DRAW_LINE
//--- � �������� ����� ����� ����� ���������� ����������� ���� Red
#property indicator_color1  Red
//--- ����� ���������� 1 - ����������� ������
#property indicator_style1  STYLE_SOLID
//--- ������� ����� ���������� 1 ����� 1
#property indicator_width1  1
//--- ����������� ����� ����� ����������
#property indicator_label1  "Fisher Cyber Cycle"
//+----------------------------------------------+
//| ��������� ��������� ���������� Trigger       |
//+----------------------------------------------+
//--- ��������� ���������� 2 � ���� �����
#property indicator_type2   DRAW_LINE
//--- � �������� ����� ����� ���������� ����������� ���� Blue
#property indicator_color2  Blue
//--- ����� ���������� 2 - ����������� ������
#property indicator_style2  STYLE_SOLID
//--- ������� ����� ���������� 2 ����� 1
#property indicator_width2  1
//--- ����������� ����� ����� ����������
#property indicator_label2  "Trigger"
//+----------------------------------------------+
//| ��������� ����������� �������������� ������� |
//+----------------------------------------------+
#property indicator_level1 +0.7
#property indicator_level2  0.0
#property indicator_level3 -0.7
#property indicator_levelcolor Gray
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input double Alpha=0.07; // ����������� ���������� 
input int Length=8;      // ������ ���������� 
input int Shift=0;       // ����� ���������� �� ����������� � ����� 
//+----------------------------------------------+
//--- ���������� ������������ ��������, ������� � ����������
//--- ����� ������������ � �������� ������������ �������
double FishCCBuffer[];
double TriggerBuffer[];
//--- ���������� ������������� ���������� ������ ������� ������
int min_rates_total;
//--- ���������� ���������� ����������
int Count1[],Count2[];
double K0,K1,K2,K3,Smooth[],Cycle[],Value1[],Price[];
//+------------------------------------------------------------------+
//|  �������� ������� ������ ������ �������� � �������               |
//+------------------------------------------------------------------+   
void Recount_ArrayZeroPos1(int &CoArr[])// ������� �� ������ ������ �������� �������� �������� ����
  {
//---
   int numb,Max1,Max2;
   static int count=1;

   Max1=Length+1;
   Max2=Length+2;

   count--;
   if(count<0) count=Max1;

   for(int iii=0; iii<Max2; iii++)
     {
      numb=iii+count;
      if(numb>Max1) numb-=Max2;
      CoArr[iii]=numb;
     }
//---
  }
//+------------------------------------------------------------------+
//|  �������� ������� ������ ������ �������� � �������               |
//+------------------------------------------------------------------+   
void Recount_ArrayZeroPos2(int &CoArr[])// ������� �� ������ ������ �������� �������� �������� ����
  {
//---
   int numb,Max1,Max2;
   static int count=1;

   Max1=Length+2;
   Max2=Length+3;

   count--;
   if(count<0) count=Max1;

   for(int iii=0; iii<Max2; iii++)
     {
      numb=iii+count;
      if(numb>Max1) numb-=Max2;
      CoArr[iii]=numb;
     }
//---
  }
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//--- ������������� ���������� ������ ������� ������
   min_rates_total=Length+3;

//--- ������������� ����������
   K0=MathPow((1.0 - 0.5*Alpha),2);
   K1=2.0;
   K2=2.0 *(1.0 - Alpha);
   K3=MathPow((1.0 - Alpha),2);

//--- ������������� ������ ��� ������� ����������  
   ArrayResize(Count1,Length+2);
   ArrayResize(Cycle,Length+2);
   ArrayResize(Count2,Length+3);
   ArrayResize(Value1,Length+3);
   ArrayResize(Price,Length+3);
   ArrayResize(Smooth,Length+3);

//--- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,FishCCBuffer,INDICATOR_DATA);
//--- ������������� ������ ���������� 1 �� ����������� �� Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//--- ������������� ������ ������ ������� ��������� ���������� 1 �� min_rates_total
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);

//--- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(1,TriggerBuffer,INDICATOR_DATA);
//--- ������������� ������ ���������� 2 �� ����������� �� Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//--- ������������� ������ ������ ������� ��������� ���������� 2 �� min_rates_total+1
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total+1);

//--- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   StringConcatenate(shortname,"Fisher Cyber Cycle(",DoubleToString(Alpha,4),", ",Length,", ",Shift,")");
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,    // ���������� ������� � ����� �� ������� ����
                const int prev_calculated,// ���������� ������� � ����� �� ���������� ����
                const datetime &time[],
                const double &open[],
                const double& high[],     // ������� ������ ���������� ���� ��� ������� ����������
                const double& low[],      // ������� ������ ��������� ����  ��� ������� ����������
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//--- �������� ���������� ����� �� ������������� ��� �������
   if(rates_total<min_rates_total) return(0);

//--- ���������� ��������� ���������� 
   int first,bar;
   double hh,ll,tmp;

//--- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
     {
      first=3; // ��������� ����� ��� ������� ���� �����
      for(int numb=0; numb<Length+2; numb++) Count1[numb]=numb;
      for(int numb=0; numb<Length+3; numb++) Count2[numb]=numb;
      double val=(high[first]+low[first])/2.0;
      ArrayInitialize(Price,val);
      ArrayInitialize(Smooth,val);
      ArrayInitialize(Cycle,val);
      ArrayInitialize(Value1,val);
     }
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����

//--- �������� ���� ������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      Price[Count2[0]]=(high[bar]+low[bar])/2.0;
      Smooth[Count2[0]]=(Price[Count2[0]]+2.0*Price[Count2[1]]+2.0*Price[Count2[2]]+Price[Count2[3]])/6.0;

      if(bar<3)
        {
         Recount_ArrayZeroPos1(Count1);
         Recount_ArrayZeroPos2(Count2);
         continue;
        }

      if(bar<min_rates_total) Cycle[Count1[0]]=(Price[Count1[0]]+2.0*Price[Count2[1]]+Price[Count2[2]])/4.0;
      else Cycle[Count1[0]]=K0*(Smooth[Count2[0]]-K1*Smooth[Count2[1]]+Smooth[Count2[2]])+K2*Cycle[Count1[1]]-K3*Cycle[Count1[2]];

      hh = Cycle[Count1[0]];
      ll = Cycle[Count1[0]];

      for(int iii=0; iii<Length; iii++)
        {
         tmp= Cycle[Count1[iii]];
         hh = MathMax(hh, tmp);
         ll = MathMin(ll, tmp);
        }

      if(hh!=ll) Value1[Count2[0]]=(Cycle[Count1[0]]-ll)/(hh-ll);
      else Value1[Count2[0]]=0.0;

      FishCCBuffer[bar]=(4.0*Value1[Count2[0]]+3.0*Value1[Count2[1]]+2.0*Value1[Count2[2]]+Value1[Count2[3]])/10.0;
      FishCCBuffer[bar] = 0.5 * MathLog((1.0 + 1.98 * (FishCCBuffer[bar] - 0.5)) / (1.0 - 1.98 * (FishCCBuffer[bar] - 0.5)));
      TriggerBuffer[bar]= FishCCBuffer[bar-1];

      if(bar<rates_total-1)
        {
         Recount_ArrayZeroPos1(Count1);
         Recount_ArrayZeroPos2(Count2);
        }

     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+

   