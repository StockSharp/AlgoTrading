//+------------------------------------------------------------------+ 
//|                                                       MACD-2.mq5 | 
//|                      Copyright � 2005, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Copyright � 2005, MetaQuotes Software Corp."
#property link      "http://www.metaquotes.net"
//---- ����� ������ ����������
#property version   "1.00"
#property description "MACD-2"
//---- ����� ������ ����������
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window
//---- ���������� ������������ ������� 4
#property indicator_buffers 4 
//---- ������������ ��� ����������� ����������
#property indicator_plots   2
//+-----------------------------------+
//| ���������� ��������               |
//+-----------------------------------+
#define RESET  0 // ��������� ��� �������� ��������� ������� �� �������� ����������
//+-----------------------------------+
//| ��������� ��������� ����������    |
//+-----------------------------------+
//---- ��������� ���������� � ���� �������� ������
#property indicator_type1   DRAW_FILLING
//---- � �������� ������ ���������� ������������
#property indicator_color1  clrLime,clrDeepPink
//---- ����������� ����� ����������
#property indicator_label1  "MACD_Cloud"
//+----------------------------------------------+
//| ��������� ��������� ���������� 2             |
//+----------------------------------------------+
//---- ��������� ���������� � ���� �������������� �����������
#property indicator_type2 DRAW_COLOR_HISTOGRAM
//---- � �������� ������ ����������� ����������� ������������
#property indicator_color2 clrBrown,clrViolet,clrGray,clrDeepSkyBlue,clrBlue
//---- ����� ���������� - ��������
#property indicator_style2 STYLE_SOLID
//---- ������� ����� ���������� ����� 2
#property indicator_width2 2
//---- ����������� ����� ����������
#property indicator_label2  "MACD"
//+-----------------------------------+
//| ������� ��������� ����������      |
//+-----------------------------------+
input uint FastMACD     = 12;
input uint SlowMACD     = 26;
input uint SignalMACD   = 9;
input ENUM_APPLIED_PRICE   PriceMACD=PRICE_CLOSE;
//+-----------------------------------+
//---- ���������� ������������� ���������� ������ ������� ������
int  min_rates_total;
//---- ���������� ������������ ��������, ������� ����� � 
//---- ���������� ������������ � �������� ������������ �������
double ExtABuffer[],ExtBBuffer[];
double IndBuffer[],ColorIndBuffer[];
//---- ���������� ������������� ���������� ��� ������� �����������
int MACD_Handle;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_total=int(SignalMACD+MathMax(FastMACD,SlowMACD));
//---- ��������� ������ ���������� iMACD
   MACD_Handle=iMACD(NULL,0,FastMACD,SlowMACD,SignalMACD,PriceMACD);
   if(MACD_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� iMACD");
      return(INIT_FAILED);
     }
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,ExtABuffer,INDICATOR_DATA);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(ExtABuffer,true);

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(1,ExtBBuffer,INDICATOR_DATA);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(ExtBBuffer,true);

//---- ����������� ������������� ������� IndBuffer � ������������ �����
   SetIndexBuffer(2,IndBuffer,INDICATOR_DATA);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(IndBuffer,true);

//---- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(3,ColorIndBuffer,INDICATOR_COLOR_INDEX);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(ColorIndBuffer,true);

//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);

//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,"MACD-2");
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//---- ���������� �������������
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
//---- �������� ���������� ����� �� ������������� ��� �������
   if(BarsCalculated(MACD_Handle)<rates_total || rates_total<min_rates_total) return(RESET);
//---- ���������� ��������� ���������� 
   int to_copy,limit;
//---- ������ ���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
      limit=rates_total-min_rates_total-1; // ��������� ����� ��� ������� ���� �����
   else limit=rates_total-prev_calculated;  // ��������� ����� ��� ������� ������ ����� �����
//----
   to_copy=limit+1;
//---- �������� ����� ����������� ������ � �������
   if(CopyBuffer(MACD_Handle,MAIN_LINE,0,to_copy,ExtABuffer)<=0) return(RESET);
   if(CopyBuffer(MACD_Handle,SIGNAL_LINE,0,to_copy,ExtBBuffer)<=0) return(RESET);
//---- �������� ���� ������� ����������
   for(int bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      ExtABuffer[bar]/=_Point;
      ExtBBuffer[bar]/=_Point;
      IndBuffer[bar]=3*(ExtABuffer[bar]-ExtBBuffer[bar]);
     }
//----
   if(prev_calculated>rates_total || prev_calculated<=0) limit--;
//---- �������� ���� ��������� ���������� Ind
   for(int bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      int clr=2;
      //----
      if(IndBuffer[bar]>0)
        {
         if(IndBuffer[bar]>IndBuffer[bar+1]) clr=4;
         if(IndBuffer[bar]<IndBuffer[bar+1]) clr=3;
        }
      //----
      if(IndBuffer[bar]<0)
        {
         if(IndBuffer[bar]<IndBuffer[bar+1]) clr=0;
         if(IndBuffer[bar]>IndBuffer[bar+1]) clr=1;
        }
      //----
      ColorIndBuffer[bar]=clr;
     }
//----    
   return(rates_total);
  }
//+------------------------------------------------------------------+
