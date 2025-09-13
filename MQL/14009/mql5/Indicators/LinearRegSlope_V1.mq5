//+------------------------------------------------------------------+
//|                                            LinearRegSlope_V1.mq5 | 
//|                                Copyright � 2006, TrendLaboratory |
//|            http://finance.groups.yahoo.com/group/TrendLaboratory |
//|                                   E-mail: igorad2003@yahoo.co.uk |
//|                                                                  |
//|                         Modified from LinearRegSlope_v1 by Toshi |
//|                                  http://toshi52583.blogspot.com/ |
//+------------------------------------------------------------------+
//| ��� ������ ���������� ���� SmoothAlgorithms.mqh                  |
//| ������� �������� � �����: �������_������_���������\MQL5\Include  |
//+------------------------------------------------------------------+
#property copyright "Copyright � 2006, TrendLaboratory"
#property link      "http://finance.groups.yahoo.com/group/TrendLaboratory"
//---- ����� ������ ����������
#property version   "1.11"
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window
//---- ���������� ������������ ������� 2
#property indicator_buffers 2 
//---- ������������ ����� ��� ����������� ����������
#property indicator_plots   2
//+-----------------------------------+
//|  ��������� ��������� ���������� 1 |
//+-----------------------------------+
//---- ��������� ���������� � ���� �����
#property indicator_type1   DRAW_LINE
//---- � �������� ����� ����� ���������� ����������� DodgerBlue ����
#property indicator_color1 clrDodgerBlue
//---- ����� ���������� - ����������� ������
#property indicator_style1  STYLE_SOLID
//---- ������� ����� ���������� ����� 1
#property indicator_width1  1
//---- ����������� ����� ����������
#property indicator_label1  "Linear Reg Slope line"

//+-----------------------------------+
//|  ��������� ��������� ���������� 2 |
//+-----------------------------------+
//---- ��������� ���������� � ���� �����
#property indicator_type2   DRAW_LINE
//---- � �������� ����� ����� ���������� ����������� Coral ����
#property indicator_color2 clrCoral
//---- ����� ���������� - ����������� ������
#property indicator_style2  STYLE_SOLID
//---- ������� ����� ���������� ����� 1
#property indicator_width2  1
//---- ����������� ����� ����������
#property indicator_label2  "Trigger line"

//+-----------------------------------+
//|  �������� ������ CXMA             |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+

//---- ���������� ���������� ������ CXMA �� ����� SmoothAlgorithms.mqh
CXMA XMA1;
//+-----------------------------------+
//|  ���������� ������������          |
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
   PRICE_SIMPL_,         //Simpl Price (OC/2)
   PRICE_QUARTER_,       //Quarted Price (HLOC/4) 
   PRICE_TRENDFOLLOW0_,  //TrendFollow_1 Price 
   PRICE_TRENDFOLLOW1_,  // TrendFollow_2 Price 
   PRICE_DEMARK_         // Demark Price
  };
//+-----------------------------------+
//|  ���������� ������������          |
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
//|  ������� ��������� ����������     |
//+-----------------------------------+
input Smooth_Method SlMethod=MODE_SMA; //����� ����������
input int SlLength=12; //������� �����������                    
input int SlPhase=15; //�������� �����������,
                      //��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������;
// ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input Applied_price_ IPC=PRICE_CLOSE;//������� ���������
/* , �� ������� ������������ ������ ���������� ( 1-CLOSE, 2-OPEN, 3-HIGH, 4-LOW, 
  5-MEDIAN, 6-TYPICAL, 7-WEIGHTED, 8-SIMPL, 9-QUARTER, 10-TRENDFOLLOW, 11-0.5 * TRENDFOLLOW.) */
input int Shift=0; // ����� ���������� �� ����������� � �����
input uint TriggerShift=1; // c���� ���� ��� ������� 
//+-----------------------------------+

//---- ���������� ����� ���������� ������ ������� ������
int min_rates_total;
//---- ���������� ������������ ��������, ������� ����� � 
// ���������� ������������ � �������� ������������ �������
double RegSlopeBuffer[],TriggerBuffer[];
//---- ���������� ���������� ����������
int TriggerShift_;
double Num2,SumBars;
//---- ���������� ������������ ��������, ������� ����� � 
// ���������� ������������ � �������� ��������� �������
int Count[];
double Smooth[];
//+------------------------------------------------------------------+
//|  �������� ������� ������ ������ �������� � �������               |
//+------------------------------------------------------------------+   
void Recount_ArrayZeroPos
(
 int &CoArr[],// ������� �� ������ ������ �������� �������� �������� ����
 int Size // ���������� ��������� � ��������� ������
 )
// Recount_ArrayZeroPos(count, SlLength)
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
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
//| XMA indicator initialization function                            | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_total=XMA1.GetStartBars(SlMethod,SlLength,SlPhase);

//---- ��������� ������� �� ������������ �������� ������� ����������
   XMA1.XMALengthCheck("SlLength", SlLength);
   XMA1.XMAPhaseCheck("SlPhase", SlPhase, SlMethod);

//---- ������������� ����������   
   SumBars=SlLength *(SlLength-1)*0.5;
   double SumSqrBars=(SlLength-1.0)*SlLength *(2.0*SlLength-1.0)/6.0;
   Num2=SumBars*SumBars-SlLength*SumSqrBars;
   TriggerShift_=int(min_rates_total+TriggerShift-1);

//---- ������������� ������ ��� ������� ����������  
   ArrayResize(Count,SlLength);
   ArrayResize(Smooth,SlLength);

//---- ������������� �������� ����������
   ArrayInitialize(Count,0);
   ArrayInitialize(Smooth,0.0);

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,RegSlopeBuffer,INDICATOR_DATA);
//---- ������������� ������ ���������� 1 �� �����������
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(1,TriggerBuffer,INDICATOR_DATA);
//---- ������������� ������ ���������� 1 �� �����������
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   string Smooth1=XMA1.GetString_MA_Method(SlMethod);
   StringConcatenate(shortname,"Linear Reg Slope(",SlLength,", ",Smooth1,")");
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);

//--- ����������� �������� ����������� �������� ����������
// IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
//---- ���������� �������������
  }
//+------------------------------------------------------------------+ 
//| XMA iteration function                                           | 
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

//---- ���������� ���������� � ��������� ������  
   double price_,Sum1,Sum2,SumY,Num1;
//---- ���������� ����� ���������� � ��������� ��� ����������� �����
   int first,bar,iii;

//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
      first=0; // ��������� ����� ��� ������� ���� �����
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����

//---- �������� ���� ������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      //---- ����� ������� PriceSeries ��� ��������� ������� ���� price_
      price_=PriceSeries(IPC,bar,open,low,high,close);
      Smooth[Count[0]]=XMA1.XMASeries(0,prev_calculated,rates_total,SlMethod,SlPhase,SlLength,price_,bar,false);

      Sum1=0;
      SumY=0;

      if(bar>SlLength)
         for(iii=0; iii<SlLength; iii++)
           {
            SumY+=Smooth[Count[iii]];
            Sum1+=iii*Smooth[Count[iii]];
           }

      Sum2=SumBars*SumY;
      Num1=SlLength*Sum1-Sum2;

      if(Num2!=0.0) RegSlopeBuffer[bar]=100*Num1/Num2;
      else          RegSlopeBuffer[bar]=EMPTY_VALUE;

      if(bar>TriggerShift_) TriggerBuffer[bar]=RegSlopeBuffer[bar-TriggerShift];
      else                 TriggerBuffer[bar]=EMPTY_VALUE;

      //---- �������� ������� ��������� � ��������� ������ Smooth[]
      if(bar<rates_total-1) Recount_ArrayZeroPos(Count,SlLength);
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
