//+------------------------------------------------------------------+
//|                                                         Loco.mq5 |
//|                                     Copyright � 2008, John Smith | 
//|                                                                  | 
//+------------------------------------------------------------------+ 
//---- ��������� ����������
#property copyright "Copyright � 2008, John Smith"
//---- ��������� ����������
#property link      ""
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ������� ����
#property indicator_chart_window 
//---- ���������� ������������ �������
#property indicator_buffers 2 
//---- ������������ ����� ���� ����������� ����������
#property indicator_plots   1
//+----------------------------------------------+
//| ��������� ��������� ����������               |
//+----------------------------------------------+
//---- ��������� ���������� � ���� ������������ ������
#property indicator_type1   DRAW_COLOR_ARROW
//---- � �������� ������ ������ ���������� ������������
#property indicator_color1 clrLime,clrMagenta
//---- ������� ����� ���������� 1 ����� 3
#property indicator_width1  3
//---- ����������� ����� ����������
#property indicator_label1  "Loco"
//+----------------------------------------------+
//| ���������� ������������                      |
//+----------------------------------------------+
enum ENUM_APPLIED_PRICE_ //��� ���������
  {
   PRICE_CLOSE_ = 1,     //PRICE_CLOSE
   PRICE_OPEN_,          //PRICE_OPEN
   PRICE_HIGH_,          //PRICE_HIGH
   PRICE_LOW_,           //PRICE_LOW
   PRICE_MEDIAN_,        //PRICE_MEDIAN
   PRICE_TYPICAL_,       //PRICE_TYPICAL
   PRICE_WEIGHTED_,      //PRICE_WEIGHTED
   PRICE_SIMPL_,         //PRICE_SIMPL_
   PRICE_QUARTER_,       //PRICE_QUARTER_
   PRICE_TRENDFOLLOW0_,  //PRICE_TRENDFOLLOW0_
   PRICE_TRENDFOLLOW1_,  // TrendFollow_2 Price 
   PRICE_DEMARK_         // Demark Price 
  };
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input uint Length=1;                              // ������� �������                   
input ENUM_APPLIED_PRICE_ IPC=PRICE_CLOSE_;       // ������� ���������
input int Shift=0;                                // ����� ���������� �� ����������� � �����
input int PriceShift=0;                           // ����� ���������� �� ��������� � �������
//+----------------------------------------------+
//---- ���������� ������������ ��������, ������� ����� � 
//---- ���������� ������������ � �������� ������������ �������
double LocoBuffer[],ColorLocoBuffer[];
double dPriceShift;
//---- ���������� ������������� ���������� ������ ������� ������
int min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_total=int(Length)+1;
//---- ������������� ����������
   dPriceShift=PriceShift*_Point;
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,LocoBuffer,INDICATOR_DATA);
//---- ������������� ������ ���������� 1 �� ����������� �� Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ���������� �� min_rates_total
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(LocoBuffer,true);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(ColorLocoBuffer,true);
//---- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(1,ColorLocoBuffer,INDICATOR_COLOR_INDEX);
//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   StringConcatenate(shortname,"Loco(",Length,", ",EnumToString(IPC),", ",Shift,", ",PriceShift,")");
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
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
   if(rates_total<min_rates_total) return(0);
//---- ���������� ��������� ���������� 
   int limit,bar;
   double series0,series1,result;
   static double prev;
//---- ������ ���������� ������ limit ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
     {
      limit=rates_total-min_rates_total-1; // ��������� ����� ��� ������� ���� �����
      prev=PriceSeries(IPC,limit,open,low,high,close);
     }
   else limit=rates_total-prev_calculated; // ��������� ����� ��� ������� ����� �����
//---- ���������� ��������� � �������� ��� � ����������  
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
   ArraySetAsSeries(open,true);
   ArraySetAsSeries(close,true);
   result=prev;
//---- �������� ���� ������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      series0=PriceSeries(IPC,bar,open,low,high,close);
      series1=PriceSeries(IPC,bar+Length,open,low,high,close);
      //----
      if(series0==prev)result=prev;
      else
        {
         if(series1>prev && series0>prev)
           {
            result=MathMax(prev,series0*0.999);
            ColorLocoBuffer[bar]=0;
           }
         else if(series1<prev && series0<prev)
           {
            result=MathMin(prev,series0*1.001);
            ColorLocoBuffer[bar]=1;
           }
         else
           {
            if(series0>prev)
              {
               result=series0*0.999;
               ColorLocoBuffer[bar]=0;
              }
            else
              {
               result=series0*1.001;
               ColorLocoBuffer[bar]=1;
              }
           }
        }
      LocoBuffer[bar]=result+dPriceShift;
      if(bar) prev=result;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+   
//| ��������� �������� ������� ���������                             |
//+------------------------------------------------------------------+ 
double PriceSeries(uint applied_price,// ������� ���������
                   uint   bar,        // ������ ������ ������������ �������� ���� �� ��������� ���������� �������� ����� ��� ������
                   const double &Open[],
                   const double &Low[],
                   const double &High[],
                   const double &Close[])
  {
//----
   switch(applied_price)
     {
      //---- ������� ��������� �� ������������ ENUM_APPLIED_PRICE
      case  PRICE_CLOSE: return(Close[bar]);
      case  PRICE_OPEN: return(Open [bar]);
      case  PRICE_HIGH: return(High [bar]);
      case  PRICE_LOW: return(Low[bar]);
      case  PRICE_MEDIAN: return((High[bar]+Low[bar])/2.0);
      case  PRICE_TYPICAL: return((Close[bar]+High[bar]+Low[bar])/3.0);
      case  PRICE_WEIGHTED: return((2*Close[bar]+High[bar]+Low[bar])/4.0);
      //----                            
      case  8: return((Open[bar] + Close[bar])/2.0);
      case  9: return((Open[bar] + Close[bar] + High[bar] + Low[bar])/4.0);
      //----                                
      case 10:
        {
         if(Close[bar]>Open[bar])return(High[bar]);
         else
           {
            if(Close[bar]<Open[bar])
               return(Low[bar]);
            else return(Close[bar]);
           }
        }
      //----         
      case 11:
        {
         if(Close[bar]>Open[bar])return((High[bar]+Close[bar])/2.0);
         else
           {
            if(Close[bar]<Open[bar])
               return((Low[bar]+Close[bar])/2.0);
            else return(Close[bar]);
           }
         break;
        }
      //----         
      case 12:
        {
         double res=High[bar]+Low[bar]+Close[bar];
         if(Close[bar]<Open[bar]) res=(res+Low[bar])/2;
         if(Close[bar]>Open[bar]) res=(res+High[bar])/2;
         if(Close[bar]==Open[bar]) res=(res+Close[bar])/2;
         return(((res-Low[bar])+(res-High[bar]))/2);
        }
      //----
      default: return(Close[bar]);
     }
//----
//return(0);
  }
//+------------------------------------------------------------------+
