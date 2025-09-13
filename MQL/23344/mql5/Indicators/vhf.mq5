//+------------------------------------------------------------------+ 
//|                                                          VHF.mq5 | 
//|                             Copyright � 2010,   Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+ 
#property copyright "Copyright � 2010, Nikolay Kositsin"
#property link "farria@mail.redcom.ru"
//---- ����� ������ ����������
#property version   "1.00"
//---- ��������� ���������� � ��������� ����
#property indicator_separate_window 
//---- ���������� ������������ �������
#property indicator_buffers 1 
//---- ������������ ����� ���� ����������� ����������
#property indicator_plots   1
//+-----------------------------------+
//|  ��������� ��������� ����������   |
//+-----------------------------------+
//---- ��������� ���������� � ���� �����
#property indicator_type1   DRAW_LINE
//---- � �������� ����� ����� ���������� ����������� ���� BlueViolet
#property indicator_color1 BlueViolet
//---- ����� ���������� - ����������� ������
#property indicator_style1  STYLE_SOLID
//---- ������� ����� ���������� ����� 1
#property indicator_width1  1
//---- ����������� ����� ����� ����������
#property indicator_label1  "VHF"
//+-----------------------------------+
//|  ������� ��������� ����������     |
//+-----------------------------------+
input int N=28;  // ������ ����������
//+-----------------------------------+

//---- ���������� ������������� �������, ������� � ����������
//---- ����� ����������� � �������� ������������� ������
double ExtLineBuffer[];
//---- ���������� ������������� ���������� ������ ������� ������
int min_rates_total;
//---- ���������� ������������ ��������, ������� � ����������
//---- ����� ������������ � �������� ��������� �������
int Count[];
double Temp[];
//+------------------------------------------------------------------+
//|  �������� ������� ������ ������ �������� � �������               |
//+------------------------------------------------------------------+   
void Recount_ArrayZeroPos(int &CoArr[],// ������� �� ������ ������ �������� �������� �������� ����
                          int Size)    // ���������� ��������� � ��������� ������
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
//| VHF indicator initialization function                            | 
//+------------------------------------------------------------------+  
void OnInit()
  {
//---- ������������� ���������� ������ ������� ������
   min_rates_total=N+1;

//---- ������������� ������ ��� ������� ����������  
   ArrayResize(Count,N);
   ArrayResize(Temp,N);

//---- ������������� �������� ����������
   ArrayInitialize(Count,0);
   ArrayInitialize(Temp,0.0);

//---- ����������� ������������� ������� ExtLineBuffer[] � ������������ �����
   SetIndexBuffer(0,ExtLineBuffer,INDICATOR_DATA);
//---- ������������� ������ ������ ������� ��������� ����������
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- ��������� �������� ����������, ������� �� ����� ������ �� �������
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- ���������� ��������� � ������ ��� � ���������
   ArraySetAsSeries(ExtLineBuffer,true);

//---- ������������� ���������� ��� ��������� ����� ����������
   string shortname;
   StringConcatenate(shortname,"VHF( N = ",N,")");
//--- �������� ����� ��� ����������� � ��������� ������� � �� ����������� ���������
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);
//--- ����������� �������� ����������� �������� ����������
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits);
//---- ���������� �������������
  }
//+------------------------------------------------------------------+  
//| JJRSX iteration function                                         | 
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
   double hh,ll,a,b,res;
//---- ���������� ������������� ���������� � ��������� ��� ����������� �����
   int limit,bar,maxbar;

//---- ������ ���������� ������ first ��� ����� ��������� �����
   if(prev_calculated>rates_total || prev_calculated<=0)// �������� �� ������ ����� ������� ����������
      limit=rates_total-2;                 // ��������� ���������� ���� �����
   else limit=rates_total-prev_calculated; // ��������� ����� ��� ������� ����� �����

//---- ���������� ��������� � �������� ��� � ����������  
   ArraySetAsSeries(close,true);
   ArraySetAsSeries(high,true);
   ArraySetAsSeries(low,true);

   maxbar=rates_total-min_rates_total-1;

//---- �������� ���� ������� ����������
   for(bar=limit; bar>=0; bar--)
     {
      Temp[Count[0]]=MathAbs(close[bar]-close[bar+1]);

      if(bar<maxbar)
        {
         hh=high[ArrayMaximum(high,bar,N)];
         ll=low [ArrayMinimum(low, bar,N)];
         a = hh-ll;

         b=0.0;
         for(int kkk=0; kkk<N; kkk++) b+=Temp[Count[kkk]];

         if(b) res=a/b;
         else  res=0.0;
        }
      else res=EMPTY_VALUE;

      ExtLineBuffer[bar]=res;

      //---- �������� ������� ��������� � ��������� ������ Temp[]
      if(bar>0) Recount_ArrayZeroPos(Count,N);
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
