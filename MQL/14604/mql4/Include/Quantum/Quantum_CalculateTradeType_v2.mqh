// Класс эксперта Quantum_v2_Expert. Используется для определения типа необходимой торговой операции и ее параметров
#property copyright "Scriptong"
#property link      "http://advancetools.net"
#property strict

#include <Common\Common_MathUtils.mqh>
#include <Common\Common_GetSymbolInfo.mqh>
#include <Common\Common_Trade.mqh>
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
enum ENUM_TRADE_TYPE
  {
   TRADE_NONE=0,
   TRADE_OPEN,
   TRADE_MODIFY,
   TRADE_DESTROY,
   TRADE_CLOSEBY,
   TRADE_FATAL_ERROR
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
enum ENUM_SIGNAL_TYPE
  {
   SIGNAL_NONE,
   SIGNAL_BUY_OPEN,
   SIGNAL_SELL_OPEN,
   SIGNAL_BUY_CLOSE,
   SIGNAL_SELL_CLOSE
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
struct OrderInfo
  {
   int               type;
   int               ticket;
   datetime          openTime;
   double            volume;
   double            openPrice;
   double            tp;
   double            sl;

                     OrderInfo()
     {
      Init();
     }

   void Init()
     {
      type=-1;
      ticket=-1;
      openTime=0;
      volume=0.0;
      openPrice=0.0;
      tp = 0.0;
      sl = 0.0;
     }
  };
// Класс CalculateTradeType
class CalculateTradeType
  {
   // Неизменяемые члены класса
   bool              m_isRussianLang;
   double            m_staticLots;
   double            m_dynamicLots;
   uint              m_tpSize;
   uint              m_slShift;

   uint              m_periodK;
   uint              m_periodD;
   uint              m_slowing;
   double            m_highLevel;
   double            m_lowLevel;
   double            m_highCloseLevel;
   double            m_lowCloseLevel;
   uint              m_extremumRank;

   int               m_magicNumber;

   // Изменяемые члены класса

   int               m_lastExtBarIndex;                                                         // Индес бара, на котором зарегистрирован экстремум по Quantum
   datetime          m_lastSignalTime;
   datetime          m_lastOpenOrderTime;
   ENUM_SIGNAL_TYPE  m_lastSignalType;

   OrderInfo         m_curOrder;

public:
                     CalculateTradeType(double staticLots,double dynamicLots,uint tpSize,uint slShift,uint periodK,uint periodD,uint slowing,
                                                          double highLevel,double lowLevel,double highCloseLevel,double lowCloseLevel,uint extremumRank,int magicNumber);

                    ~CalculateTradeType(void);
   ENUM_TRADE_TYPE   GetTradeType(TradeParam &tradeParam,const SymbolInfo &symbolInfo,const TradeErrorState &tradeErrorState);

private:

   bool              FindExpertOrders(const SymbolInfo &symbolInfo);

   void              CalculateSignal();
   ENUM_SIGNAL_TYPE  GetStochasticSignal(void);
   bool              IsLocalMaximum(int barIndex);
   bool              IsLocalMinimum(int barIndex);

   ENUM_TRADE_TYPE   OpenMarketOrder(TradeParam &tradeParam,const SymbolInfo &symbolInfo,const TradeErrorState &tradeErrorState);
   ENUM_TRADE_TYPE   OpenSpecifiedOrder(TradeParam &tradeParam,const SymbolInfo &symbolInfo,const int orderType);
   double            GetLots(const SymbolInfo &symbolInfo) const;
  };
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Конструктор                                                                                                                                                                                       |
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
CalculateTradeType::CalculateTradeType(double staticLots,double dynamicLots,uint tpSize,uint slShift,uint periodK,uint periodD,uint slowing,double highLevel,double lowLevel,
                                       double highCloseLevel,double lowCloseLevel,uint extremumRank,int magicNumber)
   : m_staticLots(staticLots)
   ,m_dynamicLots(dynamicLots)
   ,m_tpSize(tpSize)
   ,m_slShift(slShift)
   ,m_periodK(periodK)
   ,m_periodD(periodD)
   ,m_slowing(slowing)
   ,m_highLevel(highLevel)
   ,m_lowLevel(lowLevel)
   ,m_highCloseLevel(highCloseLevel)
   ,m_lowCloseLevel(lowCloseLevel)
   ,m_extremumRank(extremumRank)
   ,m_magicNumber(magicNumber)
   ,m_lastSignalTime(0)
   ,m_lastExtBarIndex(-1)
   ,m_isRussianLang(TerminalInfoString(TERMINAL_LANGUAGE)=="Russian")
  {
  }
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Деструктор                                                                                                                                                                                        |
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
CalculateTradeType::~CalculateTradeType(void)
  {
  }
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Определение типа торговой операции, которую необходимо совершить                                                                                                                                  |
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
ENUM_TRADE_TYPE CalculateTradeType::GetTradeType(TradeParam &tradeParam,const SymbolInfo &symbolInfo,const TradeErrorState &tradeErrorState)
  {
// Поиск ордеров эксперта
   if(!FindExpertOrders(symbolInfo))
      return TRADE_FATAL_ERROR;

// Проверка наличия торгового сигнала
   CalculateSignal();
   if(m_lastSignalType==SIGNAL_NONE)
      return TRADE_NONE;

// Закрытие текущего ордера
   if((m_curOrder.type==OP_BUY && (m_lastSignalType==SIGNAL_BUY_CLOSE || m_lastSignalType==SIGNAL_SELL_OPEN)) || 
      (m_curOrder.type==OP_SELL && (m_lastSignalType==SIGNAL_SELL_CLOSE || m_lastSignalType==SIGNAL_BUY_OPEN)))
     {
      tradeParam.orderTicket = m_curOrder.ticket;
      tradeParam.orderVolume = 0.0;
      return TRADE_DESTROY;
     }

// Открытие ордера
   if(m_curOrder.type<0 && (m_lastSignalType==SIGNAL_BUY_OPEN || m_lastSignalType==SIGNAL_SELL_OPEN))
      return OpenMarketOrder(tradeParam, symbolInfo, tradeErrorState);

   return TRADE_NONE;
  }
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Определение наличия ордеров, открытых экспертом                                                                                                                                                   |
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
bool CalculateTradeType::FindExpertOrders(const SymbolInfo &symbolInfo)
  {
   m_curOrder.Init();

   for(int i=OrdersTotal()-1; i>=0; i--)
     {
      if(!OrderSelect(i,SELECT_BY_POS))
         continue;

      if(OrderSymbol()!=Symbol())
         continue;

      if(m_magicNumber!=OrderMagicNumber())
         continue;

      if(m_curOrder.ticket>0)
        {
         Alert(WindowExpertName(),m_isRussianLang? ": обнаружено два и более ордера эксперта по символу "+_Symbol+". Эксперт отключен." :
               ": two or more orders of expert at symbol "+_Symbol+" found. Expert is turned off.");
         return false;
        }

      m_curOrder.type=OrderType();
      m_curOrder.ticket = OrderTicket();
      m_curOrder.volume = OrderLots();
      m_curOrder.tp = OrderTakeProfit();
      m_curOrder.sl = OrderStopLoss();
      m_curOrder.openPrice= OrderOpenPrice();
      m_curOrder.openTime = OrderOpenTime();

      m_lastOpenOrderTime=OrderOpenTime();
     }

   return true;
  }
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Генерация торгового сигнала                                                                                                                                                                       |
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
void CalculateTradeType::CalculateSignal()
  {
   datetime curCandle=iTime(NULL,0,0);
   if(m_lastSignalTime==curCandle)
      return;

   m_lastSignalTime=curCandle;

// Сигнал стохастика
   m_lastSignalType=GetStochasticSignal();
   if(m_lastSignalType!=SIGNAL_BUY_OPEN && m_lastSignalType!=SIGNAL_SELL_OPEN)
      return;

// Поиск последнего сигнала Quantum на участке нахождения стохастика в зоне перекупленности или перепроданности  
   int total=Bars-int(m_periodK);
   for(m_lastExtBarIndex=3; m_lastExtBarIndex<total; m_lastExtBarIndex++)
     {
      double stoch=iStochastic(NULL,0,m_periodK,m_periodD,m_slowing,MODE_EMA,0,MODE_MAIN,m_lastExtBarIndex);
      if(stoch>m_lowLevel && stoch<m_highLevel)
         break;

      if((m_lastSignalType==SIGNAL_BUY_OPEN && IsLocalMinimum(m_lastExtBarIndex)) || 
         (m_lastSignalType==SIGNAL_SELL_OPEN && IsLocalMaximum(m_lastExtBarIndex)))
         return;
     }

   m_lastSignalType=SIGNAL_NONE;
  }
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Расчет сигнала по стохастику                                                                                                                                                                      |
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
ENUM_SIGNAL_TYPE CalculateTradeType::GetStochasticSignal()
  {
   double stoch1 = iStochastic(NULL, 0, m_periodK, m_periodD, m_slowing, MODE_EMA, 0, MODE_MAIN, 1);
   double stoch2 = iStochastic(NULL, 0, m_periodK, m_periodD, m_slowing, MODE_EMA, 0, MODE_MAIN, 2);

   if(stoch1>m_lowLevel && stoch2<m_lowLevel)
      return SIGNAL_BUY_OPEN;

   if(stoch1<m_highLevel && stoch2>m_highLevel)
      return SIGNAL_SELL_OPEN;

   if(stoch1>m_highCloseLevel)
      return SIGNAL_BUY_CLOSE;

   if(stoch1<m_lowCloseLevel)
      return SIGNAL_SELL_CLOSE;

   return SIGNAL_NONE;
  }
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Является ли указанный бар локальным минимумом?                                                                                                                                                    |
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
bool CalculateTradeType::IsLocalMinimum(int barIndex)
  {
   return iLowest(NULL, 0, MODE_LOW, m_extremumRank, barIndex) == barIndex;
  }
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Является ли указанный бар локальным максимумом?                                                                                                                                                   |
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
bool CalculateTradeType::IsLocalMaximum(int barIndex)
  {
   return iHighest(NULL, 0, MODE_HIGH, m_extremumRank, barIndex) == barIndex;
  }
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Открытие ордеров                                                                                                                                                                                  |
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
ENUM_TRADE_TYPE CalculateTradeType::OpenMarketOrder(TradeParam &tradeParam,const SymbolInfo &symbolInfo,const TradeErrorState &tradeErrorState)
  {
// Открытие не требуется, если сигнал не совпадает с разрешенным направлением торговли
   if((m_lastSignalType==SIGNAL_BUY_OPEN && !tradeErrorState.isLongAllowed) || 
      (m_lastSignalType==SIGNAL_SELL_OPEN && !tradeErrorState.isShortAllowed))
      return TRADE_NONE;

// Открытие ордера на текущей свече уже производилось
   if(m_lastOpenOrderTime>=iTime(NULL,0,0))
      return TRADE_NONE;

// Открытие ордера, номер которого отсутствует в списке рабочих ордеров эксперта
   return OpenSpecifiedOrder(tradeParam, symbolInfo, (m_lastSignalType == SIGNAL_BUY_OPEN)? OP_BUY : OP_SELL);
  }
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Открытие указанного ордера                                                                                                                                                                        |
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
ENUM_TRADE_TYPE CalculateTradeType::OpenSpecifiedOrder(TradeParam &tradeParam,const SymbolInfo &symbolInfo,const int orderType)
  {
   tradeParam.orderMN=m_magicNumber;
   tradeParam.orderType=orderType;

// Расчет цены Stop Loss ордера
   if(orderType==OP_BUY)
      tradeParam.orderSL=iLow(NULL,0,m_lastExtBarIndex)-m_slShift*symbolInfo.point;
   if(orderType==OP_SELL)
      tradeParam.orderSL=iHigh(NULL,0,m_lastExtBarIndex)+m_slShift*symbolInfo.point;

// Расчет цены Take Profit
   if(orderType==OP_BUY)
      tradeParam.orderTP=symbolInfo.ask+m_tpSize*symbolInfo.point;
   if(orderType==OP_SELL)
      tradeParam.orderTP=symbolInfo.bid-m_tpSize*symbolInfo.point;

   if(m_tpSize==0)
      tradeParam.orderTP=0.0;

   tradeParam.orderVolume=GetLots(symbolInfo);

   if(!IsOrderParametersCorrect(tradeParam,symbolInfo.stopLevel,symbolInfo.bid,symbolInfo.ask,symbolInfo.point/10,true))
      return TRADE_NONE;

   return TRADE_OPEN;
  }
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Расчет объема ордера                                                                                                                                                                              |
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
double CalculateTradeType::GetLots(const SymbolInfo &symbolInfo) const
  {
   double lots=0.0;

// Фиксированный объем ордера
   if(m_staticLots<=0.0)
     {
      double marginRequired=MarketInfo(Symbol(),MODE_MARGINREQUIRED);
      if(marginRequired==0)
         return 0;

      lots=(m_dynamicLots/100)*AccountBalance()/marginRequired;
     }
   else
      lots=m_staticLots;

// Динамический объем
   return VolumeCast(lots, symbolInfo.volumeMin, symbolInfo.volumeMax, symbolInfo.volumeStep);
  }
//+------------------------------------------------------------------+
