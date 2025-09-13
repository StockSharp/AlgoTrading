//+------------------------------------------------------------------+ 
//|                                                 Bezier_StDev.mq5 | 
//|                                     Copyright � 2007, Lizhniyk E |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Copyright � 2007, Lizhniyk E"
#property link      "Lizhniyk E"
//---- ����� ������ ����������
#property version   "1.01"
//---- ��������� ���������� � ������� ����
#property indicator_chart_window 
//---- ��� ������� � ��������� ���������� ������������ ������ ������
#property indicator_buffers 4
//---- ������������ ��� ����������� ����������
#property indicator_plots   3
//+----------------------------------------------+
//|  ��������� ��������� ����������              |
//+----------------------------------------------+
//---- ��������� ���������� � ���� �����
#property indicator_type1   DRAW_COLOR_LINE
//---- � �������� ����� ����� ���������� ������������
#property indicator_color1 clrGray,clrDodgerBlue,clrChocolate
//---- ����� ���������� - ����������� ������
#property indicator_style1  STYLE_SOLID
//---- ������� ����� ���������� ����� 3
#property indicator_width1  3
//---- ����������� ����� ����������
#property indicator_label1  "Bezier"
//+----------------------------------------------+
//|  ��������� ��������� ���������� ����������   |
//+----------------------------------------------+
//---- ��������� ���������� 2 � ���� �������
#property indicator_type2   DRAW_ARROW
//---- � �������� ����� ���������� ���������� ����������� ������� ����
#property indicator_color2  clrMagenta
//---- ������� ����� ���������� 2 ����� 3
#property indicator_width2  3
//---- ����������� ��������� ����� ����������
#property indicator_label2  "Dn_Signal"
//+----------------------------------------------+
//|  ��������� ��������� ������ ����������       |
//+----------------------------------------------+
//---- ��������� ���������� 3 � ���� �������
#property indicator_type3   DRAW_ARROW
//---- � �������� ����� ������� ���������� ����������� ������ ����
#property indicator_color3  clrLime
//---- ������� ����� ���������� 3 ����� 3
#property indicator_width3  3
//---- ����������� ����� ����� ����������
#property indicator_label3  "Up_Signal"
//+----------------------------------------------+
//|  ���������� ������������                     |
//+----------------------------------------------+
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
//+----------------------------------------------+
//|  ���������� ������������                     |
//+----------------------------------------------+
enum Signal_mode
  {
   Trend, //�� ������
   Kalman //�� ��������
  };
//+----------------------------------------------+
//|  ������� ��������� ����������                |
//+----------------------------------------------+
input uint BPeriod=8;                      //������ ����������
input double T=0.5;                        //����������� ���������������� (�� 0 �� 1)               
input Applied_price_ IPC=PRICE_WEIGHTED_;  //������� ���������
input double dK=2.0;                       //����������� ��� ������������� �������
input uint std_period=9;                   //������ ������������� �������
input int Shift=0;                         //����� ���������� �� ����������� � �����
input int PriceShift=0;                    //c���� ���������� �� ��������� � �������
//+----------------------------------------------+
//---- ������������ ������
double BezierBuffer[],ColorBezierBuffer[];
double BearsBuffer[];
double BullsBuffer[];
//----
double dPriceShift,t,dBezier[];
//---- ���������� ������������� ���������� ������ ������� ������
int min_rates_total,min_rates_1;
//+------------------------------------------------------------------+    
//| Bezier indicator initialization function                         | 
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_1=int(BPeriod);
   min_rates_total=min_rates_1+1+int(std_period);

//---- ������������� ������ �� ���������
   dPriceShift=_Point*PriceShift;
   t=MathMin(MathMax(T,0),1);
   
//---- ������������� ������ ��� ������� ����������  
   ArrayResize(dBezier,std_period);

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,BezierBuffer,INDICATOR_DATA);
//---- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(1,ColorBezierBuffer,INDICATOR_COLOR_INDEX);
//---- ������������� ������ ���������� 1 �� �����������
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0.0);
   
//---- ����������� ������������� ������� BearsBuffer � ������������ �����
   SetIndexBuffer(2,BearsBuffer,INDICATOR_DATA);
//---- ������������� ������ ���������� 2 �� �����������
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- ����� ������� ��� ���������
   PlotIndexSetInteger(1,PLOT_ARROW,159);
//---- ������ �� ��������� ����������� ������ ��������
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,0.0);

//---- ����������� ������������� ������� BullsBuffer � ������������ �����
   SetIndexBuffer(3,BullsBuffer,INDICATOR_DATA);
//---- ������������� ������ ���������� 3 �� �����������
   PlotIndexSetInteger(2,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total);
//---- ����� ������� ��� ���������
   PlotIndexSetInteger(2,PLOT_ARROW,159);
//---- ������ �� ��������� ����������� ������ ��������
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,0.0);
   
//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   StringConcatenate(shortname,"Bezier(",BPeriod,")");
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
//---- ���������� �������������
  }
//+------------------------------------------------------------------+  
//| Bezier iteration function                                        | 
//+------------------------------------------------------------------+  
int OnCalculate(
                const int rates_total,    // ���������� ������� � ����� �� ������� ����
                const int prev_calculated,// ���������� ������� � ����� �� ���������� ����
                const datetime &time[],
                const double &open[],
                const double& high[],     // ������� ������ ���������� ���� ��� ������� ����������
                const double& low[],      // ������� ������ ��������� ����  ��� ������� ����������
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[]
                )
  {
//---- �������� ���������� ����� �� ������������� ��� �������
   if(rates_total<min_rates_total) return(0);

//---- ���������� ��������� ����������
   int first,bar;
   double SMAdif,Sum,StDev,dstd,BEARS,BULLS,Filter;
//----

   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
        first=min_rates_1+1; // ��������� ����� ��� ������� ���� �����
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����

//---- �������� ���� ������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      double r=0;
      for(int iii=int(BPeriod); iii>=0; iii--)
         r+=PriceSeries(IPC,bar-iii,open,low,high,close)*
            (factorial(BPeriod)/(factorial(iii)*factorial(BPeriod-iii)))*MathPow(t,iii)*MathPow(1-t,BPeriod-iii);
            
      BezierBuffer[bar]=r+dPriceShift;
     }
     
//---- ������������� �������� ���������� first
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
      first=min_rates_total; // ��������� ����� ��� ������� ���� �����
           
//---- �������� ���� ��������� ���������� �����
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      ColorBezierBuffer[bar]=0;
      if(BezierBuffer[bar-1]<BezierBuffer[bar]) ColorBezierBuffer[bar]=1;
      if(BezierBuffer[bar-1]>BezierBuffer[bar]) ColorBezierBuffer[bar]=2;
     }

//---- �������� ���� ������� ���������� ����������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      //---- ��������� ���������� ���������� � ������ ��� ������������� ����������
      for(int iii=0; iii<int(std_period); iii++) dBezier[iii]=BezierBuffer[bar-iii-0]-BezierBuffer[bar-iii-1];

      //---- ������� ������� ������� ���������� ����������
      Sum=0.0;
      for(int iii=0; iii<int(std_period); iii++) Sum+=dBezier[iii];
      SMAdif=Sum/std_period;

      //---- ������� ����� ��������� ��������� ���������� � ��������
      Sum=0.0;
      for(int iii=0; iii<int(std_period); iii++) Sum+=MathPow(dBezier[iii]-SMAdif,2);

      //---- ���������� �������� �������� ������������������� ���������� StDev �� ���������� ����������
      StDev=MathSqrt(Sum/std_period);

      //---- ������������� ����������
      dstd=NormalizeDouble(dBezier[0],_Digits+2);
      Filter=NormalizeDouble(dK*StDev,_Digits+2);
      BEARS=0;
      BULLS=0;

      //---- ���������� ������������ ��������
      if(dstd<-Filter) BEARS=BezierBuffer[bar]; //���� ���������� �����
      if(dstd>+Filter) BULLS=BezierBuffer[bar]; //���� ���������� �����

      //---- ������������� ����� ������������ ������� ����������� ���������� 
      BullsBuffer[bar]=BULLS;
      BearsBuffer[bar]=BEARS;
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+    
//| ���������� ����������                                            | 
//+------------------------------------------------------------------+  
int factorial(int value)
  {
//---- 
   int res=1;
   for(int j=2; j<value+1; j++) res*=j;
//---- ������� ����������
   return(res);
  }
//+------------------------------------------------------------------+   
//| ��������� �������� ������� ���������                             |
//+------------------------------------------------------------------+ 
double PriceSeries
(
 uint applied_price,// ������� ���������
 uint   bar,// ������ ������ ������������ �������� ���� �� ��������� ���������� �������� ����� ��� �����).
 const double &Open[],
 const double &Low[],
 const double &High[],
 const double &Close[]
 )
//PriceSeries(applied_price, bar, open, low, high, close)
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
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
