//+------------------------------------------------------------------+ 
//|                                                     iAnchMom.mq5 | 
//|                                            Copyright � 2007, NNN | 
//|                                                                  | 
//+------------------------------------------------------------------+  
#property copyright "Copyright � 2007, NNN"
#property link ""
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window 
//---- ���������� ������������ �������
#property indicator_buffers 2 
//---- ������������ ����� ���� ����������� ����������
#property indicator_plots   1
//+-----------------------------------+
//| ���������� ��������               |
//+-----------------------------------+
#define RESET 0 // ��������� ��� �������� ��������� ������� �� �������� ����������
//+-----------------------------------+
//| ��������� ��������� ����������    |
//+-----------------------------------+
//---- ��������� ���������� � ���� ������� �����������
#property indicator_type1   DRAW_COLOR_HISTOGRAM
#property indicator_color1  clrRed,clrMagenta,clrGray,clrBlue,clrGreen
#property indicator_width1  2
#property indicator_label1  "iAnchMom"
//+-----------------------------------+
//| ������� ��������� ����������      |
//+-----------------------------------+
input uint SMAPeriod=34;                  // ������ SMA
input uint EMAPeriod=20;                  // ������ EMA
input ENUM_APPLIED_PRICE IPC=PRICE_CLOSE; // ������� ���������, �� ������� ������������ ������ ����������
input int Shift=0;                        // ����� ���������� �� ����������� � �����
//+-----------------------------------+
//---- ������������ ������
double IndBuffer[];
double ColorIndBuffer[];
//---- ���������� ������������� ���������� ��� ������� �����������
int SMA_Handle,EMA_Handle;
//---- ���������� ������������� ���������� ������ ������� ������
int min_rates_total;
//+------------------------------------------------------------------+    
//| Momentum indicator initialization function                       | 
//+------------------------------------------------------------------+  
int OnInit()
  {
//---- ��������� ������ ���������� SMA
   SMA_Handle=iMA(NULL,0,SMAPeriod,0,MODE_SMA,IPC);
   if(SMA_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� SMA");
      return(INIT_FAILED);
     }
//---- ��������� ������ ���������� SMA
   EMA_Handle=iMA(NULL,0,EMAPeriod,0,MODE_EMA,IPC);
   if(EMA_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� EMA");
      return(INIT_FAILED);
     }
//---- ������������� ���������� ������ ������� ������   
   min_rates_total=int(SMAPeriod+1);
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(IndBuffer,true);
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(1,ColorIndBuffer,INDICATOR_COLOR_INDEX);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(ColorIndBuffer,true);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   StringConcatenate(shortname,"iAnchMom(",SMAPeriod,")");
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,4);
//---- ���������� �������������
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+  
//| Momentum iteration function                                      | 
//+------------------------------------------------------------------+  
int OnCalculate(const int rates_total,    // ���������� ������� � ����� �� ������� ����
                const int prev_calculated,// ���������� ������� � ����� �� ���������� ����
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
   if(BarsCalculated(SMA_Handle)<rates_total
      || BarsCalculated(EMA_Handle)<rates_total
      || rates_total<min_rates_total) return(RESET);
//---- ���������� ���������� � ��������� ������  
   double SMA[],EMA[];
//---- ���������� ������������� ���������� � ��������� ��� ����������� �����
   int to_copy,limit,bar;
//---- ������ ���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      limit=rates_total-1-min_rates_total+1; // ��������� ����� ��� ������� ���� �����
     }
   else
     {
      limit=rates_total-prev_calculated; // ��������� ����� ��� ������� ����� �����
     }
//----
   to_copy=limit+1;
//---- �������� ����� ����������� ������ � �������
   if(CopyBuffer(SMA_Handle,0,0,to_copy,SMA)<=0) return(RESET);
   if(CopyBuffer(EMA_Handle,0,0,to_copy,EMA)<=0) return(RESET);
//---- ���������� ��������� � �������� ��� � ����������  
   ArraySetAsSeries(SMA,true);
   ArraySetAsSeries(EMA,true);
//---- �������� ���� ������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      if(SMA[bar]) IndBuffer[bar]=100*((EMA[bar]/SMA[bar])-1.0);
      else IndBuffer[bar]=EMPTY_VALUE;
     }
//----
   if(prev_calculated>rates_total || prev_calculated<=0) limit--;
//---- �������� ���� ��������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
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
