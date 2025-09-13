//+------------------------------------------------------------------+ 
//|                                         ForexProfitBoost_2nb.mq5 | 
//|                               Copyright � 2015, TradeLikeaPro.ru | 
//|                                         http://tradelikeapro.ru/ | 
//+------------------------------------------------------------------+ 
#property copyright "Copyright � 2015, TradeLikeaPro.ru"
#property link "http://tradelikeapro.ru/"
//--- ����� ������ ����������
#property version   "1.00"
//--- ��������� ���������� � ������� ����
#property indicator_chart_window 
//--- ���������� ������������ ������� 6
#property indicator_buffers 6 
//--- ������������ ��� ����������� ����������
#property indicator_plots   3
//+-----------------------------------+
//| ��������� ��������� ���������� 1  |
//+-----------------------------------+
//--- ��������� ���������� � ���� �����������
#property indicator_type1   DRAW_HISTOGRAM2
//--- � �������� ����� ���������� �����������
#property indicator_color1  clrOrange
//--- ������� ����� ���������� ����� 2
#property indicator_width1  2
//--- ����������� ����� ����������
#property indicator_label1  "ForexProfitBoost_2nb 1"
//+-----------------------------------+
//| ��������� ��������� ���������� 2  |
//+-----------------------------------+
//--- ��������� ���������� � ���� �����������
#property indicator_type2   DRAW_HISTOGRAM2
//--- � �������� ����� ���������� �����������
#property indicator_color2  clrDeepPink
//--- ������� ����� ���������� ����� 2
#property indicator_width2  2
//--- ����������� ����� ����������
#property indicator_label2  "ForexProfitBoost_2nb 2"
//+-----------------------------------+
//| ��������� ��������� ���������� 3  |
//+-----------------------------------+
//--- ��������� ���������� � ���� �����������
#property indicator_type3   DRAW_HISTOGRAM2
//--- � �������� ����� ���������� �����������
#property indicator_color3  clrBlue
//--- ������� ����� ���������� ����� 2
#property indicator_width3  2
//--- ����������� ����� ����������
#property indicator_label3  "ForexProfitBoost_2nb 3"
//+-----------------------------------+
//| ���������� ��������               |
//+-----------------------------------+
#define RESET  0 // ��������� ��� �������� ��������� ������� �� �������� ����������
//+-----------------------------------+
//| ������� ��������� ����������      |
//+-----------------------------------+
//--- ��������� ����������� �������� 1
input uint   MAPeriod1=7;
input  ENUM_MA_METHOD   MAType1=MODE_EMA;
input ENUM_APPLIED_PRICE   MAPrice1=PRICE_CLOSE;
//--- ��������� ����������� �������� 2
input uint   MAPeriod2=21;
input  ENUM_MA_METHOD   MAType2=MODE_SMA;
input ENUM_APPLIED_PRICE   MAPrice2=PRICE_CLOSE;
input uint BBPeriod=15;
input double BBDeviation=1;
input uint BBShift=1;
//+-----------------------------------+
//--- ���������� ����� ���������� ������ ������� ������
int  min_rates_total;
//--- ���������� ������������ ��������, ������� � ����������
//--- ����� ������������ � �������� ������������ �������
double upmax[],upmin[],dnmax[],dnmin[],max[],min[];
//--- ���������� ������������� ���������� ��� ������� �����������
int MA1_Handle,MA2_Handle,BB_Handle;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- ������������� ���������� ������ ������� ������
   min_rates_total=int(MathMax(BBPeriod,MathMax(MAPeriod1,MAPeriod2)));
//--- ��������� ������ ���������� iMA 1
   MA1_Handle=iMA(NULL,0,MAPeriod1,0,MAType1,MAPrice1);
   if(MA1_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iMA 1");
      return(INIT_FAILED);
     }
//--- ��������� ������ ���������� iMA 2
   MA2_Handle=iMA(NULL,0,MAPeriod2,0,MAType2,MAPrice2);
   if(MA2_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iMA 2");
      return(INIT_FAILED);
     }
//--- ��������� ������ ���������� iMA 2
   MA2_Handle=iMA(NULL,0,MAPeriod2,0,MAType2,MAPrice2);
   if(MA2_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iMA 2");
      return(INIT_FAILED);
     }
//--- ��������� ������ ���������� iBands
   BB_Handle=iBands(NULL,0,BBPeriod,BBShift,BBDeviation,PRICE_CLOSE);
   if(BB_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iBands");
      return(INIT_FAILED);
     }
//--- ����������� ������������� �������� � ������������ ������
   SetIndexBuffer(0,max,INDICATOR_DATA);
   SetIndexBuffer(1,min,INDICATOR_DATA);
   SetIndexBuffer(2,dnmax,INDICATOR_DATA);
   SetIndexBuffer(3,dnmin,INDICATOR_DATA);
   SetIndexBuffer(4,upmax,INDICATOR_DATA);
   SetIndexBuffer(5,upmin,INDICATOR_DATA);
//--- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(upmax,true);
   ArraySetAsSeries(upmin,true);
   ArraySetAsSeries(dnmax,true);
   ArraySetAsSeries(dnmin,true);
   ArraySetAsSeries(max,true);
   ArraySetAsSeries(min,true);
//--- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//--- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total);
//--- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//--- ������������� ���������� ��� ��������� ����� ����������
   string shortname="ForexProfitBoost_2nb";
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//--- ���������� �������������
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+  
//| Custom indicator iteration function                              | 
//+------------------------------------------------------------------+  
int OnCalculate(const int rates_total,    // ���������� ������� � ����� �� ������� ����
                const int prev_calculated,// ���������� ������� � ����� �� ���������� ����
                const datetime &Time[],
                const double &Open[],
                const double &High[],
                const double &Low[],
                const double &Close[],
                const long &Tick_Volume[],
                const long &Volume[],
                const int &Spread[])
  {
//--- �������� ���������� ����� �� ������������� ��� �������
   if(BarsCalculated(BB_Handle)<rates_total
      || BarsCalculated(MA1_Handle)<rates_total
      || BarsCalculated(MA2_Handle)<rates_total
      || rates_total<min_rates_total)
      return(RESET);
//--- ���������� ���������� � ��������� ������  
   double MA1[],MA2[],UpBB[],DnBB[];
//--- ���������� ������������� ����������
   int limit,to_copy;
//--- ������ ���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
      limit=rates_total-min_rates_total-1; // ��������� ����� ��� ������� ���� �����
   else limit=rates_total-prev_calculated;  // ��������� ����� ��� ������� ������ ����� �����
//--- ���������� ��������� � �������� ��� � ����������  
   ArraySetAsSeries(MA1,true);
   ArraySetAsSeries(MA2,true);
   ArraySetAsSeries(UpBB,true);
   ArraySetAsSeries(DnBB,true);
//---   
   to_copy=limit+1;
//--- �������� ����� ����������� ������ � �������
   if(CopyBuffer(MA1_Handle,0,0,to_copy,MA1)<=0) return(RESET);
   if(CopyBuffer(MA2_Handle,0,0,to_copy,MA2)<=0) return(RESET);
   if(CopyBuffer(BB_Handle,UPPER_BAND,0,to_copy,UpBB)<=0) return(RESET);
   if(CopyBuffer(BB_Handle,LOWER_BAND,0,to_copy,DnBB)<=0) return(RESET);
//--- �������� ���� ������� ����������
   for(int bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      max[bar]=MathMax(UpBB[bar],MathMax(MA1[bar],MA2[bar]));
      min[bar]=MathMin(DnBB[bar],MathMin(MA1[bar],MA2[bar]));
      //---
      if(MA1[bar]>MA2[bar])
        {
         upmax[bar]=MathMax(MA1[bar],MA2[bar]);
         upmin[bar]=MathMin(DnBB[bar],MathMin(MA1[bar],MA2[bar]));
         dnmax[bar]=EMPTY_VALUE;
         dnmin[bar]=EMPTY_VALUE;
        }
      else
        {
         dnmax[bar]=MathMax(UpBB[bar],MathMax(MA1[bar],MA2[bar]));
         dnmin[bar]=MathMin(MA1[bar],MA2[bar]);
         upmax[bar]=EMPTY_VALUE;
         upmin[bar]=EMPTY_VALUE;
        }
     }
//---    
   return(rates_total);
  }
//+------------------------------------------------------------------+
