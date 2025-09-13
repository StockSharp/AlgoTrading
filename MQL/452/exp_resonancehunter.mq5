//+------------------------------------------------------------------+ 
//|                   �������������� ������� Exp_ResonanceHunter.mq5 | 
//|                               Copyright � 2010, Nikolay Kositsin | 
//|                                Khabarovsk, farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright � 2010, Nikolay Kositsin"
#property link "farria@mail.redcom.ru" 
//----  ����� ������ ����������
#property version   "1.00"
//+-----------------------------------+
//|  ������� ��������� ��������       |
//+-----------------------------------+
input  bool Trade0=true;                    // ���������� ��������
input int Kperiod0 = 5;                     // K-������ (���������� ����� ��� ��������)
input int Dperiod0 = 3;                     // D-������ (������ ���������� �����������)
input int slowing0 = 3;                     // ������������� �����������
input ENUM_MA_METHOD ma_method0 = MODE_SMA; // ��� �����������
input ENUM_STO_PRICE price_0 = STO_LOWHIGH; // ������ ������� ����������
input string SymbolA0 = "EURUSD";
input string SymbolB0 = "EURJPY";
input string SymbolC0 = "USDJPY";
input int StopLoss0 = 500;
input double Lots0  = 0.1;
input int Slippage0 = 30;
//+-----------------------------------+
input  bool Trade1=true;                    // ���������� ��������
input int Kperiod1 = 5;                     // K-������ (���������� ����� ��� ��������)
input int Dperiod1 = 3;                     // D-������ (������ ���������� �����������)
input int slowing1 = 3;                     // ������������� �����������
input ENUM_MA_METHOD ma_method1 = MODE_SMA; // ��� �����������
input ENUM_STO_PRICE price_1 = STO_LOWHIGH; // ������ ������� ����������
input string SymbolA1 = "GBPUSD";
input string SymbolB1 = "GBPJPY";
input string SymbolC1 = "USDJPY";
input int StopLoss1 = 500;
input double Lots1  = 0.1;
input int Slippage1 = 30;
//+-----------------------------------+
input  bool Trade2=true;                    // ���������� ��������
input int Kperiod2 = 5;                     // K-������ (���������� ����� ��� ��������)
input int Dperiod2 = 3;                     // D-������ (������ ���������� �����������)
input int slowing2 = 3;                     // ������������� �����������
input ENUM_MA_METHOD ma_method2 = MODE_SMA; // ��� �����������
input ENUM_STO_PRICE price_2 = STO_LOWHIGH; // ������ ������� ����������
input string SymbolA2 = "AUDUSD";
input string SymbolB2 = "AUDJPY";
input string SymbolC2 = "USDJPY";
input int StopLoss2 = 500;
input double Lots2  = 0.1;
input int Slippage2 = 30;
//+------------------------------------------------------------------+
//| Custom TradeSignalCounter() function                             |
//+------------------------------------------------------------------+
bool TradeSignalCounter(int Number,
                        bool Trade,
                        int Kperiod,
                        int Dperiod,
                        int slowing,
                        ENUM_MA_METHOD ma_method,
                        ENUM_STO_PRICE price_,
                        string SymbolA,
                        string SymbolB,
                        string SymbolC,
                        bool &UpSignal[],
                        bool &DnSignal[],
                        bool &UpStop[],
                        bool &DnStop[])
  {
//----  �������� ������� �� ��������
   if(!Trade)return(true);
//----  ��������� ���������� ��� �������� �������� �������� ����������
   static int Size_=0;
//----  ��������� �������� ��� �������� ������� ����������� ��� ����������� ����������
   static int Handle[];
   static int Recount[],MinBars[];
//---- 
   double dUpSignal_[1],dDnSignal_[1],dUpStop_[1],dDnStop_[1];

//----  ��������� �������� �������� ����������
   if(Number+1>Size_)
     {
      uint size=Number+1;
      //---- 
      if(ArrayResize(Handle,size)==-1
         || ArrayResize(Recount,size)==-1
         || ArrayResize(UpSignal,size)==-1
         || ArrayResize(DnSignal,size)== -1
         || ArrayResize(UpStop,size)== -1
         || ArrayResize(DnStop,size)==-1
         || ArrayResize(MinBars,size)==-1)
        {
         string word="";
         StringConcatenate(word,"TradeSignalCounter( ",Number,
                           " ): ������!!! �� ������� �������� ������� �������� ����������!!!");
         int error=GetLastError();
         ResetLastError();
         //---- 
         if(error>4000)
           {
            StringConcatenate(word,"TradeSignalCounter( ",Number," ): ��� ������ ",error);
            Print(word);
           }
         Size_=-2;
         return(false);
        }
      else
        {
         ArrayInitialize(Handle,0);
         ArrayInitialize(Recount,0);
         ArrayInitialize(UpSignal,0);
         ArrayInitialize(DnSignal,0);
         ArrayInitialize(UpStop,0);
         ArrayInitialize(DnStop,0);
         ArrayInitialize(MinBars,0);
        }
      Size_=(int)size;
      Recount[Number] = false;
      MinBars[Number] = Kperiod + Dperiod + slowing;

      //---- ��������� ������ ����������
      Handle[Number]=iCustom(SymbolA,0,"MultiStochastic_Exp",
                             Kperiod,Dperiod,slowing,ma_method,price_,
                             SymbolA,SymbolB,SymbolC);
     }

//----  �������� ���������� ����� �� ������������� ��� ������� 
   if(Rates_Total(SymbolA,SymbolB,SymbolC)<MinBars[Number])return(true);

//----  �������� ������������� ���������
   if(!SynchroCheck(SymbolA,SymbolB,SymbolC))return(true);

//----  ��������� �������� �������� 
   if(IsNewBar(Number,SymbolA,0) || Recount[Number])
     {
      DnSignal[Number] = false;
      UpSignal[Number] = false;
      DnStop  [Number] = false;
      UpStop  [Number] = false;

      //---- ��������� ������ �����������, �������� �������� ������������ �������
      //---- � ���������� �������������� ��� ����� ����������� �������
      if(CopyBuffer(Handle[Number], 1, 1, 1, dDnSignal_) < 0){Recount[Number] = true; return(false);}
      if(CopyBuffer(Handle[Number], 2, 1, 1, dUpSignal_) < 0){Recount[Number] = true; return(false);}
      if(CopyBuffer(Handle[Number], 3, 1, 1, dDnStop_  ) < 0){Recount[Number] = true; return(false);}
      if(CopyBuffer(Handle[Number], 4, 1, 1, dUpStop_  ) < 0){Recount[Number] = true; return(false);}

      //---- ������������ ���������� �������� � �������� ���������� ���������� �������� ������
      if(dDnSignal_[0] == 300)DnSignal[Number] = true;
      if(dUpSignal_[0] == 300)UpSignal[Number] = true;
      if(dDnStop_  [0] == 300)DnStop  [Number] = true;
      if(dUpStop_  [0] == 300)UpStop  [Number] = true;

      //---- ��� �������� ����������� �� ������������ ������� ��������� �������
      //---- ����� �� ������������ � ���� ���� �� ��������� ����� ����
      Recount[Number]=false;
     }
//---- 
   return(true);
  }
//+------------------------------------------------------------------+
//| Custom TradePerformer() function                                 |
//+------------------------------------------------------------------+
bool TradePerformer(int    Number,
                    string Symbol_,
                    bool   Trade,
                    int    StLoss,
                    int    TkProfit,
                    double Lots,
                    int    Slippage,
                    bool  &UpSignal[],
                    bool  &DnSignal[],
                    bool  &UpStop[],
                    bool  &DnStop[])
  {
//---- �������� ������� �� ��������
   if(!Trade)return(true);

//---- ��������� �������� ������� 
   if(UpStop[Number])BuyPositionClose(Symbol_,Slippage);
   if(DnStop[Number])SellPositionClose(Symbol_,Slippage);

//---- ��������� ����� �������
   if(UpSignal[Number])
      if(BuyPositionOpen(Symbol_,Slippage,Lots,StLoss,TkProfit))
         UpSignal[Number]=false; // �� ���� ���� ������ ����� ��������� ������� �� �����!
//----   
   if(DnSignal[Number])
      if(SellPositionOpen(Symbol_,Slippage,Lots,StLoss,TkProfit))
         DnSignal[Number]=false; // �� ���� ���� ������ ����� ��������� ������� �� �����!
//---- 
   return(true);
  }
//+------------------------------------------------------------------+
//| Open buy position.                                               |
//| INPUT:  symbol    -symbol for fish,                              |
//|         deviation -deviation for price close.                    |
//| OUTPUT: true-if successful, false otherwise.                     |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool BuyPositionOpen(const string symbol,
                     ulong deviation,
                     double volume,
                     int StopLoss,
                     int Takeprofit)
  {
//---- ���������� �������� ��������� ������� � ���������� ��������� �������
   MqlTradeRequest request;
   MqlTradeResult result;
   
//---- ��������� ��������
   ZeroMemory(request);
   ZeroMemory(result);

//---- �������� �� ������� �������� ������� �� ������ ��� ������
   if(PositionSelect(symbol))return(false);

   int digit=(int)SymbolInfoInteger(symbol,SYMBOL_DIGITS);
   int stoplevel= (int)SymbolInfoInteger(symbol,SYMBOL_TRADE_STOPS_LEVEL);
   double point = SymbolInfoDouble(symbol,SYMBOL_POINT);

//---- ����������� ���������� �� ��������� � �������� �������� �������
   if(StopLoss<stoplevel && StopLoss>0)StopLoss=stoplevel;
   double dStopLoss=StopLoss*point;

//---- ����������� ���������� �� ����������� �������� �������� �������
   if(Takeprofit<stoplevel && Takeprofit>0)Takeprofit=stoplevel;
   double dTakeprofit=Takeprofit*point;

//---- ������������� ��������� ��������� ������� MqlTradeRequest ��� ���������� BUY �������
   request.type   = ORDER_TYPE_BUY;
   request.price  = SymbolInfoDouble(symbol, SYMBOL_ASK);
   request.action = TRADE_ACTION_DEAL;
   request.symbol = symbol;
   request.volume = volume;
//---
   if(StopLoss   != 0) request.sl = NormalizeDouble(request.price - dStopLoss,   digit); else request.sl = 0.0;
   if(Takeprofit != 0) request.tp = NormalizeDouble(request.price + dTakeprofit, digit); else request.tp = 0.0;
//---
   request.deviation=(deviation==ULONG_MAX) ? deviation : deviation;
   request.type_filling=ORDER_FILLING_FOK;
//---
   string word="";
   StringConcatenate(word,"<<< ============ BuyPositionOpen():   ��������� Buy ������� �� ",symbol," ============ >>>");
   Print(word);

//---- ��������� BUY ������� � ������ �������� ���������� ��������� �������
   if(!OrderSend(request,result) || result.deal==0)
     {
      Print(ResultRetcodeDescription(result.retcode));
      return(false);
     }
//---- 
   return(true);
  }
//+------------------------------------------------------------------+
//| Open sell position.                                              |
//| INPUT:  symbol    -symbol for fish,                              |
//|         deviation -deviation for price close.                    |
//| OUTPUT: true-if successful, false otherwise.                     |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool SellPositionOpen(const string symbol,
                      ulong deviation,
                      double volume,
                      int StopLoss,
                      int Takeprofit)
  {
//---- ���������� �������� ��������� ������� � ���������� ��������� �������
   MqlTradeRequest request;
   MqlTradeResult result;
   
//---- ��������� ��������
   ZeroMemory(request);
   ZeroMemory(result);

//---- �������� �� ������� �������� ������� �� ������ ��� ������
   if(PositionSelect(symbol))return(false);

   int digit=(int)SymbolInfoInteger(symbol,SYMBOL_DIGITS);
   int stoplevel= (int)SymbolInfoInteger(symbol,SYMBOL_TRADE_STOPS_LEVEL);
   double point = SymbolInfoDouble(symbol,SYMBOL_POINT);

//---- ����������� ���������� �� ��������� � �������� �������� �������
   if(StopLoss<stoplevel && StopLoss>0)StopLoss=stoplevel;
   double dStopLoss=StopLoss*point;

//---- ����������� ���������� �� ����������� �������� �������� �������
   if(Takeprofit<stoplevel && Takeprofit>0)Takeprofit=stoplevel;
   double dTakeprofit=Takeprofit*point;

//---- ������������� ��������� ��������� ������� MqlTradeRequest ��� ���������� SELL �������
   request.type   = ORDER_TYPE_SELL;
   request.price  = SymbolInfoDouble(symbol, SYMBOL_BID);
   request.action = TRADE_ACTION_DEAL;
   request.symbol = symbol;
   request.volume = volume;
//---
   if(StopLoss   != 0) request.sl = NormalizeDouble(request.price + dStopLoss,   digit); else request.sl = 0.0;
   if(Takeprofit != 0) request.tp = NormalizeDouble(request.price - dTakeprofit, digit); else request.tp = 0.0;
//---
   request.deviation=(deviation==ULONG_MAX) ? deviation : deviation;
   request.type_filling=ORDER_FILLING_FOK;

//---
   string word="";
   StringConcatenate(word,"<<< ============ SellPositionOpen():   ��������� Sell ������� �� ",symbol," ============ >>>");
   Print(word);

//---- ��������� SELL ������� � ������ �������� ���������� ��������� �������
   if(!OrderSend(request,result) || result.deal==0)
     {
      Print(ResultRetcodeDescription(result.retcode));
      return(false);
     }
//---- 
   return(true);
  }
//+------------------------------------------------------------------+
//| Close specified opened buy position.                             |
//| INPUT:  symbol    -symbol for fish,                              |
//|         deviation -deviation for price close.                    |
//| OUTPUT: true-if successful, false otherwise.                     |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool BuyPositionClose(const string symbol,ulong deviation)
  {
//---- ���������� �������� ��������� ������� � ���������� ��������� �������
   MqlTradeRequest request;
   MqlTradeResult result;

//---- ��������� ��������
   ZeroMemory(request);
   ZeroMemory(result);

//---- �������� �� ������� �������� BUY �������
   if(PositionSelect(symbol))
     {
      if(PositionGetInteger(POSITION_TYPE)!=POSITION_TYPE_BUY) return(false);
     }
   else  return(false);

//---- ������������� ��������� ��������� ������� MqlTradeRequest ��� ���������� BUY �������
   request.type   = ORDER_TYPE_SELL;
   request.price  = SymbolInfoDouble(symbol, SYMBOL_BID);
   request.action = TRADE_ACTION_DEAL;
   request.symbol = symbol;
   request.volume = PositionGetDouble(POSITION_VOLUME);
   request.sl = 0.0;
   request.tp = 0.0;
   request.deviation=(deviation==ULONG_MAX) ? deviation : deviation;
   request.type_filling=ORDER_FILLING_FOK;
//----
   string word="";
   StringConcatenate(word,
                     "<<< ============ BuyPositionClose():   ��������� Buy ������� �� ",
                     symbol," ============ >>>");
   Print(word);

//---- �������� ������� �� ���������� ������� �� �������� ������
   if(!OrderSend(request,result))
     {
      Print(ResultRetcodeDescription(result.retcode));
      return(false);
     }
//---- 
   return(true);
  }
//+------------------------------------------------------------------+
//| Close specified sell opened position.                            |
//| INPUT:  symbol    -symbol for fish,                              |
//|         deviation -deviation for price close.                    |
//| OUTPUT: true-if successful, false otherwise.                     |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool SellPositionClose(const string symbol,ulong deviation)
  {
//---- ���������� �������� ��������� ������� � ���������� ��������� �������
   MqlTradeRequest request;
   MqlTradeResult result;
   
//---- ��������� ��������
   ZeroMemory(request);
   ZeroMemory(result);

//---- �������� �� ������� �������� BUY �������
   if(PositionSelect(symbol))
     {
      if(PositionGetInteger(POSITION_TYPE)!=POSITION_TYPE_SELL)return(false);
     }
   else return(false);

//---- ������������� ��������� ��������� ������� MqlTradeRequest ��� ���������� SELL �������
   request.type   = ORDER_TYPE_BUY;
   request.price  = SymbolInfoDouble(symbol, SYMBOL_ASK);
   request.action = TRADE_ACTION_DEAL;
   request.symbol = symbol;
   request.volume = PositionGetDouble(POSITION_VOLUME);
   request.sl = 0.0;
   request.tp = 0.0;
   request.deviation=(deviation==ULONG_MAX) ? deviation : deviation;
   request.type_filling=ORDER_FILLING_FOK;
//----
   string word="";
   StringConcatenate(word,
                     "<<< ============ SellPositionClose():   ��������� Sell ������� �� ",
                     symbol," ============ >>>");
   Print(word);

//---- �������� ������� �� ���������� ������� �� �������� ������
   if(!OrderSend(request,result))
     {
      Print(ResultRetcodeDescription(result.retcode));
      return(false);
     }
//---- 
   return(true);
  }
//+------------------------------------------------------------------+
//| Rates_Total() function                                           |
//+------------------------------------------------------------------+
int Rates_Total(string Symbol0,string Symbol1,string Symbol2)
  {
//---- 
   int Bars0 = Bars(Symbol0, 0);
   int Bars1 = Bars(Symbol1, 0);
   int Bars2 = Bars(Symbol2, 0);
//---- 
   int error=GetLastError();
   ResetLastError();
//---- 
   if(error==4401)return(0);
//---- 
   return(MathMin(Bars0,MathMin(Bars1,Bars2)));
  }
//+------------------------------------------------------------------+
//|  SynchroCheck() function                                         |
//+------------------------------------------------------------------+
bool SynchroCheck(string SymbolA_,string SymbolB_,string SymbolC_)
  {
//---- 
   datetime Time_[1],Vel0,Vel1,Vel2;
//---- 
   CopyTime(SymbolA_, 0, 0, 1, Time_); Vel0 = Time_[0];
   CopyTime(SymbolB_, 0, 0, 1, Time_); Vel1 = Time_[0];
   CopyTime(SymbolC_, 0, 0, 1, Time_); Vel2 = Time_[0];

   if(Vel0!=Vel1 || Vel1!=Vel2) return(false);
//---- 
   return(true);
  }
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//----  

//---- 
   return(0);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---- 

//----    
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---- ���������� �������� ���������� ��� �������� ��������  
   static bool UpSignal[],DnSignal[],UpStop[],DnStop[];

//---- ��������� �������� ��������
   TradeSignalCounter(0,Trade0,Kperiod0,Dperiod0,slowing0,ma_method0,price_0,SymbolA0,SymbolB0,SymbolC0,UpSignal,DnSignal,UpStop,DnStop);
   TradeSignalCounter(1,Trade1,Kperiod1,Dperiod1,slowing1,ma_method1,price_1,SymbolA1,SymbolB1,SymbolC1,UpSignal,DnSignal,UpStop,DnStop);
   TradeSignalCounter(2,Trade2,Kperiod2,Dperiod2,slowing2,ma_method2,price_2,SymbolA2,SymbolB2,SymbolC2,UpSignal,DnSignal,UpStop,DnStop);
//---- ���������� �������� ��������
   TradePerformer(0,SymbolA0,Trade0,StopLoss0,0,Lots0,Slippage0,UpSignal,DnSignal,UpStop,DnStop);
   TradePerformer(1,SymbolA1,Trade1,StopLoss1,0,Lots1,Slippage1,UpSignal,DnSignal,UpStop,DnStop);
   TradePerformer(2,SymbolA2,Trade2,StopLoss2,0,Lots2,Slippage2,UpSignal,DnSignal,UpStop,DnStop);
//----    
  }
//+------------------------------------------------------------------+
//| IsNewBar() function                                              |
//+------------------------------------------------------------------+
bool IsNewBar(int Number,string symbol,ENUM_TIMEFRAMES timeframe)
  {
//---- 
   static datetime Told[];
   datetime Tnew[1];
//---- ���������� ���������� ��� �������� �������� �������� ����������
   static int Size_=0;

//---- ��������� �������� �������� ����������
   if(Number+1>Size_)
     {
      uint size=Number+1;
      //---- 
      if(ArrayResize(Told,size)==-1)
        {
         string word="";
         StringConcatenate(word,"IsNewBar( ",Number,
                           " ): ������!!! �� ������� �������� ������� �������� ����������!!!");
         Print(word);
         //----           
         int error=GetLastError();
         ResetLastError();
         if(error>4000)
           {
            StringConcatenate(word,"IsNewBar( ",Number," ): ��� ������ ",error);
            Print(word);
           }
         //----                                                                                                                                                                                                   
         Size_=-2;
         return(false);
        }
     }

   CopyTime(symbol,timeframe,0,1,Tnew);
   if(Tnew[0]!=Told[Number])
     {
      Told[Number]=Tnew[0];
      return(true);
     }
//---- 
   return(false);
  }
//+------------------------------------------------------------------+
//| Get the retcode value as string.                                 |
//| INPUT:  no.                                                      |
//| OUTPUT: the retcode value as string.                             |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
string ResultRetcodeDescription(int retcode)
  {
   string str;
//----
   switch(retcode)
     {
      case TRADE_RETCODE_REQUOTE:
         str="Requote";
         break;
      case TRADE_RETCODE_REJECT:
         str="Request rejected";
         break;
      case TRADE_RETCODE_CANCEL:
         str="Request cancelled by trader";
         break;
      case TRADE_RETCODE_PLACED:
         str="Order placed";
         break;
      case TRADE_RETCODE_DONE:
         str="Request done";
         break;
      case TRADE_RETCODE_DONE_PARTIAL:
         str="Request done partially";
         break;
      case TRADE_RETCODE_ERROR:
         str="Common error";
         break;
      case TRADE_RETCODE_TIMEOUT:
         str="Request cancelled by timeout";
         break;
      case TRADE_RETCODE_INVALID:
         str="Invalid request";
         break;
      case TRADE_RETCODE_INVALID_VOLUME:
         str="Invalid volume in request";
         break;
      case TRADE_RETCODE_INVALID_PRICE:
         str="Invalid price in request";
         break;
      case TRADE_RETCODE_INVALID_STOPS:
         str="Invalid stop(s) request";
         break;
      case TRADE_RETCODE_TRADE_DISABLED:
         str="Trade is disabled";
         break;
      case TRADE_RETCODE_MARKET_CLOSED:
         str="Market is closed";
         break;
      case TRADE_RETCODE_NO_MONEY:
         str="No enough money";
         break;
      case TRADE_RETCODE_PRICE_CHANGED:
         str="Price changed";
         break;
      case TRADE_RETCODE_PRICE_OFF:
         str="No quotes for query processing";
         break;
      case TRADE_RETCODE_INVALID_EXPIRATION:
         str="Invalid expiration time in request";
         break;
      case TRADE_RETCODE_ORDER_CHANGED:
         str="Order state changed";
         break;
      case TRADE_RETCODE_TOO_MANY_REQUESTS:
         str="Too frequent requests";
         break;
      default:
         str="Unknown result";
     }
//----
   return(str);
  }
//+------------------------------------------------------------------+
