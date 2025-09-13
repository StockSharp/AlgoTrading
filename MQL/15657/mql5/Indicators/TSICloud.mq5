//+------------------------------------------------------------------+
//|                                                     TSICloud.mq5 |
//|                      Copyright � 2005, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//|                                                                  |
//|                                   Modified from TSI-Osc by Toshi |
//|                                  http://toshi52583.blogspot.com/ |
//+------------------------------------------------------------------+
//| ��� ������  ���������� ���� SmoothAlgorithms.mqh                 |
//| ������� �������� � �����: �������_������_���������\\MQL5\Include |
//+------------------------------------------------------------------+
#property copyright "Copyright � 2005, MetaQuotes Software Corp."
#property link      "http://www.metaquotes.net"
//---- ����� ������ ����������
#property version   "1.01"
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window
//---- ��� ������� � ��������� ���������� ������������ ��� ������
#property indicator_buffers 2
//---- ������������ ���� ����������� ����������
#property indicator_plots   1
//+----------------------------------------------+
//|  ��������� ��������� ���������� 1            |
//+----------------------------------------------+
//---- ��������� ���������� 1 � ���� �����
#property indicator_type1   DRAW_FILLING
//---- � �������� ������ ������ ���������� ������������
#property indicator_color1  clrAqua,clrRed
//---- ������� ����� ���������� ����� 1
#property indicator_width1  1
//---- ����������� ����� ����������
#property indicator_label1  "TSICloud"
//+----------------------------------------------+
//| ��������� ����������� �������������� ������� |
//+----------------------------------------------+
#property indicator_level1 +50.0
#property indicator_level2   0.0
#property indicator_level3 -50.0
#property indicator_levelcolor clrGray
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//|  �������� ������ CXMA                        |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//---- ���������� ���������� ������ CXMA �� ����� SmoothAlgorithms.mqh
CXMA MTM1,MTM2,ABSMTM1,ABSMTM2;
//+----------------------------------------------+
//|  ���������� ������������                     |
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
   PRICE_TRENDFOLLOW1_,  //TrendFollow_2 Price 
   PRICE_DEMARK_         //Demark Price
  };
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
//|  ������� ��������� ����������                |
//+----------------------------------------------+
input Smooth_Method First_Method=MODE_SMA_; //����� ���������� 1
input uint First_Length=12; //������� ����������� 1                    
input int First_Phase=15; //�������� ����������� 1,
//---- ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������;
//---- ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ���������� 
input Smooth_Method Second_Method=MODE_SMA_; //����� ���������� 2
input uint Second_Length=12; //������� ����������� 2                    
input int Second_Phase=15; //�������� ����������� 2,
//---- ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input Applied_price_ IPC=PRICE_CLOSE_;//������� ���������  
input int Shift=0; // ����� ���������� �� ����������� � �����
input uint TriggerShift=1; // c���� ���� ��� ������� 
//+----------------------------------------------+
//---- ���������� ����� ���������� ������ ������� ������
int min_rates_total,min_rates_total1,min_rates_total2;
//---- ���������� ������������ ��������, ������� ����� � 
// ���������� ������������ � �������� ������������ �������
double TSIBuffer[],TriggerBuffer[];
//+------------------------------------------------------------------+   
//| TSI indicator initialization function                            | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_total1=GetStartBars(First_Method,First_Length,First_Phase)+1;
   min_rates_total2=min_rates_total1+GetStartBars(First_Method,First_Length,First_Phase);
   min_rates_total=int(min_rates_total1+GetStartBars(Second_Method,Second_Length,Second_Phase)+TriggerShift);

//---- ��������� ������� �� ������������ �������� ������� ����������
   MTM1.XMALengthCheck("First_Length",First_Length);
   MTM1.XMAPhaseCheck("First_Phase",First_Phase, First_Method);
   MTM1.XMALengthCheck("Second_Length",Second_Length);
   MTM1.XMAPhaseCheck("Second_Phase",Second_Phase,Second_Method);

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,TSIBuffer,INDICATOR_DATA);
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(1,TriggerBuffer,INDICATOR_DATA);
//---- ������������� ������ ���������� 1 �� �����������
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   string Smooth1=MTM1.GetString_MA_Method(First_Method);
   string Smooth2=MTM1.GetString_MA_Method(Second_Method);
   StringConcatenate(shortname,"TSI-Oscillator(",Smooth1,", ",First_Length,", ",Smooth2,", ",Second_Length,")");
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);

//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//---- ���������� �������������
  }
//+------------------------------------------------------------------+ 
//| TSI iteration function                                           | 
//+------------------------------------------------------------------+ 
int OnCalculate(
                const int rates_total,    // ���������� ������� � ����� �� ������� ����
                const int prev_calculated,// ���������� ������� � ����� �� ���������� ����
                const int begin,          // ����� ������ ������������ ������� �����
                const double &price[]     // ������� ������ ��� ������� ����������
                )
  {
//---- �������� ���������� ����� �� ������������� ��� �������
   if(rates_total<min_rates_total+begin) return(0);

//---- ���������� ���������� � ��������� ������  
   double dprice,absdprice,mtm1,absmtm1,mtm2,absmtm2;
//---- ���������� ����� ���������� � ��������� ��� ����������� �����
   int first,bar;

//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
      first=1; // ��������� ����� ��� ������� ���� �����
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����

//---- �������� ���� ������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      dprice=price[bar]-price[bar-1];
      absdprice=MathAbs(dprice);
      mtm1=MTM1.XMASeries(1,prev_calculated,rates_total,First_Method,First_Phase,First_Length,dprice,bar,false);
      absmtm1=ABSMTM1.XMASeries(1,prev_calculated,rates_total,First_Method,First_Phase,First_Length,absdprice,bar,false);
      mtm2=MTM2.XMASeries(min_rates_total1,prev_calculated,rates_total,Second_Method,Second_Phase,Second_Length,mtm1,bar,false);
      absmtm2=ABSMTM2.XMASeries(min_rates_total1,prev_calculated,rates_total,Second_Method,Second_Phase,Second_Length,absmtm1,bar,false);
      if(bar>min_rates_total2) TSIBuffer[bar]=100.0*mtm2/absmtm2;
      else TSIBuffer[bar]=EMPTY_VALUE;
      if(bar>min_rates_total) TriggerBuffer[bar]=TSIBuffer[bar-TriggerShift];
      else                    TriggerBuffer[bar]=EMPTY_VALUE;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
