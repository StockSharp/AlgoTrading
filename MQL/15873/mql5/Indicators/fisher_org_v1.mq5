//+------------------------------------------------------------------+
//|                                                Fisher_org_v1.mq5 |
//|                           Copyright � 2006, TrendLaboratory Ltd. |
//|            http://finance.groups.yahoo.com/group/TrendLaboratory |
//|                                       E-mail: igorad2004@list.ru |
//+------------------------------------------------------------------+
//---- ��������� ����������
#property copyright "Copyright � 2005, TrendLaboratory Ltd."
//---- ��������� ����������
#property link      "http://finance.groups.yahoo.com/group/TrendLaboratory"
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window
//---- ��� ������� � ��������� ���������� ������������ ��� ������
#property indicator_buffers 2
//---- ������������ ���� ����������� ����������
#property indicator_plots   1
//+----------------------------------------------+
//| ��������� ��������� ���������� Fisher_org_v1 |
//+----------------------------------------------+
//---- ��������� ���������� 1 � ���� �������� ������
#property indicator_type1   DRAW_FILLING
//---- � �������� ����jd ���������� ������������
#property indicator_color1  clrBlue,clrRed
//---- ����������� ����� ����� ����������
#property indicator_label1  "Fisher_org_v1"
//+----------------------------------------------+
//| ��������� ����������� �������������� ������� |
//+----------------------------------------------+
#property indicator_level1 +1.5
#property indicator_level2  0.0
#property indicator_level3 -1.5
#property indicator_levelcolor clrGray
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//|  ���������� ��������                         |
//+----------------------------------------------+
#define RESET 0       // ��������� ��� �������� ��������� ������� �� �������� ����������
//+----------------------------------------------+
//|  ���������� ������������                     |
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
input uint Length=7;                              // ������� �����������                   
input ENUM_APPLIED_PRICE_ IPC=PRICE_CLOSE_;       // ������� ���������
input int Shift=0;                                // ����� ���������� �� ����������� � ����� 
//+----------------------------------------------+
//---- ���������� ������������ ��������, ������� � ����������
//---- ����� ������������ � �������� ������������ �������
double IndBuffer[];
double TriggerBuffer[];
//---- ���������� ������������� ���������� ������ ������� ������
int min_rates_total;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
int OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_total=int(Length)+1;

//---- ����������� ������������� ������� IndBuffer[] � ������������ �����
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- ����������� ������������� ������� TriggerBuffer[] � ������������ �����
   SetIndexBuffer(1,TriggerBuffer,INDICATOR_DATA);
//---- ������������� ������ ���������� 1 �� ����������� �� Shift
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ���������� 1 �� min_rates_total
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   StringConcatenate(shortname,"Fisher_org_v1(",Length,")");
//---- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//---- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,4);
//---- ���������� �������������
   return(INIT_SUCCEEDED);
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
   if(rates_total<min_rates_total) return(RESET);

//---- ���������� ��������� ���������� 
   int first,bar;
   double smax,smin,price,wpr,Value0,res1,res2;
   static double Value1;

//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
     {
      first=min_rates_total-1;                           // ��������� ����� ��� ������� ���� �����
      Value1=0.0;
      IndBuffer[first-1]=0.0;
     }
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����

//---- �������� ���� ������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      smax=0.0;
      smin=9999999999.0;

      for(int iii=0; iii<int(Length); iii++)
        {
         int barx=bar-iii;
         smax=MathMax(smax,high[barx]);
         smin=MathMin(smin,low[barx]);
        }
      if(smax==smin) smax+=_Point;
      price=PriceSeries(IPC,bar,open,low,high,close);
      wpr=(price-smin)/(smax-smin);
      Value0=(wpr-0.5)+0.67*Value1;
      Value0=MathMin(Value0,+999);
      Value0=MathMax(Value0,-999);
      res1=1.0-Value0;
      if(!res1) res1=1.0;
      res2=(1.0+Value0)/(1.0-Value0);
      if(res2<0.0000001) res2=1.0;
      IndBuffer[bar]=0.5*MathLog(res2)+0.5*IndBuffer[bar-1];
      TriggerBuffer[bar]=IndBuffer[bar-1];
      if(bar<rates_total-1) Value1=Value0;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+   
//| ��������� �������� ������� ���������                             |
//+------------------------------------------------------------------+ 
double PriceSeries(uint applied_price,// ������� ���������
                   uint   bar,// ������ ������ ������������ �������� ���� �� ��������� ���������� �������� ����� ��� �����).
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
