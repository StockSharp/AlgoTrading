//+------------------------------------------------------------------+
//|                                           ColorMaRsi-Trigger.mq5 | 
//|                              Copyright � 2010, fx-system@mail.ru |
//|                                                fx-system@mail.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright � 2010, fx-system@mail.ru"
#property link      "fx-system@mail.ru"
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window
//---- ���������� ������������ ������� 2
#property indicator_buffers 2 
//---- ������������ ����� ���� ����������� ����������
#property indicator_plots   1
//+-----------------------------------+
//| ��������� ��������� ����������    |
//+-----------------------------------+
//---- ��������� ���������� � ���� ������
#property indicator_type1   DRAW_FILLING
//---- � �������� ������ ������ ���������� ������������
#property indicator_color1 clrMagenta,clrRoyalBlue
//---- ����������� ����� ����������
#property indicator_label1  "MaRsi-Trigger"
//+----------------------------------------------+
//| ���������� ��������                          |
//+----------------------------------------------+
#define RESET  0 // ��������� ��� �������� ��������� ������� �� �������� ����������
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input uint nPeriodRsi=3;
input ENUM_APPLIED_PRICE nRSIPrice=PRICE_WEIGHTED;
input uint nPeriodRsiLong=13;
input ENUM_APPLIED_PRICE nRSIPriceLong=PRICE_MEDIAN;
input uint nPeriodMa=5;
input  ENUM_MA_METHOD nMAType=MODE_EMA;
input ENUM_APPLIED_PRICE nMAPrice=PRICE_CLOSE;
input uint nPeriodMaLong=10;
input  ENUM_MA_METHOD nMATypeLong=MODE_EMA;
input ENUM_APPLIED_PRICE nMAPriceLong=PRICE_CLOSE;
input int  Shift=0; // ����� ���������� �� ����������� � �����
//+-----------------------------------+
//---- ������������ ������
double ExtMapBuffer[];
double ExtZerBuffer[];
//---- ���������� ������������� ���������� ������ ������� ������
int min_rates_total;
//---- ���������� ������������� ���������� ��� ������� �����������
int MA_Handle,RSI_Handle,MAl_Handle,RSIl_Handle;
//+------------------------------------------------------------------+   
//| MaRsi-Trigger indicator initialization function                  | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- ��������� ������ ���������� iRSI
   RSI_Handle=iRSI(NULL,0,nPeriodRsi,nRSIPrice);
   if(RSI_Handle==INVALID_HANDLE) Print(" �� ������� �������� ����� ���������� iRSI");
//---- ��������� ������ ���������� iRSIl
   RSIl_Handle=iRSI(NULL,0,nPeriodRsiLong,nRSIPriceLong);
   if(RSIl_Handle==INVALID_HANDLE) Print(" �� ������� �������� ����� ���������� iRSIl");
//---- ��������� ������ ���������� iMA
   MA_Handle=iMA(NULL,0,nPeriodMa,0,nMAType,nMAPrice);
   if(MA_Handle==INVALID_HANDLE) Print(" �� ������� �������� ����� ���������� iMA");
//---- ��������� ������ ���������� iMAl
   MAl_Handle=iMA(NULL,0,nPeriodMaLong,0,nMATypeLong,nMAPriceLong);
   if(MAl_Handle==INVALID_HANDLE) Print(" �� ������� �������� ����� ���������� iMAl");
//---- ������������� ���������� ������ ������� ������
   min_rates_total=int(MathMax(MathMax(MathMax(nPeriodRsi,nPeriodRsiLong),nPeriodMa),nPeriodMaLong));
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,ExtZerBuffer,INDICATOR_DATA);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(ExtZerBuffer,true);
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(1,ExtMapBuffer,INDICATOR_DATA);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(ExtMapBuffer,true);
//---- ������������� ������ ���������� 1 �� �����������
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname="MaRsi-Trigger()";
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//---- ���������� �������������
  }
//+------------------------------------------------------------------+ 
//| MaRsi-Trigger iteration function                                 | 
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
   if(BarsCalculated(MA_Handle)<rates_total
      || BarsCalculated(MAl_Handle)<rates_total
      || BarsCalculated(RSI_Handle)<rates_total
      || BarsCalculated(RSIl_Handle)<rates_total
      || rates_total<min_rates_total)
      return(RESET);
//---- ���������� ��������� ���������� 
   int to_copy,limit,bar;
   double res,MA_[],MAl_[],RSI_[],RSIl_[];
//---- ������� ������������ ���������� ���������� ������ �
//---- ���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      limit=rates_total-min_rates_total-1; // ��������� ����� ��� ������� ���� �����
     }
   else
     {
      limit=rates_total-prev_calculated; // ��������� ����� ��� ������� ����� �����
     }
//----
   to_copy=limit+1;
//---- �������� ����� ����������� ������ � �������
   if(CopyBuffer(MA_Handle,0,0,to_copy,MA_)<=0) return(RESET);
   if(CopyBuffer(MAl_Handle,0,0,to_copy,MAl_)<=0) return(RESET);
   if(CopyBuffer(RSI_Handle,0,0,to_copy,RSI_)<=0) return(RESET);
   if(CopyBuffer(RSIl_Handle,0,0,to_copy,RSIl_)<=0) return(RESET);
//---- ���������� ��������� � �������� ��� � ����������  
   ArraySetAsSeries(MA_,true);
   ArraySetAsSeries(MAl_,true);
   ArraySetAsSeries(RSI_,true);
   ArraySetAsSeries(RSIl_,true);
//---- �������� ���� ������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      res=0;
      if(MA_[bar] > MAl_[bar]) res = +1;
      if(MA_[bar] < MAl_[bar]) res = -1;
      //----
      if(RSI_[bar] > RSIl_[bar]) res += 1;
      if(RSI_[bar] < RSIl_[bar]) res -= 1;
      //----
      ExtMapBuffer[bar]=MathMax(MathMin(1,res),-1);
      ExtZerBuffer[bar]=0;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
