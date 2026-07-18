import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, UnitTypes, Unit
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class risk_monitor_strategy(Strategy):
    def __init__(self):
        super(risk_monitor_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 10).SetGreaterThanZero().SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 30).SetGreaterThanZero().SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._tp_points = self.Param("TakeProfitPoints", 500.0).SetNotNegative().SetDisplay("Take Profit", "TP distance", "Risk")
        self._sl_points = self.Param("StopLossPoints", 500.0).SetNotNegative().SetDisplay("Stop Loss", "SL distance", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candle timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(risk_monitor_strategy, self).OnReseted()
        self._prev_fast = None
        self._prev_slow = None
        self._entry_price = 0

    def OnStarted2(self, time):
        super(risk_monitor_strategy, self).OnStarted2(time)
        self._prev_fast = None
        self._prev_slow = None
        self._entry_price = 0

        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self._fast_period.Value
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self._slow_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(fast_ema, slow_ema, self.OnProcess).Start()

        tp_val = float(self._tp_points.Value)
        sl_val = float(self._sl_points.Value)
        tp = Unit(tp_val, UnitTypes.Absolute) if tp_val > 0 else None
        sl = Unit(sl_val, UnitTypes.Absolute) if sl_val > 0 else None
        self.StartProtection(tp, sl)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_ema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return

        pf = self._prev_fast
        ps = self._prev_slow
        self._prev_fast = fast_val
        self._prev_slow = slow_val

        if pf is None or ps is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        prev_diff = pf - ps
        curr_diff = fast_val - slow_val

        if prev_diff <= 0 and curr_diff > 0:
            if self.Position < 0:
                self.BuyMarket(abs(self.Position))
            if self.Position == 0:
                self.BuyMarket(self.Volume)
        elif prev_diff >= 0 and curr_diff < 0:
            if self.Position > 0:
                self.SellMarket(self.Position)
            if self.Position == 0:
                self.SellMarket(self.Volume)

    def CreateClone(self):
        return risk_monitor_strategy()
