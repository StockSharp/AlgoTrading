//+------------------------------------------------------------------+
//|                                                   BykovTrend.mq5 |
//|                                        Ramdass - Conversion only |
//|                                                                  |
//+------------------------------------------------------------------+
//--- ��������� ����������
#property copyright "Ramdass - Conversion only"
//--- ������ �� ���� ������
#property link      ""
//--- ����� ������ ����������
#property version   "1.02"
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
//--- � �������� ����� ��������� ����� ���������� ����������� ������� ����
#property indicator_color1  clrMagenta
//--- ������� ����� ���������� 1 ����� 4
#property indicator_width1  4
//--- ����������� ����� ����� ����������
#property indicator_label1  "BykovTrend Sell"
//+----------------------------------------------+
//|  ��������� ��������� ������ ����������       |
//+----------------------------------------------+
//--- ��������� ���������� 2 � ���� �������
#property indicator_type2   DRAW_ARROW
//--- � �������� ����� ����� ����� ���������� ����������� ������� ����
#property indicator_color2  clrLime
//--- ������� ����� ���������� 2 ����� 4
#property indicator_width2  4
//--- ����������� ��������� ����� ����������
#property indicator_label2 "BykovTrend Buy"
//+----------------------------------------------+
//|  ���������� ��������                         |
//+----------------------------------------------+
#define RESET  0 // ��������� ��� �������� ��������� ������� �� �������� ����������
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input int RISK=3;
input int SSP=9;
//+----------------------------------------------+
//--- ���������� ������������ ��������, ������� � ����������
//--- ����� ������������ � �������� ������������ �������
double SellBuffer[];
double BuyBuffer[];
//---
bool uptrend_,old;
int K,WPR_Handle,ATR_Handle,min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- ������������� ���������� ���������� 
   K=33-RISK;
   int ATR_Period=15;
   min_rates_total=int(MathMax(SSP,ATR_Period))+1;
//--- ��������� ������ ���������� ATR
   WPR_Handle=iWPR(NULL,0,SSP);
   if(WPR_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iWPR");
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
   string short_name="BykovTrend";
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
   if(BarsCalculated(WPR_Handle)<rates_total
      || BarsCalculated(ATR_Handle)<rates_total
      || rates_total<min_rates_total)
      return(RESET);

//--- ���������� ��������� ���������� 
   int to_copy,limit,bar;
   double range,wpr,WPR[],ATR[];
   bool uptrend;

//--- ������� ������������ ���������� ���������� ������ �
//���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      to_copy=rates_total; // ��������� ���������� ���� �����
      limit=rates_total-min_rates_total; // ��������� ����� ��� ������� ���� �����
      uptrend_=false;
      old=false;
     }
   else
     {
      to_copy=rates_total-prev_calculated+1; // ��������� ���������� ������ ����� �����
      limit=rates_total-prev_calculated; // ��������� ����� ��� ������� ����� �����
     }
//--- �������� ����� ����������� ������ � ������� WPR[] � ATR[]
   if(CopyBuffer(WPR_Handle,0,0,to_copy,WPR)<=0) return(RESET);
   if(CopyBuffer(ATR_Handle,0,0,to_copy,ATR)<=0) return(RESET);
//--- ���������� ��������� � �������� ��� � ����������  
   ArraySetAsSeries(WPR,true);
   ArraySetAsSeries(ATR,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
//--- ��������������� �������� ����������
   uptrend=uptrend_;
//--- �������� ���� ������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      //--- ���������� �������� ���������� ����� ��������� �� ������� ����
      if(rates_total!=prev_calculated && bar==0)
        {
         uptrend_=uptrend;
        }
      //---  
      wpr=WPR[bar];
      range=ATR[bar]*3/8;
      //---
      BuyBuffer[bar]=0.0;
      SellBuffer[bar]=0.0;
      //---
      if(wpr<-100+K) uptrend=false;
      if(wpr>-K)     uptrend=true;
      //---
      if(!old &&  uptrend) BuyBuffer [bar]=low[bar]-range;
      if( old && !uptrend) SellBuffer[bar]=high[bar]+range;
      //---
      if(bar) old=uptrend;
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
