import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import AverageTrueRange, SmoothedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class volatile_action_strategy(Strategy):
    def __init__(self):
        super(volatile_action_strategy, self).__init__()
        self._volatility_coef = self.Param("VolatilityCoef", 1.0) \
            .SetDisplay("Volatility Coef", "ATR1 multiplier against base ATR", "General")
        self._atr_period = self.Param("AtrPeriod", 23) \
            .SetDisplay("ATR Period", "Base ATR period", "General")
        self._stop_loss_pct = self.Param("StopLossPct", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
        self._take_profit_pct = self.Param("TakeProfitPct", 3.0) \
            .SetDisplay("Take Profit %", "Take profit percentage", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe for main calculation", "General")
        self._atr1 = None
        self._atr_base = None
        self._jaw = None
        self._teeth = None
        self._lips = None

    @property
    def volatility_coef(self):
        return self._volatility_coef.Value

    @property
    def atr_period(self):
        return self._atr_period.Value

    @property
    def stop_loss_pct(self):
        return self._stop_loss_pct.Value

    @property
    def take_profit_pct(self):
        return self._take_profit_pct.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(volatile_action_strategy, self).OnReseted()
        self._atr1 = None
        self._atr_base = None
        self._jaw = None
        self._teeth = None
        self._lips = None

    def OnStarted(self, time):
        super(volatile_action_strategy, self).OnStarted(time)
        self._atr1 = AverageTrueRange()
        self._atr1.Length = 1
        self._atr_base = AverageTrueRange()
        self._atr_base.Length = self.atr_period
        self._jaw = SmoothedMovingAverage()
        self._jaw.Length = 13
        self._teeth = SmoothedMovingAverage()
        self._teeth.Length = 8
        self._lips = SmoothedMovingAverage()
        self._lips.Length = 5
        self.Indicators.Add(self._atr1)
        self.Indicators.Add(self._atr_base)
        self.Indicators.Add(self._jaw)
        self.Indicators.Add(self._teeth)
        self.Indicators.Add(self._lips)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()
        self.StartProtection(
            takeProfit=Unit(self.take_profit_pct, UnitTypes.Percent),
            stopLoss=Unit(self.stop_loss_pct, UnitTypes.Percent),
            useMarketOrders=True)
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        atr1_val = self._atr1.Process(candle)
        atr_base_val = self._atr_base.Process(candle)

        median = (float(candle.HighPrice) + float(candle.LowPrice)) / 2.0
        jaw_val = self._jaw.Process(median, candle.OpenTime, True)
        teeth_val = self._teeth.Process(median, candle.OpenTime, True)
        lips_val = self._lips.Process(median, candle.OpenTime, True)

        if (not atr1_val.IsFormed or not atr_base_val.IsFormed or
                not jaw_val.IsFormed or not teeth_val.IsFormed or not lips_val.IsFormed):
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        atr1 = float(atr1_val.ToDecimal())
        atr_base = float(atr_base_val.ToDecimal())
        jaw = float(jaw_val.ToDecimal())
        teeth = float(teeth_val.ToDecimal())
        lips = float(lips_val.ToDecimal())

        bull_gator = lips > teeth and teeth > jaw
        bear_gator = lips < teeth and teeth < jaw

        vol_breakout = atr_base > 0 and atr1 > float(self.volatility_coef) * atr_base

        if self.Position == 0:
            if vol_breakout and bull_gator and float(candle.ClosePrice) > float(candle.OpenPrice):
                self.BuyMarket()
            elif vol_breakout and bear_gator and float(candle.ClosePrice) < float(candle.OpenPrice):
                self.SellMarket()

    def CreateClone(self):
        return volatile_action_strategy()
