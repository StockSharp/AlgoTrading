//+------------------------------------------------------------------+
//|                                     Donchian_Channels_System.mq5 |
//|                               Copyright � 2013, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+ 
#property copyright "Copyright � 2013, Nikolay Kositsin"
#property link "farria@mail.redcom.ru"
#property description "��������� ������� � �������������� ���������� Donchian_Channels"
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ������� ����
#property indicator_chart_window
//---- ��� ������� � ��������� ���������� ������������ ������ �������
#property indicator_buffers 9
//---- ������������ ������ ����������� ����������
#property indicator_plots   4
//+----------------------------------------------+
//|  ��������� ��������� ���������� 1            |
//+----------------------------------------------+
//---- ��������� ���������� � ���� ������������ ������
#property indicator_type1   DRAW_FILLING
//---- � �������� ����� ���������� ����������� WhiteSmoke ����
#property indicator_color1  clrWhiteSmoke
//---- ����������� ����� ����������
#property indicator_label1  "Donchian_Channels"
//+----------------------------------------------+
//|  ��������� ��������� ���������� 2            |
//+----------------------------------------------+
//---- ��������� ���������� 2 � ���� �����
#property indicator_type2   DRAW_LINE
//---- � �������� ����� ����� ����� ���������� ����������� MediumSeaGreen ����
#property indicator_color2  clrMediumSeaGreen
//---- ����� ���������� 2 - ����������� ������
#property indicator_style2  STYLE_SOLID
//---- ������� ����� ���������� 2 ����� 2
#property indicator_width2  2
//---- ����������� ����� ����� ����������
#property indicator_label2  "Upper Donchian_Channels"
//+----------------------------------------------+
//|  ��������� ��������� ���������� 3            |
//+----------------------------------------------+
//---- ��������� ���������� 3 � ���� �����
#property indicator_type3   DRAW_LINE
//---- � �������� ����� ��������� ����� ���������� ����������� Magenta ����
#property indicator_color3  clrMagenta
//---- ����� ���������� 3 - ����������� ������
#property indicator_style3  STYLE_SOLID
//---- ������� ����� ���������� 3 ����� 2
#property indicator_width3  2
//---- ����������� ��������� ����� ����������
#property indicator_label3  "Lower Donchian_Channels"
//+----------------------------------------------+
//|  ��������� ��������� ���������� 4            |
//+----------------------------------------------+
//---- ��������� ���������� � ���� ������� ����
#property indicator_type4 DRAW_COLOR_CANDLES
//---- � �������� ������ ���������� ������������
#property indicator_color4 clrDeepPink,clrPurple,clrGray,clrMediumBlue,clrDodgerBlue
//---- ����� ���������� - ��������
#property indicator_style4 STYLE_SOLID
//---- ������� ����� ���������� ����� 2
#property indicator_width4 2
//---- ����������� ����� ����������
#property indicator_label4 "Donchian_Channels_BARS"
//+----------------------------------------------+
//|  ���������� ������������                     |
//+----------------------------------------------+
enum Applied_Extrem //��� �����������
  {
   HIGH_LOW,
   HIGH_LOW_OPEN,
   HIGH_LOW_CLOSE,
   OPEN_HIGH_LOW,
   CLOSE_HIGH_LOW
  };
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input uint DonchianPeriod=20;           // ������ ����������
input Applied_Extrem Extremes=HIGH_LOW; // ��� �����������
input int Margins=-2;
input uint   Shift=2;                   // ����� ������ �� ����������� � ����� 
//+----------------------------------------------+
//---- ���������� ������������ ��������, ������� ����� � 
// ���������� ������������ � �������� ������������ �������
double Up1Buffer[],Dn1Buffer[];
double Up2Buffer[],Dn2Buffer[];
double ExtOpenBuffer[],ExtHighBuffer[],ExtLowBuffer[],ExtCloseBuffer[],ExtColorBuffer[];
//---- ���������� ����� ���������� ������ ������� ������
int min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_total=int(DonchianPeriod+1+Shift);

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,Up1Buffer,INDICATOR_DATA);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(Up1Buffer,true);

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(1,Dn1Buffer,INDICATOR_DATA);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(Dn1Buffer,true);

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(2,Up2Buffer,INDICATOR_DATA);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(Up2Buffer,true);

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(3,Dn2Buffer,INDICATOR_DATA);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(Dn2Buffer,true);

//---- ����������� ������������� ������� IndBuffer � ������������ �����
   SetIndexBuffer(4,ExtOpenBuffer,INDICATOR_DATA);
   SetIndexBuffer(5,ExtHighBuffer,INDICATOR_DATA);
   SetIndexBuffer(6,ExtLowBuffer,INDICATOR_DATA);
   SetIndexBuffer(7,ExtCloseBuffer,INDICATOR_DATA);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(ExtOpenBuffer,true);
   ArraySetAsSeries(ExtHighBuffer,true);
   ArraySetAsSeries(ExtLowBuffer,true);
   ArraySetAsSeries(ExtCloseBuffer,true);

//---- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(8,ExtColorBuffer,INDICATOR_COLOR_INDEX);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(ExtColorBuffer,true);

//---- ������������� ������ ���������� 1 �� ����������� �� Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ���������� 1 �� min_rates_total
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);

//---- ������������� ������ ���������� 2 �� ����������� �� Shift
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ���������� 2 �� min_rates_total
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);

//---- ������������� ������ ���������� 3 �� ����������� �� Shift
   PlotIndexSetInteger(2,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ���������� 3 �� min_rates_total
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total);

//---- ������������� ������ ���������� 3 �� ����������� �� Shift
   PlotIndexSetInteger(3,PLOT_SHIFT,0);
//---- ������������� ������ ������ ������� ��������� ���������� 4 �� min_rates_total
   PlotIndexSetInteger(3,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(3,PLOT_EMPTY_VALUE,0);

//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   StringConcatenate(shortname,"Donchian_Channels(",DonchianPeriod,")");
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//----
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(
                const int rates_total,    // ���������� ������� � ����� �� ������� ����
                const int prev_calculated,// ���������� ������� � ����� �� ���������� ����
                const datetime &time[],
                const double &open[],
                const double &high[],
                const double &low[],
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[]
                )
  {
//---- �������� ���������� ����� �� ������������� ��� �������
   if(rates_total<min_rates_total) return(0);

//---- ���������� ��������� � �������� ��� � ����������  
   ArraySetAsSeries(open,true);
   ArraySetAsSeries(close,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);

//---- ���������� ����� ����������
   int limit;
//---- ���������� ���������� � ��������� ������  
   double smin,smax,SsMax=0.0,SsMin=0.0;

//---- ������� ���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      limit=rates_total-min_rates_total; // ��������� ����� ��� ������� ���� �����
     }
   else
     {
      limit=rates_total-prev_calculated; // ��������� ����� ��� ������� ����� �����
     }

//---- �������� ���� ������� ����������
   for(int bar=limit; bar>=0 && !IsStopped(); bar--)
     {

      switch(Extremes)
        {
         case HIGH_LOW:
            SsMax=high[ArrayMaximum(high,bar,DonchianPeriod)];
            SsMin=low[ArrayMinimum(low,bar,DonchianPeriod)];
            break;

         case HIGH_LOW_OPEN:
            SsMax=(open[ArrayMaximum(open,bar,DonchianPeriod)]+high[ArrayMaximum(high,bar,DonchianPeriod)])/2;
            SsMin=(open[ArrayMinimum(open,bar,DonchianPeriod)]+low[ArrayMinimum(low,bar,DonchianPeriod)])/2;
            break;

         case HIGH_LOW_CLOSE:
            SsMax=(close[ArrayMaximum(close,bar,DonchianPeriod)]+high[ArrayMaximum(high,bar,DonchianPeriod)])/2;
            SsMin=(close[ArrayMinimum(close,bar,DonchianPeriod)]+low[ArrayMinimum(low,bar,DonchianPeriod)])/2;
            break;

         case OPEN_HIGH_LOW:
            SsMax=open[ArrayMaximum(open,bar,DonchianPeriod)];
            SsMin=open[ArrayMinimum(open,bar,DonchianPeriod)];
            break;

         case CLOSE_HIGH_LOW:
            SsMax=close[ArrayMaximum(close,bar,DonchianPeriod)];
            SsMin=close[ArrayMinimum(close,bar,DonchianPeriod)];
            break;
        }

      smin=SsMin+(SsMax-SsMin)*Margins/100;
      smax=SsMax-(SsMax-SsMin)*Margins/100;

      Up1Buffer[bar]=smax;
      Dn1Buffer[bar]=smin;
      Up2Buffer[bar]=smax;
      Dn2Buffer[bar]=smin;
     }

//---- ������ ���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) limit-=int(Shift);
//---- �������� ���� ��������� ����� ����������
   for(int bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      int clr=2;
      ExtOpenBuffer[bar]=0.0;
      ExtCloseBuffer[bar]=0.0;
      ExtHighBuffer[bar]=0.0;
      ExtLowBuffer[bar]=0.0;

      if(close[bar]>Up1Buffer[bar+Shift])
        {
         if(open[bar]<=close[bar]) clr=4;
         else clr=3;
         ExtOpenBuffer[bar]=open[bar];
         ExtCloseBuffer[bar]=close[bar];
         ExtHighBuffer[bar]=high[bar];
         ExtLowBuffer[bar]=low[bar];
        }

      if(close[bar]<Dn1Buffer[bar+Shift])
        {
         if(open[bar]>close[bar]) clr=0;
         else clr=1;
         ExtOpenBuffer[bar]=open[bar];
         ExtCloseBuffer[bar]=close[bar];
         ExtHighBuffer[bar]=high[bar];
         ExtLowBuffer[bar]=low[bar];
        }

      ExtColorBuffer[bar]=clr;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
