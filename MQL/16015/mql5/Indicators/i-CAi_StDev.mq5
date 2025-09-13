//+---------------------------------------------------------------------+
//|                                                     i-CAi_StDev.mq5 | 
//|                         Copyright � RickD 2006, Alexander Piechotta | 
//|                                        http://onix-trade.net/forum/ | 
//+---------------------------------------------------------------------+ 
//| ��� ������  ����������  �������  �������� ���� SmoothAlgorithms.mqh |
//| � ����� (����������): �������_������_���������\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright � RickD 2006, Alexander Piechotta"
#property link      "http://onix-trade.net/forum/"
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � �������� ����
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
#property indicator_color1  clrDodgerBlue,clrMagenta
//---- ����� ���������� - ����������� ������
#property indicator_style1  STYLE_SOLID
//---- ������� ����� ���������� ����� 3
#property indicator_width1  3
//---- ����������� ����� ����������
#property indicator_label1  "i-CAi"
//+----------------------------------------------+
//|  ��������� ��������� ���������� ����������   |
//+----------------------------------------------+
//---- ��������� ���������� 2 � ���� �������
#property indicator_type2   DRAW_ARROW
//---- � �������� ����� ���������� ���������� ����������� ������� ����
#property indicator_color2  clrRed
//---- ������� ����� ���������� 2 ����� 1
#property indicator_width2  1
//---- ����������� ��������� ����� ����������
#property indicator_label2  "Dn_Signal 1"
//+----------------------------------------------+
//|  ��������� ��������� ������ ����������       |
//+----------------------------------------------+
//---- ��������� ���������� 3 � ���� �������
#property indicator_type3   DRAW_ARROW
//---- � �������� ����� ������� ���������� ����������� ������������� ����
#property indicator_color3  clrAqua
//---- ������� ����� ���������� 3 ����� 1
#property indicator_width3  1
//---- ����������� ����� ����� ����������
#property indicator_label3  "Up_Signal 1"
//+----------------------------------------------+
//|  ��������� ��������� ���������� ����������   |
//+----------------------------------------------+
//---- ��������� ���������� 4 � ���� �������
#property indicator_type4   DRAW_ARROW
//---- � �������� ����� ���������� ���������� ����������� ������� ����
#property indicator_color4  clrRed
//---- ������� ����� ���������� 4 ����� 4
#property indicator_width4  4
//---- ����������� ��������� ����� ����������
#property indicator_label4  "Dn_Signal 2"
//+----------------------------------------------+
//|  ��������� ��������� ������ ����������       |
//+----------------------------------------------+
//---- ��������� ���������� 5 � ���� �������
#property indicator_type5   DRAW_ARROW
//---- � �������� ����� ������� ���������� ����������� ������������� ����
#property indicator_color5  clrAqua
//---- ������� ����� ���������� 5 ����� 4
#property indicator_width5  4
//---- ����������� ����� ����� ����������
#property indicator_label5  "Up_Signal 2"
//+----------------------------------------------+
//|  �������� ������ CXMA                        |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+

//---- ���������� ���������� ������� CXMA � CStdDeviation �� ����� SmoothAlgorithms.mqh
CXMA XMA1;
CStdDeviation STD;
//+----------------------------------------------+
//|  ���������� ������������                     |
//+----------------------------------------------+
enum Applied_price_      //��� ���������
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
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input Smooth_Method XMA_Method=MODE_SMA_; //����� ����������
input uint XLength=12;                    //������� �����������                    
input int XPhase=15;                      //�������� �����������,
//---- ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������;
//---- ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input Applied_price_ IPC=PRICE_CLOSE_;    //������� ���������
input int PriceShift=0;                   //c���� ������� �� ��������� � �������
input double dK1=1.5;                     //����������� 1 ��� ������������� �������
input double dK2=2.5;                     //����������� 2 ��� ������������� �������
input uint std_period=9;                  //������ ������������� �������
input int Shift=0;                        //����� ���������� �� ����������� � �����
//+----------------------------------------------+
//---- ���������� ������������ ��������, ������� ����� � ���������� ������������ � �������� ������������ �������
double ExtLineBuffer[],ColorExtLineBuffer[];
double BearsBuffer1[],BullsBuffer1[];
double BearsBuffer2[],BullsBuffer2[];

//---- ���������� ���������� �������� ������������� ������ �������
double dPriceShift,dLine[];
//---- ���������� ����� ���������� ������ ������� ������
int min_rates_total;
//+------------------------------------------------------------------+   
//| i-CAi indicator initialization function                          | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_total=GetStartBars(XMA_Method,XLength,XPhase)+int(std_period);
//---- ������������� ������ �� ���������
   dPriceShift=_Point*PriceShift;
//---- ������������� ������ ��� ������� ����������  
   ArrayResize(dLine,std_period);

//---- ����������� ������������� ������� ExtLineBuffer � ������������ �����
   SetIndexBuffer(0,ExtLineBuffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
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
   string Smooth1=XMA1.GetString_MA_Method(XMA_Method);
   StringConcatenate(shortname,"i-CAi(",XLength,", ",Smooth1,")");
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);

//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
//---- ���������� �������������
  }
//+------------------------------------------------------------------+ 
//| i-CAi iteration function                                         | 
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
   double price,xma,stdev,powstdev,powdxma,koeff,prev;
   double line;
   double SMAdif,Sum,StDev,dstd,BEARS1,BULLS1,BEARS2,BULLS2,Filter1,Filter2;
//---- ���������� ����� ���������� � ��������� ��� ����������� �����
   int first,bar;

//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
     {
      first=1; // ��������� ����� ��� ������� ���� �����
     }
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����

//---- �������� ���� ������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      //---- ����� ������� PriceSeries ��� ��������� ������� ���� price
      price=PriceSeries(IPC,bar,open,low,high,close);
      xma=XMA1.XMASeries(1,prev_calculated,rates_total,XMA_Method,XPhase,XLength,price,bar,false);
      stdev=STD.StdDevSeries(1,prev_calculated,rates_total,XLength,1,price,xma,bar,false);
      powstdev=MathPow(stdev,2);
      if(bar>min_rates_total) prev=ExtLineBuffer[bar-1];
      else prev=xma;
      powdxma=MathPow(prev-xma,2);
      if(powdxma<powstdev || !powdxma) koeff=0.0;
      else koeff=1.0-powstdev/powdxma;
      ExtLineBuffer[bar]=prev+koeff*(xma-prev);
     }
//---- �������� ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
      first++;

//---- �������� ���� ��������� ���������� �����
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      int clr=int(ColorExtLineBuffer[bar-1]);
      if(ExtLineBuffer[bar-1]<ExtLineBuffer[bar]) clr=0;
      if(ExtLineBuffer[bar-1]>ExtLineBuffer[bar]) clr=1;
      ColorExtLineBuffer[bar]=clr;
     }

//---- �������� ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
      first=min_rates_total;
//---- �������� ���� ������� ���������� ����������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      //---- ��������� ���������� ���������� � ������ ��� ������������� ����������
      for(int iii=0; iii<int(std_period); iii++) dLine[iii]=ExtLineBuffer[bar-iii]-ExtLineBuffer[bar-iii-1];

      //---- ������� ������� ������� ���������� ����������
      Sum=0.0;
      for(int iii=0; iii<int(std_period); iii++) Sum+=dLine[iii];
      SMAdif=Sum/std_period;

      //---- ������� ����� ��������� ��������� ���������� � ��������
      Sum=0.0;
      for(int iii=0; iii<int(std_period); iii++) Sum+=MathPow(dLine[iii]-SMAdif,2);

      //---- ���������� �������� �������� ������������������� ���������� StDev �� ���������� ����������
      StDev=MathSqrt(Sum/std_period);

      //---- ������������� ����������
      dstd=NormalizeDouble(dLine[0],_Digits+2);
      Filter1=NormalizeDouble(dK1*StDev,_Digits+2);
      Filter2=NormalizeDouble(dK2*StDev,_Digits+2);
      BEARS1=EMPTY_VALUE;
      BULLS1=EMPTY_VALUE;
      BEARS2=EMPTY_VALUE;
      BULLS2=EMPTY_VALUE;
      line=ExtLineBuffer[bar];

      //---- ���������� ������������ ��������
      if(dstd<-Filter1 && dstd>=-Filter2) BEARS1=line; //���� ���������� �����
      if(dstd<-Filter2) BEARS2=line; //���� ���������� �����
      if(dstd>+Filter1 && dstd<=+Filter2) BULLS1=line; //���� ���������� �����
      if(dstd>+Filter2) BULLS2=line; //���� ���������� �����

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
