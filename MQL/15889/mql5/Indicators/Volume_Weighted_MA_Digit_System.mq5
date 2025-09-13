//+------------------------------------------------------------------+
//|                              Volume_Weighted_MA_Digit_System.mq5 |
//|                               Copyright � 2016, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+ 
#property copyright "Copyright � 2016, Nikolay Kositsin"
#property link "farria@mail.redcom.ru"
#property description "��������� ������� � �������������� ���������� Volume_Weighted_MA_Digit"
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ������� ����
#property indicator_chart_window
//---- ��� ������� � ��������� ���������� ������������ ���� �������
#property indicator_buffers 7
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
#property indicator_label1  "Volume_Weighted_MA_Digit"
//+----------------------------------------------+
//|  ��������� ��������� ���������� 2            |
//+----------------------------------------------+
//---- ��������� ���������� 2 � ���� �����
#property indicator_type2   DRAW_LINE
//---- � �������� ����� ����� ����� ���������� ����������� DodgerBlue ����
#property indicator_color2  clrDodgerBlue
//---- ����� ���������� 2 - ����������� ������
#property indicator_style2  STYLE_SOLID
//---- ������� ����� ���������� 2 ����� 2
#property indicator_width2  2
//---- ����������� ����� ����� ����������
#property indicator_label2  "Upper Volume_Weighted_MA_Digit"
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
#property indicator_label3  "Lower Volume_Weighted_MA_Digit"
//+----------------------------------------------+
//|  ��������� ��������� ���������� 4            |
//+----------------------------------------------+
//---- ��������� ���������� � ���� ������������� �����������
#property indicator_type4 DRAW_COLOR_HISTOGRAM2
//---- � �������� ������ ������������� ����������� ������������
#property indicator_color4 clrRed,clrPurple,clrGray,clrTeal,clrLime
//---- ����� ���������� - ��������
#property indicator_style4 STYLE_SOLID
//---- ������� ����� ���������� ����� 2
#property indicator_width4 2
//---- ����������� ����� ����������
#property indicator_label4 "Volume_Weighted_MA_Digit_BARS"
//+----------------------------------------------+
//| ������� ��������� ����������                 |
//+----------------------------------------------+
input string  SirName="Volume_Weighted_MA_Digit";     //������ ����� ����� ����������� ��������
input uint Length=12;                                 //������� �����������                    
input ENUM_APPLIED_VOLUME VolumeType=VOLUME_TICK;     //����� 
input uint Digit=2;                                   //���������� �������� ����������
input int PriceShift=0;                               //c���� ������ �� ��������� � �������
input uint   Shift=2;                                 //����� ������ �� ����������� � ����� 
input bool ShowPrice=true;                            //���������� ������� �����
//---- ����� ������� �����
input color  Up_Price_color=clrTeal;
input color  Dn_Price_color=clrMagenta;
//+----------------------------------------------+
//---- ���������� ������������ ��������, ������� ����� � 
// ���������� ������������ � �������� ������������ �������
double Up1Buffer[],Dn1Buffer[];
double Up2Buffer[],Dn2Buffer[];
double UpIndBuffer[],DnIndBuffer[],ColorIndBuffer[];
//---- ���������� ����� ���������� ������ ������� ������
int min_rates_total;
int FATLSize;
double dPriceShift;
double PointPow10;
//---- ���������� �������� ��� ��������� �����
string Dn_Price_name,Up_Price_name;
double Vol[];
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- ������������� ���������� 
   min_rates_total=int(Length);
   min_rates_total+=int(Shift);
//---- ������������� ������ �� ���������
   dPriceShift=_Point*PriceShift;
   PointPow10=_Point*MathPow(10,Digit);
//---- ������������� ��������
   Up_Price_name=SirName+"Up_Price";
   Dn_Price_name=SirName+"Dn_Price";
//---- ������������� ������ ��� ������� ����������  
   ArrayResize(Vol,Length);

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(0,Up1Buffer,INDICATOR_DATA);

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(1,Dn1Buffer,INDICATOR_DATA);
   
//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(2,Up2Buffer,INDICATOR_DATA);

//---- ����������� ������������� ������� � ������������ �����
   SetIndexBuffer(3,Dn2Buffer,INDICATOR_DATA);
   
//---- ����������� ������������� ������� IndBuffer � ������������ �����
   SetIndexBuffer(4,UpIndBuffer,INDICATOR_DATA);

//---- ����������� ������������� ������� IndBuffer � ������������ �����
   SetIndexBuffer(5,DnIndBuffer,INDICATOR_DATA);

//---- ����������� ������������� ������� � ��������, ��������� �����   
   SetIndexBuffer(6,ColorIndBuffer,INDICATOR_COLOR_INDEX);

   
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
   StringConcatenate(shortname,"Volume_Weighted_MA_Digit_System(",Shift,")");
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//----
  }
//+------------------------------------------------------------------+
//| Custom indicator deinitialization function                       |
//+------------------------------------------------------------------+    
void OnDeinit(const int reason)
  {
//----
   ObjectDelete(0,Up_Price_name);
   ObjectDelete(0,Dn_Price_name);
//----
   ChartRedraw(0);
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

//---- ���������� ��������� ���������� 
   int first,bar;
   //---- ���������� ���������� � ��������� ������  
   double mov,sum;

//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) // �������� �� ������ ����� ������� ����������
     {
      first=min_rates_total-1; // ��������� ����� ��� ������� ���� �����
     }
   else first=prev_calculated-1; // ��������� ����� ��� ������� ����� �����

//---- �������� ���� ������� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      //---- ������ ������� ������� ������
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
      for(int kkk=int(bar-Length+1); kkk<=bar; kkk++) mov+=high[kkk]*Vol[bar-kkk];
      mov+=dPriceShift;
      Up1Buffer[bar]=Up2Buffer[bar]=PointPow10*MathRound(mov/PointPow10);
      //---- ������ ������ ������� ������
      mov=0.0;
      for(int kkk=int(bar-Length+1); kkk<=bar; kkk++) mov+=low[kkk]*Vol[bar-kkk];
      mov+=dPriceShift;
      Dn1Buffer[bar]=Dn2Buffer[bar]=PointPow10*MathRound(mov/PointPow10);
     }


//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0) first=min_rates_total;     
//---- �������� ���� ��������� ����� ����������
   for(bar=first; bar<rates_total && !IsStopped(); bar++)
     {
      int clr=2;
      UpIndBuffer[bar]=0.0;
      DnIndBuffer[bar]=0.0;
      
      if(close[bar]>Up1Buffer[bar-Shift])
        {
         if(open[bar]<close[bar]) clr=4;
         else clr=3;
         UpIndBuffer[bar]=high[bar];
         DnIndBuffer[bar]=low[bar];
        }

      if(close[bar]<Dn1Buffer[bar-Shift])
        {
         if(open[bar]>close[bar]) clr=0;
         else clr=1;
         UpIndBuffer[bar]=high[bar];
         DnIndBuffer[bar]=low[bar];
        }

      ColorIndBuffer[bar]=clr;
     }
   if(ShowPrice)
     {
      int bar0=rates_total-1;
      datetime time0=time[bar0]+Shift*PeriodSeconds();
      SetRightPrice(0,Up_Price_name,0,time0,Up1Buffer[bar0-Shift],Up_Price_color);
      SetRightPrice(0,Dn_Price_name,0,time0,Dn1Buffer[bar0-Shift],Dn_Price_color);
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
