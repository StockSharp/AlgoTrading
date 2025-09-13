//+------------------------------------------------------------------+ 
//|                                                          HVR.mq5 | 
//|                                         Copyright � 2005, Albert | 
//|                                                                  | 
//+------------------------------------------------------------------+ 
#property copyright "Copyright � 2005, Albert"
#property link ""
//---- ����� ������ ����������
#property version   "1.01"
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window 
//---- ���������� ������������ �������
#property indicator_buffers 1 
//---- ������������ ����� ���� ����������� ����������
#property indicator_plots   1
//+-----------------------------------+
//| ��������� ��������� ����������    |
//+-----------------------------------+
//---- ��������� ���������� � ���� �����
#property indicator_type1   DRAW_LINE
//---- � �������� ����� ����� ���������� ����������� ���������-����� ����
#property indicator_color1 clrBlueViolet
//---- ����� ���������� - ����������� ������
#property indicator_style1  STYLE_SOLID
//---- ������� ����� ���������� ����� 1
#property indicator_width1  1
//---- ����������� ����� ����� ����������
#property indicator_label1  "HVR"
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
input Applied_price_ IPC=PRICE_CLOSE_; // ������� ���������
//+-----------------------------------+
//---- ���������� ������������� �������, ������� ����� � 
//---- ���������� ����������� � �������� ������������� ������
double ExtLineBuffer[];
//---- ���������� ������������� ���������� ������ ������� ������
int min_rates_total,N;
//---- ���������� ������������ ��������, ������� ����� � 
//---- ���������� ������������ � �������� ��������� �������
int Count[];
double diff[],x6[],x100[];
//+------------------------------------------------------------------+
//| �������� ������� ������ ������ �������� � �������                |
//+------------------------------------------------------------------+   
void Recount_ArrayZeroPos(int &CoArr[],// ������� �� ������ ������ �������� �������� �������� ����
                          int Size)    // ���������� ��������� � ��������� ������
  {
//----
   int numb,Max1,Max2;
   static int count=1;
//----
   Max2=Size;
   Max1=Max2-1;
//----
   count--;
   if(count<0) count=Max1;
//----
   for(int iii=0; iii<Max2; iii++)
     {
      numb=iii+count;
      if(numb>Max1) numb-=Max2;
      CoArr[iii]=numb;
     }
  }
//+------------------------------------------------------------------+    
//| Custom indicator initialization function                         | 
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   N=100;
   min_rates_total=N;
//---- ������������� ������ ��� ������� ����������  
   ArrayResize(Count,N);
   ArrayResize(diff,N);
   ArrayResize(x100,N);
   ArrayResize(x6,N);
//---- ������������� �������� ����������
   ArrayInitialize(Count,0);
   ArrayInitialize(diff,0.0);
   ArrayInitialize(x100,0.0);
   ArrayInitialize(x6,0.0);
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,ExtLineBuffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(ExtLineBuffer,true);
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,"HVR");
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,2);
//---- ���������� �������������
  }
//+------------------------------------------------------------------+  
//| Custom indicator iteration function                              | 
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
   if(rates_total<min_rates_total) return(0);
//---- ���������� ���������� � ��������� ������  
   double  hv6,hv100,mean6,mean100;
//---- ���������� ������������� ���������� � ��������� ��� ����������� �����
   int limit,bar,bar0,i;
//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
      limit=rates_total-2; // ��������� ���������� ���� �����
   else limit=rates_total-prev_calculated; // ��������� ����� ��� ������� ����� �����
//---- ���������� ��������� � �������� ��� � ����������  
   ArraySetAsSeries(open,true);
   ArraySetAsSeries(close,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);
//---- �������� ���� ������� ����������
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      bar0=Count[0];
      diff[bar0]=MathLog(PriceSeries(IPC,bar,open,low,high,close)/PriceSeries(IPC,bar+1,open,low,high,close));
      //----
      for(i=0; i<6; i++) x6[bar0]=diff[Count[i]];
      for(i=0; i<100; i++) x100[bar0]=diff[Count[i]];
      //----
      mean6=0;
      for(i=0; i<6; i++) mean6+=x6[Count[i]];
      mean6/=6;
      //----
      mean100=0;
      for(i=0; i<100; i++) mean100+=x100[Count[i]];
      mean100/=100;
      //----
      hv6=0;
      for(i=0; i<6; i++) hv6+=MathPow(x6[Count[i]]-mean6,2);
      hv6=MathSqrt(hv6/5)*7.211102550927978586238442534941;
      //----
      hv100=0;
      for(i=0; i<100; i++) hv100+=MathPow(x100[Count[i]]-mean100,2);
      hv100=MathSqrt(hv100/99)*7.211102550927978586238442534941;
      //----
      if(hv100) ExtLineBuffer[bar]=hv6/hv100;
      else ExtLineBuffer[bar]=ExtLineBuffer[bar+1];
      //---- �������� ������� ��������� � ��������� �������
      if(bar>0) Recount_ArrayZeroPos(Count,N);
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
         //----
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
