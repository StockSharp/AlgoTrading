//+---------------------------------------------------------------------+
//|                                                 ColorJ2JMAStDev.mq5 | 
//|                                  Copyright � 2015, Nikolay Kositsin | 
//|                                 Khabarovsk,   farria@mail.redcom.ru | 
//+---------------------------------------------------------------------+ 
//| ��� ������  ����������  �������  �������� ���� SmoothAlgorithms.mqh |
//| � ����� (����������): �������_������_���������\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright � 2015, Nikolay Kositsin"
#property link "farria@mail.redcom.ru"
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ������� ����
#property indicator_chart_window 
//---- ��� ������� � ��������� ���������� ������������ ����� �������
#property indicator_buffers 6
//---- ������������ ����� ���� ����������� ����������
#property indicator_plots   5
//+----------------------------------------------+
//|  ��������� ��������� ����� ����������        |
//+----------------------------------------------+
//---- ��������� ���������� � ���� �����
#property indicator_type1   DRAW_COLOR_LINE
//---- � �������� ������ ���������� ����� ������������
#property indicator_color1  clrGray,clrBlue,clrRed
//---- ����� ���������� - ����������� ������
#property indicator_style1  STYLE_SOLID
//---- ������� ����� ���������� ����� 2
#property indicator_width1  2
//---- ����������� ����� ����������
#property indicator_label1  "J2JMA"
//+----------------------------------------------+
//|  ��������� ��������� ���������� ����������   |
//+----------------------------------------------+
//---- ��������� ���������� 2 � ���� �������
#property indicator_type2   DRAW_ARROW
//---- � �������� ����� ���������� ���������� ����������� ������� ����
#property indicator_color2  clrMagenta
//---- ������� ����� ���������� 2 ����� 2
#property indicator_width2  2
//---- ����������� ��������� ����� ����������
#property indicator_label2  "Dn_Signal 1"
//+----------------------------------------------+
//|  ��������� ��������� ������ ����������       |
//+----------------------------------------------+
//---- ��������� ���������� 3 � ���� �������
#property indicator_type3   DRAW_ARROW
//---- � �������� ����� ������� ���������� ����������� ��������� ����
#property indicator_color3  clrChartreuse
//---- ������� ����� ���������� 3 ����� 2
#property indicator_width3  2
//---- ����������� ����� ����� ����������
#property indicator_label3  "Up_Signal 1"
//+----------------------------------------------+
//|  ��������� ��������� ���������� ����������   |
//+----------------------------------------------+
//---- ��������� ���������� 4 � ���� �������
#property indicator_type4   DRAW_ARROW
//---- � �������� ����� ���������� ���������� ����������� ������� ����
#property indicator_color4  clrMagenta
//---- ������� ����� ���������� 4 ����� 5
#property indicator_width4  5
//---- ����������� ��������� ����� ����������
#property indicator_label4  "Dn_Signal 2"
//+----------------------------------------------+
//|  ��������� ��������� ������ ����������       |
//+----------------------------------------------+
//---- ��������� ���������� 5 � ���� �������
#property indicator_type5   DRAW_ARROW
//---- � �������� ����� ������� ���������� ����������� ��������� ����
#property indicator_color5  clrChartreuse
//---- ������� ����� ���������� 5 ����� 5
#property indicator_width5  5
//---- ����������� ����� ����� ����������
#property indicator_label5  "Up_Signal 2"
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
//|  ������� ��������� ����������                |
//+----------------------------------------------+
input double Length1 = 5; // �������  ������� ����������� 
input double Length2 = 5; // �������  ������� �����������                    
input double Phase1= 100; // �������� ������� �����������,
                          //������������ � �������� -100 ... +100,
//������ �� �������� ����������� ��������;
input double Phase2=100;  // �������� ������� �����������,
                          //������������ � �������� -100 ... +100,
//������ �� �������� ����������� ��������;
input Applied_price_ IPC=PRICE_CLOSE_;//������� ���������
input int Shift=0; // ����� ���������� �� ����������� � �����
input int PriceShift=0; // c���� ���������� �� ��������� � �������
input double dK1=1.5;  //����������� 1 ��� ������������� �������
input double dK2=2.5;  //����������� 2 ��� ������������� �������
input uint std_period=9; //������ ������������� �������
//+----------------------------------------------+

//---- ���������� ������������ ��������, ������� ����� � 
// ���������� ������������ � �������� ������������ �������
double ExtLineBuffer[],ColorExtLineBuffer[];
double BearsBuffer1[],BullsBuffer1[];
double BearsBuffer2[],BullsBuffer2[];
//---- ���������� ����� ���������� ������ ������� ������
int min_rates_total;
double dPriceShift,dJ2JMA[];
//+------------------------------------------------------------------+
// �������� ������ CJJMA                                             |
//+------------------------------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+------------------------------------------------------------------+   
//| J2JMA indicator initialization function                          | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- ������������� ���������� ������ ������� ������  
   min_rates_total=60+int(std_period);
//---- ������������� ������ ��� ������� ����������  
   ArrayResize(dJ2JMA,std_period);
   
//---- ����������� ������������� ������� ExtLineBuffer � ������������ �����
   SetIndexBuffer(0,ExtLineBuffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,"J2JMA");
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
//--- ������ �� ��������� ����������� ������ ��������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- ������������� ������ ���������� 2 �� �����������
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);

//---- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(1,ColorExtLineBuffer,INDICATOR_COLOR_INDEX);

//---- ����������� ������������� ������� BearsBuffer � ������������ �����
   SetIndexBuffer(2,BearsBuffer1,INDICATOR_DATA);
//---- ������������� ������ ���������� 2 �� �����������
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- ����� ������� ��� ���������
   PlotIndexSetInteger(1,PLOT_ARROW,159);
//---- ������ �� ��������� ����������� ������ ��������
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- ����������� ������������� ������� BullsBuffer � ������������ �����
   SetIndexBuffer(3,BullsBuffer1,INDICATOR_DATA);
//---- ������������� ������ ���������� 3 �� �����������
   PlotIndexSetInteger(2,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total);
//---- ����� ������� ��� ���������
   PlotIndexSetInteger(2,PLOT_ARROW,159);
//---- ������ �� ��������� ����������� ������ ��������
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- ����������� ������������� ������� BearsBuffer � ������������ �����
   SetIndexBuffer(4,BearsBuffer2,INDICATOR_DATA);
//---- ������������� ������ ���������� 2 �� �����������
   PlotIndexSetInteger(3,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(3,PLOT_DRAW_BEGIN,min_rates_total);
//---- ����� ������� ��� ���������
   PlotIndexSetInteger(3,PLOT_ARROW,159);
//---- ������ �� ��������� ����������� ������ ��������
   PlotIndexSetDouble(3,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- ����������� ������������� ������� BullsBuffer � ������������ �����
   SetIndexBuffer(5,BullsBuffer2,INDICATOR_DATA);
//---- ������������� ������ ���������� 3 �� �����������
   PlotIndexSetInteger(4,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(4,PLOT_DRAW_BEGIN,min_rates_total);
//---- ����� ������� ��� ���������
   PlotIndexSetInteger(4,PLOT_ARROW,159);
//---- ������ �� ��������� ����������� ������ ��������
   PlotIndexSetDouble(4,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   StringConcatenate(shortname,"J2JMA( Length1 = ",Length1,", Length2 = ",Length2,")");
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
//---- ���������� ���������� ������ CJJMA �� ����� JJMASeries_Cls.mqh
   CJJMA JMA;
//---- ��������� ������� �� ������������ �������� ������� ����������
   JMA.JJMALengthCheck("Length1", (int)Length1);
   JMA.JJMALengthCheck("Length2", (int)Length2);
//---- ��������� ������� �� ������������ �������� ������� ����������
   JMA.JJMAPhaseCheck("Phase1", (int)Phase1);
   JMA.JJMAPhaseCheck("Phase2", (int)Phase2);
//---- ������������� ������ �� ���������
   dPriceShift=_Point*PriceShift;
//---- ���������� �������������
  }
//+------------------------------------------------------------------+ 
//| J2JMA iteration function                                         | 
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
   double price_,j1jma,j2jma;
//---- ���������� ����� ���������� � ��������� ��� ����������� �����
   int first,bar;
   double SMAdif,Sum,StDev,dstd,BEARS1,BULLS1,BEARS2,BULLS2,Filter1,Filter2;

//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
      first=0; // ��������� ����� ��� ������� ���� �����
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����

//---- ���������� ���������� ������ CJJMA �� ����� JJMASeries_Cls.mqh
   static CJJMA JMA1,JMA2;

//---- �������� ���� ������� ����������
   for(bar=first; bar<rates_total; bar++)
     {
      //---- ����� ������� PriceSeries ��� ��������� ������� ���� price_
      price_=PriceSeries(IPC,bar,open,low,high,close);

      //---- ��� ������ ������� JJMASeries. 
      //��������� Phase � Length �� �������� �� ������ ���� (Din = 0)
      //�� ������ ������ �������� begin �������� �� 30 �. �. ��� ��������� JMA �����������  
      j1jma = JMA1.JJMASeries( 0, prev_calculated, rates_total, 0, Phase1, Length1, price_, bar, false);
      j2jma = JMA2.JJMASeries(30, prev_calculated, rates_total, 0, Phase2, Length2,  j1jma, bar, false);
      //----       
      ExtLineBuffer[bar]=j2jma+dPriceShift;
     }

//---- �������� ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
      first++;

//---- �������� ���� ��������� ���������� �����
   for(bar=first; bar<rates_total; bar++)
     {
      ColorExtLineBuffer[bar]=0;
      if(ExtLineBuffer[bar-1]<ExtLineBuffer[bar]) ColorExtLineBuffer[bar]=1;
      if(ExtLineBuffer[bar-1]>ExtLineBuffer[bar]) ColorExtLineBuffer[bar]=2;
     }
//---- �������� ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
      first=min_rates_total;
//---- �������� ���� ������� ���������� ����������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      //---- ��������� ���������� ���������� � ������ ��� ������������� ����������
      for(int iii=0; iii<int(std_period); iii++) dJ2JMA[iii]=ExtLineBuffer[bar-iii]-ExtLineBuffer[bar-iii-1];

      //---- ������� ������� ������� ���������� ����������
      Sum=0.0;
      for(int iii=0; iii<int(std_period); iii++) Sum+=dJ2JMA[iii];
      SMAdif=Sum/std_period;

      //---- ������� ����� ��������� ��������� ���������� � ��������
      Sum=0.0;
      for(int iii=0; iii<int(std_period); iii++) Sum+=MathPow(dJ2JMA[iii]-SMAdif,2);

      //---- ���������� �������� �������� ������������������� ���������� StDev �� ���������� ����������
      StDev=MathSqrt(Sum/std_period);

      //---- ������������� ����������
      dstd=NormalizeDouble(dJ2JMA[0],_Digits+2);
      Filter1=NormalizeDouble(dK1*StDev,_Digits+2);
      Filter2=NormalizeDouble(dK2*StDev,_Digits+2);
      BEARS1=EMPTY_VALUE;
      BULLS1=EMPTY_VALUE;
      BEARS2=EMPTY_VALUE;
      BULLS2=EMPTY_VALUE;
      j2jma=ExtLineBuffer[bar];

      //---- ���������� ������������ ��������
      if(dstd<-Filter1 && dstd>=-Filter2) BEARS1=j2jma; //���� ���������� �����
      if(dstd<-Filter2) BEARS2=j2jma; //���� ���������� �����
      if(dstd>+Filter1 && dstd<=+Filter2) BULLS1=j2jma; //���� ���������� �����
      if(dstd>+Filter2) BULLS2=j2jma; //���� ���������� �����

      //---- ������������� ����� ������������ ������� ����������� ���������� 
      BullsBuffer1[bar]=BULLS1;
      BearsBuffer1[bar]=BEARS1;
      BullsBuffer2[bar]=BULLS2;
      BearsBuffer2[bar]=BEARS2;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
