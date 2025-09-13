//+------------------------------------------------------------------+ 
//|                                                       DecEMA.mq5 | 
//|                                         Developed by Coders Guru |
//|                                            http://www.xpworx.com |                      
//|                         Revised by IgorAD,igorad2003@yahoo.co.uk |   
//|                                        http://www.forex-tsd.com/ |                                      
//+------------------------------------------------------------------+
#property copyright "Copyright � 2008, Guru"
#property link "farria@mail.redcom.ru" 
//---- ����� ������ ����������
#property version   "1.01"
//---- ��������� ���������� � ������� ����
#property indicator_chart_window 
//---- ���������� ������������ �������
#property indicator_buffers 1 
//---- ������������ ����� ���� ����������� ����������
#property indicator_plots   1
//+-----------------------------------+
//| ��������� ��������� ����������    |
//+-----------------------------------+
//---- ��������� ���������� � ���� �����
#property indicator_type1   DRAW_LINE
//---- � �������� ����� ����� ���������� ����������� ������� ����
#property indicator_color1 clrRed
//---- ����� ���������� - ����������� ������
#property indicator_style1  STYLE_SOLID
//---- ������� ����� ���������� ����� 1
#property indicator_width1  1
//---- ����������� ����� ����������
#property indicator_label1  "DecEMA"
//+-----------------------------------+
//| �������� ������ CXMA              |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+
//---- ���������� ���������� ������ CXMA �� ����� SmoothAlgorithms.mqh
CXMA XMA1;
//+-----------------------------------+
//| ���������� ������������           |
//+-----------------------------------+
enum Applied_price_ //��� ���������
  {
   PRICE_CLOSE_ = 1,     //Close
   PRICE_OPEN_,          //Open
   PRICE_HIGH_,          //High
   PRICE_LOW_,           //Low
   PRICE_MEDIAN_,        //Median Price (HL/2)
   PRICE_TYPICAL_,       //Typical Price (HLC/3)
   PRICE_WEIGHTED_,      //Weighted Close (HLCC/4)
   PRICE_SIMPL_,         //Simpl Price (OC/2)
   PRICE_QUARTER_,       //Quarted Price (HLOC/4) 
   PRICE_TRENDFOLLOW0_,  //TrendFollow_1 Price 
   PRICE_TRENDFOLLOW1_,  //TrendFollow_2 Price
   PRICE_DEMARK_         //Demark Price
  };
//+-----------------------------------+
//| ������� ��������� ����������      |
//+-----------------------------------+
input uint EMA_Period=3; // ������ EMA 
input int ELength=15;    // ������� �����������                   
input Applied_price_ IPC=PRICE_CLOSE; // ������� ���������
input int Shift=0; // ����� ���������� �� ����������� � �����
input int PriceShift=0; // ����� ���������� �� ��������� � �������
//+-----------------------------------+
//---- ������������ �����
double IndBuffer[];
//---- ���������� ������������� ���������� ������ ������� ������
int min_rates_total;
//---- ���������� ���������� ����������
double alfa,dPriceShift;
//+------------------------------------------------------------------+    
//| DecEMA indicator initialization function                         | 
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- ������������� ���������� ������ ������� ������ 
   min_rates_total=2;
   alfa=2.0/(1.0+ELength);
//---- ������������� ������ �� ���������
   dPriceShift=_Point*PriceShift;
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- ������������� ������ ���������� �� ����������� �� Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   StringConcatenate(shortname,"DecEMA(",EMA_Period,",",ELength,")");
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
//---- ���������� �������������
  }
//+------------------------------------------------------------------+  
//| DecEMA iteration function                                        | 
//+------------------------------------------------------------------+  
int OnCalculate(const int rates_total,    // ���������� ������� � ����� �� ������� ����
                const int prev_calculated,// ���������� ������� � ����� �� ���������� ����
                const datetime &time[],
                const double &open[],
                const double& high[],     // ������� ������ ���������� ���� ��� ������� ����������
                const double& low[],      // ������� ������ ��������� ����  ��� ������� ����������
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {
//---- �������� ���������� ����� �� ������������� ��� �������
   if(rates_total<min_rates_total)return(0);
//---- ���������� ��������� ����������
   int first,bar,maxbar;
//----
   double price,EMA0,EMA1,EMA2,EMA3,EMA4,EMA5,EMA6,EMA7,EMA8,EMA9,EMA10;
   static double sdEMA1,sdEMA2,sdEMA3,sdEMA4,sdEMA5,sdEMA6,sdEMA7,sdEMA8,sdEMA9,sdEMA10;
//----
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
     {
      first=1; // ��������� ����� ��� ������� ���� �����
      //---- ������������� �������������
      sdEMA1=PriceSeries(IPC,first-1,open,low,high,close);
      sdEMA2=sdEMA1;
      sdEMA3=sdEMA1;
      sdEMA4=sdEMA1;
      sdEMA5=sdEMA1;
      sdEMA6=sdEMA1;
      sdEMA7=sdEMA1;
      sdEMA8=sdEMA1;
      sdEMA9=sdEMA1;
      sdEMA10=sdEMA1;
     }
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����
//---- �������������� ����������
   EMA1=sdEMA1;
   EMA2=sdEMA2;
   EMA3=sdEMA3;
   EMA4=sdEMA4;
   EMA5=sdEMA5;
   EMA6=sdEMA6;
   EMA7=sdEMA7;
   EMA8=sdEMA8;
   EMA9=sdEMA9;
   EMA10=sdEMA10;
   maxbar=rates_total-1;
//---- �������� ���� ������� ����������
   for(bar=first; bar<rates_total; bar++)
     {
      price=PriceSeries(IPC,bar,open,low,high,close);
      EMA0=XMA1.XMASeries(1,prev_calculated,rates_total,1,0,EMA_Period,price,bar,false);
      EMA1=alfa*EMA0 + (1-alfa)*EMA1;
      EMA2=alfa*EMA1 + (1-alfa)*EMA2;
      EMA3=alfa*EMA2 + (1-alfa)*EMA3;
      EMA4=alfa*EMA3 + (1-alfa)*EMA4;
      EMA5=alfa*EMA4 + (1-alfa)*EMA5;
      EMA6=alfa*EMA5 + (1-alfa)*EMA6;
      EMA7=alfa*EMA6 + (1-alfa)*EMA7;
      EMA8=alfa*EMA7 + (1-alfa)*EMA8;
      EMA9=alfa*EMA8 + (1-alfa)*EMA9;
      EMA10=alfa*EMA9+(1-alfa)*EMA10;
      IndBuffer[bar]=10*EMA1-45*EMA2+120*EMA3-210*EMA4+252*EMA5-210*EMA6+120*EMA7-45*EMA8+10*EMA9-EMA10;
      IndBuffer[bar]+=dPriceShift;
      //---- ���������� ���������� 
      if(bar<maxbar)
        {
         sdEMA1=EMA1;
         sdEMA2=EMA2;
         sdEMA3=EMA3;
         sdEMA4=EMA4;
         sdEMA5=EMA5;
         sdEMA6=EMA6;
         sdEMA7=EMA7;
         sdEMA8=EMA8;
         sdEMA9=EMA9;
         sdEMA10=EMA10;
        }
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
