// ������������� ����� ��� ������������� ��������� �������� ��������:
//  1) �������� �������� �������
//  2) ��������� ���������� �������
//  3) ����������� �������� � ���������� �������
//  4) �������� �������� �������
//  5) �������� ���������� �������


#property copyright "Scriptong"
#property link      "http://scriptong.myqip.ru/"
#property version "1.00"

#include <Common\Common_GetSymbolInfo.mqh>
#include <Common\Common_MathUtils.mqh>
// ====================================================================== ������ ������������� ������� ����������� ����� ==================================================================================

// --- �������� ������������ ���������� ������ ���� ������. ���� �������� isCorrectionNeeded ����� true, �� ������������ ���������� �������� ���� ��������, ����� � �������. ��� �������� �������..
// ..���������� ���� �������� (Bid ��� Ask) ������������� ����������.
// bool IsOrderParametersCorrect(TradeParam &tradeParam, double stopLevel, double bid, double ask, double delta, bool isCorrectionNeeded = false)

// --- �������� ������������ ���������� �� ����� � ������� �� ���� �������� Buy ������ � ���������� ��������, ���� isCorrectionNeeded ����� true
// bool IsBuyOrderStopsCorrect(TradeParam &tradeParam, double basePrice, double stopLevel, double delta, bool isCorrectionNeeded = false)

// --- �������� ������������ ���������� �� ����� � ������� �� ���� �������� Sell ������ � ���������� ��������, ���� isCorrectionNeeded ����� true
// bool IsSellOrderStopsCorrect(TradeParam &tradeParam, double basePrice, double stopLevel, double delta, bool isCorrectionNeeded = false)

// ��������� ��������� �������
struct TradeParam
  {
   int               orderTicket;
   int               orderTicketCounter;
   int               orderType;
   int               orderMN;

   color             arrowColor;

   double            orderVolume;
   double            orderOP;
   double            orderSL;
   double            orderTP;

                     TradeParam()
     {
      Init();
     }

   void Init()
     {
      orderTicket=-1;
      orderTicketCounter=-1;
      orderType=-1;
      orderMN=0;

      arrowColor=clrNONE;

      orderVolume=0.0;
      orderOP = 0.0;
      orderSL = 0.0;
      orderTP = 0.0;
     }
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
struct TradeErrorState                                                                             // ��������� ��������� ��������� �������, � ������� ����� ������ ������ ����� �������� ��������� �������
  {
   bool              isCounterClosingAllowed;                                                            // ��������� �� ��������� �������� �������� �������?
   bool              isLongAllowed;                                                                      // ��������� �� ������ Buy?
   bool              isShortAllowed;                                                                     // ��������� �� ������ Sell?
  };


// ����������� �����, ������� �������� ����� ������� �������� � �������� ��������� ������ - 10 ���
#define MIN_EXPIRATION_TIME      600
// ����� CTrade
class CTrade
  {
   bool              m_freeMarginAlert;
   string            m_orderOpenSound;
   TradeErrorState   m_tradeErrorState;

public:
   // �����������. ���������� ��� ��������� �����, ���������������� ����� ��������� ������
   void              CTrade(string orderOpenSound,bool isCounterClosingAllowed=true);

   // ���������� �������� �������� ������. ���������� ��������� ���������:
   //  pGetSymbolInfo - ��������� ������ GetSymbolInfo, ������� ������ ���� ������ �� ������� ������ ������
   //  orderType - ��� ������ (OP_BUY, OP_SELL � �. �)
   //  volume - ����� ������. ������������� ���������� � ���������� ����������� ��������
   //  openPrice - ���� �������� ����������� ������. ��� ��������� ����� �� ���������. ������������� ���������� � ���������� ����������� ��������
   //  slPrice - ���� Stop Loss ������. ������������� ���������� � ���������� ����������� ��������
   //  tpPrice - ���� take Profit ������. ������������� ���������� � ���������� ����������� ��������
   //  magic - ������������� ������ ��� ������������ ������������ �������������
   //  comment - ��������� ����������� � ������
   //  expiration - ����/����� ��������� ����������� ������
   //  arrowColor - ���� �������, ����������� �� ������� ���� ���������� ��������
   bool              OpenOrder(GetSymbolInfo &pGetSymbolInfo,TradeParam &tradeParam,string comment=NULL,datetime expiration=0);

   // ����������� ������ ������ ����. ���������� ��������� ���������:
   //  pGetSymbolInfo - ��������� ������ GetSymbolInfo, ������� ������ ���� ������ �� ������� ������ ������
   //  orderTicket - ����� ��������������� ������
   //  slPrice - ���� Stop Loss ������. ������������� ���������� � ���������� ����������� ��������
   //  tpPrice - ���� take Profit ������. ������������� ���������� � ���������� ����������� ��������
   //  openPrice - ���� �������� ����������� ������. ��� ��������� ����� �� ���������. ������������� ���������� � ���������� ����������� ��������
   //  expiration - ����/����� ��������� ����������� ������
   //  arrowColor - ���� �������, ����������� �� ������� ���� ���������� ��������
   bool              ModifyOrder(GetSymbolInfo &pGetSymbolInfo,TradeParam &tradeParam,datetime expiration=0);

   // �������� ��������� ������ ��� �������� ����������� ������. ����������� �������� ������������� �������������. ���������� ��������� ���������:
   //  pGetSymbolInfo - ��������� ������ GetSymbolInfo, ������� ������ ���� ������ �� ������� ������ ������
   //  orderTicket - ����� ��������������� ������
   //  orderVolume - ����� ������������ ������. 0, ������������� ��� ������ �������� ������ ������ - ���� �����.
   //  arrowColor - ���� �������, ����������� �� ������� ���� ���������� ��������
   bool              DestroyOrder(GetSymbolInfo &pGetSymbolInfo,TradeParam &tradeParam);

   // ��������� �������� �������� �������. ���������� ��������� ���������:
   //  orderTicket - ����� ������� ��������� ������
   //  orderTicketCounter - ����� ������� ��������� ������           
   //  arrowColor - ���� �������, ����������� �� ������� ���� ���������� ��������
   // ��� ������ ������ ���� �������������� ���� �����, �. �. ���� Sell, ������ - Buy. � ��������� ������ - ������
   bool              CloseCounter(TradeParam &tradeParam);

   // ����� �������� ������ �� ������ orderTicket
   bool              SelectOrder(int orderTicket);

   // �������������� ���� ������ � ��������� �������������. ��������, ���� orderType == OP_BUY, �� ����� ������ ������ "Buy"
   string            OrderTypeToString(int orderType);

   // ��������� ������ � ����������� ������� ��������
   TradeErrorState   GetTradeErrorState(void) const;

private:
   bool              OpenOrderByMarket(GetSymbolInfo &pGetSymbolInfo,TradeParam &tradeParam,string comment,datetime expiration);
   bool              OpenOrderWithInstantMode(string symbol,TradeParam &tradeParam,int slippage,datetime expiration,string comment,int &ticket);
   bool              OpenPendingOrder(GetSymbolInfo &pGetSymbolInfo,TradeParam &tradeParam,string comment,datetime expiration);
   bool              OpenOrderWithMarketMode(GetSymbolInfo &pGetSymbolInfo,TradeParam &tradeParam,datetime expiration,string comment);

   bool              IsEnoughMoney(string symbol,double volume,int orderType);

   void              CorrectionOfStops(GetSymbolInfo &pGetSymbolInfo,int orderType,double &slPrice,double &tpPrice);

   bool              ModifyDeal(GetSymbolInfo &pGetSymbolInfo,TradeParam &tradeParam);
   bool              ModifyPendingOrder(GetSymbolInfo &pGetSymbolInfo,TradeParam &tradeParam,datetime expiration);

   bool              IsOrderMarketAndWorking(int ticket,int &type);
  };
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| ����������� ������                                                                                                                                                                       |
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
void CTrade::CTrade(string orderOpenSound,bool isCounterClosingAllowed=true) : m_freeMarginAlert(false)
                                                                       ,m_orderOpenSound(orderOpenSound)
  {
   m_tradeErrorState.isCounterClosingAllowed=isCounterClosingAllowed;
   m_tradeErrorState.isLongAllowed=true;
   m_tradeErrorState.isShortAllowed=true;
  }
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| ��������/��������� ������ ���� ������                                                                                                                                                    |
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
bool CTrade::OpenOrder(GetSymbolInfo &pGetSymbolInfo,TradeParam &tradeParam,string comment=NULL,datetime expiration=0)
  {
// ������������ ��� ������
   if(tradeParam.orderType<OP_BUY || tradeParam.orderType>OP_SELLSTOP)
      return (true);

// ������������ ������ - �������� ��� ������ ��������
   if((!m_tradeErrorState.isLongAllowed && MathMod(tradeParam.orderType,2)==0) || 
      (!m_tradeErrorState.isShortAllowed && MathMod(tradeParam.orderType,2)==1))
      return false;

// ����������� ����
   if(tradeParam.orderSL<0 || tradeParam.orderTP<0)
      return (false);

// ���������� ������ � ��� � ��������� ���������� ���������
   tradeParam.orderVolume=VolumeRound(tradeParam.orderVolume,pGetSymbolInfo.GetVolumeMin(),pGetSymbolInfo.GetVolumeMax(),pGetSymbolInfo.GetVolumeStep());

   double tickSize=pGetSymbolInfo.GetTickSize();
   tradeParam.orderSL = NP(tradeParam.orderSL, tickSize);
   tradeParam.orderTP = NP(tradeParam.orderTP, tickSize);
   tradeParam.orderOP = NP(tradeParam.orderOP, tickSize);

// �������� ��������� ������
   if(tradeParam.orderType==OP_BUY || tradeParam.orderType==OP_SELL)
      return (OpenOrderByMarket(pGetSymbolInfo, tradeParam, comment, expiration));

// ��������� ����������� ������
   return (OpenPendingOrder(pGetSymbolInfo, tradeParam, comment, expiration));
  }
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| �������� ��������� ������ � �������������� ������������ ���� ���������� ������                                                                                                           |
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
bool CTrade::OpenOrderByMarket(GetSymbolInfo &pGetSymbolInfo,TradeParam &tradeParam,string comment,datetime expiration)
  {
   string symbol=pGetSymbolInfo.GetSymbol();
   if(!IsEnoughMoney(symbol,tradeParam.orderVolume,tradeParam.orderType))
      return (false);

   if(!IsOrderParametersCorrect(tradeParam,pGetSymbolInfo.GetStopLevel(),pGetSymbolInfo.GetBid(),pGetSymbolInfo.GetAsk(),pGetSymbolInfo.GetPoint()/100))
      return(false);

   int ticket=-1;
   if(SymbolInfoInteger(symbol,SYMBOL_TRADE_EXEMODE)==SYMBOL_TRADE_EXECUTION_INSTANT || (tradeParam.orderSL==0 && tradeParam.orderTP==0))
      return (OpenOrderWithInstantMode(symbol, tradeParam, pGetSymbolInfo.GetSlippage(), expiration, comment, ticket));

   return (OpenOrderWithMarketMode(pGetSymbolInfo, tradeParam, expiration, comment));
  }
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| �������� ������������� ��������� ������� � ������� ��������� �� ������                                                                                                                   |
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
bool CTrade::IsEnoughMoney(string symbol,double volume,int orderType)
  {
   double equityLeft=AccountFreeMarginCheck(symbol,orderType,volume);
   if(equityLeft>0 && GetLastError()!=ERR_NOT_ENOUGH_MONEY)
     {
      m_freeMarginAlert=false;
      return(true);
     }

   if(m_freeMarginAlert)
      return (false);

   double freeMargin=AccountFreeMargin();
   Print("������������ ������� ��� �������� ������� ",OrderTypeToString(orderType)," ������� ",volume,". ���������: ",freeMargin-equityLeft,", ������� = ",freeMargin);
   m_freeMarginAlert=true;

   return(false);
  }
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| �������� ������ ��� ���� ���������� Instant Execution                                                                                                                                    |
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
bool CTrade::OpenOrderWithInstantMode(string symbol,TradeParam &tradeParam,int slippage,datetime expiration,string comment,int &ticket)
  {
   ticket=OrderSend(symbol,tradeParam.orderType,tradeParam.orderVolume,tradeParam.orderOP,slippage,tradeParam.orderSL,tradeParam.orderTP,comment,
                    tradeParam.orderMN,expiration,tradeParam.arrowColor);

   if(ticket>0) // �������� �������� ������
     {
      PlaySound(m_orderOpenSound);
      return (true);
     }

   int error=GetLastError();                                                                     // ��������� �������� ������
   Print("������ �������� ������ ",OrderTypeToString(tradeParam.orderType),": ",error,", price = ",tradeParam.orderOP,", sl = ",tradeParam.orderSL,", tp = ",tradeParam.orderTP);

   switch(error)
     {
      case ERR_LONGS_NOT_ALLOWED:         m_tradeErrorState.isLongAllowed=false;          break;
      case ERR_SHORTS_NOT_ALLOWED:        m_tradeErrorState.isShortAllowed=false;         break;
     }

   return (false);
  }
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| ���������� ���� ������ � ���������� �����������                                                                                                                                          |
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
string CTrade::OrderTypeToString(int orderType)
  {
   switch(orderType)
     {
      case OP_BUY:         return("Buy");
      case OP_SELL:        return("Sell");
      case OP_BUYLIMIT:    return("Buy Limit");
      case OP_SELLLIMIT:   return("Sell Limit");
      case OP_BUYSTOP:     return("Buy Stop");
      case OP_SELLSTOP:    return("Sell Stop");
     }

   return("Unknown order");
  }
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| �������� ����������� ������                                                                                                                                                              |
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
bool CTrade::OpenPendingOrder(GetSymbolInfo &pGetSymbolInfo,TradeParam &tradeParam,string comment,datetime expiration)
  {
   if(!IsOrderParametersCorrect(tradeParam,pGetSymbolInfo.GetStopLevel(),pGetSymbolInfo.GetBid(),pGetSymbolInfo.GetAsk(),pGetSymbolInfo.GetPoint()/10))
      return(false);

   int ticket=-1;
   return (OpenOrderWithInstantMode(pGetSymbolInfo.GetSymbol(), tradeParam, pGetSymbolInfo.GetSlippage(), expiration, comment, ticket));
  }
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| �������� ������ ��� ���� ���������� Market Execution                                                                                                                                     |
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
bool CTrade::OpenOrderWithMarketMode(GetSymbolInfo &pGetSymbolInfo,TradeParam &tradeParam,datetime expiration,string comment)
  {
   double slPrice = tradeParam.orderSL;
   double tpPrice = tradeParam.orderTP;
   tradeParam.orderSL = 0;
   tradeParam.orderTP = 0;
   int ticket=-1;
   if(!OpenOrderWithInstantMode(pGetSymbolInfo.GetSymbol(),tradeParam,pGetSymbolInfo.GetSlippage(),expiration,comment,ticket))
      return false;

   if(!OrderSelect(ticket,SELECT_BY_TICKET) || OrderCloseTime()!=0)
     {
      Alert("��������� ������ ��� ��������� ������ � �������� ������ ������!");
      return(false);
     }

   while(!IsStopped())
     {
      CorrectionOfStops(pGetSymbolInfo,tradeParam.orderType,slPrice,tpPrice);
      if(OrderModify(ticket,0,slPrice,tpPrice,OrderExpiration()))
         return(true);

      Sleep(1000);
     }

   return (false);
  }
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| ��������� ��� ����-������� � ������� � ������������ � �������� ��������� ���������                                                                                                       |
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
void CTrade::CorrectionOfStops(GetSymbolInfo &pGetSymbolInfo,int orderType,double &slPrice,double &tpPrice)
  {
   pGetSymbolInfo.RefreshInfo();
   double stopLevel=pGetSymbolInfo.GetStopLevel();
   double delta=pGetSymbolInfo.GetPoint()/100;
   double tickSize=pGetSymbolInfo.GetTickSize();

   if(orderType==OP_BUY)
     {
      double bid=pGetSymbolInfo.GetBid();

      if(IsFirstMoreThanSecond(stopLevel,bid-slPrice,delta))
         slPrice=NP(bid-stopLevel,tickSize);

      if(IsFirstMoreThanSecond(stopLevel,tpPrice-bid,delta) && tpPrice!=0)
         tpPrice=NP(bid+stopLevel,tickSize);

      return;
     }

   double ask=pGetSymbolInfo.GetAsk();
   if(IsFirstMoreThanSecond(stopLevel,slPrice-ask,delta) && slPrice!=0)
      slPrice=NP(ask+stopLevel,tickSize);

   if(IsFirstMoreThanSecond(stopLevel,ask-tpPrice,delta))
      tpPrice=NP(ask-stopLevel,tickSize);
  }
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| ����������� ���������� ������                                                                                                                                                            |
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
bool CTrade::ModifyOrder(GetSymbolInfo &pGetSymbolInfo,TradeParam &tradeParam,datetime expiration=0)
  {
   if(!SelectOrder(tradeParam.orderTicket))
      return (true);

   double tickSize=pGetSymbolInfo.GetTickSize();
   tradeParam.orderSL = NP(tradeParam.orderSL, tickSize);
   tradeParam.orderTP = NP(tradeParam.orderTP, tickSize);

   if(OrderType()==OP_BUY || OrderType()==OP_SELL)
      return (ModifyDeal(pGetSymbolInfo, tradeParam));

   tradeParam.orderOP=NP(tradeParam.orderOP,tickSize);
   return (ModifyPendingOrder(pGetSymbolInfo, tradeParam, expiration));
  }
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| ������� ����� � ������ ������� �������                                                                                                                                                   |
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
bool CTrade::SelectOrder(int orderTicket)
  {
   return (OrderSelect(orderTicket, SELECT_BY_TICKET) && OrderCloseTime() == 0);
  }
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| ����������� ���������� ��������� ������                                                                                                                                                  |
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
bool CTrade::ModifyDeal(GetSymbolInfo &pGetSymbolInfo,TradeParam &tradeParam)
  {
// ����� �� ����������� � ��������?
   double delta=pGetSymbolInfo.GetPoint()/10;
   if(IsValuesEquals(tradeParam.orderSL,OrderStopLoss(),delta) && IsValuesEquals(tradeParam.orderTP,OrderTakeProfit(),delta))
      return (true);

// �������� ����������� �����������
   double stopLevel=pGetSymbolInfo.GetStopLevel();
   if(OrderType()==OP_BUY)
     {
      tradeParam.orderOP=pGetSymbolInfo.GetBid();
      if(!IsBuyOrderStopsCorrect(tradeParam,pGetSymbolInfo.GetBid(),stopLevel,delta))
         return (false);
     }
   else
     {
      tradeParam.orderOP=pGetSymbolInfo.GetAsk();
      if(!IsSellOrderStopsCorrect(tradeParam,pGetSymbolInfo.GetAsk(),stopLevel,delta))
         return (false);
     }

// ����������� ������
   return (OrderModify(OrderTicket(), 0, tradeParam.orderSL, tradeParam.orderTP, 0, tradeParam.arrowColor));
  }
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| ����������� ���������� ����������� ������                                                                                                                                                |
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
bool CTrade::ModifyPendingOrder(GetSymbolInfo &pGetSymbolInfo,TradeParam &tradeParam,datetime expiration)
  {
// ����� �� ����������� � ��������?
   double delta=pGetSymbolInfo.GetPoint()/10;
   if(IsValuesEquals(tradeParam.orderOP,OrderOpenPrice(),delta) && 
      IsValuesEquals(tradeParam.orderSL, OrderStopLoss(), delta) &&
      IsValuesEquals(tradeParam.orderTP, OrderTakeProfit(), delta))
      return (true);

// �������� �� �����������?
   tradeParam.orderType=OrderType();
   if(!IsOrderParametersCorrect(tradeParam,pGetSymbolInfo.GetStopLevel(),pGetSymbolInfo.GetBid(),pGetSymbolInfo.GetAsk(),pGetSymbolInfo.GetPoint()/10))
      return (false);
   if(expiration!=0 && expiration-TimeCurrent()<MIN_EXPIRATION_TIME)
      return (false);

// ��������������� �����������
   return (OrderModify(OrderTicket(), tradeParam.orderOP, tradeParam.orderSL, tradeParam.orderTP, expiration, tradeParam.arrowColor));
  }
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| �������� ��� �������� ���������� ������                                                                                                                                                  |
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
bool CTrade::DestroyOrder(GetSymbolInfo &pGetSymbolInfo,TradeParam &tradeParam)
  {
// ����� ������
   if(!SelectOrder(tradeParam.orderTicket))
      return (true);

// �������� ����������� ������
   if(OrderType()>=OP_BUYLIMIT)
      return (OrderDelete(tradeParam.orderTicket));

// ����������� ���� �������� ��������� ������
   double price;
   if(OrderType()==OP_BUY)
      price=pGetSymbolInfo.GetBid();
   else
      price=pGetSymbolInfo.GetAsk();

// ����������� ������������ ������
   if(tradeParam.orderVolume<= 0 || tradeParam.orderVolume>OrderLots())
      tradeParam.orderVolume = OrderLots();

// �������� ������
   return (OrderClose(tradeParam.orderTicket, tradeParam.orderVolume, price, pGetSymbolInfo.GetSlippage(), tradeParam.arrowColor));
  }
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| �������� �� ��������� ����� �������� � ��������� �� �� � ������ ������� �������?                                                                                                         |
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
bool CTrade::IsOrderMarketAndWorking(int ticket,int &type)
  {
   if(!SelectOrder(ticket))
      return false;

   type=OrderType();
   return (type == OP_BUY || type == OP_SELL);
  }
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| ��������� �������� �������                                                                                                                                                               |
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
bool CTrade::CloseCounter(TradeParam &tradeParam)
  {
   if(!m_tradeErrorState.isCounterClosingAllowed)
      return false;

   int order1Type,order2Type;
   if(!IsOrderMarketAndWorking(tradeParam.orderTicket,order1Type) || !IsOrderMarketAndWorking(tradeParam.orderTicketCounter,order2Type))
      return false;

   if(order1Type==order2Type)
      return false;

   if(OrderCloseBy(tradeParam.orderTicket,tradeParam.orderTicketCounter,tradeParam.arrowColor))
      return true;

   if(GetLastError()==ERR_INVALID_TRADE_PARAMETERS)
      m_tradeErrorState.isCounterClosingAllowed=false;

   return false;
  }
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| ������� �������� ��������� �������                                                                                                                                                       |
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
TradeErrorState CTrade::GetTradeErrorState(void) const
  {
   return m_tradeErrorState;
  }
// ===================================================================== ������������� �������, �� ���������� ������� ������ =================================================================
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| �������� ������������ ���������� ������ ���� ������. ���� �������� isCorrectionNeeded ����� true, �� ������������ ���������� �������� ���� ��������, ����� � �������                     |
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
bool IsOrderParametersCorrect(TradeParam &tradeParam,double stopLevel,double bid,double ask,double delta,bool isCorrectionNeeded=false)
  {
   switch(tradeParam.orderType)
     {
      case OP_BUY:      tradeParam.orderOP=ask;
      return IsBuyOrderStopsCorrect(tradeParam, bid, stopLevel, delta, isCorrectionNeeded);

      case OP_SELL:     tradeParam.orderOP=bid;
      return IsSellOrderStopsCorrect(tradeParam, bid, stopLevel, delta, isCorrectionNeeded);

      case OP_BUYLIMIT: if(IsFirstMoreThanSecond(stopLevel,ask-tradeParam.orderOP,delta))
        {
         if(!isCorrectionNeeded)
            return false;

         tradeParam.orderOP=ask-stopLevel;
        }

      return IsBuyOrderStopsCorrect(tradeParam, tradeParam.orderOP, stopLevel, delta, isCorrectionNeeded);

      case OP_SELLLIMIT: if(IsFirstMoreThanSecond(stopLevel,tradeParam.orderOP-bid,delta))
        {
         if(!isCorrectionNeeded)
            return false;

         tradeParam.orderOP=bid+stopLevel;
        }

      return IsSellOrderStopsCorrect(tradeParam, tradeParam.orderOP, stopLevel, delta, isCorrectionNeeded);

      case OP_BUYSTOP:  if(IsFirstMoreThanSecond(stopLevel,tradeParam.orderOP-ask,delta))
        {
         if(!isCorrectionNeeded)
            return false;

         tradeParam.orderOP=ask+stopLevel;
        }

      return IsBuyOrderStopsCorrect(tradeParam, tradeParam.orderOP, stopLevel, delta, isCorrectionNeeded);

      case OP_SELLSTOP: if(IsFirstMoreThanSecond(stopLevel,bid-tradeParam.orderOP,delta))
         return false;

         return IsSellOrderStopsCorrect(tradeParam, tradeParam.orderOP, stopLevel, delta, isCorrectionNeeded);
     }

   return false;                                                                                   // ����� �� �������� ����������
  }
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| �������� ������������ ���������� �� ����� � ������� �� ���� �������� Buy ������                                                                                                          |
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
bool IsBuyOrderStopsCorrect(TradeParam &tradeParam,double basePrice,double stopLevel,double delta,bool isCorrectionNeeded=false)
  {
   if(IsFirstMoreThanSecond(stopLevel,tradeParam.orderTP-basePrice,delta) && tradeParam.orderTP!=0.0)
     {
      if(!isCorrectionNeeded)
         return false;

      tradeParam.orderTP=basePrice+stopLevel;
     }

   if(IsFirstMoreThanSecond(stopLevel,basePrice-tradeParam.orderSL,delta))
     {
      if(!isCorrectionNeeded)
         return false;

      tradeParam.orderSL=basePrice-stopLevel;
     }

   return true;
  }
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| �������� ������������ ���������� �� ����� � ������� �� ���� �������� Sell ������                                                                                                         |
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
bool IsSellOrderStopsCorrect(TradeParam &tradeParam,double basePrice,double stopLevel,double delta,bool isCorrectionNeeded=false)
  {
   if(IsFirstMoreThanSecond(stopLevel,basePrice-tradeParam.orderTP,delta))
     {
      if(!isCorrectionNeeded)
         return false;

      tradeParam.orderTP=basePrice-stopLevel;
     }

   if(IsFirstMoreThanSecond(stopLevel,tradeParam.orderSL-basePrice,delta) && tradeParam.orderSL!=0.0)
     {
      if(!isCorrectionNeeded)
         return false;

      tradeParam.orderSL=basePrice+stopLevel;
     }

   return true;
  }
//+------------------------------------------------------------------+
