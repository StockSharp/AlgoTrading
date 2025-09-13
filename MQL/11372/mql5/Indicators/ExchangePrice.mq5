//+----------------------------------------------------------------------------+
//|                                                          ExchangePrice.mq5 |
//|                                                  Copyright 2013, papaklass |
//|                                     http://www.mql4.com/ru/users/papaklass |
//+----------------------------------------------------------------------------+
//--- ��������� ����������
#property copyright "Copyright 2013, papaklass"
//--- ������ �� ���� ������
#property link      "http://www.mql4.com/ru/users/papaklass"
#property description "ExchangePrice"
//--- ����� ������ ����������
#property version   "1.00"
//--- ��������� ���������� � ��������� ����
#property indicator_separate_window
//--- ��� ������� � ��������� ���������� ������������ ��� ������
#property indicator_buffers 2
//--- ������������ ���� ����������� ����������
#property indicator_plots   1
//+----------------------------------------------+
//| ��������� ��������� ���������� 1             |
//+----------------------------------------------+
//--- ��������� ���������� � ���� �������� ������
#property indicator_type1   DRAW_FILLING
//--- � �������� ������ ���������� ������������
#property indicator_color1  clrBlue,clrIndianRed
//--- ����������� ����� ����������
#property indicator_label1  "ExchangePrice"
//+----------------------------------------------+
//| ���������� ��������                          |
//+----------------------------------------------+
#define RESET 0 // ��������� ��� �������� ��������� ������� �� �������� ����������
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input int countBarsS = 96;
input int countBarsL = 288;
//+----------------------------------------------+
//--- ���������� ������������ ��������, ������� � ����������
//--- ����� ������������ � �������� ������������ �������
double UpBuffer[],DnBuffer[];
//--- ���������� ������������� ���������� ��� �������� ������� �����������
int Ind_Handle;
//--- ���������� �������������  ���������� ������ ������� ������
int min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
int OnInit()
  {
//--- ������������� ���������� ������ ������� ������
   min_rates_total=int(MathMax(countBarsS,countBarsL));
//--- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,UpBuffer,INDICATOR_DATA);
//--- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(UpBuffer,true);
//--- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(1,DnBuffer,INDICATOR_DATA);
//--- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(DnBuffer,true);
//--- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,"ExchangePrice");
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//--- ���������� �������������
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(
                const int rates_total,    // ���������� ������� � ����� �� ������� ����
                const int prev_calculated,// ���������� ������� � ����� �� ���������� ����
                const int begin,          // ����� ������ ������������ ������� �����
                const double &price[])    // ������� ������ ��� ������� ����������
  {
//--- �������� ���������� ����� �� ������������� ��� �������
   if(rates_total<min_rates_total+begin) return(RESET);
//--- ���������� ��������� ���������� 
   double current,historyL,historyS;
   int limit,bar;
//--- ���������� ��������� � �������� ��� � ����������  
   ArraySetAsSeries(price,true);
//--- ������� ������������ ���������� ���������� ������
//--- � ���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      limit=rates_total-min_rates_total-1-begin; // ��������� ����� ��� ������� ���� �����
      //--- ������������� ������ ������ ������� ��������� ����������
      PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total+begin);
     }
   else limit=rates_total-prev_calculated; // ��������� ����� ��� ������� ����� �����
//--- �������� ���� ������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      current=price[bar];
      historyS=price[bar+countBarsS];
      historyL=price[bar+countBarsL];
      UpBuffer[bar]=(current-historyS)/_Point;
      DnBuffer[bar]=(current-historyL)/_Point;
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
