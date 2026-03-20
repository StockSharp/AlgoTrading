import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class two_x_spy_tips_strategy(Strategy):
    def __init__(self):
        super(two_x_spy_tips_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._fast_sma_length = self.Param("FastSmaLength", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast SMA", "Fast SMA period", "Indicators")
        self._slow_sma_length = self.Param("SlowSmaLength", 200) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow SMA", "Slow SMA period", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 15) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value
    @property
    def fast_sma_length(self):
        return self._fast_sma_length.Value
    @property
    def slow_sma_length(self):
        return self._slow_sma_length.Value
    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    def OnReseted(self):
        super(two_x_spy_tips_strategy, self).OnReseted()
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(two_x_spy_tips_strategy, self).OnStarted(time)
        self._fast_sma = SimpleMovingAverage()
        self._fast_sma.Length = self.fast_sma_length
        self._slow_sma = SimpleMovingAverage()
        self._slow_sma.Length = self.slow_sma_length

        subscription = self.SubscribeCandles(self.candle_type)
        subscription \
            .Bind(self._fast_sma, self._slow_sma, self.OnProcess) \
            .Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._fast_sma)
            self.DrawIndicator(area, self._slow_sma)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return
        if not self._fast_sma.IsFormed or not self._slow_sma.IsFormed:
            return
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        price = float(candle.ClosePrice)
        fast_v = float(fast_val)
        slow_v = float(slow_val)

        if price > fast_v and price > slow_v and fast_v > slow_v and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(abs(self.Position))
            self.BuyMarket()
            self._cooldown_remaining = self.cooldown_bars
        elif price < fast_v and price < slow_v and fast_v < slow_v and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(abs(self.Position))
            self.SellMarket()
            self._cooldown_remaining = self.cooldown_bars
        elif self.Position > 0 and price < fast_v:
            self.SellMarket(abs(self.Position))
            self._cooldown_remaining = self.cooldown_bars
        elif self.Position < 0 and price > fast_v:
            self.BuyMarket(abs(self.Position))
            self._cooldown_remaining = self.cooldown_bars

    def CreateClone(self):
        return two_x_spy_tips_strategy()
