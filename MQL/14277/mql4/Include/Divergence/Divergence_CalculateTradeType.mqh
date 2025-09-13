// Класс эксперта Divergence_Expert. Используется для определения типа необходимой торговой операции и ее параметров
#property copyright "Scriptong"
#property link      "http://advancetools.net"
#property strict

#include <Common\Common_Trade.mqh>
#include <Divergence\Divergence_CalculateSignal.mqh>

#define OP_NONE                  -1                                                                // Идентификатор неопределенного типа ордера
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
   SIGNAL_BUY,
   SIGNAL_SELL
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
struct OrderInfo
  {
   int               ticket;
   int               type;
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
      ticket=-1;
      type=OP_NONE;
      openTime=0;
      volume=0.0;
      openPrice=0.0;
      tp = 0.0;
      sl = 0.0;
     }
  };
// Класс CCalculateTradeType
class CCalculateTradeType
  {
   // Неизменяемые члены класса
   bool              m_isDivergenceInit;
   bool              m_isRussianLang;
   int               m_magicNumber;

   double            m_staticLots;
   double            m_dynamicLots;
   double            m_tpToSLRatio;
   double            m_slOffset;

   // Изменяемые члены класса
   datetime          m_lastProcessedSignal;                                                        // Время открытия последнего рыночного ордера
   datetime          m_signalTime;                                                                 // Время расчета последнего сигнала
   ENUM_SIGNAL_TYPE  m_signalType;                                                                 // Тип последнего рассчитанного сигнала

   OrderInfo         m_curOrder;                                                                   // Информация о рабочем ордере эксперта
   CDivergence       m_divergence;

public:
                     CCalculateTradeType(double staticLots,double dynamicLots,double tpToSLRatio,double slOffset,int magicNumber,ENUM_INDICATOR_TYPE indicatorType,int divergenceDepth,
                                                           int barsPeriod1,int barsPeriod2,int barsPeriod3,ENUM_APPLIED_PRICE indAppliedPrice,ENUM_MA_METHOD indMAMethod,int findExtInterval,
                                                           ENUM_MARKET_APPLIED_PRICE marketAppliedPrice,string customName,int customBuffer,ENUM_CUSTOM_PARAM_CNT customParamCnt,double customParam1,
                                                           double customParam2,double customParam3,double customParam4,double customParam5,double customParam6,double customParam7,double customParam8,
                                                           double customParam9,double customParam10,double customParam11,double customParam12,double customParam13,double customParam14,double customParam15,
                                                           double customParam16,double customParam17,double customParam18,double customParam19,double customParam20,bool coincidenceCharts,bool excludeOverlaps,
                                                           bool useClassA,bool useClassB,bool useClassC,bool useHidden);

   ENUM_TRADE_TYPE   GetTradeType(TradeParam &tradeParam,const SymbolInfo &symbolInfo,const TradeErrorState &tradeErrorState);

private:
   void              FindLastOrder();

   bool              FindExpertOrders(void);

   ENUM_TRADE_TYPE   OpenSpecifiedOrder(TradeParam &tradeParam,const SymbolInfo &symbolInfo,int orderType,double sl,double tp) const;
   ENUM_TRADE_TYPE   ProcessSignal(TradeParam &tradeParam,const SymbolInfo &symbolInfo,const TradeErrorState &tradeState,const DivergenceData &divData);
   double            GetLots(const SymbolInfo &symbolInfo) const;
  };
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Конструктор                                                                                                                                                                                       |
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
CCalculateTradeType::CCalculateTradeType(double staticLots,double dynamicLots,double tpToSLRatio,double slOffset,int magicNumber,ENUM_INDICATOR_TYPE indicatorType,int divergenceDepth,
                                         int barsPeriod1,int barsPeriod2,int barsPeriod3,ENUM_APPLIED_PRICE indAppliedPrice,ENUM_MA_METHOD indMAMethod,int findExtInterval,
                                         ENUM_MARKET_APPLIED_PRICE marketAppliedPrice,string customName,int customBuffer,ENUM_CUSTOM_PARAM_CNT customParamCnt,double customParam1,
                                         double customParam2,double customParam3,double customParam4,double customParam5,double customParam6,double customParam7,double customParam8,
                                         double customParam9,double customParam10,double customParam11,double customParam12,double customParam13,double customParam14,double customParam15,
                                         double customParam16,double customParam17,double customParam18,double customParam19,double customParam20,bool coincidenceCharts,bool excludeOverlaps,
                                         bool useClassA,bool useClassB,bool useClassC,bool useHidden)
   : m_divergence(_Symbol,PERIOD_CURRENT,indicatorType,divergenceDepth,barsPeriod1,barsPeriod2,barsPeriod3,indAppliedPrice,indMAMethod,
                     findExtInterval,marketAppliedPrice,customName,customBuffer,customParamCnt,customParam1,customParam2,customParam3,
                     customParam4,customParam5,customParam6,customParam7,customParam8,customParam9,customParam10,customParam11,customParam12,
                     customParam13,customParam14,customParam15,customParam16,customParam17,customParam18,customParam19,customParam20,
                     coincidenceCharts,excludeOverlaps,useClassA,clrNONE,clrNONE,useClassB,clrNONE,clrNONE,useClassC,clrNONE,clrNONE,useHidden,
                     clrNONE,clrNONE,1)
   ,m_staticLots(staticLots)
   ,m_dynamicLots(dynamicLots)
   ,m_tpToSLRatio(tpToSLRatio)
   ,m_slOffset(slOffset)
   ,m_magicNumber(magicNumber)
   ,m_lastProcessedSignal(0)
   ,m_signalTime(0)
   ,m_signalType(SIGNAL_NONE)
   ,m_isRussianLang(TerminalInfoString(TERMINAL_LANGUAGE)=="Russian")
   ,m_isDivergenceInit(false)
  {
   m_isDivergenceInit=m_divergence.Init();
   FindLastOrder();
  }
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Поиск последней сделки, совершенной экспертом, в истории счета                                                                                                                                    |
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
void CCalculateTradeType::FindLastOrder(void)
  {
   for(int i=OrdersHistoryTotal()-1; i>=0; i--)
     {
      if(!OrderSelect(i,SELECT_BY_POS,MODE_HISTORY))
         continue;

      if(OrderSymbol()!=Symbol())
         continue;

      if(OrderMagicNumber()!=m_magicNumber)
         continue;

      if(m_lastProcessedSignal<OrderOpenTime())
         m_lastProcessedSignal=OrderOpenTime();
     }
  }
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Определение типа торговой операции, которую необходимо совершить                                                                                                                                  |
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
ENUM_TRADE_TYPE CCalculateTradeType::GetTradeType(TradeParam &tradeParam,const SymbolInfo &symbolInfo,const TradeErrorState &tradeErrorState)
  {
// Проверка успешной инициализации класса CDivergence
   if(!m_isDivergenceInit)
      return TRADE_FATAL_ERROR;

// Поиск ордеров эксперта
   if(!FindExpertOrders())
      return TRADE_FATAL_ERROR;

// Есть ли сигнал?
   DivergenceData divData;
   m_divergence.ProcessTick(divData);
   if(divData.type==DIV_TYPE_NONE)
      return TRADE_NONE;

// Обработка сигнала - открытие или закрытие ордера
   return ProcessSignal(tradeParam, symbolInfo, tradeErrorState, divData);
  }
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Определение наличия ордеров, открытых экспертом                                                                                                                                                   |
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
bool CCalculateTradeType::FindExpertOrders()
  {
   m_curOrder.Init();

   for(int i=OrdersTotal()-1; i>=0; i--)
     {
      if(!OrderSelect(i,SELECT_BY_POS))
         continue;

      if(OrderSymbol()!=Symbol())
         continue;

      if(OrderMagicNumber()!=m_magicNumber)
         continue;

      if(OrderType()!=OP_BUY && OrderType()!=OP_SELL)
         continue;

      if(m_curOrder.ticket>0)
        {
         Alert(WindowExpertName(),m_isRussianLang? ": обнаружено два и более ордера эксперта по символу "+_Symbol+". Эксперт отключен." :
               ": two or more orders of expert at symbol "+_Symbol+" was found. Expert is turned off.");
         return false;
        }

      m_curOrder.ticket=OrderTicket();
      m_curOrder.type=OrderType();
      m_curOrder.volume=OrderLots();
      m_curOrder.openPrice= OrderOpenPrice();
      m_curOrder.openTime = OrderOpenTime();
      m_curOrder.sl = OrderStopLoss();
      m_curOrder.tp = OrderTakeProfit();

      if(m_lastProcessedSignal<OrderOpenTime())
         m_lastProcessedSignal=OrderOpenTime();
     }

   return true;
  }
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Проверка необходимости открытия нового ордера на новой свече                                                                                                                                      |
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
ENUM_TRADE_TYPE CCalculateTradeType::ProcessSignal(TradeParam &tradeParam,const SymbolInfo &symbolInfo,const TradeErrorState &tradeState,const DivergenceData &divData)
  {
// На этой свече уже было открытие ордера
   if(m_lastProcessedSignal>=iTime(NULL,0,0))
      return TRADE_NONE;

// Если ордер, подобный текущей дивергенции, уже пристуствует, то ничего не предпринимается
   if((m_curOrder.type==OP_BUY && divData.type==DIV_TYPE_BULLISH) || (m_curOrder.type==OP_SELL && divData.type==DIV_TYPE_BEARISH))
      return TRADE_NONE;

// Нужно ли закрыть текущий ордер?
   if((m_curOrder.type==OP_BUY && divData.type==DIV_TYPE_BEARISH) || (m_curOrder.type==OP_SELL && divData.type==DIV_TYPE_BULLISH))
     {
      tradeParam.orderTicket=m_curOrder.ticket;
      return TRADE_DESTROY;
     }

// Открытие ордера Buy
   if(divData.type==DIV_TYPE_BULLISH && tradeState.isLongAllowed)
     {
      double sl = divData.extremePrice - m_slOffset;
      double tp = symbolInfo.ask + m_tpToSLRatio * (symbolInfo.ask - sl);
      return OpenSpecifiedOrder(tradeParam, symbolInfo, OP_BUY, sl, tp);
     }

// Открытие ордера Sell
   if(divData.type==DIV_TYPE_BEARISH && tradeState.isShortAllowed)
     {
      double sl = divData.extremePrice + m_slOffset + symbolInfo.spread;
      double tp = symbolInfo.bid - m_tpToSLRatio * (sl - symbolInfo.bid);
      return OpenSpecifiedOrder(tradeParam, symbolInfo, OP_SELL, sl, tp);
     }

   return TRADE_NONE;
  }
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Открытие первого ордера эксперта                                                                                                                                                                  |
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
ENUM_TRADE_TYPE CCalculateTradeType::OpenSpecifiedOrder(TradeParam &tradeParam,const SymbolInfo &symbolInfo,int orderType,double sl,double tp) const
  {
   tradeParam.orderVolume=GetLots(symbolInfo);
   tradeParam.orderType=orderType;
   tradeParam.orderSL = sl;
   tradeParam.orderTP = tp;
   tradeParam.orderMN = m_magicNumber;


   if(!IsOrderParametersCorrect(tradeParam,symbolInfo.stopLevel,symbolInfo.bid,symbolInfo.ask,true))
      return TRADE_NONE;

   return TRADE_OPEN;
  }
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| Расчет объема ордера                                                                                                                                                                              |
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
double CCalculateTradeType::GetLots(const SymbolInfo &symbolInfo) const
  {
   double lots=0.0;

// Фиксированный объем ордера
   if(m_staticLots>0.0)
      lots=m_staticLots;
   else
      lots=AccountInfoDouble(ACCOUNT_BALANCE)/1000*m_dynamicLots*0.01;

// Динамический объем
   return VolumeCast(lots, symbolInfo.volumeMin, symbolInfo.volumeMax, symbolInfo.volumeStep);
  }
//+------------------------------------------------------------------+
