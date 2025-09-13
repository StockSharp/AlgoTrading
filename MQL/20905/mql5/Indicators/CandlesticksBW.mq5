//+------------------------------------------------------------------+
//|                                               CandlesticksBW.mq5 |
//|                                       Copyright � 2008, Vladimir | 
//|                                         finance@allmotion.com.ua | 
//+------------------------------------------------------------------+
#property copyright "Copyright � 2008, Vladimir"
#property link "finance@allmotion.com.ua"
#property description "CandlesticksBW"
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ������� ����
#property indicator_chart_window
//---- ��� ������� � ��������� ���������� ������������ ���� �������
#property indicator_buffers 5
//---- ������������ ����� ���� ����������� ����������
#property indicator_plots   1
//+----------------------------------------------+
//| ��������� ��������� ����������               |
//+----------------------------------------------+
//---- � �������� ���������� ������������ ������� �����
#property indicator_type1   DRAW_COLOR_CANDLES
#property indicator_color1   clrAqua,clrBlue,clrGreen,clrRed,clrPurple,clrMagenta
//---- ����������� ����� ����������
#property indicator_label1  "CandlesticksBW/Open;High;Low;Close"
//+----------------------------------------------+
//| ���������� ��������                          |
//+----------------------------------------------+
#define RESET  0 // ��������� ��� �������� ��������� ������� �� �������� ����������
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+

//+----------------------------------------------+
//---- ���������� ������������ ��������, ������� ����� � 
//---- ���������� ������������ � �������� ������������ �������
double ExtOpenBuffer[];
double ExtHighBuffer[];
double ExtLowBuffer[];
double ExtCloseBuffer[];
double ExtColorBuffer[];
//---- ���������� ������������� ���������� ������ ������� ������
int min_rates_total;
//---- ���������� ������������� ���������� ��� ������� �����������
int AC_Handle,AO_Handle;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
  {
//---- ������������� ���������� ���������� 
   min_rates_total=34+2;
//---- ��������� ������ ����������   Awesome oscillator 
   AO_Handle=iAO(Symbol(),PERIOD_CURRENT);
   if(AO_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ����������   Awesome oscillator");
      return(INIT_FAILED);
     }
//---- ��������� ������ ����������  Accelerator Oscillator 
   AC_Handle=iAC(Symbol(),PERIOD_CURRENT);
   if(AC_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ����������  Accelerator Oscillator");
      return(INIT_FAILED);
     }

//---- ����������� ������������ �������� � ������������ ������
   SetIndexBuffer(0,ExtOpenBuffer,INDICATOR_DATA);
   SetIndexBuffer(1,ExtHighBuffer,INDICATOR_DATA);
   SetIndexBuffer(2,ExtLowBuffer,INDICATOR_DATA);
   SetIndexBuffer(3,ExtCloseBuffer,INDICATOR_DATA);
//---- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(4,ExtColorBuffer,INDICATOR_COLOR_INDEX);
//---- ���������� ��������� � ������� ��� � ����������
   ArraySetAsSeries(ExtOpenBuffer,true);
   ArraySetAsSeries(ExtHighBuffer,true);
   ArraySetAsSeries(ExtLowBuffer,true);
   ArraySetAsSeries(ExtCloseBuffer,true);
   ArraySetAsSeries(ExtColorBuffer,true);
//---- ������������� ������ ������ ������� ��������� ���������� 1
   PlotIndexSetInteger(4,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� ������� �������� ����������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- ��� ��� ���� ������ � ����� ��� �������� 
   string short_name="CandlesticksBW";
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
//---- �������� ���������� ����� �� ������������� ��� �������
   if(BarsCalculated(AO_Handle)<rates_total
      || BarsCalculated(AC_Handle)<rates_total
      || rates_total<min_rates_total)
      return(RESET);
//---- ���������� ��������� ���������� 
   int to_copy,limit,bar;
   double AO[],AC[];
//---- ������� ������������ ���������� ���������� ������ � ���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      limit=rates_total-min_rates_total-1; // ��������� ����� ��� ������� ���� �����
     }
   else
     {
      limit=rates_total-prev_calculated; // ��������� ����� ��� ������� ����� �����
     }
//---
   to_copy=limit+1;
//---- �������� ����� ����������� ������ � �������
   if(CopyOpen(Symbol(),PERIOD_CURRENT,0,to_copy,ExtOpenBuffer)<=0) return(RESET);
   if(CopyHigh(Symbol(),PERIOD_CURRENT,0,to_copy,ExtHighBuffer)<=0) return(RESET);
   if(CopyLow(Symbol(),PERIOD_CURRENT,0,to_copy,ExtLowBuffer)<=0) return(RESET);
   if(CopyClose(Symbol(),PERIOD_CURRENT,0,to_copy,ExtCloseBuffer)<=0) return(RESET);
   to_copy++;
   if(CopyBuffer(AO_Handle,0,0,to_copy,AO)<=0) return(RESET);
   if(CopyBuffer(AC_Handle,0,0,to_copy,AC)<=0) return(RESET);

//---- ���������� ��������� � �������� ��� � ����������  
   ArraySetAsSeries(AO,true);
   ArraySetAsSeries(AC,true);
   ArraySetAsSeries(open,true);
   ArraySetAsSeries(close,true);

//---- �������� ���� ����������� � ����������� ������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      int clr;
      
      if(AO[bar]>=AO[bar+1] && AC[bar]>=AC[bar+1])
        {
         if(open[bar]<=close[bar]) clr=0;
         else clr=1;
        }
      else
      if(AO[bar]<=AO[bar+1] && AC[bar]<=AC[bar+1])
        {
         if(open[bar]>=close[bar]) clr=5;
         else clr=4;
        }
      else
        {
         if(open[bar]<=close[bar]) clr=2;
         else clr=3;        
        }
      ExtColorBuffer[bar]=clr;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
