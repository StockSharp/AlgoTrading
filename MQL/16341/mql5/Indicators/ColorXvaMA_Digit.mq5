//+---------------------------------------------------------------------+
//|                                                ColorXvaMA_Digit.mq5 | 
//|                                               Copyright � 2013, J.B | 
//|                                                                     | 
//+---------------------------------------------------------------------+ 
//| ��� ������  ����������  �������  �������� ���� SmoothAlgorithms.mqh |
//| � ����� (����������): �������_������_���������\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright � 2013, J.B"
#property link ""
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ������� ����
#property indicator_chart_window 
//---- ���������� ������������ �������
#property indicator_buffers 2 
//---- ������������ ����� ���� ����������� ����������
#property indicator_plots   1
//+----------------------------------------------+
//|  ��������� ��������� ����������              |
//+----------------------------------------------+
//---- ��������� ���������� � ���� �����
#property indicator_type1   DRAW_COLOR_LINE
//---- � �������� ������ ����� ������������
#property indicator_color1  clrDeepPink,clrDodgerBlue
//---- ����� ���������� - ����������� ������
#property indicator_style1  STYLE_SOLID
//---- ������� ����� ���������� ����� 3
#property indicator_width1  3
//---- ����������� ����� ����������
#property indicator_label1  "ColorXvaMA_Digit"
//+----------------------------------------------+
//|  �������� ������ CXMA                        |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+

//---- ���������� ���������� ������ CXMA �� ����� SmoothAlgorithms.mqh
CXMA XMA1,XMA2;
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
//|  ���������� ������������                     |
//+----------------------------------------------+
/*enum SmoothMethod - ������������ ��������� � ����� SmoothAlgorithms.mqh
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
input string  SirName="ColorXvaMA_Digit"; //������ ����� ����� ����������� ��������
input Smooth_Method XMA_Method1=MODE_EMA_;//����� ����������
input uint XLength1=15;                   //������� ����������                    
input int XPhase1=15;                     //�������� ����������,
//---- ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������;
//---- ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input Smooth_Method XMA_Method2=MODE_JJMA;//����� �����������
input uint XLength2=5;                    //������� �����������                    
input int XPhase2=100;                    //�������� �����������,
//---- ��� JJMA ������������ � �������� -100 ... +100, ������ �� �������� ����������� ��������;
//---- ��� VIDIA ��� ������ CMO, ��� AMA ��� ������ ��������� ����������
input Applied_price_ IPC=PRICE_CLOSE_;    //������� ���������
input int Shift=0;                        //����� ���������� �� ����������� � �����
input int PriceShift=0;                   //c���� ���������� �� ��������� � �������
input uint Digit=2;                       //���������� �������� ����������
input bool ShowPrice=true;                //���������� ������� �����
//---- ����� ������� �����
input color  Price_color=clrGray;         //���� ������� �����
//+----------------------------------------------+
//---- ���������� ������������ ��������, ������� ����� � ���������� ������������ � �������� ������������ �������
double ExtLineBuffer[],ColorExtLineBuffer[];
//---- ���������� ���������� �������� ������������� ������ �������
double dPriceShift;
double PointPow10;
//---- ���������� ����� ���������� ������ ������� ������
int min_rates,min_rates_total,XLength4,XLength8,XLength12;
//---- ���������� ���������� ����������
int Count[];
double Xma[];
//---- ���������� �������� ��� ��������� �����
string Price_name;
//+------------------------------------------------------------------+
//|  �������� ������� ������ ������ �������� � �������               |
//+------------------------------------------------------------------+   
void Recount_ArrayZeroPos(int &CoArr[],// ������� �� ������ ������ �������� �������� �������� ����
                          int Size)
  {
//----
   int numb,Max1,Max2;
   static int count=1;

   Max2=Size;
   Max1=Max2-1;

   count--;
   if(count<0) count=Max1;

   for(int iii=0; iii<Max2; iii++)
     {
      numb=iii+count;
      if(numb>Max1) numb-=Max2;
      CoArr[iii]=numb;
     }
//----
  }
//+------------------------------------------------------------------+   
//| XvaMA indicator initialization function                          | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates=GetStartBars(XMA_Method1,XLength1,XPhase1);
   min_rates_total=min_rates+GetStartBars(XMA_Method2,XLength2,XPhase2);
   XLength4=int(XLength1/4);
   XLength8=int(XLength1/8);
   XLength12=int(XLength1/12);
   PointPow10=_Point*MathPow(10,Digit);
//---- ������������� ��������
   Price_name=SirName+"Price";
//---- ��������� ������� �� ������������ �������� ������� ����������
   XMA1.XMALengthCheck("XLength1",XLength1);
   XMA1.XMALengthCheck("XLength2",XLength2);
   XMA1.XMAPhaseCheck("XPhase1",XPhase1,XMA_Method1);
   XMA1.XMAPhaseCheck("XPhase2",XPhase2,XMA_Method2);
//---- ������������� ������ �� ���������
   dPriceShift=_Point*PriceShift;
//---- ������������� ������ ��� ������� ����������  
   ArrayResize(Count,XLength1);
   ArrayResize(Xma,XLength1);

//---- ����������� ������������� ������� ExtLineBuffer � ������������ �����
   SetIndexBuffer(0,ExtLineBuffer,INDICATOR_DATA);
//---- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(1,ColorExtLineBuffer,INDICATOR_COLOR_INDEX);
//---- ������������� ������ ���������� 1 �� �����������
   PlotIndexSetInteger(0,PLOT_SHIFT,Shift);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);

//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   string Smooth1=XMA1.GetString_MA_Method(XMA_Method1);
   StringConcatenate(shortname,"XvaMA(",XLength1,", ",Smooth1,")");
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);

//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
//---- ���������� �������������
  }
//+------------------------------------------------------------------+
//| Custom indicator deinitialization function                       |
//+------------------------------------------------------------------+    
void OnDeinit(const int reason)
  {
//----
   ObjectDelete(0,Price_name);
//----
   ChartRedraw(0);
  }
//+------------------------------------------------------------------+ 
//| XvaMA iteration function                                         | 
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
   double price,vel,acc,aaa,vama,xvama,trend;
//---- ���������� ����� ���������� � ��������� ��� ����������� �����
   int first,bar;

//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
     {
      first=0; // ��������� ����� ��� ������� ���� �����
      //---- ��������� ����������� ����������� �������
      ArrayInitialize(Count,0);
      ArrayInitialize(Xma,0.0);
     }
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����

//---- �������� ���� ������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      price=PriceSeries(IPC,bar,open,low,high,close);                                                             //��������� ����
      Xma[Count[0]]=XMA1.XMASeries(0,prev_calculated,rates_total,XMA_Method1,XPhase1,XLength1,price,bar,false);   //���������� ���� 
      vel=Xma[Count[0]]-Xma[Count[XLength4]];                                                                     //���������� ����� ������
      acc=Xma[Count[0]]-2*Xma[Count[XLength4]]+Xma[Count[XLength8]];                                              //���������� ���������� ����� ������
      aaa=Xma[Count[0]]-3*Xma[Count[XLength4]]+3*Xma[Count[XLength8]]-Xma[Count[XLength12]];                      //���������� ���������� ����������...                                                                                         
      vama=Xma[Count[0]]+vel+acc/2+aaa/6;                                                                         //������������ ����
      xvama=XMA2.XMASeries(min_rates,prev_calculated,rates_total,XMA_Method2,XPhase2,XLength2,vama,bar,false);    //����������� ����������   
      ExtLineBuffer[bar]=PointPow10*MathRound(xvama/PointPow10);                                                  //������������� ������ � ���������� ��������� �������� ���������� 
      ExtLineBuffer[bar]+=dPriceShift;                                                                            //���������� ������������� �����      
      if(bar<rates_total-1) Recount_ArrayZeroPos(Count,XLength1);                                                 //�������� ������� ������� � ����������� ������
     }
//---- ����������� ������� �����
   if(ShowPrice)
     {
      int bar0=rates_total-1;
      datetime time0=time[bar0]+1*PeriodSeconds();
      SetRightPrice(0,Price_name,0,time0,ExtLineBuffer[bar0],Price_color);
     }
//---- ������������� �������� ���������� first
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
      first++; // ��������� ����� ��� ������� ���� �����

//---- �������� ���� ��������� ���������� �����
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      double clr=ColorExtLineBuffer[bar-1];
      trend=ExtLineBuffer[bar]-ExtLineBuffer[bar-1];
      if(trend>0) clr=1;
      if(trend<0) clr=0;
      ColorExtLineBuffer[bar]=clr;
     }
//----    
   ChartRedraw(0);
   return(rates_total);
  }
//+------------------------------------------------------------------+
//|  RightPrice creation                                             |
//+------------------------------------------------------------------+
void CreateRightPrice(long chart_id,// chart ID
                      string   name,              // object name
                      int      nwin,              // window index
                      datetime time,              // price level time
                      double   price,             // price level
                      color    Color              // Text color
                      )
//---- 
  {
//----
   ObjectCreate(chart_id,name,OBJ_ARROW_RIGHT_PRICE,nwin,time,price);
   ObjectSetInteger(chart_id,name,OBJPROP_COLOR,Color);
   ObjectSetInteger(chart_id,name,OBJPROP_BACK,true);
   ObjectSetInteger(chart_id,name,OBJPROP_WIDTH,2);
//----
  }
//+------------------------------------------------------------------+
//|  RightPrice reinstallation                                       |
//+------------------------------------------------------------------+
void SetRightPrice(long chart_id,// chart ID
                   string   name,              // object name
                   int      nwin,              // window index
                   datetime time,              // price level time
                   double   price,             // price level
                   color    Color              // Text color
                   )
//---- 
  {
//----
   if(ObjectFind(chart_id,name)==-1) CreateRightPrice(chart_id,name,nwin,time,price,Color);
   else ObjectMove(chart_id,name,0,time,price);
//----
  }
//+------------------------------------------------------------------+
