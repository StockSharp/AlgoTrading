//+------------------------------------------------------------------+
//|                                                  LeManSignal.mq5 |
//|                                         Copyright � 2009, LeMan. |
//|                                                 b-market@mail.ru |
//+------------------------------------------------------------------+
//---- ��������� ����������
#property copyright "Copyright � 2009, LeMan."
//---- ������ �� ���� ������
#property link      "b-market@mail.ru"
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
//---- � �������� ����� ��������� ������� ���������� ����������� ���� Magenta
#property indicator_color1  Magenta
//---- ������� ����� ���������� 1 ����� 4
#property indicator_width1  4
//---- ����������� ����� ����������
#property indicator_label1  "LeManSell"
//+----------------------------------------------+
//|  ��������� ��������� ������� ����������      |
//+----------------------------------------------+
//---- ��������� ���������� 2 � ���� �������
#property indicator_type2   DRAW_ARROW
//---- � �������� ����� ������ ������� ���������� ����������� ���� Lime
#property indicator_color2  Lime
//---- ������� ����� ���������� 2 ����� 4
#property indicator_width2  4
//---- ����������� ����� ����������
#property indicator_label2 "LeManBuy"

//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input int LPeriod=12; // ������ ���������� 

//+----------------------------------------------+
//---- ���������� ������������ ��������, ������� � ����������
//---- ����� ������������ � �������� ������������ �������
double SellBuffer[];
double BuyBuffer[];
//---
int StartBars;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
  {
//---- ������������� ���������� ���������� 
   StartBars=LPeriod+LPeriod+2+1;

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,SellBuffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ���������� 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,StartBars);
//--- �������� ����� ��� ����������� � DataWindow
   PlotIndexSetString(0,PLOT_LABEL,"LeManSell");
//---- ������ ��� ����������
   PlotIndexSetInteger(0,PLOT_ARROW,108);
//---- ���������� ��������� � ������, ��� � ���������
   ArraySetAsSeries(SellBuffer,true);

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(1,BuyBuffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ���������� 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,StartBars);
//--- �������� ����� ��� ����������� � DataWindow
   PlotIndexSetString(1,PLOT_LABEL,"LeManBuy");
//---- ������ ��� ����������
   PlotIndexSetInteger(1,PLOT_ARROW,108);
//---- ���������� ��������� � ������, ��� � ���������
   ArraySetAsSeries(BuyBuffer,true);

//---- ��������� ������� �������� ����������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- ��� ��� ���� ������ � ����� ��� ������� 
   string short_name="LeManSignal";
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
   if(rates_total<StartBars) return(0);

//---- ���������� ��������� ���������� 
   int limit,bar,bar1,bar2,bar1p,bar2p;
   double H1,H2,H3,H4,L1,L2,L3,L4;

//---- ���������� ��������� � �������� ��� � ����������
   ArraySetAsSeries(low,true);
   ArraySetAsSeries(high,true);

//---- ������ ���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
      limit=rates_total-1-StartBars;                     // ��������� ����� ��� ������� ���� �����
   else limit=rates_total-prev_calculated;               // ��������� ����� ��� ������� ����� �����

//---- �������� ���� ������� ����������
   for(bar=limit; bar>=0; bar--)
     {
      bar1=bar+1;
      bar2=bar+2;
      bar1p=bar1+LPeriod;
      bar2p=bar2+LPeriod;
      //----
      H1 = high[ArrayMaximum(high,bar1, LPeriod)];
      H2 = high[ArrayMaximum(high,bar1p,LPeriod)];
      H3 = high[ArrayMaximum(high,bar2, LPeriod)];
      H4 = high[ArrayMaximum(high,bar2p,LPeriod)];
      L1 = low [ArrayMinimum(low, bar1, LPeriod)];
      L2 = low [ArrayMinimum(low, bar1p,LPeriod)];
      L3 = low [ArrayMinimum(low, bar2, LPeriod)];
      L4 = low [ArrayMinimum(low, bar2p,LPeriod)];
      //----
      BuyBuffer[bar]=EMPTY_VALUE;
      SellBuffer[bar]=EMPTY_VALUE;

      //---- ������� �������                       
      if(H3<=H4 && H1>H2) BuyBuffer[bar]=high[bar+1]+_Point;
      //---- ������� �������      
      if(L3>=L4 && L1<L2) SellBuffer[bar]=low[bar+1]-_Point;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
