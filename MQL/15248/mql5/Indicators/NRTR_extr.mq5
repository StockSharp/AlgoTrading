//+------------------------------------------------------------------+
//|                                                    NRTR_extr.mq5 |
//|                                        Copyright � 2005, Ramdass | 
//|                                                                  | 
//+------------------------------------------------------------------+ 
#property copyright "Copyright � 2005, Ramdass" 
#property link      "" 
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � �������� ����
#property indicator_chart_window
//---- ���������� ������������ ������� 4
#property indicator_buffers 4 
//---- ������������ ����� ������ ����������� ����������
#property indicator_plots   4
//+----------------------------------------------+
//|  ��������� ��������� ����������              |
//+----------------------------------------------+
//---- ��������� ���������� � ���� ������
#property indicator_type1 DRAW_ARROW
//---- � �������� ������� ���������� �����������
#property indicator_color1 clrDodgerBlue
//---- ����� ���������� - ��������
#property indicator_style1 STYLE_SOLID
//---- ������� ����� ���������� ����� 2
#property indicator_width1 2
//---- ����������� ����� ���������� �����
#property indicator_label1  "NRTR Up"
//+----------------------------------------------+
//|  ��������� ��������� ����������              |
//+----------------------------------------------+
//---- ��������� ���������� � ���� ������
#property indicator_type2 DRAW_ARROW
//---- � �������� ������� ���������� �����������
#property indicator_color2 clrMagenta
//---- ����� ���������� - ��������
#property indicator_style2 STYLE_SOLID
//---- ������� ����� ���������� ����� 2
#property indicator_width2 2
//---- ����������� ����� ���������� �����
#property indicator_label2  "NRTR Down"
//+----------------------------------------------+
//|  ��������� ��������� ������� ����������      |
//+----------------------------------------------+
//---- ��������� ���������� 3 � ���� ������
#property indicator_type3   DRAW_ARROW
//---- � �������� ����� ����� ����� ���������� �����������
#property indicator_color3  clrBlue
//---- ����� ���������� 3 - ����������� ������
#property indicator_style3  STYLE_SOLID
//---- ������� ����� ���������� 3 ����� 2
#property indicator_width3  2
//---- ����������� ������ ����� ����������
#property indicator_label3  "Buy NRTR signal"
//+----------------------------------------------+
//|  ��������� ��������� ���������� ����������   |
//+----------------------------------------------+
//---- ��������� ���������� 4 � ���� ������
#property indicator_type4   DRAW_ARROW
//---- � �������� ����� ��������� ����� ���������� �����������
#property indicator_color4  clrGold
//---- ����� ���������� 2 - ����������� ������
#property indicator_style4  STYLE_SOLID
//---- ������� ����� ���������� 2 ����� 2
#property indicator_width4  2
//---- ����������� ��������� ����� ����������
#property indicator_label4  "Sell NRTR signal"
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input uint iPeriod=10;  // ������ ����������
input int iDig=0;       // ������
input int Shift=0;      // ����� ���������� �� ����������� � ����� 
//+----------------------------------------------+
//---- ���������� ������������ ��������, ������� ����� � 
//---- ���������� ������������ � �������� ������������ �������
double TrendUp[],TrendDown[];
double SignUp[];
double SignDown[];
//---- ���������� ������������� ���������� ������ ������� ������
int min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_total=int(iPeriod);

//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   StringConcatenate(shortname,"NRTR(",string(iPeriod),", ",string(Shift),")");
//---- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//---- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,TrendUp,INDICATOR_DATA);
//---- ������������� ������ ���������� �� ����������� �� Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ������ �� ��������� ����������� ������ ��������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);
//---- ���������� ��������� � �������, ��� � ����������   
   ArraySetAsSeries(TrendUp,true);

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(1,TrendDown,INDICATOR_DATA);
//---- ������������� ������ ���������� �� ����������� �� Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- ������ �� ��������� ����������� ������ ��������
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,0.0);
//---- ���������� ��������� � �������, ��� � ����������   
   ArraySetAsSeries(TrendDown,true);

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(2,SignUp,INDICATOR_DATA);
//---- ������������� ������ ���������� 1 �� ����������� �� Shift
   PlotIndexSetInteger(2,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ���������� 1
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total);
//---- ���������� ��������� � �������, ��� � ����������   
   ArraySetAsSeries(SignUp,true);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,0.0);
//---- ������ ��� ����������
   PlotIndexSetInteger(2,PLOT_ARROW,108);

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(3,SignDown,INDICATOR_DATA);
//---- ������������� ������ ���������� 2 �� ����������� �� Shift
   PlotIndexSetInteger(3,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ���������� 2
   PlotIndexSetInteger(3,PLOT_DRAW_BEGIN,min_rates_total);
//---- ���������� ��������� � �������, ��� � ����������   
   ArraySetAsSeries(SignDown,true);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(3,PLOT_EMPTY_VALUE,0.0);
//---- ������ ��� ����������
   PlotIndexSetInteger(3,PLOT_ARROW,108);
//----
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,    // ���������� ������� � ����� �� ������� ����
                const int prev_calculated,// ���������� ������� � ����� �� ���������� ����
                const datetime &time[],
                const double &open[],
                const double& high[],     // ������� ������ ���������� ���� ��� ������� ����������
                const double& low[],      // ������� ������ ��������� ���� ��� ������� ����������
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//---- �������� ���������� ����� �� ������������� ��� �������
   if(rates_total<min_rates_total) return(0);

//---- ���������� ��������� ���������� 
   double price,value,dK;
   static double price_prev,value_prev;
   int limit,bar,trend;
   static int trend_prev;

//---- ���������� ��������� � ��������, ��� � ����������  
   ArraySetAsSeries(low,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(close,true);

//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
     {
      limit=rates_total-min_rates_total-1;               // ��������� ����� ��� ������� ���� �����
      trend_prev=0;
      price_prev=value_prev=close[limit];     
     }
   else
     {
      limit=rates_total-prev_calculated;                 // ��������� ����� ��� ������� ����� �����
     }
   trend=trend_prev;
   price=price_prev;
   value=value_prev;

//---- �������� ���� ������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      TrendUp[bar]=0.0;
      TrendDown[bar]=0.0;
      SignUp[bar]=0.0;
      SignDown[bar]=0.0;

      double AvgRange=0.0;
      for(int iii=0; iii<int(iPeriod); iii++) AvgRange+=MathAbs(high[bar+iii]-low[bar+iii]);
      dK=(AvgRange/iPeriod)/MathPow(10,SymbolInfoInteger("EURUSD",SYMBOL_DIGITS)-_Digits-iDig);

      if(trend>=0)
        {
         price=MathMax(price,high[bar]);
         value=MathMax(value,price*(1.0-dK));
         if(high[bar]<value)
           {
            price = high[bar];
            value = price*(1.0+dK);
            trend = -1;
           }
        }
      else if(trend<=0)
        {
         price=MathMin(price,low[bar]);
         value=MathMin(value,price*(1.0+dK));
         if(low[bar]>value)
           {
            price = low[bar];
            value = price*(1.0-dK);
            trend = +1;
           }
        }

      if(trend>0) TrendUp[bar]=value;
      if(trend<0) TrendDown[bar]=value;

      if(trend_prev<0 && trend>0) SignUp[bar]=TrendUp[bar];
      if(trend_prev>0 && trend<0) SignDown[bar]=TrendDown[bar];

      if(bar)
        {
         trend_prev=trend;
         price_prev=price;
         value_prev=value;
        }
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
