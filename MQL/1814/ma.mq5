//+------------------------------------------------------------------+
//|                                                           MA.mq5 |
//|                        Copyright 2013, MetaQuotes Software Corp. |
//|                                              http://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2013, MetaQuotes Software Corp."
#property link      "http://www.mql5.com"
#property version   "1.00"
//--- input parameters
input int      StopLoss=100;
input int      TakeProfit=100;
input int      MA_Period=57;
input int      MA_Period1=3;
input int      EA_Magic=12345;
input double   Lot=1.0;

//--- ���������� ����������
int ma1Handle;   // ����� ����������  Moving Average
int maHandle;    // ����� ���������� Moving Average
double ma1Val[]; // ������������ ������� ��� �������� ��������� �������� Moving Average ��� ������� ����
double maVal[];  // ������������ ������ ��� �������� �������� ���������� Moving Average ��� ������� ����
double p_close;  // ���������� ��� �������� �������� close ����
int STP,TKP;     // ����� ������������ ��� �������� Stop Loss � Take Profit
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- �������� ����� ���������� MA
   ma1Handle=iMA(_Symbol,_Period,MA_Period1,0,MODE_LWMA,PRICE_CLOSE);

//---�������� ����� ���������� Moving Average
   maHandle=iMA(_Symbol,_Period,MA_Period,0,MODE_EMA,PRICE_CLOSE);
//--- ����� ���������, �� ���� �� ���������� �������� Invalid Handle
   if(ma1Handle<0 || maHandle<0)
     {
      Alert("������ ��� �������� ����������� - ����� ������: ",GetLastError(),"!!");
      return(-1);
     }

//--- ��� ������ � ���������, ������������� 5-�� ������� ���������,
//--- �������� �� 10 �������� SL � TP
   STP = StopLoss;
   TKP = TakeProfit;
   if(_Digits==5 || _Digits==3)
     {
      STP = STP*10;
      TKP = TKP*10;
     }
   return(0);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//--- ����������� ������ �����������
   IndicatorRelease(ma1Handle);

   IndicatorRelease(maHandle);
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//--- ���������� �� ���������� ����� ��� ������
   if(Bars(_Symbol,_Period)<60) // ����� ���������� ����� �� ������� ������ 60?
     {
      Alert("�� ������� ������ 60 �����, �������� �� ����� ��������!!");
      return;
     }

//--- ��� ���������� �������� ������� ���� �� ���������� static-���������� Old_Time.
//--- ��� ������ ���������� ������� OnTick �� ����� ���������� ����� �������� ���� � ����������� ��������.
//--- ���� ��� �� �����, ��� ��������, ��� ����� �������� ����� ���.

   static datetime Old_Time;
   datetime New_Time[1];
   bool IsNewBar=false;

//--- �������� ����� �������� ���� � ������� New_Time[0]
   int copied=CopyTime(_Symbol,_Period,0,1,New_Time);
   if(copied>0) // ok, ������� �����������
     {
      if(Old_Time!=New_Time[0]) // ���� ������ ����� �� �����
        {
         IsNewBar=true;   // ����� ���
         if(MQL5InfoInteger(MQL5_DEBUGGING)) Print("����� ���",New_Time[0],"������ ���",Old_Time);
         Old_Time=New_Time[0];   // ��������� ����� ����
        }
     }
   else
     {
      Alert("������ ����������� �������, ����� ������ =",GetLastError());
      ResetLastError();
      return;
     }

//--- �������� ������ ��������� ������� ���������� ����� �������� �������� ������ ��� ����� ����
   if(IsNewBar==false)
     {
      return;
     }

//--- ����� �� �� ����������� ���������� ����� �� ������� ��� ������
   int Mybars=Bars(_Symbol,_Period);
   if(Mybars<60) // ���� ����� ���������� ����� ������ 60
     {
      Alert("�� ������� ����� 60 �����, �������� �������� �� �����!!");
      return;
     }

//--- ��������� ���������, ������� ����� �������������� ��� ��������
   MqlTick latest_price;       // ����� �������������� ��� ������� ���������
   MqlTradeRequest mrequest;   // ����� �������������� ��� ������� �������� ��������
   MqlTradeResult mresult;     // ����� �������������� ��� ��������� ����������� ���������� �������� ��������
   MqlRates mrate[];           // ����� ��������� ����, ������ � ����� ��� ������� ����
   ZeroMemory(mrequest);
/*
     ��������� ���������� � �������� ��������� � ����������� 
     ��� � ����������
*/
//--- ������ ���������
   ArraySetAsSeries(mrate,true);

//--- ������ �������� ���������� MA
   ArraySetAsSeries(ma1Val,true);

//--- ������ �������� ���������� MA-8
   ArraySetAsSeries(maVal,true);

//--- �������� ������� �������� ��������� � ��������� ���� MqlTick
   if(!SymbolInfoTick(_Symbol,latest_price))
     {
      Alert("������ ��������� ��������� ��������� - ������:",GetLastError(),"!!");
      return;
     }

//--- �������� ������������ ������ ��������� 3-� �����
   if(CopyRates(_Symbol,_Period,0,3,mrate)<0)
     {
      Alert("������ ����������� ������������ ������ - ������:",GetLastError(),"!!");
      return;
     }

//--- �������� �������� ����������� �� ������������ �������
   if(CopyBuffer(ma1Handle,0,0,3,ma1Val)<0)
     {
      Alert("������ ����������� ������� ���������� Moving Average - ����� ������:",GetLastError(),"!!");
      return;
     }
   if(CopyBuffer(maHandle,0,0,3,maVal)<0)
     {
      Alert("������ ����������� ������� ���������� Moving Average - ����� ������:",GetLastError());
      return;
     }
//--- ���� �� �������� �������?
   bool Buy_opened=false;  // ����������, � ������� ����� ��������� ���������� 
   bool Sell_opened=false; // � ������� ��������������� �������� �������

   if(PositionSelect(_Symbol)==true) // ���� �������� �������
     {
      if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_BUY)
        {
         Buy_opened=true;  //��� ������� �������
        }
      else if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_SELL)
        {
         Sell_opened=true; // ��� �������� �������
        }
     }

//--- ��������� ������� ���� �������� ����������� ���� (��� ��� 1)
   p_close=mrate[1].close;  // ���� �������� ����������� ����

/*
    1. �������� ������� ��� ������� : MA-8 ������, 
    ���������� ���� �������� ���� ������ MA-8, 
*/

//--- ��������� ���������� ���� boolean, ��� ����� �������������� ��� �������� ������� ��� �������
   bool Buy_Condition_1=(maVal[0]>maVal[1]) && (maVal[1]>maVal[2]); // MA-8 ������
   bool Buy_Condition_2=(ma1Val[0]>ma1Val[1]) &&(ma1Val[1]>ma1Val[2]);
   bool Buy_Condition_3=(p_close>maVal[1]);         // ���������� ���� �������� ���� ���������� ������� MA-8
   bool Buy_Condition_4 =(maVal[0]>ma1Val[0]);

//--- �������� ��� ������
   if(Buy_Condition_1 && Buy_Condition_2)
     {
      if(Buy_Condition_3 && Buy_Condition_4)
        {
         // ���� �� � ������ ������ �������� ������� �� �������?
         if(Buy_opened)
           {
            Alert("��� ���� ������� �� �������!!!");
            return;    // �� ��������� � �������� ������� �� �������
           }
         mrequest.action = TRADE_ACTION_DEAL;                                  // ����������� ����������
         mrequest.price = NormalizeDouble(latest_price.ask,_Digits);           // ��������� ���� ask
         mrequest.sl = NormalizeDouble(latest_price.ask - STP*_Point,_Digits); // Stop Loss
         mrequest.tp = NormalizeDouble(latest_price.ask + TKP*_Point,_Digits); // Take Profit
         mrequest.symbol = _Symbol;                                            // ������
         mrequest.volume = Lot;                                                // ���������� ����� ��� ��������
         mrequest.magic = EA_Magic;                                            // Magic Number
         mrequest.type = ORDER_TYPE_BUY;                                       // ����� �� �������
         mrequest.type_filling = ORDER_FILLING_FOK;                            // ��� ���������� ������ - ��� ��� ������
         mrequest.deviation=100;                                               // ��������������� �� ������� ����
         //--- �������� �����
         if(OrderSend(mrequest,mresult))
         // ����������� ��� �������� ��������� �������
         if(mresult.retcode==10009 || mresult.retcode==10008) //������ �������� ��� ����� ������� �������
           {
            Alert("����� Buy ������� �������, ����� ������ #:",mresult.order,"!!");
           }
         else
           {
            Alert("������ �� ��������� ������ Buy �� �������� - ��� ������:",GetLastError());
            return;
           }
        }
     }
/*
    2. �������� ������� ��� ������� : MA-8 ������, 
    ���������� ���� �������� ���� ������ MA-8
*/

//--- ��������� ���������� ���� boolean, ��� ����� �������������� ��� �������� ������� ��� �������
   bool Sell_Condition_1 = (maVal[0]<maVal[1]) && (maVal[1]<maVal[2]);  // MA-8 ������
   bool Sell_Condition_2 = (p_close <maVal[1]);                         // ���������� ���� �������� ���� MA-8
   bool Sell_Condition_3=(ma1Val[0]<ma1Val[1]) && (ma1Val[1]<ma1Val[2]);                         // ������� �������� ADX value ������ ��������� (22)
   bool Sell_Condition_4=(maVal[0]<ma1Val[0]);                         // -DI ������, ��� +DI

//--- �������� ��� ������
   if(Sell_Condition_1 && Sell_Condition_2)
     {
      if(Sell_Condition_3 && Sell_Condition_4)
        {
         // ���� �� � ������ ������ �������� ������� �� �������?
         if(Sell_opened)
           {
            Alert("��� ���� ������� �� �������!!!");
            return;    // �� ��������� � �������� ������� �� �������
           }
         mrequest.action = TRADE_ACTION_DEAL;                                  // ����������� ����������
         mrequest.price = NormalizeDouble(latest_price.bid,_Digits);           // ��������� ���� Bid
         mrequest.sl = NormalizeDouble(latest_price.bid + STP*_Point,_Digits); // Stop Loss
         mrequest.tp = NormalizeDouble(latest_price.bid - TKP*_Point,_Digits); // Take Profit
         mrequest.symbol = _Symbol;                                            // ������
         mrequest.volume = Lot;                                                // ���������� ����� ��� ��������
         mrequest.magic = EA_Magic;                                            // Magic Number
         mrequest.type= ORDER_TYPE_SELL;                                       // ����� �� �������
         mrequest.type_filling = ORDER_FILLING_FOK;                            // ��� ���������� ������ - ��� ��� ������
         mrequest.deviation=100;                                               // ��������������� �� ������� ����
         //--- �������� �����
         if(OrderSend(mrequest,mresult))
         // ����������� ��� �������� ��������� �������
         if(mresult.retcode==10009 || mresult.retcode==10008) //Request is completed or order placed
           {
            Alert("����� Sell ������� �������, ����� ������ #:",mresult.order,"!!");
           }
         else
           {
            Alert("������ �� ��������� ������ Sell �� �������� - ��� ������:",GetLastError());
            return;
           }
        }
     }
   return;
  }
//+------------------------------------------------------------------+
