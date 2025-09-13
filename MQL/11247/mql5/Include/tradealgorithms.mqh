//+------------------------------------------------------------------+
//|                                              TradeAlgorithms.mqh |
//|                               Copyright � 2013, Nikolay Kositsin |
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+ 
//| �������� ��������� ��� �������� ������������ �� ������� �����!   |
//+------------------------------------------------------------------+ 
#property copyright "2013,   Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.21"
//+------------------------------------------------------------------+
//|  ������������ ��� ��������� ������� ����                         |
//+------------------------------------------------------------------+
enum MarginMode //��� ��������� ��� ���������� Margin_Mode �������� �������
  {
   FREEMARGIN=0,     //MM �� ��������� ������� �� �����
   BALANCE,          //MM �� ������� ������� �� �����
   LOSSFREEMARGIN,   //MM �� ������� �� ��������� ������� �� �����
   LOSSBALANCE,      //MM �� ������� �� ������� ������� �� �����
   LOT               //��� ��� ���������
  };
//+------------------------------------------------------------------+
//|  �������� ����������� ������� ��������� ������ ����              |
//+------------------------------------------------------------------+  
class CIsNewBar
  {
   //----
public:
   //---- ������� ����������� ������� ��������� ������ ����
   bool IsNewBar(string symbol,ENUM_TIMEFRAMES timeframe)
     {
      //---- ������� ����� ��������� �������� ����
      datetime TNew=datetime(SeriesInfoInteger(symbol,timeframe,SERIES_LASTBAR_DATE));

      if(TNew!=m_TOld && TNew) // �������� �� ��������� ������ ����
        {
         m_TOld=TNew;
         return(true); // �������� ����� ���!
        }
      //----
      return(false); // ����� ����� ���� ���!
     };

   //---- ����������� ������    
                     CIsNewBar(){m_TOld=-1;};

protected: datetime m_TOld;
   //---- 
  };
//+==================================================================+
//| ��������� ��� �������� ��������                                  |
//+==================================================================+

//+------------------------------------------------------------------+
//| ��������� ������� �������                                        |
//+------------------------------------------------------------------+
bool BuyPositionOpen
(
 bool &BUY_Signal,           // ���� ���������� �� ������
 const string symbol,        // �������� ���� ������
 const datetime &TimeLevel,  // �����, ����� �������� ����� ������������ �������� ������ ����� �������
 double Money_Management,    // MM
 int Margin_Mode,            // ������ ������� �������� ����
 uint deviation,             // �������
 int StopLoss,               // �������� � �������
 int Takeprofit              // ���������� � �������
 )
//BuyPositionOpen(BUY_Signal,symbol,TimeLevel,Money_Management,deviation,Margin_Mode,StopLoss,Takeprofit);
  {
//----
   if(!BUY_Signal) return(true);

   ENUM_POSITION_TYPE PosType=POSITION_TYPE_BUY;
//---- �������� �� ��������� ���������� ������ ��� ���������� ������ � ������� ������
   if(!TradeTimeLevelCheck(symbol,PosType,TimeLevel)) return(true);

//---- �������� �� �� ������� �������� �������
   if(PositionSelect(symbol)) return(true);

//----
   double volume=BuyLotCount(symbol,Money_Management,Margin_Mode,StopLoss,deviation);
   if(volume<=0)
     {
      Print(__FUNCTION__,"(): �������� ����� ��� ��������� ��������� �������");
      return(false);
     }

//---- ���������� �������� ��������� ������� � ���������� ��������� �������
   MqlTradeRequest request;
   MqlTradeResult result;
//---- ���������� ��������� ���������� �������� ��������� ������� 
   MqlTradeCheckResult check;

//---- ��������� ��������
   ZeroMemory(request);
   ZeroMemory(result);
   ZeroMemory(check);

   long digit;
   double point,Ask;
//----   
   if(!SymbolInfoInteger(symbol,SYMBOL_DIGITS,digit)) return(true);
   if(!SymbolInfoDouble(symbol,SYMBOL_POINT,point)) return(true);
   if(!SymbolInfoDouble(symbol,SYMBOL_ASK,Ask)) return(true);

//---- ������������� ��������� ��������� ������� MqlTradeRequest ��� ���������� BUY �������
   request.type   = ORDER_TYPE_BUY;
   request.price  = Ask;
   request.action = TRADE_ACTION_DEAL;
   request.symbol = symbol;
   request.volume = volume;

//---- ����������� ���������� �� ��������� � �������� �������� �������
   if(StopLoss)
     {
      if(!StopCorrect(symbol,StopLoss))return(false);
      double dStopLoss=StopLoss*point;
      request.sl=NormalizeDouble(request.price-dStopLoss,int(digit));
     }
   else request.sl=0.0;

//---- ����������� ���������� �� ����������� �������� �������� �������
   if(Takeprofit)
     {
      if(!StopCorrect(symbol,Takeprofit))return(false);
      double dTakeprofit=Takeprofit*point;
      request.tp=NormalizeDouble(request.price+dTakeprofit,int(digit));
     }
   else request.tp=0.0;

//----
   request.deviation=deviation;
   request.type_filling=ORDER_FILLING_FOK;

//---- �������� ��������� ������� �� ������������
   if(!OrderCheck(request,check))
     {
      Print(__FUNCTION__,"(): �������� ������ ��� ��������� ��������� �������!");
      Print(__FUNCTION__,"(): OrderCheck(): ",ResultRetcodeDescription(check.retcode));
      return(false);
     }

   string comment="";
   StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): ��������� Buy ������� �� ",symbol," ============ >>>");
   Print(comment);

//---- ��������� BUY ������� � ������ �������� ���������� ��������� �������
   if(!OrderSend(request,result) || result.retcode!=TRADE_RETCODE_DONE)
     {
      Print(__FUNCTION__,"(): ���������� ��������� ������!");
      Print(__FUNCTION__,"(): OrderSend(): ",ResultRetcodeDescription(result.retcode));
      return(false);
     }
   else
   if(result.retcode==TRADE_RETCODE_DONE)
     {
      TradeTimeLevelSet(symbol,PosType,TimeLevel);
      BUY_Signal=false;
      comment="";
      StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): Buy ������� �� ",symbol," ������� ============ >>>");
      Print(comment);
      PlaySound("ok.wav");
     }
   else
     {
      Print(__FUNCTION__,"(): ���������� ��������� ������!");
      Print(__FUNCTION__,"(): OrderSend(): ",ResultRetcodeDescription(result.retcode));
     }
//----
   return(true);
  }
//+------------------------------------------------------------------+
//| ��������� �������� �������                                       |
//+------------------------------------------------------------------+
bool SellPositionOpen
(
 bool &SELL_Signal,          // ���� ���������� �� ������
 const string symbol,        // �������� ���� ������
 const datetime &TimeLevel,  // �����, ����� �������� ����� ������������ �������� ������ ����� �������
 double Money_Management,    // MM
 int Margin_Mode,            // ������ ������� �������� ����
 uint deviation,             // �������
 int StopLoss,               // �������� � �������
 int Takeprofit              // ���������� � �������
 )
//SellPositionOpen(SELL_Signal,symbol,TimeLevel,Money_Management,deviation,Margin_Mode,StopLoss,Takeprofit);
  {
//----
   if(!SELL_Signal) return(true);

   ENUM_POSITION_TYPE PosType=POSITION_TYPE_SELL;
//---- �������� �� ��������� ���������� ������ ��� ���������� ������ � ������� ������
   if(!TradeTimeLevelCheck(symbol,PosType,TimeLevel)) return(true);

//---- �������� �� �� ������� �������� �������
   if(PositionSelect(symbol)) return(true);

//----
   double volume=SellLotCount(symbol,Money_Management,Margin_Mode,StopLoss,deviation);
   if(volume<=0)
     {
      Print(__FUNCTION__,"(): �������� ����� ��� ��������� ��������� �������");
      return(false);
     }

//---- ���������� �������� ��������� ������� � ���������� ��������� �������
   MqlTradeRequest request;
   MqlTradeResult result;
//---- ���������� ��������� ���������� �������� ��������� ������� 
   MqlTradeCheckResult check;

//---- ��������� ��������
   ZeroMemory(request);
   ZeroMemory(result);
   ZeroMemory(check);

   long digit;
   double point,Bid;
//----
   if(!SymbolInfoInteger(symbol,SYMBOL_DIGITS,digit)) return(true);
   if(!SymbolInfoDouble(symbol,SYMBOL_POINT,point)) return(true);
   if(!SymbolInfoDouble(symbol,SYMBOL_BID,Bid)) return(true);

//---- ������������� ��������� ��������� ������� MqlTradeRequest ��� ���������� SELL �������
   request.type   = ORDER_TYPE_SELL;
   request.price  = Bid;
   request.action = TRADE_ACTION_DEAL;
   request.symbol = symbol;
   request.volume = volume;

//---- ����������� ���������� �� ��������� � �������� �������� �������
   if(StopLoss!=0)
     {
      if(!StopCorrect(symbol,StopLoss))return(false);
      double dStopLoss=StopLoss*point;
      request.sl=NormalizeDouble(request.price+dStopLoss,int(digit));
     }
   else request.sl=0.0;

//---- ����������� ���������� �� ����������� �������� �������� �������
   if(Takeprofit!=0)
     {
      if(!StopCorrect(symbol,Takeprofit))return(false);
      double dTakeprofit=Takeprofit*point;
      request.tp=NormalizeDouble(request.price-dTakeprofit,int(digit));
     }
   else request.tp=0.0;
//----
   request.deviation=deviation;
   request.type_filling=ORDER_FILLING_FOK;

//---- �������� ��������� ������� �� ������������
   if(!OrderCheck(request,check))
     {
      Print(__FUNCTION__,"(): �������� ������ ��� ��������� ��������� �������!");
      Print(__FUNCTION__,"(): OrderCheck(): ",ResultRetcodeDescription(check.retcode));
      return(false);
     }

   string comment="";
   StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): ��������� Sell ������� �� ",symbol," ============ >>>");
   Print(comment);

//---- ��������� SELL ������� � ������ �������� ���������� ��������� �������
   if(!OrderSend(request,result) || result.retcode!=TRADE_RETCODE_DONE)
     {
      Print(__FUNCTION__,"(): ���������� ��������� ������!");
      Print(__FUNCTION__,"(): OrderSend(): ",ResultRetcodeDescription(result.retcode));
      return(false);
     }
   else
   if(result.retcode==TRADE_RETCODE_DONE)
     {
      TradeTimeLevelSet(symbol,PosType,TimeLevel);
      SELL_Signal=false;
      comment="";
      StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): Sell ������� �� ",symbol," ������� ============ >>>");
      Print(comment);
      PlaySound("ok.wav");
     }
   else
     {
      Print(__FUNCTION__,"(): ���������� ��������� ������!");
      Print(__FUNCTION__,"(): OrderSend(): ",ResultRetcodeDescription(result.retcode));
     }
//----
   return(true);
  }
//+------------------------------------------------------------------+
//| ��������� ������� �������                                        |
//+------------------------------------------------------------------+
bool BuyPositionOpen
(
 bool &BUY_Signal,           // ���� ���������� �� ������
 const string symbol,        // �������� ���� ������
 const datetime &TimeLevel,  // �����, ����� �������� ����� ������������ �������� ������ ����� �������
 double Money_Management,    // MM
 int Margin_Mode,            // ������ ������� �������� ����
 uint deviation,             // �������
 double dStopLoss,           // �������� � �������� �������� �������
 double dTakeprofit          // ���������� � �������� �������� �������
 )
//BuyPositionOpen(BUY_Signal,symbol,TimeLevel,Money_Management,deviation,Margin_Mode,StopLoss,Takeprofit);
  {
//----
   if(!BUY_Signal) return(true);

   ENUM_POSITION_TYPE PosType=POSITION_TYPE_BUY;
//---- �������� �� ��������� ���������� ������ ��� ���������� ������ � ������� ������
   if(!TradeTimeLevelCheck(symbol,PosType,TimeLevel)) return(true);

//---- �������� �� �� ������� �������� �������
   if(PositionSelect(symbol)) return(true);

//---- ���������� �������� ��������� ������� � ���������� ��������� �������
   MqlTradeRequest request;
   MqlTradeResult result;
//---- ���������� ��������� ���������� �������� ��������� ������� 
   MqlTradeCheckResult check;

//---- ��������� ��������
   ZeroMemory(request);
   ZeroMemory(result);
   ZeroMemory(check);

   long digit;
   double point,Ask;
//----
   if(!SymbolInfoInteger(symbol,SYMBOL_DIGITS,digit)) return(true);
   if(!SymbolInfoDouble(symbol,SYMBOL_POINT,point)) return(true);
   if(!SymbolInfoDouble(symbol,SYMBOL_ASK,Ask)) return(true);

//---- ��������� ���������� �� ��������� � ����������� � �������� �������� �������
   if(!dStopCorrect(symbol,dStopLoss,dTakeprofit,PosType)) return(false);
   int StopLoss=int((Ask-dStopLoss)/point);
//----
   double volume=BuyLotCount(symbol,Money_Management,Margin_Mode,StopLoss,deviation);
   if(volume<=0)
     {
      Print(__FUNCTION__,"(): �������� ����� ��� ��������� ��������� �������");
      return(false);
     }

//---- ������������� ��������� ��������� ������� MqlTradeRequest ��� ���������� BUY �������
   request.type   = ORDER_TYPE_BUY;
   request.price  = Ask;
   request.action = TRADE_ACTION_DEAL;
   request.symbol = symbol;
   request.volume = volume;
   request.sl=dStopLoss;
   request.tp=dTakeprofit;
   request.deviation=deviation;
   request.type_filling=ORDER_FILLING_FOK;

//---- �������� ��������� ������� �� ������������
   if(!OrderCheck(request,check))
     {
      Print(__FUNCTION__,"(): �������� ������ ��� ��������� ��������� �������!");
      Print(__FUNCTION__,"(): OrderCheck(): ",ResultRetcodeDescription(check.retcode));
      return(false);
     }

   string comment="";
   StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): ��������� Buy ������� �� ",symbol," ============ >>>");
   Print(comment);

//---- ��������� BUY ������� � ������ �������� ���������� ��������� �������
   if(!OrderSend(request,result) || result.retcode!=TRADE_RETCODE_DONE)
     {
      Print(__FUNCTION__,"(): ���������� ��������� ������!");
      Print(__FUNCTION__,"(): OrderSend(): ",ResultRetcodeDescription(result.retcode));
      return(false);
     }
   else
   if(result.retcode==TRADE_RETCODE_DONE)
     {
      TradeTimeLevelSet(symbol,PosType,TimeLevel);
      BUY_Signal=false;
      comment="";
      StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): Buy ������� �� ",symbol," ������� ============ >>>");
      Print(comment);
      PlaySound("ok.wav");
     }
   else
     {
      Print(__FUNCTION__,"(): ���������� ��������� ������!");
      Print(__FUNCTION__,"(): OrderSend(): ",ResultRetcodeDescription(result.retcode));
     }
//----
   return(true);
  }
//+------------------------------------------------------------------+
//| ��������� �������� �������                                       |
//+------------------------------------------------------------------+
bool SellPositionOpen
(
 bool &SELL_Signal,          // ���� ���������� �� ������
 const string symbol,        // �������� ���� ������
 const datetime &TimeLevel,  // �����, ����� �������� ����� ������������ �������� ������ ����� �������
 double Money_Management,    // MM
 int Margin_Mode,            // ������ ������� �������� ����
 uint deviation,             // �������
 double dStopLoss,           // �������� � �������� �������� �������
 double dTakeprofit          // ���������� � �������� �������� �������
 )
//SellPositionOpen(SELL_Signal,symbol,TimeLevel,Money_Management,deviation,Margin_Mode,StopLoss,Takeprofit);
  {
//----
   if(!SELL_Signal) return(true);

   ENUM_POSITION_TYPE PosType=POSITION_TYPE_SELL;
//---- �������� �� ��������� ���������� ������ ��� ���������� ������ � ������� ������
   if(!TradeTimeLevelCheck(symbol,PosType,TimeLevel)) return(true);

//---- �������� �� �� ������� �������� �������
   if(PositionSelect(symbol)) return(true);

//---- ���������� �������� ��������� ������� � ���������� ��������� �������
   MqlTradeRequest request;
   MqlTradeResult result;
//---- ���������� ��������� ���������� �������� ��������� ������� 
   MqlTradeCheckResult check;

//---- ��������� ��������
   ZeroMemory(request);
   ZeroMemory(result);
   ZeroMemory(check);

   long digit;
   double point,Bid;
//----
   if(!SymbolInfoInteger(symbol,SYMBOL_DIGITS,digit)) return(true);
   if(!SymbolInfoDouble(symbol,SYMBOL_POINT,point)) return(true);
   if(!SymbolInfoDouble(symbol,SYMBOL_BID,Bid)) return(true);

//---- ��������� ���������� �� ��������� � ����������� � �������� �������� �������
   if(!dStopCorrect(symbol,dStopLoss,dTakeprofit,PosType)) return(false);
   int StopLoss=int((dStopLoss-Bid)/point);
//----
   double volume=SellLotCount(symbol,Money_Management,Margin_Mode,StopLoss,deviation);
   if(volume<=0)
     {
      Print(__FUNCTION__,"(): �������� ����� ��� ��������� ��������� �������");
      return(false);
     }

//---- ������������� ��������� ��������� ������� MqlTradeRequest ��� ���������� SELL �������
   request.type   = ORDER_TYPE_SELL;
   request.price  = Bid;
   request.action = TRADE_ACTION_DEAL;
   request.symbol = symbol;
   request.volume = volume;
   request.deviation=deviation;
   request.type_filling=ORDER_FILLING_FOK;

//---- �������� ��������� ������� �� ������������
   if(!OrderCheck(request,check))
     {
      Print(__FUNCTION__,"(): OrderCheck(): �������� ������ ��� ��������� ��������� �������!");
      Print(__FUNCTION__,"(): OrderCheck(): ",ResultRetcodeDescription(check.retcode));
      return(false);
     }

   string comment="";
   StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): ��������� Sell ������� �� ",symbol," ============ >>>");
   Print(comment);

//---- ��������� SELL ������� � ������ �������� ���������� ��������� �������
   if(!OrderSend(request,result) || result.retcode!=TRADE_RETCODE_DONE)
     {
      Print(__FUNCTION__,"(): OrderSend(): ���������� ��������� ������!");
      Print(__FUNCTION__,"(): OrderSend(): ",ResultRetcodeDescription(result.retcode));
      return(false);
     }
   else
   if(result.retcode==TRADE_RETCODE_DONE)
     {
      TradeTimeLevelSet(symbol,PosType,TimeLevel);
      SELL_Signal=false;
      comment="";
      StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): Sell ������� �� ",symbol," ������� ============ >>>");
      Print(comment);
      PlaySound("ok.wav");
     }
   else
     {
      Print(__FUNCTION__,"(): OrderSend(): ���������� ��������� ������!");
      Print(__FUNCTION__,"(): OrderSend(): ",ResultRetcodeDescription(result.retcode));
     }
//----
   return(true);
  }
//+------------------------------------------------------------------+
//| ��������� ������� �������                                        |
//+------------------------------------------------------------------+
bool BuyPositionClose
(
 bool &Signal,         // ���� ���������� �� ������
 const string symbol,  // �������� ���� ������
 uint deviation        // �������
 )
  {
//----
   if(!Signal) return(true);

//---- ���������� �������� ��������� ������� � ���������� ��������� �������
   MqlTradeRequest request;
   MqlTradeResult result;
//---- ���������� ��������� ���������� �������� ��������� ������� 
   MqlTradeCheckResult check;

//---- ��������� ��������
   ZeroMemory(request);
   ZeroMemory(result);
   ZeroMemory(check);

//---- �������� �� ������� �������� BUY �������
   if(PositionSelect(symbol))
     {
      if(PositionGetInteger(POSITION_TYPE)!=POSITION_TYPE_BUY) return(false);
     }
   else return(false);

   double MaxLot,volume,Bid;
//---- ��������� ������ ��� �������    
   if(!PositionGetDouble(POSITION_VOLUME,volume)) return(true);
   if(!SymbolInfoDouble(symbol,SYMBOL_VOLUME_MAX,MaxLot)) return(true);
   if(!SymbolInfoDouble(symbol,SYMBOL_BID,Bid)) return(true);

//---- �������� ���� �� ������������ ���������� ��������       
   if(volume>MaxLot) volume=MaxLot;

//---- ������������� ��������� ��������� ������� MqlTradeRequest ��� ���������� BUY �������
   request.type   = ORDER_TYPE_SELL;
   request.price  = Bid;
   request.action = TRADE_ACTION_DEAL;
   request.symbol = symbol;
   request.volume = volume;
   request.sl = 0.0;
   request.tp = 0.0;
   request.deviation=deviation;
   request.type_filling=ORDER_FILLING_FOK;

//---- �������� ��������� ������� �� ������������
   if(!OrderCheck(request,check))
     {
      Print(__FUNCTION__,"(): �������� ������ ��� ��������� ��������� �������!");
      Print(__FUNCTION__,"(): OrderCheck(): ",ResultRetcodeDescription(check.retcode));
      return(false);
     }
//----     
   string comment="";
   StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): ��������� Buy ������� �� ",symbol," ============ >>>");
   Print(comment);

//---- �������� ������� �� ���������� ������� �� �������� ������
   if(!OrderSend(request,result) || result.retcode!=TRADE_RETCODE_DONE)
     {
      Print(__FUNCTION__,"(): ���������� ������� �������!");
      Print(__FUNCTION__,"(): OrderSend(): ",ResultRetcodeDescription(result.retcode));
      return(false);
     }
   else
   if(result.retcode==TRADE_RETCODE_DONE)
     {
      Signal=false;
      comment="";
      StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): Buy ������� �� ",symbol," ������� ============ >>>");
      Print(comment);
      PlaySound("ok.wav");
     }
   else
     {
      Print(__FUNCTION__,"(): ���������� ������� �������!");
      Print(__FUNCTION__,"(): OrderSend(): ",ResultRetcodeDescription(result.retcode));
     }
//----
   return(true);
  }
//+------------------------------------------------------------------+
//| ��������� �������� �������                                       |
//+------------------------------------------------------------------+
bool SellPositionClose
(
 bool &Signal,         // ���� ���������� �� ������
 const string symbol,  // �������� ���� ������
 uint deviation        // �������
 )
  {
//----
   if(!Signal) return(true);

//---- ���������� �������� ��������� ������� � ���������� ��������� �������
   MqlTradeRequest request;
   MqlTradeResult result;
//---- ���������� ��������� ���������� �������� ��������� ������� 
   MqlTradeCheckResult check;

//---- ��������� ��������
   ZeroMemory(request);
   ZeroMemory(result);
   ZeroMemory(check);

//---- �������� �� ������� �������� SELL �������
   if(PositionSelect(symbol))
     {
      if(PositionGetInteger(POSITION_TYPE)!=POSITION_TYPE_SELL)return(false);
     }
   else return(false);

   double MaxLot,volume,Ask;
//---- ��������� ������ ��� �������    
   if(!PositionGetDouble(POSITION_VOLUME,volume)) return(true);
   if(!SymbolInfoDouble(symbol,SYMBOL_VOLUME_MAX,MaxLot)) return(true);
   if(!SymbolInfoDouble(symbol,SYMBOL_ASK,Ask)) return(true);

//---- �������� ���� �� ������������ ���������� ��������       
   if(volume>MaxLot) volume=MaxLot;

//---- ������������� ��������� ��������� ������� MqlTradeRequest ��� ���������� SELL �������
   request.type   = ORDER_TYPE_BUY;
   request.price  = Ask;
   request.action = TRADE_ACTION_DEAL;
   request.symbol = symbol;
   request.volume = volume;
   request.sl = 0.0;
   request.tp = 0.0;
   request.deviation=deviation;
   request.type_filling=ORDER_FILLING_FOK;

//---- �������� ��������� ������� �� ������������
   if(!OrderCheck(request,check))
     {
      Print(__FUNCTION__,"(): �������� ������ ��� ��������� ��������� �������!");
      Print(__FUNCTION__,"(): OrderCheck(): ",ResultRetcodeDescription(check.retcode));
      return(false);
     }
//----    
   string comment="";
   StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): ��������� Sell ������� �� ",symbol," ============ >>>");
   Print(comment);

//---- �������� ������� �� ���������� ������� �� �������� ������
   if(!OrderSend(request,result) || result.retcode!=TRADE_RETCODE_DONE)
     {
      Print(__FUNCTION__,"(): ���������� ������� �������!");
      Print(__FUNCTION__,"(): OrderSend(): ",ResultRetcodeDescription(result.retcode));
      return(false);
     }
   else
   if(result.retcode==TRADE_RETCODE_DONE)
     {
      Signal=false;
     }
   else
     {
      Print(__FUNCTION__,"(): ���������� ������� �������!");
      Print(__FUNCTION__,"(): OrderSend(): ",ResultRetcodeDescription(result.retcode));
      comment="";
      StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): Sell ������� �� ",symbol," ������� ============ >>>");
      Print(comment);
      PlaySound("ok.wav");
     }
//----
   return(true);
  }
//+------------------------------------------------------------------+
//| ������������ ������� �������                                     |
//+------------------------------------------------------------------+
bool BuyPositionModify
(
 bool &Modify_Signal,        // ���� ���������� �����������
 const string symbol,        // �������� ���� ������
 uint deviation,             // �������
 int StopLoss,               // �������� � �������
 int Takeprofit              // ���������� � �������
 )
//BuyPositionModify(Modify_Signal,symbol,deviation,StopLoss,Takeprofit);
  {
//----
   if(!Modify_Signal) return(true);

   ENUM_POSITION_TYPE PosType=POSITION_TYPE_BUY;

//---- �������� �� �� ������� �������� �������
   if(!PositionSelect(symbol)) return(true);

//---- ���������� �������� ��������� ������� � ���������� ��������� �������
   MqlTradeRequest request;
   MqlTradeResult result;

//---- ���������� ��������� ���������� �������� ��������� ������� 
   MqlTradeCheckResult check;

//---- ��������� ��������
   ZeroMemory(request);
   ZeroMemory(result);
   ZeroMemory(check);

   long digit;
   double point,Ask;
//----
   if(!SymbolInfoInteger(symbol,SYMBOL_DIGITS,digit)) return(true);
   if(!SymbolInfoDouble(symbol,SYMBOL_POINT,point)) return(true);
   if(!SymbolInfoDouble(symbol,SYMBOL_ASK,Ask)) return(true);

//---- ������������� ��������� ��������� ������� MqlTradeRequest ��� ���������� BUY �������
   request.type   = ORDER_TYPE_BUY;
   request.price  = Ask;
   request.action = TRADE_ACTION_SLTP;
   request.symbol = symbol;

//---- ����������� ���������� �� ��������� � �������� �������� �������
   if(StopLoss)
     {
      if(!StopCorrect(symbol,StopLoss))return(false);
      double dStopLoss=StopLoss*point;
      request.sl=NormalizeDouble(request.price-dStopLoss,int(digit));
      if(request.sl<PositionGetDouble(POSITION_SL)) request.sl=PositionGetDouble(POSITION_SL);
     }
   else request.sl=PositionGetDouble(POSITION_SL);

//---- ����������� ���������� �� ����������� �������� �������� �������
   if(Takeprofit)
     {
      if(!StopCorrect(symbol,Takeprofit))return(false);
      double dTakeprofit=Takeprofit*point;
      request.tp=NormalizeDouble(request.price+dTakeprofit,int(digit));
      if(request.tp<PositionGetDouble(POSITION_TP)) request.tp=PositionGetDouble(POSITION_TP);
     }
   else request.tp=PositionGetDouble(POSITION_TP);

//----   
   if(request.tp==PositionGetDouble(POSITION_TP) && request.sl==PositionGetDouble(POSITION_SL)) return(true);
   request.deviation=deviation;
   request.type_filling=ORDER_FILLING_FOK;

//---- �������� ��������� ������� �� ������������
   if(!OrderCheck(request,check))
     {
      Print(__FUNCTION__,"(): �������� ������ ��� ��������� ��������� �������!");
      Print(__FUNCTION__,"(): OrderCheck(): ",ResultRetcodeDescription(check.retcode));
      return(false);
     }

   string comment="";
   StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): ������������ Buy ������� �� ",symbol," ============ >>>");
   Print(comment);

//---- ������������ BUY ������� � ������ �������� ���������� ��������� �������
   if(!OrderSend(request,result) || result.retcode!=TRADE_RETCODE_DONE)
     {
      Print(__FUNCTION__,"(): ���������� �������������� �������!");
      Print(__FUNCTION__,"(): OrderSend(): ",ResultRetcodeDescription(result.retcode));
      return(false);
     }
   else
   if(result.retcode==TRADE_RETCODE_DONE)
     {
      Modify_Signal=false;
      comment="";
      StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): Buy ������� �� ",symbol," �������������� ============ >>>");
      Print(comment);
      PlaySound("ok.wav");
     }
   else
     {
      Print(__FUNCTION__,"(): ���������� �������������� �������!");
      Print(__FUNCTION__,"(): OrderSend(): ",ResultRetcodeDescription(result.retcode));
     }
//----
   return(true);
  }
//+------------------------------------------------------------------+
//| ������������ �������� �������                                    |
//+------------------------------------------------------------------+
bool SellPositionModify
(
 bool &Modify_Signal,        // ���� ���������� �����������
 const string symbol,        // �������� ���� ������
 uint deviation,             // �������
 int StopLoss,               // �������� � �������
 int Takeprofit              // ���������� � �������
 )
//SellPositionModify(Modify_Signal,symbol,deviation,StopLoss,Takeprofit);
  {
//----
   if(!Modify_Signal) return(true);

   ENUM_POSITION_TYPE PosType=POSITION_TYPE_SELL;

//---- �������� �� �� ������� �������� �������
   if(!PositionSelect(symbol)) return(true);

//---- ���������� �������� ��������� ������� � ���������� ��������� �������
   MqlTradeRequest request;
   MqlTradeResult result;

//---- ���������� ��������� ���������� �������� ��������� ������� 
   MqlTradeCheckResult check;

//---- ��������� ��������
   ZeroMemory(request);
   ZeroMemory(result);
   ZeroMemory(check);
//----
   long digit;
   double point,Bid;
//----
   if(!SymbolInfoInteger(symbol,SYMBOL_DIGITS,digit)) return(true);
   if(!SymbolInfoDouble(symbol,SYMBOL_POINT,point)) return(true);
   if(!SymbolInfoDouble(symbol,SYMBOL_BID,Bid)) return(true);

//---- ������������� ��������� ��������� ������� MqlTradeRequest ��� ���������� BUY �������
   request.type   = ORDER_TYPE_SELL;
   request.price  = Bid;
   request.action = TRADE_ACTION_SLTP;
   request.symbol = symbol;

//---- ����������� ���������� �� ��������� � �������� �������� �������
   if(StopLoss!=0)
     {
      if(!StopCorrect(symbol,StopLoss))return(false);
      double dStopLoss=StopLoss*point;
      request.sl=NormalizeDouble(request.price+dStopLoss,int(digit));
      double laststop=PositionGetDouble(POSITION_SL);
      if(request.sl>laststop && laststop) request.sl=PositionGetDouble(POSITION_SL);
     }
   else request.sl=PositionGetDouble(POSITION_SL);

//---- ����������� ���������� �� ����������� �������� �������� �������
   if(Takeprofit!=0)
     {
      if(!StopCorrect(symbol,Takeprofit))return(false);
      double dTakeprofit=Takeprofit*point;
      request.tp=NormalizeDouble(request.price-dTakeprofit,int(digit));
      double lasttake=PositionGetDouble(POSITION_TP);
      if(request.tp>lasttake && lasttake) request.tp=PositionGetDouble(POSITION_TP);
     }
   else request.tp=PositionGetDouble(POSITION_TP);

//----   
   if(request.tp==PositionGetDouble(POSITION_TP) && request.sl==PositionGetDouble(POSITION_SL)) return(true);
   request.deviation=deviation;
   request.type_filling=ORDER_FILLING_FOK;

//---- �������� ��������� ������� �� ������������
   if(!OrderCheck(request,check))
     {
      Print(__FUNCTION__,"(): �������� ������ ��� ��������� ��������� �������!");
      Print(__FUNCTION__,"(): OrderCheck(): ",ResultRetcodeDescription(check.retcode));
      return(false);
     }

   string comment="";
   StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): ������������ Sell ������� �� ",symbol," ============ >>>");
   Print(comment);

//---- ������������ SELL ������� � ������ �������� ���������� ��������� �������
   if(!OrderSend(request,result) || result.retcode!=TRADE_RETCODE_DONE)
     {
      Print(__FUNCTION__,"(): ���������� �������������� �������!");
      Print(__FUNCTION__,"(): OrderSend(): ",ResultRetcodeDescription(result.retcode));
      return(false);
     }
   else
   if(result.retcode==TRADE_RETCODE_DONE)
     {
      Modify_Signal=false;
      comment="";
      StringConcatenate(comment,"<<< ============ ",__FUNCTION__,"(): Sell ������� �� ",symbol," �������������� ============ >>>");
      Print(comment);
      PlaySound("ok.wav");
     }
   else
     {
      Print(__FUNCTION__,"(): ���������� �������������� �������!");
      Print(__FUNCTION__,"(): OrderSend(): ",ResultRetcodeDescription(result.retcode));
     }
//----
   return(true);
  }
//+------------------------------------------------------------------+
//| GetTimeLevelName() function                                      |
//+------------------------------------------------------------------+
string GetTimeLevelName(string symbol,ENUM_POSITION_TYPE trade_operation)
  {
//----
   string G_Name_;
//----  
   if(MQL5InfoInteger(MQL5_TESTING)
      || MQL5InfoInteger(MQL5_OPTIMIZATION)
      || MQL5InfoInteger(MQL5_DEBUGGING))
      StringConcatenate(G_Name_,"TimeLevel_",AccountInfoInteger(ACCOUNT_LOGIN),"_",symbol,"_",trade_operation,"_Test_");
   else StringConcatenate(G_Name_,"TimeLevel_",AccountInfoInteger(ACCOUNT_LOGIN),"_",symbol,"_",trade_operation);
//----
   return(G_Name_);
  }
//+------------------------------------------------------------------+
//| TradeTimeLevelCheck() function                                   |
//+------------------------------------------------------------------+
bool TradeTimeLevelCheck
(
 string symbol,
 ENUM_POSITION_TYPE trade_operation,
 datetime TradeTimeLevel
 )
  {
//----
   if(TradeTimeLevel>0)
     {
      //---- �������� �� ��������� ���������� ������ ��� ���������� ������ 
      if(TimeCurrent()<GlobalVariableGet(GetTimeLevelName(symbol,trade_operation))) return(false);
     }
//----
   return(true);
  }
//+------------------------------------------------------------------+
//| TradeTimeLevelSet() function                                     |
//+------------------------------------------------------------------+
void TradeTimeLevelSet
(
 string symbol,
 ENUM_POSITION_TYPE trade_operation,
 datetime TradeTimeLevel
 )
  {
//----
   GlobalVariableSet(GetTimeLevelName(symbol,trade_operation),TradeTimeLevel);
  }
//+------------------------------------------------------------------+
//| TradeTimeLevelSet() function                                     |
//+------------------------------------------------------------------+
datetime TradeTimeLevelGet
(
 string symbol,
 ENUM_POSITION_TYPE trade_operation
 )
  {
//----
   return(datetime(GlobalVariableGet(GetTimeLevelName(symbol,trade_operation))));
  }
//+------------------------------------------------------------------+
//| TimeLevelGlobalVariableDel() function                            |
//+------------------------------------------------------------------+
void TimeLevelGlobalVariableDel
(
 string symbol,
 ENUM_POSITION_TYPE trade_operation
 )
  {
//----
   if(MQL5InfoInteger(MQL5_TESTING)
      || MQL5InfoInteger(MQL5_OPTIMIZATION)
      || MQL5InfoInteger(MQL5_DEBUGGING))
      GlobalVariableDel(GetTimeLevelName(symbol,trade_operation));
//----
  }
//+------------------------------------------------------------------+
//| GlobalVariableDel_() function                                    |
//+------------------------------------------------------------------+
void GlobalVariableDel_(string symbol)
  {
//----
   TimeLevelGlobalVariableDel(symbol,POSITION_TYPE_BUY);
   TimeLevelGlobalVariableDel(symbol,POSITION_TYPE_SELL);
//----
  }
//+------------------------------------------------------------------+
//| ������ ������� ���� ��� ���������� �����                         |  
//+------------------------------------------------------------------+
/*                                                                   |
 �������  ���������� Margin_Mode ���������� ������ �������  �������� | 
 ����                                                                |
 0 - MM �� ��������� ��������� �� �����                              |
 1 - MM �� ������� ������� �� �����                                  |
 2 - MM �� ������� �� ��������� ������� �� �����                     |
 3 - MM �� ������� �� ������� ������� �� �����                       |
 �� ��������� - MM �� ��������� ��������� �� �����                   |
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
 ���� Money_Management ������ ����,  �� ��������  ������� � �������� | 
 ��������  ����  ����������  ����������  �� ���������� ������������ |
 �������� ���������� �������� Money_Management.                      |
*///                                                                 |
//+------------------------------------------------------------------+
double BuyLotCount
(
 string symbol,
 double Money_Management,
 int Margin_Mode,
 int STOPLOSS,
 uint Slippage_
 )
// BuyLotCount_(string symbol, double Money_Management, int Margin_Mode, int STOPLOSS,Slippage_)
  {
//----
   double margin,Lot;

//---1+ ���ר� �������� ���� ��� ���������� �������
   if(Money_Management<0) Lot=MathAbs(Money_Management);
   else
   switch(Margin_Mode)
     {
      //---- ������ ���� �� ��������� ������� �� �����
      case  0:
         margin=AccountInfoDouble(ACCOUNT_FREEMARGIN)*Money_Management;
         Lot=GetLotForOpeningPos(symbol,POSITION_TYPE_BUY,margin);
         break;

         //---- ������ ���� �� ������� ������� �� �����
      case  1:
         margin=AccountInfoDouble(ACCOUNT_BALANCE)*Money_Management;
         Lot=GetLotForOpeningPos(symbol,POSITION_TYPE_BUY,margin);
         break;

         //---- ������ ���� �� ������� �� ��������� ������� �� �����             
      case  2:
        {
         if(STOPLOSS<=0)
           {
            Print(__FUNCTION__,": �������� ��������!!!");
            STOPLOSS=0;
           }
         //---- 
         long digit;
         double point,price_open;
         //----   
         if(!SymbolInfoInteger(symbol,SYMBOL_DIGITS,digit)) return(-1);
         if(!SymbolInfoDouble(symbol,SYMBOL_POINT,point)) return(-1);
         if(!SymbolInfoDouble(symbol,SYMBOL_ASK,price_open)) return(-1);

         //---- ����������� ���������� �� ��������� � �������� �������� �������
         if(!StopCorrect(symbol,STOPLOSS)) return(TRADE_RETCODE_ERROR);
         double price_close=NormalizeDouble(price_open-STOPLOSS*point,int(digit));

         double profit;
         if(!OrderCalcProfit(ORDER_TYPE_BUY,symbol,1,price_open,price_close,profit)) return(-1);
         if(!profit) return(-1);

         //---- ������ ������ �� ��������� ������� �� �����
         double Loss=AccountInfoDouble(ACCOUNT_FREEMARGIN)*Money_Management;
         if(!Loss) return(-1);

         Lot=Loss/MathAbs(profit);
         break;
        }

      //---- ������ ���� �� ������� �� ������� ������� �� �����
      case  3:
        {
         if(STOPLOSS<=0)
           {
            Print(__FUNCTION__,": �������� ��������!!!");
            STOPLOSS=0;
           }
         //---- 
         long digit;
         double point,price_open;
         //----   
         if(!SymbolInfoInteger(symbol,SYMBOL_DIGITS,digit)) return(-1);
         if(!SymbolInfoDouble(symbol,SYMBOL_POINT,point)) return(-1);
         if(!SymbolInfoDouble(symbol,SYMBOL_ASK,price_open)) return(-1);

         //---- ����������� ���������� �� ��������� � �������� �������� �������
         if(!StopCorrect(symbol,STOPLOSS)) return(TRADE_RETCODE_ERROR);
         double price_close=NormalizeDouble(price_open-STOPLOSS*point,int(digit));

         double profit;
         if(!OrderCalcProfit(ORDER_TYPE_BUY,symbol,1,price_open,price_close,profit)) return(-1);

         if(!profit) return(-1);

         //---- ������ ������ �� ������� ������� �� �����
         double Loss=AccountInfoDouble(ACCOUNT_BALANCE)*Money_Management;
         if(!Loss) return(-1);

         Lot=Loss/MathAbs(profit);
         break;
        }

      //---- ������ ���� ��� ���������
      case  4:
        {
         Lot=MathAbs(Money_Management);
         break;
        }

      //---- ������ ���� �� ��������� ������� �� ����� �� ���������
      default:
        {
         margin=AccountInfoDouble(ACCOUNT_FREEMARGIN)*Money_Management;
         Lot=GetLotForOpeningPos(symbol,POSITION_TYPE_BUY,margin);
        }
     }
//---1+    

//---- ������������ �������� ���� �� ���������� ������������ �������� 
   if(!LotCorrect(symbol,Lot,POSITION_TYPE_BUY)) return(-1);
//----
   return(Lot);
  }
//+------------------------------------------------------------------+
//| ������ ������� ���� ��� ���������� �����                         |  
//+------------------------------------------------------------------+
/*                                                                   |
 �������  ���������� Margin_Mode ���������� ������ �������  �������� | 
 ����                                                                |
 0 - MM �� ��������� ��������� �� �����                              |
 1 - MM �� ������� ������� �� �����                                  |
 2 - MM �� ������� �� ��������� ������� �� �����                     |
 3 - MM �� ������� �� ������� ������� �� �����                       |
 �� ��������� - MM �� ��������� ��������� �� �����                   |
//+ - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -+
 ���� Money_Management ������ ����,  �� ��������  ������� � �������� | 
 ��������  ����  ����������  ����������  �� ���������� ������������ |
 �������� ���������� �������� Money_Management.                      |
*///                                                                 |
//+------------------------------------------------------------------+
double SellLotCount
(
 string symbol,
 double Money_Management,
 int Margin_Mode,
 int STOPLOSS,
 uint Slippage_
 )
// (string symbol, double Money_Management, int Margin_Mode, int STOPLOSS)
  {
//----
   double margin,Lot;

//---1+ ���ר� �������� ���� ��� ���������� �������
   if(Money_Management<0) Lot=MathAbs(Money_Management);
   else
   switch(Margin_Mode)
     {
      //---- ������ ���� �� ��������� ������� �� �����
      case  0:
         margin=AccountInfoDouble(ACCOUNT_FREEMARGIN)*Money_Management;
         Lot=GetLotForOpeningPos(symbol,POSITION_TYPE_SELL,margin);
         break;

         //---- ������ ���� �� ������� ������� �� �����
      case  1:
         margin=AccountInfoDouble(ACCOUNT_BALANCE)*Money_Management;
         Lot=GetLotForOpeningPos(symbol,POSITION_TYPE_SELL,margin);
         break;

         //---- ������ ���� �� ������� �� ��������� ������� �� �����             
      case  2:
        {
         if(STOPLOSS<=0)
           {
            Print(__FUNCTION__,": �������� ��������!!!");
            STOPLOSS=0;
           }
         //---- 
         long digit;
         double point,price_open;
         //----   
         if(!SymbolInfoInteger(symbol,SYMBOL_DIGITS,digit)) return(-1);
         if(!SymbolInfoDouble(symbol,SYMBOL_POINT,point)) return(-1);
         if(!SymbolInfoDouble(symbol,SYMBOL_BID,price_open)) return(-1);

         //---- ����������� ���������� �� ��������� � �������� �������� �������
         if(!StopCorrect(symbol,STOPLOSS)) return(TRADE_RETCODE_ERROR);
         double price_close=NormalizeDouble(price_open+
                                            STOPLOSS*point,int(digit));

         double profit;
         if(!OrderCalcProfit(ORDER_TYPE_SELL,symbol,1,price_open,price_close,profit)) return(-1);

         if(!profit) return(-1);

         //---- ������ ������ �� ��������� ������� �� �����
         double Loss=AccountInfoDouble(ACCOUNT_FREEMARGIN)*Money_Management;
         if(!Loss) return(-1);

         Lot=Loss/MathAbs(profit);
         break;
        }

      //---- ������ ���� �� ������� �� ������� ������� �� �����
      case  3:
        {
         if(STOPLOSS<=0)
           {
            Print(__FUNCTION__,": �������� ��������!!!");
            STOPLOSS=0;
           }
         //---- 
         long digit;
         double point,price_open;
         //----   
         if(!SymbolInfoInteger(symbol,SYMBOL_DIGITS,digit)) return(-1);
         if(!SymbolInfoDouble(symbol,SYMBOL_POINT,point)) return(-1);
         if(!SymbolInfoDouble(symbol,SYMBOL_BID,price_open)) return(-1);

         //---- ����������� ���������� �� ��������� � �������� �������� �������
         if(!StopCorrect(symbol,STOPLOSS)) return(TRADE_RETCODE_ERROR);
         double price_close=NormalizeDouble(price_open+STOPLOSS*point,int(digit));

         double profit;
         if(!OrderCalcProfit(ORDER_TYPE_SELL,symbol,1,price_open,price_close,profit)) return(-1);
         if(!profit) return(-1);

         //---- ������ ������ �� ������� ������� �� �����
         double Loss=AccountInfoDouble(ACCOUNT_BALANCE)*Money_Management;
         if(!Loss) return(-1);

         Lot=Loss/MathAbs(profit);
         break;
        }

      //---- ������ ���� ��� ���������
      case  4:
        {
         Lot=MathAbs(Money_Management);
         break;
        }

      //---- ������ ���� �� ��������� ������� �� ����� �� ���������
      default:
        {
         margin=AccountInfoDouble(ACCOUNT_FREEMARGIN)*Money_Management;
         Lot=GetLotForOpeningPos(symbol,POSITION_TYPE_SELL,margin);
        }
     }
//---1+ 

//---- ������������ �������� ���� �� ���������� ������������ �������� 
   if(!LotCorrect(symbol,Lot,POSITION_TYPE_SELL)) return(-1);
//----
   return(Lot);
  }
//+------------------------------------------------------------------+
//| ��������� ������� ����������� ������ �� ����������� ��������     |
//+------------------------------------------------------------------+
bool StopCorrect(string symbol,int &Stop)
  {
//----
   long Extrem_Stop;
   if(!SymbolInfoInteger(symbol,SYMBOL_TRADE_STOPS_LEVEL,Extrem_Stop)) return(false);
   if(Stop<Extrem_Stop) Stop=int(Extrem_Stop);
//----
   return(true);
  }
//+------------------------------------------------------------------+
//| ��������� ������� ����������� ������ �� ����������� ��������     |
//+------------------------------------------------------------------+
bool dStopCorrect
(
 string symbol,
 double &dStopLoss,
 double &dTakeprofit,
 ENUM_POSITION_TYPE trade_operation
 )
// dStopCorrect(symbol,dStopLoss,dTakeprofit,trade_operation)
  {
//----
   if(!dStopLoss && !dTakeprofit) return(true);

   if(dStopLoss<0)
     {
      Print(__FUNCTION__,"(): ������������� �������� ���������!");
      return(false);
     }

   if(dTakeprofit<0)
     {
      Print(__FUNCTION__,"(): ������������� �������� �����������!");
      return(false);
     }
//---- 
   int Stop;
   long digit;
   double point,dStop,ExtrStop,ExtrTake;

//---- �������� ����������� ���������� �� ����������� ������ 
   Stop=0;
   if(!StopCorrect(symbol,Stop))return(false);
//----   
   if(!SymbolInfoInteger(symbol,SYMBOL_DIGITS,digit)) return(false);
   if(!SymbolInfoDouble(symbol,SYMBOL_POINT,point)) return(false);
   dStop=Stop*point;

//---- ��������� ������� ����������� ������ ��� �����
   if(trade_operation==POSITION_TYPE_BUY)
     {
      double Ask;
      if(!SymbolInfoDouble(symbol,SYMBOL_ASK,Ask)) return(false);

      ExtrStop=NormalizeDouble(Ask-dStop,int(digit));
      ExtrTake=NormalizeDouble(Ask+dStop,int(digit));

      if(dStopLoss>ExtrStop && dStopLoss) dStopLoss=ExtrStop;
      if(dTakeprofit<ExtrTake && dTakeprofit) dTakeprofit=ExtrTake;
     }

//---- ��������� ������� ����������� ������ ��� �����
   if(trade_operation==POSITION_TYPE_SELL)
     {
      double Bid;
      if(!SymbolInfoDouble(symbol,SYMBOL_BID,Bid)) return(false);

      ExtrStop=NormalizeDouble(Bid+dStop,int(digit));
      ExtrTake=NormalizeDouble(Bid-dStop,int(digit));

      if(dStopLoss<ExtrStop && dStopLoss) dStopLoss=ExtrStop;
      if(dTakeprofit>ExtrTake && dTakeprofit) dTakeprofit=ExtrTake;
     }
//----
   return(true);
  }
//+------------------------------------------------------------------+
//| ��������� ������� ���� �� ���������� ����������� ��������        |
//+------------------------------------------------------------------+
bool LotCorrect
(
 string symbol,
 double &Lot,
 ENUM_POSITION_TYPE trade_operation
 )
//LotCorrect(string symbol, double& Lot, ENUM_POSITION_TYPE trade_operation)
  {
//---- ��������� ������ ��� �������   
   double Step,MaxLot,MinLot;
   if(!SymbolInfoDouble(symbol,SYMBOL_VOLUME_STEP,Step)) return(false);
   if(!SymbolInfoDouble(symbol,SYMBOL_VOLUME_MAX,MaxLot)) return(false);
   if(!SymbolInfoDouble(symbol,SYMBOL_VOLUME_MIN,MinLot)) return(false);

//---- ������������ �������� ���� �� ���������� ������������ �������� 
   Lot=Step*MathFloor(Lot/Step);

//---- �������� ���� �� ����������� ���������� ��������
   if(Lot<MinLot) Lot=MinLot;
//---- �������� ���� �� ������������ ���������� ��������       
   if(Lot>MaxLot) Lot=MaxLot;

//---- �������� ������� �� �������������
   if(!LotFreeMarginCorrect(symbol,Lot,trade_operation))return(false);
//----
   return(true);
  }
//+------------------------------------------------------------------+
//| ����������� ������� ���� ������������� ��������                  |
//+------------------------------------------------------------------+
bool LotFreeMarginCorrect
(
 string symbol,
 double &Lot,
 ENUM_POSITION_TYPE trade_operation
 )
//(string symbol, double& Lot, ENUM_POSITION_TYPE trade_operation)
  {
//---- �������� ������� �� �������������
   double freemargin=AccountInfoDouble(ACCOUNT_FREEMARGIN);
   if(freemargin<=0) return(false);

//---- ��������� ������ ��� �������   
   double Step,MaxLot,MinLot;
   if(!SymbolInfoDouble(symbol,SYMBOL_VOLUME_STEP,Step)) return(false);
   if(!SymbolInfoDouble(symbol,SYMBOL_VOLUME_MAX,MaxLot)) return(false);
   if(!SymbolInfoDouble(symbol,SYMBOL_VOLUME_MIN,MinLot)) return(false);

   double ExtremLot=GetLotForOpeningPos(symbol,trade_operation,freemargin);
//---- ������������ �������� ���� �� ���������� ������������ �������� 
   ExtremLot=Step*MathFloor(ExtremLot/Step);

   if(ExtremLot<MinLot) return(false); // ������������ ����� ���� �� ����������� ���!
   if(Lot>ExtremLot) Lot=ExtremLot; // ������� ������ ���� �� ����, ��� ���� �� ��������!
   if(Lot>MaxLot) Lot=MaxLot; // ������� ������ ���� �� ���������� �����������
//----
   return(true);
  }
//+------------------------------------------------------------------+
//| ������ ������ ���� ��� ���������� ������� � ������ lot_margin    |
//+------------------------------------------------------------------+
double GetLotForOpeningPos(string symbol,ENUM_POSITION_TYPE direction,double lot_margin)
  {
//----
   double price=0.0,n_margin;
   if(direction==POSITION_TYPE_BUY)  if(!SymbolInfoDouble(symbol,SYMBOL_ASK,price)) return(0);
   if(direction==POSITION_TYPE_SELL) if(!SymbolInfoDouble(symbol,SYMBOL_BID,price)) return(0);
   if(!price) return(NULL);

   if(!OrderCalcMargin(ENUM_ORDER_TYPE(direction),symbol,1,price,n_margin) || !n_margin) return(0);
   double lot=lot_margin/n_margin;

//---- ��������� �������� ��������
   double LOTSTEP,MaxLot,MinLot;
   if(!SymbolInfoDouble(symbol,SYMBOL_VOLUME_STEP,LOTSTEP)) return(0);
   if(!SymbolInfoDouble(symbol,SYMBOL_VOLUME_MAX,MaxLot)) return(0);
   if(!SymbolInfoDouble(symbol,SYMBOL_VOLUME_MIN,MinLot)) return(0);

//---- ������������ �������� ���� �� ���������� ������������ �������� 
   lot=LOTSTEP*MathFloor(lot/LOTSTEP);

//---- �������� ���� �� ����������� ���������� ��������
   if(lot<MinLot) lot=0;
//---- �������� ���� �� ������������ ���������� ��������       
   if(lot>MaxLot) lot=MaxLot;
//----
   return(lot);
  }
//+------------------------------------------------------------------+
//| ������� ������� � ��������� �������� ������ � ���������          |
//+------------------------------------------------------------------+
string GetSymbolByCurrencies(string margin_currency,string profit_currency)
  {
//---- ��������� � ����� ��� �������, ������� ������������ � ���� "����� �����"
   int total=SymbolsTotal(true);
   for(int numb=0; numb<total; numb++)
     {
      //---- ������� ��� ������� �� ������ � ������ "����� �����"
      string symbolname=SymbolName(numb,true);

      //---- ������� ������ ������
      string m_cur=SymbolInfoString(symbolname,SYMBOL_CURRENCY_MARGIN);

      //---- ������� ������ ��������� (� ��� ���������� ������� ��� ��������� ����)
      string p_cur=SymbolInfoString(symbolname,SYMBOL_CURRENCY_PROFIT);

      //---- ���� ������ ������� �� ����� �������� �������, ������  ��� �������
      if(m_cur==margin_currency && p_cur==profit_currency) return(symbolname);
     }
//----    
   return(NULL);
  }
//+------------------------------------------------------------------+
//| ������� ����������� ���������� �������� �������� �� ��� ����     |
//+------------------------------------------------------------------+
string ResultRetcodeDescription(int retcode)
  {
   string str;
//----
   switch(retcode)
     {
      case TRADE_RETCODE_REQUOTE: str="�������"; break;
      case TRADE_RETCODE_REJECT: str="������ ���������"; break;
      case TRADE_RETCODE_CANCEL: str="������ ������� ���������"; break;
      case TRADE_RETCODE_PLACED: str="����� ��������"; break;
      case TRADE_RETCODE_DONE: str="������ ���������"; break;
      case TRADE_RETCODE_DONE_PARTIAL: str="������ ��������� ��������"; break;
      case TRADE_RETCODE_ERROR: str="������ ��������� �������"; break;
      case TRADE_RETCODE_TIMEOUT: str="������ ������� �� ��������� �������";break;
      case TRADE_RETCODE_INVALID: str="������������ ������"; break;
      case TRADE_RETCODE_INVALID_VOLUME: str="������������ ����� � �������"; break;
      case TRADE_RETCODE_INVALID_PRICE: str="������������ ���� � �������"; break;
      case TRADE_RETCODE_INVALID_STOPS: str="������������ ����� � �������"; break;
      case TRADE_RETCODE_TRADE_DISABLED: str="�������� ���������"; break;
      case TRADE_RETCODE_MARKET_CLOSED: str="����� ������"; break;
      case TRADE_RETCODE_NO_MONEY: str="��� ����������� �������� ������� ��� ���������� �������"; break;
      case TRADE_RETCODE_PRICE_CHANGED: str="���� ����������"; break;
      case TRADE_RETCODE_PRICE_OFF: str="����������� ��������� ��� ��������� �������"; break;
      case TRADE_RETCODE_INVALID_EXPIRATION: str="�������� ���� ��������� ������ � �������"; break;
      case TRADE_RETCODE_ORDER_CHANGED: str="��������� ������ ����������"; break;
      case TRADE_RETCODE_TOO_MANY_REQUESTS: str="������� ������ �������"; break;
      case TRADE_RETCODE_NO_CHANGES: str="� ������� ��� ���������"; break;
      case TRADE_RETCODE_SERVER_DISABLES_AT: str="������������ �������� ��������"; break;
      case TRADE_RETCODE_CLIENT_DISABLES_AT: str="������������ �������� ���������� ����������"; break;
      case TRADE_RETCODE_LOCKED: str="������ ������������ ��� ���������"; break;
      case TRADE_RETCODE_FROZEN: str="����� ��� ������� ����������"; break;
      case TRADE_RETCODE_INVALID_FILL: str="������ ���������������� ��� ���������� ������ �� ������� "; break;
      case TRADE_RETCODE_CONNECTION: str="��� ���������� � �������� ��������"; break;
      case TRADE_RETCODE_ONLY_REAL: str="�������� ��������� ������ ��� �������� ������"; break;
      case TRADE_RETCODE_LIMIT_ORDERS: str="��������� ����� �� ���������� ���������� �������"; break;
      case TRADE_RETCODE_LIMIT_VOLUME: str="��������� ����� �� ����� ������� � ������� ��� ������� �������"; break;
      default: str="����������� ���������";
     }
//----
   return(str);
  }
//+------------------------------------------------------------------+
//|                                                HistoryLoader.mqh |
//|                      Copyright � 2009, MetaQuotes Software Corp. |
//|                                       http://www.metaquotes.net/ |
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| �������� ������� ��� ��������������� ��������                    |
//+------------------------------------------------------------------+
int LoadHistory(datetime StartDate,           // ��������� ���� ��� ��������� �������
                string LoadedSymbol,          // ������ ������������� ������������ ������
                ENUM_TIMEFRAMES LoadedPeriod) // ��������� ������������� ������������ ������
  {
//----+ 
//Print(__FUNCTION__, ": Start load ", LoadedSymbol+ " , " + EnumToString(LoadedPeriod) + " from ", StartDate);
   int res=CheckLoadHistory(LoadedSymbol,LoadedPeriod,StartDate);
   switch(res)
     {
      case -1 : Print(__FUNCTION__, "(", LoadedSymbol, " ", EnumToString(LoadedPeriod), "): Unknown symbol ", LoadedSymbol);               break;
      case -2 : Print(__FUNCTION__, "(", LoadedSymbol, " ", EnumToString(LoadedPeriod), "): Requested bars more than max bars in chart "); break;
      case -3 : Print(__FUNCTION__, "(", LoadedSymbol, " ", EnumToString(LoadedPeriod), "): Program was stopped ");                        break;
      case -4 : Print(__FUNCTION__, "(", LoadedSymbol, " ", EnumToString(LoadedPeriod), "): Indicator shouldn't load its own data ");      break;
      case -5 : Print(__FUNCTION__, "(", LoadedSymbol, " ", EnumToString(LoadedPeriod), "): Load failed ");                                break;
      case  0 : /* Print(__FUNCTION__, "(", LoadedSymbol, " ", EnumToString(LoadedPeriod), "): Loaded OK ");  */                           break;
      case  1 : /* Print(__FUNCTION__, "(", LoadedSymbol, " ", EnumToString(LoadedPeriod), "): Loaded previously ");  */                   break;
      case  2 : /* Print(__FUNCTION__, "(", LoadedSymbol, " ", EnumToString(LoadedPeriod), "): Loaded previously and built ");  */         break;
      default : { /* Print(__FUNCTION__, "(", LoadedSymbol, " ", EnumToString(LoadedPeriod), "): Unknown result "); */}
     }
/* 
   if (res > 0)
    {   
     bars = Bars(LoadedSymbol, LoadedPeriod);
     Print(__FUNCTION__, "(", LoadedSymbol, " ", GetPeriodName(LoadedPeriod), "): First date ", first_date, " - ", bars, " bars");
    }
   */
//----+
   return(res);
  }
//+------------------------------------------------------------------+
//|  �������� ������� ��� ���������                                  |
//+------------------------------------------------------------------+
int CheckLoadHistory(string symbol,ENUM_TIMEFRAMES period,datetime start_date)
  {
//----+
   datetime first_date=0;
   datetime times[100];
//--- check symbol & period
   if(symbol == NULL || symbol == "") symbol = Symbol();
   if(period == PERIOD_CURRENT)     period = Period();
//--- check if symbol is selected in the MarketWatch
   if(!SymbolInfoInteger(symbol,SYMBOL_SELECT))
     {
      if(GetLastError()==ERR_MARKET_UNKNOWN_SYMBOL) return(-1);
      if(!SymbolSelect(symbol,true)) Print(__FUNCTION__,"(): �� ������� �������� ������ ",symbol," � ���� MarketWatch!!!");
     }
//--- check if data is present
   SeriesInfoInteger(symbol,period,SERIES_FIRSTDATE,first_date);
   if(first_date>0 && first_date<=start_date) return(1);
//--- don't ask for load of its own data if it is an indicator
   if(MQL5InfoInteger(MQL5_PROGRAM_TYPE)==PROGRAM_INDICATOR && Period()==period && Symbol()==symbol)
      return(-4);
//--- second attempt
   if(SeriesInfoInteger(symbol,PERIOD_M1,SERIES_TERMINAL_FIRSTDATE,first_date))
     {
      //--- there is loaded data to build timeseries
      if(first_date>0)
        {
         //--- force timeseries build
         CopyTime(symbol,period,first_date+PeriodSeconds(period),1,times);
         //--- check date
         if(SeriesInfoInteger(symbol,period,SERIES_FIRSTDATE,first_date))
            if(first_date>0 && first_date<=start_date) return(2);
        }
     }
//--- max bars in chart from terminal options
   int max_bars=TerminalInfoInteger(TERMINAL_MAXBARS);
//--- load symbol history info
   datetime first_server_date=0;
   while(!SeriesInfoInteger(symbol,PERIOD_M1,SERIES_SERVER_FIRSTDATE,first_server_date) && !IsStopped())
      Sleep(5);
//--- fix start date for loading
   if(first_server_date>start_date) start_date=first_server_date;
   if(first_date>0 && first_date<first_server_date)
      Print(__FUNCTION__,"(): Warning: first server date ",first_server_date," for ",symbol,
            " does not match to first series date ",first_date);
//--- load data step by step
   int fail_cnt=0;
   while(!IsStopped())
     {
      //--- wait for timeseries build
      while(!SeriesInfoInteger(symbol,period,SERIES_SYNCHRONIZED) && !IsStopped())
         Sleep(5);
      //--- ask for built bars
      int bars=Bars(symbol,period);
      if(bars>0)
        {
         if(bars>=max_bars) return(-2);
         //--- ask for first date
         if(SeriesInfoInteger(symbol,period,SERIES_FIRSTDATE,first_date))
            if(first_date>0 && first_date<=start_date) return(0);
        }
      //--- copying of next part forces data loading
      int copied=CopyTime(symbol,period,bars,100,times);
      if(copied>0)
        {
         //--- check for data
         if(times[0]<=start_date) return(0);
         if(bars+copied>=max_bars) return(-2);
         fail_cnt=0;
        }
      else
        {
         //--- no more than 100 failed attempts
         fail_cnt++;
         if(fail_cnt>=100) return(-5);
         Sleep(10);
        }
     }
//----+ stopped
   return(-3);
  }
//+------------------------------------------------------------------+
