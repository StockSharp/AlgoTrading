//+------------------------------------------------------------------+
//|                                     Volume_Weighted_MA_StDev.mq5 | 
//|                                    Copyright � 2011, StatBars TO | 
//|                                      http://ridecrufter.narod.ru | 
//+------------------------------------------------------------------+ 
#property copyright "Copyright � 2011, StatBars TO"
#property link "http://ridecrufter.narod.ru"
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ������� ����
#property indicator_chart_window 
//---- ��� ������� � ��������� ���������� ������������ ����� �������
#property indicator_buffers 6
//---- ������������ ����� ���� ����������� ����������
#property indicator_plots   5
//+----------------------------------------------+
//|  ��������� ��������� ����� ����������        |
//+----------------------------------------------+
//---- ��������� ���������� � ���� �����
#property indicator_type1   DRAW_COLOR_LINE
//---- � �������� ������ ���������� ����� ������������
#property indicator_color1  clrMagenta,clrGray,clrBlue
//---- ����� ���������� - ����������� ������
#property indicator_style1  STYLE_SOLID
//---- ������� ����� ���������� ����� 3
#property indicator_width1  3
//---- ����������� ����� ����������
#property indicator_label1  "Volume_Weighted_MA"
//+----------------------------------------------+
//|  ��������� ��������� ���������� ����������   |
//+----------------------------------------------+
//---- ��������� ���������� 2 � ���� �������
#property indicator_type2   DRAW_ARROW
//---- � �������� ����� ���������� ���������� ����������� ������� ����
#property indicator_color2  clrRed
//---- ������� ����� ���������� 2 ����� 2
#property indicator_width2  2
//---- ����������� ��������� ����� ����������
#property indicator_label2  "Dn_Signal 1"
//+----------------------------------------------+
//|  ��������� ��������� ������ ����������       |
//+----------------------------------------------+
//---- ��������� ���������� 3 � ���� �������
#property indicator_type3   DRAW_ARROW
//---- � �������� ����� ������� ���������� ����������� ��������� ����
#property indicator_color3  clrLawnGreen
//---- ������� ����� ���������� 3 ����� 2
#property indicator_width3  2
//---- ����������� ����� ����� ����������
#property indicator_label3  "Up_Signal 1"
//+----------------------------------------------+
//|  ��������� ��������� ���������� ����������   |
//+----------------------------------------------+
//---- ��������� ���������� 4 � ���� �������
#property indicator_type4   DRAW_ARROW
//---- � �������� ����� ���������� ���������� ����������� ������� ����
#property indicator_color4  clrRed
//---- ������� ����� ���������� 4 ����� 4
#property indicator_width4  4
//---- ����������� ��������� ����� ����������
#property indicator_label4  "Dn_Signal 2"
//+----------------------------------------------+
//|  ��������� ��������� ������ ����������       |
//+----------------------------------------------+
//---- ��������� ���������� 5 � ���� �������
#property indicator_type5   DRAW_ARROW
//---- � �������� ����� ������� ���������� ����������� ��������� ����
#property indicator_color5  clrLawnGreen
//---- ������� ����� ���������� 5 ����� 4
#property indicator_width5  4
//---- ����������� ����� ����� ����������
#property indicator_label5  "Up_Signal 2"
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
input double dK1=1.5;                              //����������� 1 ��� ������������� �������
input double dK2=2.5;                              //����������� 2 ��� ������������� �������
input uint std_period=9;                           //������ ������������� �������
//+----------------------------------------------+
//---- ���������� ������������ ��������, ������� ����� � 
// ���������� ������������ � �������� ������������ �������
double ExtLineBuffer[],ColorExtLineBuffer[];
double BearsBuffer1[],BullsBuffer1[];
double BearsBuffer2[],BullsBuffer2[];
//---- ���������� ���������� �������� ������������� ������ �������
double dPriceShift;
//---- ���������� ����� ���������� ������ ������� ������
int min_rates_total;
double Vol[],dVwma[];
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
   ArrayResize(dVwma,std_period);

//---- ����������� ������������� ������� ExtLineBuffer � ������������ �����
   SetIndexBuffer(0,ExtLineBuffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- ������ �� ��������� ����������� ������ ��������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- ������������� ������ ���������� 2 �� �����������
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);

//---- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(1,ColorExtLineBuffer,INDICATOR_COLOR_INDEX);

//---- ����������� ������������� ������� BearsBuffer � ������������ �����
   SetIndexBuffer(2,BearsBuffer1,INDICATOR_DATA);
//---- ������������� ������ ���������� 2 �� �����������
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//---- ����� ������� ��� ���������
   PlotIndexSetInteger(1,PLOT_ARROW,159);
//---- ������ �� ��������� ����������� ������ ��������
   PlotIndexSetDouble(1,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- ����������� ������������� ������� BullsBuffer � ������������ �����
   SetIndexBuffer(3,BullsBuffer1,INDICATOR_DATA);
//---- ������������� ������ ���������� 3 �� �����������
   PlotIndexSetInteger(2,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(2,PLOT_DRAW_BEGIN,min_rates_total);
//---- ����� ������� ��� ���������
   PlotIndexSetInteger(2,PLOT_ARROW,159);
//---- ������ �� ��������� ����������� ������ ��������
   PlotIndexSetDouble(2,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- ����������� ������������� ������� BearsBuffer � ������������ �����
   SetIndexBuffer(4,BearsBuffer2,INDICATOR_DATA);
//---- ������������� ������ ���������� 2 �� �����������
   PlotIndexSetInteger(3,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(3,PLOT_DRAW_BEGIN,min_rates_total);
//---- ����� ������� ��� ���������
   PlotIndexSetInteger(3,PLOT_ARROW,159);
//---- ������ �� ��������� ����������� ������ ��������
   PlotIndexSetDouble(3,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- ����������� ������������� ������� BullsBuffer � ������������ �����
   SetIndexBuffer(5,BullsBuffer2,INDICATOR_DATA);
//---- ������������� ������ ���������� 3 �� �����������
   PlotIndexSetInteger(4,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(4,PLOT_DRAW_BEGIN,min_rates_total);
//---- ����� ������� ��� ���������
   PlotIndexSetInteger(4,PLOT_ARROW,159);
//---- ������ �� ��������� ����������� ������ ��������
   PlotIndexSetDouble(4,PLOT_EMPTY_VALUE,EMPTY_VALUE);

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
   double mov,sum,vwma;
   double SMAdif,Sum,StDev,dstd,BEARS1,BULLS1,BEARS2,BULLS2,Filter1,Filter2;
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
      ExtLineBuffer[bar]=mov+dPriceShift; 
     }
//---- �������� ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
      first++;

//---- �������� ���� ��������� ���������� �����
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      int clr=1;
      if(ExtLineBuffer[bar-1]<ExtLineBuffer[bar]) clr=2;
      if(ExtLineBuffer[bar-1]>ExtLineBuffer[bar]) clr=0;
      ColorExtLineBuffer[bar]=clr;
     }

//---- �������� ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
      first=min_rates_total;
//---- �������� ���� ������� ���������� ����������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      //---- ��������� ���������� ���������� � ������ ��� ������������� ����������
      for(int iii=0; iii<int(std_period); iii++) dVwma[iii]=ExtLineBuffer[bar-iii]-ExtLineBuffer[bar-iii-1];

      //---- ������� ������� ������� ���������� ����������
      Sum=0.0;
      for(int iii=0; iii<int(std_period); iii++) Sum+=dVwma[iii];
      SMAdif=Sum/std_period;

      //---- ������� ����� ��������� ��������� ���������� � ��������
      Sum=0.0;
      for(int iii=0; iii<int(std_period); iii++) Sum+=MathPow(dVwma[iii]-SMAdif,2);

      //---- ���������� �������� �������� ������������������� ���������� StDev �� ���������� ����������
      StDev=MathSqrt(Sum/std_period);

      //---- ������������� ����������
      dstd=NormalizeDouble(dVwma[0],_Digits+2);
      Filter1=NormalizeDouble(dK1*StDev,_Digits+2);
      Filter2=NormalizeDouble(dK2*StDev,_Digits+2);
      BEARS1=EMPTY_VALUE;
      BULLS1=EMPTY_VALUE;
      BEARS2=EMPTY_VALUE;
      BULLS2=EMPTY_VALUE;
      vwma=ExtLineBuffer[bar];

      //---- ���������� ������������ ��������
      if(dstd<-Filter1 && dstd>=-Filter2) BEARS1=vwma; //���� ���������� �����
      if(dstd<-Filter2) BEARS2=vwma; //���� ���������� �����
      if(dstd>+Filter1 && dstd<=+Filter2) BULLS1=vwma; //���� ���������� �����
      if(dstd>+Filter2) BULLS2=vwma; //���� ���������� �����

      //---- ������������� ����� ������������ ������� ����������� ���������� 
      BullsBuffer1[bar]=BULLS1;
      BearsBuffer1[bar]=BEARS1;
      BullsBuffer2[bar]=BULLS2;
      BearsBuffer2[bar]=BEARS2;
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
