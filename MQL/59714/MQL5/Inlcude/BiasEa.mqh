//+------------------------------------------------------------------+
//|                                                     HTF Bots.mqh |
//|                                             Copyright 2025, Leo. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2025, Leo."
#property link      "https://www.mql5.com"
#property strict


#include <Estrategias bases.mqh>
#include <Bias.mqh>


//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CRiskManagemet *risk;

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
class CStrategy : public CStrategyBase
 {
private:
  CBias*             bias;

public:
                     CStrategy(ulong magic_number_, string symbol_, ENUM_TIMEFRAMES timeframe_  = PERIOD_CURRENT
            , long chart_id_ = 0, int subwindow_ = 0, ulong max_deviation_ = NO_MAX_DEVIATION_DEFINIED);

                    ~CStrategy(void);
  void               OnNewBar();
 };
//+------------------------------------------------------------------+
CStrategy::~CStrategy(void)
 {
  delete bias;
 }

//+------------------------------------------------------------------+
CStrategy::CStrategy(ulong magic_number_, string symbol_, ENUM_TIMEFRAMES timeframe_  = PERIOD_CURRENT
                     , long chart_id_ = 0, int subwindow_ = 0, ulong max_deviation_ = NO_MAX_DEVIATION_DEFINIED)
  :   CStrategyBase(magic_number_, symbol_, timeframe_, chart_id_, subwindow_, max_deviation_)
 {
  bias = new CBias(Spanish);
  bias.Set(PERIOD_CURRENT, this.symbol, true);
 }
//+------------------------------------------------------------------+
void CStrategy::OnNewBar(void)
 {
  ENUM_TYPE_BIAS bias_type = bias.GetBias();
  SymbolInfoTick(this.symbol, this.tick);
  if(risk.GetPositionsTotal() > 0)
    return;

  if((bias_type == BIAS_BEAR_REVERSAL || bias_type == BIAS_BULL_CONTINUATION) && (operate_flags & FLAG_OPERATE_BUY) != 0)
   {
    double sl = GetSL(tick.ask, POSITION_TYPE_BUY);
    double tp = GetTP(tick.ask, POSITION_TYPE_BUY);
    risk.SetStopLoss(tick.ask - sl);

    double lot_size = LOT_SIZE > 0.00 ? LOT_SIZE : risk.GetLote(ORDER_TYPE_BUY);

    trade.Buy(lot_size, this.symbol, tick.ask, sl, tp, "EA Buy");
    return;
   }

  if((bias_type == BIAS_BULL_REVERSAL || bias_type == BIAS_BEAR_CONTINUATION) && (operate_flags & FLAG_OPERATE_SELL) != 0)
   {
    double sl = GetSL(tick.bid, POSITION_TYPE_SELL);
    double tp = GetTP(tick.bid, POSITION_TYPE_SELL);
    risk.SetStopLoss(sl - tick.bid);

    double lot_size = LOT_SIZE > 0.00 ? LOT_SIZE : risk.GetLote(ORDER_TYPE_SELL);
    trade.Sell(lot_size, this.symbol, tick.bid, sl, tp, "EA Sell");
   }
 }
//+------------------------------------------------------------------+
