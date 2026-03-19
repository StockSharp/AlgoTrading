import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class macd_signal_strategy(Strategy):
    """
    MACD signal: EMA crossover with StartProtection for TP/trailing SL.
    """

    def __init__(self):
        super(macd_signal_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 9).SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 15).SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._take_profit_ticks = self.Param("TakeProfitTicks", 10.0).SetDisplay("Take Profit", "TP in ticks", "Risk")
        self._trailing_stop_ticks = self.Param("TrailingStopTicks", 25.0).SetDisplay("Trailing Stop", "Trailing SL in ticks", "Risk")
        self._cooldown_bars = self.Param("CooldownBars", 10).SetDisplay("Cooldown", "Bars between signals", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))).SetDisplay("Candle Type", "Candles", "General")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._initialized = False
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(macd_signal_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._initialized = False
        self._cooldown = 0

    def OnStarted(self, time):
        super(macd_signal_strategy, self).OnStarted(time)
        fast = ExponentialMovingAverage()
        fast.Length = self._fast_period.Value
        slow = ExponentialMovingAverage()
        slow.Length = self._slow_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, self._process_candle).Start()
        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        tp = float(self._take_profit_ticks.Value) * step
        sl = float(self._trailing_stop_ticks.Value) * step
        if tp > 0 or sl > 0:
            self.StartProtection(
                Unit(tp, UnitTypes.Absolute) if tp > 0 else Unit(),
                Unit(sl, UnitTypes.Absolute) if sl > 0 else Unit(),
                True
            )
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        fast = float(fast_val)
        slow = float(slow_val)
        if not self._initialized:
            self._prev_fast = fast
            self._prev_slow = slow
            self._initialized = True
            return
        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_fast = fast
            self._prev_slow = slow
            return
        cross_up = self._prev_fast <= self._prev_slow and fast > slow
        cross_down = self._prev_fast >= self._prev_slow and fast < slow
        if cross_up and self.Position <= 0:
            self.BuyMarket()
            self._cooldown = self._cooldown_bars.Value
        elif cross_down and self.Position >= 0:
            self.SellMarket()
            self._cooldown = self._cooldown_bars.Value
        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return macd_signal_strategy()
