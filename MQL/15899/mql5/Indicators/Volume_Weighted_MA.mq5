//+------------------------------------------------------------------+
//|                                           Volume_Weighted_MA.mq5 | 
//|                                    Copyright � 2011, StatBars TO | 
//|                                      http://ridecrufter.narod.ru | 
//+------------------------------------------------------------------+ 
#property copyright "Copyright � 2011, StatBars TO"
#property link "http://ridecrufter.narod.ru"
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ������� ����
#property indicator_chart_window 
//---- ���������� ������������ �������
#property indicator_buffers 1 
//---- ������������ ����� ���� ����������� ����������
#property indicator_plots   1
//+----------------------------------------------+
//|  ��������� ��������� ����������              |
//+----------------------------------------------+
//---- ��������� ���������� � ���� �����
#property indicator_type1   DRAW_LINE
//---- � �������� ����� ����� ���������� ����������� DodgerBlue ����
#property indicator_color1 clrDodgerBlue
//---- ����� ���������� - ����������� ������
#property indicator_style1  STYLE_SOLID
//---- ������� ����� ���������� ����� 2
#property indicator_width1  2
//---- ����������� ����� ����������
#property indicator_label1  "Volume_Weighted_MA"
//+----------------------------------------------+
//|  ���������� ������������                     |
//+----------------------------------------------+
enum Applied_price_      //��� ���������
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
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input uint Length=12;                              //������� �����������                    
input Applied_price_ IPC=PRICE_CLOSE_;             //������� ���������
input ENUM_APPLIED_VOLUME VolumeType=VOLUME_TICK;  //����� 
input int Shift=0;                                 //����� ���������� �� ����������� � �����
input int PriceShift=0;                            //c���� ���������� �� ��������� � �������
//+----------------------------------------------+
//---- ���������� ������������� �������, ������� ����� � 
// ���������� ����������� � �������� ������������� ������
double IndBuffer[];
//---- ���������� ���������� �������� ������������� ������ �������
double dPriceShift;
//---- ���������� ����� ���������� ������ ������� ������
int min_rates_total;
double Vol[];
//+------------------------------------------------------------------+   
//| Volume_Weighted_MA initialization function                       | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_total=int(Length);
//---- ������������� ������ �� ���������
   dPriceShift=_Point*PriceShift;
//---- ������������� ������ ��� ������� ����������  
   ArrayResize(Vol,Length);

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- ������������� ������ ���������� 1 �� �����������
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,0);

//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   StringConcatenate(shortname,"Volume_Weighted_MA(",Length,")");
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);

//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- ���������� �������������
  }
//+------------------------------------------------------------------+ 
//| Volume_Weighted_MA iteration function                            | 
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

//---- ���������� ���������� � ��������� ������  
   double mov,sum;
//---- ���������� ����� ����������
   int first,bar;

//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
      first=min_rates_total-1; // ��������� ����� ��� ������� ���� �����
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����

//---- �������� ���� ������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      sum=0.0;
      for(int kkk=int(bar-Length+1); kkk<=bar; kkk++)
        {
         int index=bar-kkk;
         if(VolumeType==VOLUME_TICK) Vol[index]=double(tick_volume[kkk]);
         else Vol[index]=double(volume[kkk]);
         sum+=Vol[index];
        }
      for(int rrr=0; rrr<int(Length); rrr++) Vol[rrr]/=sum;
      mov=0.0;
      for(int kkk=int(bar-Length+1); kkk<=bar; kkk++) mov+=PriceSeries(IPC,kkk,open,low,high,close)*Vol[bar-kkk];
      //----       
      IndBuffer[bar]=mov+dPriceShift; 
     }
//----     
   return(rates_total);
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
