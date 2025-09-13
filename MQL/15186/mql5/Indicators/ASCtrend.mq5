//+------------------------------------------------------------------+
//|                                                     ASCtrend.mq5 |
//|                             Copyright � 2011,   Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright � 2011, Nikolay Kositsin"
#property link "farria@mail.redcom.ru"
#property description "ASCtrend"
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
//---- � �������� ����� ��������� ����� ���������� ����������� ������� ����
#property indicator_color1  clrMagenta
//---- ������� ����� ���������� 1 ����� 4
#property indicator_width1  4
//---- ����������� ������ ����� ����������
#property indicator_label1  "ASCtrend Sell"
//+----------------------------------------------+
//|  ��������� ��������� ������� ����������      |
//+----------------------------------------------+
//---- ��������� ���������� 2 � ���� �������
#property indicator_type2   DRAW_ARROW
//---- � �������� ����� ������ ����� ���������� ����������� ����� ����
#property indicator_color2  clrBlue
//---- ������� ����� ���������� 2 ����� 4
#property indicator_width2  4
//---- ����������� ��������� ����� ����������
#property indicator_label2 "ASCtrend Buy"

//+----------------------------------------------+
//|  ���������� ��������                         |
//+----------------------------------------------+
#define RESET  0 // ��������� ��� �������� ��������� ������� �� �������� ����������
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input int RISK=4;
//+----------------------------------------------+

//---- ���������� ������������ ��������, ������� ����� � 
// ���������� ������������ � �������� ������������ �������
double SellBuffer[];
double BuyBuffer[];
//---- ���������� ����� ���������� ������ ������� ������
int min_rates_total;
int  x1,x2,value10,value11,WPR_Handle[3];
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
  {
//---- ������������� ���������� ����������   
   x1=67+RISK;
   x2=33-RISK;
   value10=2;
   value11=value10;
   min_rates_total=int(MathMax(3+RISK*2,4)+1);

//---- ��������� ������ ���������� iWPR 1
   WPR_Handle[0]=iWPR(NULL,0,3);
   if(WPR_Handle[0]==INVALID_HANDLE)Print(" �� ������� �������� ����� ���������� iWPR 1");
//---- ��������� ������ ���������� iWPR 2
   WPR_Handle[1]=iWPR(NULL,0,4);
   if(WPR_Handle[1]==INVALID_HANDLE)Print(" �� ������� �������� ����� ���������� iWPR 2");
//---- ��������� ������ ���������� iWPR 3
   WPR_Handle[2]=iWPR(NULL,0,3+RISK*2);
   if(WPR_Handle[2]==INVALID_HANDLE)Print(" �� ������� �������� ����� ���������� iWPR 3");

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,SellBuffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ���������� 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- �������� ����� ��� ����������� � DataWindow
   PlotIndexSetString(0,PLOT_LABEL,"ASCtrend Sell");
//---- ������ ��� ����������
   PlotIndexSetInteger(0,PLOT_ARROW,108);
//---- ���������� ��������� � ������, ��� � ���������
   ArraySetAsSeries(SellBuffer,true);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0);

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(1,BuyBuffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ���������� 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- �������� ����� ��� ����������� � DataWindow
   PlotIndexSetString(1,PLOT_LABEL,"ASCtrend Buy");
//---- ������ ��� ����������
   PlotIndexSetInteger(1,PLOT_ARROW,108);
//---- ���������� ��������� � ������, ��� � ���������
   ArraySetAsSeries(BuyBuffer,true);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,0);

//---- ��������� ������� �������� ����������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- ��� ��� ���� ������ � ����� ��� ������� 
   string short_name="ASCtrend";
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
   if(BarsCalculated(WPR_Handle[0])<rates_total
      || BarsCalculated(WPR_Handle[1])<rates_total
      || BarsCalculated(WPR_Handle[2])<rates_total
      || rates_total<min_rates_total)
      return(RESET);

//---- ���������� ��������� ���������� 
   int limit,bar,count,iii;
   double value2,value3,Vel=0,WPR[];
   double TrueCount,Range,AvgRange,MRO1,MRO2;

//---- ������� ������������ ���������� ���������� ������ �
//���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
      limit=rates_total-min_rates_total; // ��������� ����� ��� ������� ���� �����
   else limit=rates_total-prev_calculated; // ��������� ����� ��� ������� ����� �����

//---- ���������� ��������� � ��������, ��� � ����������  
   ArraySetAsSeries(WPR,true);
   ArraySetAsSeries(open,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
   ArraySetAsSeries(close,true);

//---- �������� ���� ������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      Range=0.0;
      AvgRange=0.0;
      for(count=bar; count<=bar+9; count++) AvgRange=AvgRange+MathAbs(high[count]-low[count]);

      Range=AvgRange/10;
      count=bar;
      TrueCount=0;

      while(count<bar+9 && TrueCount<1)
        {
         if(MathAbs(open[count]-close[count+1])>=Range*2.0) TrueCount++;
         count++;
        }

      if(TrueCount>=1) MRO1=count;
      else             MRO1=-1;

      count=bar;
      TrueCount=0;

      while(count<bar+6 && TrueCount<1)
        {
         if(MathAbs(close[count+3]-close[count])>=Range*4.6) TrueCount++;
         count++;
        }

      if(TrueCount>=1) MRO2=count;
      else             MRO2=-1;

      if(MRO1>-1) {value11=0;} else {value11=value10;}
      if(MRO2>-1) {value11=1;} else {value11=value10;}

      if(CopyBuffer(WPR_Handle[value11],0,bar,1,WPR)<=0) return(RESET);

      value2=100-MathAbs(WPR[0]); // PercentR(value11=9)

      SellBuffer[bar]=0;
      BuyBuffer[bar]=0;

      value3=0;

      if(value2<x2)
        {
         iii=1;
         while(bar+iii<rates_total)
           {
            if(CopyBuffer(WPR_Handle[value11],0,bar+iii,1,WPR)<=0) return(RESET);
            Vel=100-MathAbs(WPR[0]);
            if(Vel>=x2 && Vel<=x1) iii++;
            else break;
           }

         if(Vel>x1)
           {
            value3=high[bar]+Range*0.5;
            SellBuffer[bar]=value3;
           }
        }
      if(value2>x1)
        {
         iii=1;
         while(bar+iii<rates_total)
           {
            if(CopyBuffer(WPR_Handle[value11],0,bar+iii,1,WPR)<=0) return(RESET);
            Vel=100-MathAbs(WPR[0]);
            if(Vel>=x2 && Vel<=x1) iii++;
            else break;
           }

         if(Vel<x2)
           {
            value3=low[bar]-Range*0.5;
            BuyBuffer[bar]=value3;
           }
        }
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
