//+------------------------------------------------------------------+
//|                                                  ADXCrossing.mq5 |
//|                                           Copyright � 2005, Amir |
//|                                                                  |
//+------------------------------------------------------------------+
//--- ��������� ����������
#property copyright "Copyright � 2005, Amir"
//--- ������ �� ���� ������
#property link      ""
//--- ����� ������ ����������
#property version   "1.00"
//--- ��������� ���������� � ������� ����
#property indicator_chart_window 
//--- ��� ������� � ��������� ���������� ������������ ��� ������
#property indicator_buffers 2
//--- ������������ ����� ��� ����������� ����������
#property indicator_plots   2
//+----------------------------------------------+
//| ��������� ��������� ���������� ����������    |
//+----------------------------------------------+
//--- ��������� ���������� 1 � ���� �������
#property indicator_type1   DRAW_ARROW
//--- � �������� ����� ��������� ����� ���������� ����������� ������� ����
#property indicator_color1  clrMagenta
//--- ������� ����� ���������� 1 ����� 2
#property indicator_width1  2
//--- ����������� ����� ����� ����������
#property indicator_label1  "ADXCrossing Sell"
//+----------------------------------------------+
//| ��������� ��������� ������ ����������        |
//+----------------------------------------------+
//--- ��������� ���������� 2 � ���� �������
#property indicator_type2   DRAW_ARROW
//--- � �������� ����� ����� ����� ���������� ����������� ������� ����
#property indicator_color2  clrLime
//--- ������� ����� ���������� 2 ����� 2
#property indicator_width2  2
//--- ����������� ��������� ����� ����������
#property indicator_label2 "ADXCrossing Buy"
//+----------------------------------------------+
//| ���������� ��������                          |
//+----------------------------------------------+
#define RESET 0  // ��������� ��� �������� ��������� ������� �� �������� ����������
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input uint ADXPeriod=50;
//+----------------------------------------------+
//--- ���������� ������������ ��������, ������� � ����������
//--- ����� ������������ � �������� ������������ �������
double SellBuffer[];
double BuyBuffer[];
//--- ���������� ������������� ���������� ������ ������� ������
int min_rates_total;
//--- ���������� ������������� ���������� ��� ������� �����������
int ATR_Handle,ADX_Handle;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- ������������� ���������� ���������� 
   int AtrPeriod=14;
   min_rates_total=int(ADXPeriod)+1;
   min_rates_total=int(MathMax(min_rates_total,AtrPeriod));
//--- ��������� ������ ���������� ATR
   ATR_Handle=iATR(NULL,0,AtrPeriod);
   if(ATR_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� ATR");
      return(INIT_FAILED);
     }
//--- ��������� ������ ���������� iADX
   ADX_Handle=iADX(NULL,0,ADXPeriod);
   if(ADX_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iADX");
      return(INIT_FAILED);
     }
//--- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,SellBuffer,INDICATOR_DATA);
//--- ������������� ������ ������ ������� ��������� ���������� 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- ������ ��� ����������
   PlotIndexSetInteger(0,PLOT_ARROW,108);
//--- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(SellBuffer,true);
//--- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(1,BuyBuffer,INDICATOR_DATA);
//--- ������������� ������ ������ ������� ��������� ���������� 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//--- ������ ��� ����������
   PlotIndexSetInteger(1,PLOT_ARROW,108);
//--- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(BuyBuffer,true);
//--- ��������� ������� �������� ����������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//--- ��� ��� ���� ������ � ����� ��� ������� 
   string short_name="ADXCrossing";
   IndicatorSetString(INDICATOR_SHORTNAME,short_name);
//--- ���������� �������������
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,
                const int prev_calculated,
                const datetime &time[],
                const double &open[],
                const double &high[],
                const double &low[],
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//--- �������� ���������� ����� �� ������������� ��� �������
   if(BarsCalculated(ATR_Handle)<rates_total
      || BarsCalculated(ADX_Handle)<rates_total
      || rates_total<min_rates_total) return(RESET);
//--- ���������� ��������� ���������� 
   int to_copy,limit,bar;
   double ATR[],Up[],Dn[];
//--- ������� ������������ ���������� ���������� ������
//--- � ���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      limit=rates_total-min_rates_total; // ��������� ����� ��� ������� ���� �����
     }
   else
     {
      limit=rates_total-prev_calculated; // ��������� ����� ��� ������� ����� �����
     }
   to_copy=limit+1;
//--- �������� ����� ����������� ������ � �������
   if(CopyBuffer(ATR_Handle,0,0,to_copy,ATR)<=0) return(RESET);
   to_copy++;
   if(CopyBuffer(ADX_Handle,1,0,to_copy,Up)<=0) return(RESET);
   if(CopyBuffer(ADX_Handle,2,0,to_copy,Dn)<=0) return(RESET);
//--- ���������� ��������� � �������� ��� � ����������  
   ArraySetAsSeries(Up,true);
   ArraySetAsSeries(Dn,true);
   ArraySetAsSeries(ATR,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
//--- �������� ���� ������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      BuyBuffer[bar]=0.0;
      SellBuffer[bar]=0.0;
      //---
      if(Up[bar]>Dn[bar] &&  Up[bar+1]<=Dn[bar+1]) BuyBuffer[bar]=low[bar]-ATR[bar]*3/8;
      if(Up[bar]<Dn[bar] &&  Up[bar+1]>=Dn[bar+1]) SellBuffer[bar]=high[bar]+ATR[bar]*3/8;
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
