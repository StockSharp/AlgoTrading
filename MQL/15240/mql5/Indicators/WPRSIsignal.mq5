//+------------------------------------------------------------------+
//|                                                  WPRSIsignal.mq5 |
//|                                         Copyright � 2009, gumgum |
//|                                           1967policmen@gmail.com |
//+------------------------------------------------------------------+
#property copyright "Copyright � 2009, gumgum"
//---- ������ �� ���� ������
#property link      "1967policmen@gmail.com"
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ������� ����
#property indicator_chart_window 
//---- ��� ������� � ��������� ���������� ������������ ��� ������
#property indicator_buffers 2
//---- ������������ ����� ��� ����������� ����������
#property indicator_plots   2
//+----------------------------------------------+
//|  ��������� ��������� ���������� ����������   |
//+----------------------------------------------+
//---- ��������� ���������� 1 � ���� �������
#property indicator_type1   DRAW_ARROW
//---- � �������� ����� ���������� ���������� ����������� ���� Magenta
#property indicator_color1  clrMagenta
//---- ������� ����� ���������� 1 ����� 4
#property indicator_width1  4
//---- ����������� ����� ����������
#property indicator_label1  "WPRSI signal Sell"
//+----------------------------------------------+
//|  ��������� ��������� ������ ����������       |
//+----------------------------------------------+
//---- ��������� ���������� 2 � ���� �������
#property indicator_type2   DRAW_ARROW
//---- � �������� ����� ������ ���������� ����������� ���� Lime
#property indicator_color2  clrLime
//---- ������� ����� ���������� 2 ����� 4
#property indicator_width2  4
//---- ����������� ����� ����������
#property indicator_label2 "WPRSI signal Buy"
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input int WPRSI_period=27; //������ ����������
input int filterUP=10; //������� ������ ��� ������
input int filterDN=10; //������� ������ ��� ������
//+----------------------------------------------+
//---- ���������� ������������ ��������, ������� � ����������
//---- ����� ������������ � �������� ������������ �������
double SellBuffer[];
double BuyBuffer[];
//---
int WPR_Handle,RSI_Handle;
int min_rates_total,FilterMax;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
  {
//---- ������������� ���������� ������ ������� ������ 
   FilterMax=2+MathMax(filterUP,filterDN);
   min_rates_total=WPRSI_period+FilterMax;

//---- ��������� ������ ���������� WPR
   WPR_Handle=iWPR(NULL,0,WPRSI_period);
   if(WPR_Handle==INVALID_HANDLE) Print(" �� ������� �������� ����� ���������� WPR");

//---- ��������� ������ ���������� Stochastic
   RSI_Handle=iRSI(NULL,0,WPRSI_period,PRICE_CLOSE);
   if(RSI_Handle==INVALID_HANDLE)Print(" �� ������� �������� ����� ���������� RSI");

//---- ����������� ������������� ������� SellBuffer[] � ������������ �����
   SetIndexBuffer(0,SellBuffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ���������� 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ������ ��� ����������
   PlotIndexSetInteger(0,PLOT_ARROW,234);
//---- ������ �� ��������� ����������� ������ ��������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(SellBuffer,true);

//---- ����������� ������������� ������� BuyBuffer[] � ������������ �����
   SetIndexBuffer(1,BuyBuffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ���������� 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- ������ ��� ����������
   PlotIndexSetInteger(1,PLOT_ARROW,233);
//---- ������ �� ��������� ����������� ������ ��������
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(BuyBuffer,true);

//---- ��������� ������� �������� ����������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- ��� ��� ���� ������ � ����� ��� ������� 
   string short_name="WPRSI signal";
   IndicatorSetString(INDICATOR_SHORTNAME,short_name);
//----   
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
   if(BarsCalculated(WPR_Handle)<rates_total
      || BarsCalculated(RSI_Handle)<rates_total
      || rates_total<min_rates_total)
      return(0);

//---- ���������� ��������� ���������� 
   int to_copy,limit,bar;
   double WPR[],RSI[];

//---- ������� ������������ ���������� ���������� ������
//---- � ���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      limit=rates_total-min_rates_total-1; // ��������� ���������� ���� �����
     }
   else
     {
      limit=rates_total-prev_calculated; // ��������� ����� ��� ������� ����� �����
     }

//---- �������� ����� ����������� ������ � �������  
   to_copy=limit+1;
   if(CopyBuffer(RSI_Handle,0,0,to_copy,RSI)<=0) return(0);
   to_copy+=FilterMax;
   if(CopyBuffer(WPR_Handle,0,0,to_copy,WPR)<=0) return(0);

//---- ���������� ��������� � �������� ��� � ����������  
   ArraySetAsSeries(WPR,true);
   ArraySetAsSeries(RSI,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);

//---- �������� ���� ������� ����������
   for(bar=limit; bar>=0; bar--)
     {
      BuyBuffer[bar]=EMPTY_VALUE;
      SellBuffer[bar]=EMPTY_VALUE;

      if(WPR[bar]>-20 && WPR[bar+1]<-20 && RSI[bar]>50)
        {
         double z=0;
         for(int k=2;k<=filterUP+2;k++) if(WPR[bar+k]>-20) z=1;

         if(z==0) BuyBuffer[bar]=low[bar]-(high[bar]-low[bar])/2;
        }

      if(WPR[bar+1]>-80 && WPR[bar]<-80 && RSI[bar]<50)
        {
         double h=0;
         for(int c=2;c<=filterDN+2;c++) if(WPR[bar+c]<-80) h=1;

         if(h==0) SellBuffer[bar]=high[bar]+(high[bar]-low[bar])/2;
        }
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
