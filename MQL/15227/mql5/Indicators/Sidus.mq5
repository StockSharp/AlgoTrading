//+------------------------------------------------------------------+
//|                                                        Sidus.mq5 | 
//|                                  Copyright � 2006, GwadaTradeBoy |
//|                                            racooni_1975@yahoo.fr |
//+------------------------------------------------------------------+
#property copyright "Copyright � 2006, GwadaTradeBoy"
#property link      "racooni_1975@yahoo.fr"
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ������� ����
#property indicator_chart_window 
//---- ���������� ������������ ������� 6
#property indicator_buffers 6 
//---- ������������ 4 ����������� ����������
#property indicator_plots   4
//+-----------------------------------+
//|  ���������� ��������              |
//+-----------------------------------+
#define RESET  0 // ��������� ��� �������� ��������� ������� �� �������� ����������
//+-----------------------------------+
//|  ��������� ��������� ����������   |
//+-----------------------------------+
//---- ��������� ���������� 1 � ���� �������
#property indicator_type1   DRAW_ARROW
//---- � �������� ����� ���������� ����������� ���� Teal
#property indicator_color1  Teal
//---- ������� ����� ���������� 1 ����� 4
#property indicator_width1  4
//---- ����������� ����� ����������
#property indicator_label1 "Sidus Buy"
//+-----------------------------------+
//|  ��������� ��������� ����������   |
//+-----------------------------------+
//---- ��������� ���������� 2 � ���� �������
#property indicator_type2   DRAW_ARROW
//---- � �������� ����� ���������� ����������� MediumVioletRed
#property indicator_color2  MediumVioletRed
//---- ������� ����� ���������� 2 ����� 4
#property indicator_width2  4
//---- ����������� ����� ����������
#property indicator_label2  "Sidus Sell"
//+-----------------------------------+
//|  ��������� ��������� ����������   |
//+-----------------------------------+
//---- ��������� ���������� � ���� ������
#property indicator_type3 DRAW_FILLING
//---- � �������� ������ ���������� ������������ BlueViolet � Magenta
#property indicator_color3 BlueViolet,Magenta
//---- ����������� ����� ����������
#property indicator_label3  "Sidus Fast EMA"
//+-----------------------------------+
//|  ��������� ��������� ����������   |
//+-----------------------------------+
//---- ��������� ���������� � ���� ������
#property indicator_type4 DRAW_FILLING
//---- � �������� ������ ���������� ������������ ����� Lime � Red
#property indicator_color4 Lime,Red
//---- ����������� ����� ����������
#property indicator_label4  "Sidus Fast LWMA"

//+-----------------------------------+
//| ������� ��������� ����������      |
//+-----------------------------------+
input uint FastEMA=18;                    // ������ ������� EMA
input uint SlowEMA=28;                    // ������ ��������� EMA
input uint FastLWMA=5;                    // ������ ������� LWMA
input uint SlowLWMA=8;                    // ������ ��������� LWMA
input ENUM_APPLIED_PRICE IPC=PRICE_CLOSE; // ������� ���������
extern uint digit=0;                      // ������ � �������
//+-----------------------------------+
//---- ���������� ������������ ��������, ������� � ����������
//---- ����� ������������ � �������� ������������ �������
double FstEmaBuffer[],SlwEmaBuffer[],FstLwmaBuffer[],SlwLwmaBuffer[];
double SellBuffer[],BuyBuffer[];
double DIGIT;
//---- ���������� ������������� ���������� ������ ������� ������
int min_rates_total;
//---- ���������� ������������� ���������� ��� ������� �����������
int FstEma_Handle,SlwEma_Handle,FstLwma_Handle,SlwLwma_Handle,ATR_Handle;
//+------------------------------------------------------------------+   
//| Sidus indicator initialization function                          | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_total=int(MathMax(FastLWMA,SlowLWMA)+3);

//---- ������������� ����������  
   DIGIT=digit*_Point;

//---- ��������� ������ ���������� ATR
   ATR_Handle=iATR(NULL,0,15);
   if(ATR_Handle==INVALID_HANDLE) Print(" �� ������� �������� ����� ���������� ATR");

//---- ��������� ������ ���������� FastEMA
   FstEma_Handle=iMA(NULL,0,FastEMA,0,MODE_EMA,IPC);
   if(FstEma_Handle==INVALID_HANDLE) Print(" �� ������� �������� ����� ���������� FastEMA");

//---- ��������� ������ ���������� SlowEma
   SlwEma_Handle=iMA(NULL,0,SlowEMA,0,MODE_EMA,IPC);
   if(SlwEma_Handle==INVALID_HANDLE) Print(" �� ������� �������� ����� ���������� SlowEma");

//---- ��������� ������ ���������� FastLWMA
   FstLwma_Handle=iMA(NULL,0,FastLWMA,0,MODE_LWMA,IPC);
   if(FstLwma_Handle==INVALID_HANDLE) Print(" �� ������� �������� ����� ���������� FastLWMA");

//---- ��������� ������ ���������� SlowLWMA
   SlwLwma_Handle=iMA(NULL,0,SlowLWMA,0,MODE_LWMA,IPC);
   if(SlwLwma_Handle==INVALID_HANDLE) Print(" �� ������� �������� ����� ���������� SlowLWMA");

//---- ����������� ������������� ������� BuyBuffer[] � ������������ �����
   SetIndexBuffer(0,BuyBuffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0);
//---- ������ ��� ����������
   PlotIndexSetInteger(0,PLOT_ARROW,233);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(BuyBuffer,true);

//---- ����������� ������������� ������� SellBuffer[] � ������������ �����
   SetIndexBuffer(1,SellBuffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,0);
//---- ������ ��� ����������
   PlotIndexSetInteger(1,PLOT_ARROW,234);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(SellBuffer,true);

//---- ����������� ������������� ������� FstEmaBuffer[] � ������������ �����
   SetIndexBuffer(2,FstEmaBuffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,0);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(FstEmaBuffer,true);

//---- ����������� ������������� ������� SlwEmaBuffer[] � ������������ �����
   SetIndexBuffer(3,SlwEmaBuffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(3,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(3,PLOT_EMPTY_VALUE,0);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(SlwEmaBuffer,true);

//---- ����������� ������������� ������� FstLwmaBuffer[] � ������������ �����
   SetIndexBuffer(4,FstLwmaBuffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(4,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(4,PLOT_EMPTY_VALUE,0);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(FstLwmaBuffer,true);

//---- ����������� ������������� ������� SlwLwmaBuffer[] � ������������ �����
   SetIndexBuffer(5,SlwLwmaBuffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(5,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(5,PLOT_EMPTY_VALUE,0);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(SlwLwmaBuffer,true);

//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   StringConcatenate(shortname,"Sidus(",FastEMA,", ",SlowEMA,", ",FastLWMA,", ",SlowLWMA,")");
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);

//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- ���������� �������������
  }
//+------------------------------------------------------------------+ 
//| Sidus iteration function                                          | 
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
   if(BarsCalculated(ATR_Handle)<rates_total
      || BarsCalculated(FstEma_Handle)<rates_total
      || BarsCalculated(SlwEma_Handle)<rates_total
      || BarsCalculated(FstLwma_Handle)<rates_total
      || BarsCalculated(SlwLwma_Handle)<rates_total
      || rates_total<min_rates_total)
      return(RESET);

//---- ���������� ��������� ���������� 
   int to_copy,limit,bar;
   double range,ATR[];

//---- ������� ������������ ���������� ���������� ������
//---- � ���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      limit=rates_total-min_rates_total-1; // ��������� ����� ��� ������� ���� �����
      to_copy=rates_total;
     }
   else
     {
      limit=rates_total-prev_calculated; // ��������� ����� ��� ������� ����� �����     
      to_copy=limit+1;
     }

//---- �������� ����� ����������� ������ � ������� ATR[] � ������������ ������
   if(CopyBuffer(ATR_Handle,0,0,to_copy,ATR)<=0) return(RESET);
   if(CopyBuffer(FstEma_Handle,0,0,to_copy,FstEmaBuffer)<=0) return(RESET);
   if(CopyBuffer(SlwEma_Handle,0,0,to_copy,SlwEmaBuffer)<=0) return(RESET);
   if(CopyBuffer(FstLwma_Handle,0,0,to_copy,FstLwmaBuffer)<=0) return(RESET);
   if(CopyBuffer(SlwLwma_Handle,0,0,to_copy,SlwLwmaBuffer)<=0) return(RESET);

//---- ���������� ��������� � �������� ��� � ����������  
   ArraySetAsSeries(ATR,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);

//---- �������� ���� ������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      range=ATR[bar]*3/8;
      BuyBuffer[bar]=0.0;
      SellBuffer[bar]=0.0;

      if(FstLwmaBuffer[bar]>SlwLwmaBuffer[bar]+DIGIT && FstLwmaBuffer[bar+1]<=SlwLwmaBuffer[bar+1]) BuyBuffer[bar]=low[bar]-range;
      if(SlwLwmaBuffer[bar]>SlwEmaBuffer[bar]+DIGIT && SlwLwmaBuffer[bar+1]<=SlwEmaBuffer[bar]) BuyBuffer[bar]=low[bar]-range;

      if(FstLwmaBuffer[bar]<SlwLwmaBuffer[bar]-DIGIT && FstLwmaBuffer[bar+1]>=SlwLwmaBuffer[bar+1]) SellBuffer[bar]=high[bar]+range;
      if(SlwLwmaBuffer[bar]<SlwEmaBuffer[bar]-DIGIT && SlwLwmaBuffer[bar+1]>=SlwEmaBuffer[bar]) SellBuffer[bar]=high[bar]+range;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
