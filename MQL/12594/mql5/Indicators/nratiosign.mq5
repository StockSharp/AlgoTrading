//+---------------------------------------------------------------------+
//|                                                      NRatioSign.mq5 | 
//|                                              Copyright � 2006, Rosh | 
//|                                     http://konkop.narod.ru/nrma.htm | 
//+---------------------------------------------------------------------+ 
//| ��� ������  ����������  �������  �������� ���� SmoothAlgorithms.mqh |
//| � ����� (����������): �������_������_���������\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright � 2006, Rosh"
#property link "http://konkop.narod.ru/nrma.htm"
//--- ����� ������ ����������
#property version   "1.01"
//--- ��������� ���������� � ������� ����
#property indicator_chart_window 
//--- ��� ������� � ��������� ���������� ������������ ��� ������
#property indicator_buffers 2
//--- ������������ ��� ����������� ����������
#property indicator_plots   2
//+----------------------------------------------+
//| ��������� ��������� ���������� ����������    |
//+----------------------------------------------+
//--- ��������� ���������� 1 � ���� �������
#property indicator_type1   DRAW_ARROW
//--- � �������� ����� ��������� ����� ���������� ����������� DeepPink ����
#property indicator_color1  clrDeepPink
//--- ������� ����� ���������� 1 ����� 4
#property indicator_width1  4
//--- ����������� ����� ����� ����������
#property indicator_label1  "NRatioSign Sell"
//+----------------------------------------------+
//| ��������� ��������� ������ ����������        |
//+----------------------------------------------+
//--- ��������� ���������� 2 � ���� �������
#property indicator_type2   DRAW_ARROW
//--- � �������� ����� ����� ����� ���������� ����������� DodgerBlue ����
#property indicator_color2  clrDodgerBlue
//--- ������� ����� ���������� 2 ����� 4
#property indicator_width2  4
//--- ����������� ��������� ����� ����������
#property indicator_label2 "NRatioSign Buy"
//+----------------------------------------------+
//| ��������� ����������� �������������� ������� |
//+----------------------------------------------+
#property indicator_level1 80.0
#property indicator_level2 50.0
#property indicator_level3 20.0
#property indicator_levelcolor clrBlue
#property indicator_levelstyle STYLE_DASHDOTDOT
//+----------------------------------------------+
//| �������� ������ CXMA                         |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+
//--- ���������� ���������� ������ CXMA �� ����� SmoothAlgorithms.mqh
CXMA XMA1;
//+----------------------------------------------+
//| ���������� ��������                          |
//+----------------------------------------------+
#define RESET  0 // ��������� ��� �������� ��������� ������� �� �������� ����������
//+----------------------------------------------+
//| ���������� ������������                      |
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
   PRICE_SIMPL_,         //Simple Price (OC/2)
   PRICE_QUARTER_,       //Quarted Price (HLOC/4) 
   PRICE_TRENDFOLLOW0_,  //TrendFollow_1 Price 
   PRICE_TRENDFOLLOW1_,  //TrendFollow_2 Price
   PRICE_DEMARK_         //Demark Price
  };
//+----------------------------------------------+
//| ���������� ������������                      |
//+----------------------------------------------+
enum Alg_Method
  {
   MODE_IN,  //�������� �� ����� � ���� �� � ��
   MODE_OUT  //�������� �� ������ � ���� �� � ��
  };
//+----------------------------------------------+
//| ���������� ������������                      |
//+----------------------------------------------+
/*enum Smooth_Method - ������������ ��������� � ����� SmoothAlgorithms.mqh
  {
   MODE_SMA_,  //SMA
   MODE_EMA_,  //EMA
   MODE_SMMA_, //SMMA
   MODE_LWMA_, //LWMA
   MODE_JJMA,  //JJMA
   MODE_JurX,  //JurX
   MODE_ParMA, //ParMA
   MODE_T3,    //T3
   MODE_VIDYA, //VIDYA
   MODE_AMA,   //AMA
  }; */
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input Smooth_Method XMA_Method=MODE_SMA; // ����� ����������
input int XLength=3;                     // ������� �����������                    
input int XPhase=15;                     // �������� �����������
//--- XPhase: ��� JJMA ���������� � �������� -100 ... +100, ������ �� �������� ����������� ��������;
//--- XPhase: ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input Applied_price_ IPC=PRICE_CLOSE;    // ������� ���������
input double Kf=1;
input double Fast=2;
input double Sharp=2;
input Alg_Method Mode=MODE_OUT;          // ��������� ��������
input uint NRatio_UpLevel=80;            // ������� ���������������
input uint NRatio_DnLevel=20;            // ������� ���������������
input int    Shift=0;                    // ����� ���������� �� ����������� � �����
//+----------------------------------------------+
//--- ���������� ������������ ��������, ������� � ����������
//--- ����� ������������ � �������� ������������ �������
double SellBuffer[];
double BuyBuffer[];
//--- ���������� ���������� �������� ������������� ������ ���������� �������
double dF;
//--- ���������� ������������� ���������� ������ ������� ������
int min_rates_total;
int ATR_Handle;
//+------------------------------------------------------------------+   
//| Custom indicator initialization function                         | 
//+------------------------------------------------------------------+ 
int OnInit()
  {
//--- ������������� ���������� ������ ������� ������
   min_rates_total=XMA1.GetStartBars(XMA_Method,XLength,XPhase)+1;
   int ATR_Period=10;
   min_rates_total=int(MathMax(min_rates_total+1,ATR_Period));
//--- ��������� ������� �� ������������ �������� ������� ����������
   XMA1.XMALengthCheck("XLength",XLength);
   XMA1.XMAPhaseCheck("XPhase",XPhase,XMA_Method);
//--- ������������� ��������
   dF=2.0/(1.0+Fast);

//--- ��������� ������ ���������� ATR
   ATR_Handle=iATR(NULL,0,ATR_Period);
   if(ATR_Handle==INVALID_HANDLE)
     {
      Print(" �� ������� �������� ����� ���������� ATR");
      return(INIT_FAILED);
     }

   if(NRatio_UpLevel<=NRatio_DnLevel)
     {
      Print("������� ��������������� ������ ������ ���� ������ ������ ���������������!!!");
      return(INIT_FAILED);
     }

//--- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,SellBuffer,INDICATOR_DATA);
//--- ������������� ������ ������ ������� ��������� ���������� 1
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//--- ������ ��� ����������
   PlotIndexSetInteger(0,PLOT_ARROW,175);
//--- ������������� ������ ���������� 1 �� �����������
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);

//--- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(1,BuyBuffer,INDICATOR_DATA);
//--- ������������� ������ ������ ������� ��������� ���������� 2
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,min_rates_total);
//--- ������ ��� ����������
   PlotIndexSetInteger(1,PLOT_ARROW,175);
//--- ������������� ������ ���������� 1 �� �����������
   PlotIndexSetInteger(1,PLOT_SHIFT,Shift);

//--- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   string Smooth1=XMA1.GetString_MA_Method(XMA_Method);
   StringConcatenate(shortname,"NRatioSign(",XLength,", ",Smooth1,")");
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);

//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//--- ���������� �������������
   return(INIT_SUCCEEDED);
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
//--- �������� ���������� ����� �� ������������� ��� �������
   if(rates_total<min_rates_total) return(0);

//--- ���������� ���������� � ��������� ������  
   double price,NRTR0,LPrice,HPrice,Oscil,xOscil,ATR[1],NRatio0;
   static double NRTR1,NRatio1;
//--- ���������� ����� ���������� � ��������� ��� ����������� �����
   int first,bar,Trend0;
   static int Trend1;

//--- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
     {
      first=1; // ��������� ����� ��� ������� ���� �����
      bar=first-1;
      price=PriceSeries(IPC,bar,open,low,high,close);
      NRatio1=50;
      if(close[first]>open[first])
        {
         Trend1=+1;
         NRTR1=NormalizeDouble(price*(1.0-Kf*0.01),_Digits);
        }
      else
        {
         Trend1=-1;
         NRTR1=NormalizeDouble(price*(1.0+Kf*0.01),_Digits);
        }
     }
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����

//--- �������� ���� ������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      //--- ����� ������� PriceSeries ��� ��������� ������� ���� price
      price=PriceSeries(IPC,bar,open,low,high,close);
      NRTR0=NRTR1;
      Trend0=Trend1;

      if(Trend1>=0)
        {
         if(price<NRTR1)
           {
            Trend0=-1;
            NRTR0=NormalizeDouble(price*(1.0+Kf*0.01),_Digits);
           }
         else
           {
            Trend0=+1;
            LPrice=NormalizeDouble(price*(1.0-Kf*0.01),_Digits);
            if(LPrice>NRTR1) NRTR0=LPrice;
            else NRTR0=NRTR1;
           }
        }

      if(Trend1<=0)
        {
         if(price>NRTR1)
           {
            Trend0=+1;
            NRTR0=NormalizeDouble(price*(1.0-Kf*0.01),_Digits);
           }
         else
           {
            Trend0=-1;
            HPrice=NormalizeDouble(price*(1.0+Kf*0.01),_Digits);
            if(HPrice<NRTR1) NRTR0=HPrice;
            else NRTR0=NRTR1;
           }
        }

      Oscil=(100.0*MathAbs(price-NRTR0)/price)/Kf;
      xOscil=XMA1.XMASeries(1,prev_calculated,rates_total,XMA_Method,XPhase,XLength,Oscil,bar,false);
      NRatio0=100*MathPow(xOscil,Sharp);

      BuyBuffer[bar]=0.0;
      SellBuffer[bar]=0.0;
      
      if(Mode==MODE_IN)
        {
         if(NRatio0>NRatio_UpLevel && NRatio1<=NRatio_UpLevel)
           {
            //--- �������� ����� ����������� ������ � ������
            if(CopyBuffer(ATR_Handle,0,time[bar],1,ATR)<=0) return(RESET);
            BuyBuffer[bar]=low[bar]-ATR[0]*3/8;
           }
         if(NRatio0<NRatio_DnLevel && NRatio1>=NRatio_DnLevel)
           {
            //--- �������� ����� ����������� ������ � ������
            if(CopyBuffer(ATR_Handle,0,time[bar],1,ATR)<=0) return(RESET);
            SellBuffer[bar]=high[bar]+ATR[0]*3/8;
           }
        }
      else
        {
         if(NRatio0<NRatio_UpLevel && NRatio1>=NRatio_UpLevel)
           {
            //--- �������� ����� ����������� ������ � ������
            if(CopyBuffer(ATR_Handle,0,time[bar],1,ATR)<=0) return(RESET);
            SellBuffer[bar]=high[bar]+ATR[0]*3/8;
           }
         if(NRatio0>NRatio_DnLevel && NRatio1<=NRatio_DnLevel)
           {
            //--- �������� ����� ����������� ������ � ������
            if(CopyBuffer(ATR_Handle,0,time[bar],1,ATR)<=0) return(RESET);
            BuyBuffer[bar]=low[bar]-ATR[0]*3/8;
           }
        }

      if(bar<rates_total-1)
        {
         Trend1=Trend0;
         NRTR1=NRTR0;
         NRatio1=NRatio0;
        }
     }
//---     
   return(rates_total);
  }
//+------------------------------------------------------------------+
