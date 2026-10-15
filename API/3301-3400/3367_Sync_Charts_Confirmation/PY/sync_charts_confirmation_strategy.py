import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class sync_charts_confirmation_strategy(Strategy):
    """Dual SMA trend confirmation with cooldown."""
    def __init__(self):
        super(sync_charts_confirmation_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 10).SetGreaterThanZero().SetDisplay("Fast Period", "Fast SMA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 40).SetGreaterThanZero().SetDisplay("Slow Period", "Slow SMA period", "Indicators")
        self._cooldown = self.Param("SignalCooldownCandles", 6).SetGreaterThanZero().SetDisplay("Signal Cooldown", "Bars to wait between signals", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candle timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(sync_charts_confirmation_strategy, self).OnReseted()
        self._was_bullish = False
        self._has_prev = False
        self._candles_since_trade = self._cooldown.Value

    def OnStarted2(self, time):
        super(sync_charts_confirmation_strategy, self).OnStarted2(time)
        self._was_bullish = False
        self._has_prev = False
        self._candles_since_trade = self._cooldown.Value

        fast = SimpleMovingAverage()
        fast.Length = self._fast_period.Value
        slow = SimpleMovingAverage()
        slow.Length = self._slow_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(fast, slow, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return

        if self._candles_since_trade < self._cooldown.Value:
            self._candles_since_trade += 1

        is_bullish = fast > slow

        if self._has_prev and is_bullish != self._was_bullish and self._candles_since_trade >= self._cooldown.Value:
            if is_bullish and self.Position <= 0:
                self.BuyMarket()
                self._candles_since_trade = 0
            elif not is_bullish and self.Position >= 0:
                self.SellMarket()
                self._candles_since_trade = 0

        self._was_bullish = is_bullish
        self._has_prev = True

    def CreateClone(self):
        return sync_charts_confirmation_strategy()
