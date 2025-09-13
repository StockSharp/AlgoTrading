//+------------------------------------------------------------------+
//|                                            PriceChannel_Stop.mq5 | 
//|                           Copyright � 2005, TrendLaboratory Ltd. | 
//|                                       E-mail: igorad2004@list.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright � 2005, TrendLaboratory Ltd." 
//---- ������ �� ���� ������
#property link "E-mail: igorad2004@list.ru" 
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ������� ����
#property indicator_chart_window 
//---- ��� ������� � ��������� ���������� ������������ ����� �������
#property indicator_buffers 6
//---- ������������ ����� ����� ����������� ����������
#property indicator_plots   6
//+----------------------------------------------+
//|  ��������� ��������� ���������� ����������   |
//+----------------------------------------------+
//---- ��������� ���������� 1 � ���� �������
#property indicator_type1   DRAW_ARROW
//---- � �������� ����� ������� ����� ����������� ������� ����
#property indicator_color1  Magenta
//---- ������� ����� ���������� 1 ����� 1
#property indicator_width1  1
//---- ����������� ����� ���������� 1
#property indicator_label1  "SellSignal"

//---- ��������� ���������� 2 � ���� �������
#property indicator_type2   DRAW_ARROW
//---- � �������� ����� �������� ���������� ����������� ������� ����
#property indicator_color2  Magenta
//---- ������� ����� ���������� 2 ����� 1
#property indicator_width2  1
//---- ����������� ����� ���������� 2
#property indicator_label2 "SellStopSignal"

//---- ��������� ���������� 3 � ���� �������
#property indicator_type3   DRAW_LINE
//---- � �������� ����� ����� ���������� ����������� ������� ����
#property indicator_color3  Magenta
//---- ������� ����� ���������� 3 ����� 1
#property indicator_width3  1
//---- ����������� ����� ���������� 3
#property indicator_label3 "SellStopLine"
//+----------------------------------------------+
//|  ��������� ��������� ������� ����������      |
//+----------------------------------------------+
//---- ��������� ���������� 4 � ���� �������
#property indicator_type4   DRAW_ARROW
//---- � �������� ����� ������� ����� ����������� ������-������� ����
#property indicator_color4  Lime
//---- ������� ����� ���������� 4 ����� 1
#property indicator_width4  1
//---- ����������� ����� ���������� 4
#property indicator_label4  "BuySignal"

//---- ��������� ���������� 5 � ���� �������
#property indicator_type5   DRAW_ARROW
//---- � �������� ����� �������� ���������� ����������� ������-������� ����
#property indicator_color5  Lime
//---- ������� ����� ���������� 5 ����� 1
#property indicator_width5  1
//---- ����������� ����� ���������� 5
#property indicator_label5 "BuyStopSignal"

//---- ��������� ���������� 6 � ���� �������
#property indicator_type6   DRAW_LINE
//---- � �������� ����� ����� ���������� ����������� ������-������� ����
#property indicator_color6  Lime
//---- ������� ����� ���������� 6 ����� 1
#property indicator_width6  1
//---- ����������� ����� ���������� 6
#property indicator_label6 "BuyStopLine"

//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input int ChannelPeriod=5;
input double Risk=0.10;
input bool Signal=true;
input bool Line=true;
//+----------------------------------------------+

//---- ���������� ������������ ��������, ������� � ���������� 
//---- ����� ������������ � �������� ������������ �������
double DownTrendSignal[];
double DownTrendBuffer[];
double DownTrendLine[];
double UpTrendSignal[];
double UpTrendBuffer[];
double UpTrendLine[];
//----
int StartBars;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
  {
//---- ������������� ���������� ���������� 
   StartBars=ChannelPeriod+1;
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,DownTrendSignal,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ���������� 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,StartBars);
//--- �������� ����� ��� ����������� � DataWindow
   PlotIndexSetString(0,PLOT_LABEL,"SellSignal");
//---- ������ ��� ����������
   PlotIndexSetInteger(0,PLOT_ARROW,108);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(DownTrendSignal,true);

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(1,DownTrendBuffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ���������� 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,StartBars);
//--- �������� ����� ��� ����������� � DataWindow
   PlotIndexSetString(1,PLOT_LABEL,"SellStopSignal");
//---- ������ ��� ����������
   PlotIndexSetInteger(1,PLOT_ARROW,159);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(DownTrendBuffer,true);

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(2,DownTrendLine,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ���������� 3
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,StartBars);
//--- �������� ����� ��� ����������� � DataWindow
   PlotIndexSetString(2,PLOT_LABEL,"SellStopLine");
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(DownTrendLine,true);
//---- ������ �� ��������� ����������� ������ ��������
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,0.0);

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(3,UpTrendSignal,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ���������� 4
   PlotIndexSetInteger(3,PLOT_DRAW_BEGIN,StartBars);
//--- �������� ����� ��� ����������� � DataWindow
   PlotIndexSetString(3,PLOT_LABEL,"BuySignal");
//---- ������ ��� ����������
   PlotIndexSetInteger(3,PLOT_ARROW,108);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(UpTrendSignal,true);

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(4,UpTrendBuffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ���������� 5
   PlotIndexSetInteger(4,PLOT_DRAW_BEGIN,StartBars);
//--- �������� ����� ��� ����������� � DataWindow
   PlotIndexSetString(4,PLOT_LABEL,"BuyStopSignal");
//---- ������ ��� ����������
   PlotIndexSetInteger(4,PLOT_ARROW,159);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(UpTrendBuffer,true);

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(5,UpTrendLine,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ���������� 6
   PlotIndexSetInteger(5,PLOT_DRAW_BEGIN,StartBars);
//--- �������� ����� ��� ����������� � DataWindow
   PlotIndexSetString(5,PLOT_LABEL,"BuyStopLine");
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(UpTrendLine,true);
//---- ������ �� ��������� ����������� ������ ��������
   PlotIndexSetDouble(5,PLOT_EMPTY_VALUE,0.0);
   
//---- ��������� ������� �������� ����������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- ��� ��� ���� ������ � ����� ��� ������� 
   string short_name="PriceChannel_Stop";
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
   int limit,bar,iii,trend;
   double bsmax[],bsmin[],High,Low,Price,dPrice;

//---- ���������� ���������� ������  
   static int trend_;
   static double bsmax_,bsmin_;

//---- ������� ���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      limit=rates_total-StartBars; // ��������� ����� ��� ������� ���� �����
     }
   else
     {
      limit=rates_total-prev_calculated; // ��������� ����� ��� ������� ����� �����
     }

//---- ���������� ��������� � �������� ��� � ����������
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
   ArraySetAsSeries(close,true);

//---- ��������� �������� ��������� �������� 
   if(ArrayResize(bsmax,limit+2)!=limit+2) return(0);
   if(ArrayResize(bsmin,limit+2)!=limit+2) return(0);

//---- ��������������� ���� ������� ��������� ��������
   for(bar=limit; bar>=0; bar--)
     {
      High=high[bar];
      Low =low [bar];
      iii=bar-1+ChannelPeriod;
      while(iii>=bar)
        {
         Price=high[iii];
         if(High<Price)High=Price;
         Price=low[iii];
         if(Low>Price) Low=Price;
         iii--;
        }
      dPrice=(High-Low)*Risk;
      bsmax[bar]=High-dPrice;
      bsmin[bar]=Low +dPrice;
     }

//---- ��������������� �������� ����������
   bsmax[limit+1]=bsmax_;
   bsmin[limit+1]=bsmin_;
   trend=trend_;

//---- �������� ���� ������� ����������
   for(bar=limit; bar>=0; bar--)
     {
//---- ���������� �������� ���������� ����� ��������� �� ������� ����
      if(rates_total!=prev_calculated && bar==0)
        {
         bsmax_=bsmax[1];
         bsmin_=bsmin[1];
         trend_=trend;
        }
//----        
      UpTrendBuffer  [bar]=0.0;
      DownTrendBuffer[bar]=0.0;
      UpTrendSignal  [bar]=0.0;
      DownTrendSignal[bar]=0.0;
      UpTrendLine    [bar]=0.0;
      DownTrendLine  [bar]=0.0;
//----
      if(close[bar]>bsmax[bar+1]) trend= 1;
      if(close[bar]<bsmin[bar+1]) trend=-1;
//----
      if(trend>0 && bsmin[bar]<bsmin[bar+1]) bsmin[bar]=bsmin[bar+1];
      if(trend<0 && bsmax[bar]>bsmax[bar+1]) bsmax[bar]=bsmax[bar+1];
//----
      if(trend>0)
        {
         Price=bsmin[bar];
         if(Signal && DownTrendBuffer[bar+1]>0)
           {
            UpTrendSignal[bar]=Price;
            if(Line) UpTrendLine[bar]=Price;
           }
         else
           {
            UpTrendBuffer[bar]=Price;
            if(Line) UpTrendLine[bar]=Price;
           }
        }
//----
      if(trend<0)
        {
         Price=bsmax[bar];
         if(Signal && UpTrendBuffer[bar+1]>0)
           {
            DownTrendSignal[bar]=Price;
            if(Line) DownTrendLine[bar]=Price;
           }
         else
           {
            DownTrendBuffer[bar]=Price;
            if(Line) DownTrendLine[bar]=Price;
           }
        }
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
