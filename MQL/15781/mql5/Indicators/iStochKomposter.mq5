//+------------------------------------------------------------------+
//|                                              iStochKomposter.mq5 |
//|                                      Copyright � 2006, komposter |
//|                                      mailto:komposterius@mail.ru |
//+------------------------------------------------------------------+
//--- ��������� ����������
#property copyright "Copyright � 2006, komposter"
//--- ������ �� ���� ������
//--- ����� ������ ����������
#property version   "1.00"
//--- ��������� ���������� � ������� ����
#property indicator_chart_window 
//--- ��� ������� � ��������� ���������� ������������ ��� ������
#property indicator_buffers 2
//--- ������������ ����� ��� ����������� ����������
#property indicator_plots   2
//+----------------------------------------------+
//|  ��������� ��������� ���������� ����������   |
//+----------------------------------------------+
//--- ��������� ���������� 1 � ���� �������
#property indicator_type1   DRAW_ARROW
//--- � �������� ����� ���������� ���������� ����������� ��������� ����
#property indicator_color1  clrLightSalmon
//--- ������� ����� ���������� 1 ����� 4
#property indicator_width1  4
//--- ����������� ����� ����� ����������
#property indicator_label1  "iStochKomposter Sell"
//+----------------------------------------------+
//|  ��������� ��������� ������ ����������       |
//+----------------------------------------------+
//--- ��������� ���������� 2 � ���� �������
#property indicator_type2   DRAW_ARROW
//--- � �������� ����� ������� ���������� ����������� ������� ����
#property indicator_color2  clrLime
//--- ������� ����� ���������� 2 ����� 4
#property indicator_width2  4
//--- ����������� ��������� ����� ����������
#property indicator_label2 "iStochKomposter Buy"
//+----------------------------------------------+
//|  ���������� ��������                         |
//+----------------------------------------------+
#define RESET  0 // ��������� ��� �������� ��������� ������� �� �������� ����������
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input uint ATR_Period=14;
input uint KPeriod=5;
input uint DPeriod=3;
input uint Slowing=3;
input ENUM_MA_METHOD STO_Method=MODE_SMA;
input ENUM_STO_PRICE Price_field=STO_LOWHIGH;
input uint UpLevel=70; //������� ���������������
input uint DnLevel=30; //������� ���������������
//+----------------------------------------------+
//--- ���������� ������������ ��������, ������� � ����������
//--- ����� ������������ � �������� ������������ �������
double SellBuffer[];
double BuyBuffer[];
//---
int Sto_Handle,ATR_Handle,min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- ������������� ���������� ���������� 
   min_rates_total=int(MathMax(KPeriod+DPeriod+Slowing+1,ATR_Period))+1;
//--- ��������� ������ ���������� Stochastic
   Sto_Handle=iStochastic(Symbol(),NULL,KPeriod,DPeriod,Slowing,STO_Method,Price_field);
   if(Sto_Handle==INVALID_HANDLE)
     {
      Print("�� ������� �������� ����� ���������� Stochastic");
      return(INIT_FAILED);
     }
//--- ��������� ������ ���������� ATR
   ATR_Handle=iATR(NULL,0,ATR_Period);
   if(ATR_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� ATR");
      return(INIT_FAILED);
     }
//--- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,SellBuffer,INDICATOR_DATA);
//--- ������������� ������ ������ ������� ��������� ���������� 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- ������ ��� ����������
   PlotIndexSetInteger(0,PLOT_ARROW,234);
//--- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(SellBuffer,true);
//--- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(1,BuyBuffer,INDICATOR_DATA);
//--- ������������� ������ ������ ������� ��������� ���������� 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//--- ������ ��� ����������
   PlotIndexSetInteger(1,PLOT_ARROW,233);
//--- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(BuyBuffer,true);
//--- ��������� ������� �������� ����������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//--- ��� ��� ���� ������ � ����� ��� ������� 
   string short_name="iStochKomposter";
   IndicatorSetString(INDICATOR_SHORTNAME,short_name);
//---   
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
   if(BarsCalculated(Sto_Handle)<rates_total
      || BarsCalculated(ATR_Handle)<rates_total
      || rates_total<min_rates_total)
      return(RESET);

//--- ���������� ��������� ���������� 
   int to_copy,limit,bar;
   double STO[],ATR[];

//--- ������� ������������ ���������� ���������� ������ �
//���������� ������ limit ��� ����� ��������� �����
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
   if(CopyBuffer(Sto_Handle,0,MAIN_LINE,to_copy+1,STO)<=0) return(RESET);
   if(CopyBuffer(ATR_Handle,0,0,to_copy,ATR)<=0) return(RESET);
//--- ���������� ��������� � �������� ��� � ����������  
   ArraySetAsSeries(STO,true);
   ArraySetAsSeries(ATR,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
//--- �������� ���� ������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      BuyBuffer[bar]=NULL;
      SellBuffer[bar]=NULL;
      if(STO[bar] > DnLevel && STO[bar+1] <= DnLevel) BuyBuffer[bar] = low[bar] - ATR[bar]*3/8;
		    if(STO[bar] < UpLevel && STO[bar+1] >= UpLevel) SellBuffer[bar] = high[bar] + ATR[bar]*3/8;         
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
