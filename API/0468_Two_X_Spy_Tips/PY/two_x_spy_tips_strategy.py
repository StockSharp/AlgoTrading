import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class two_x_spy_tips_strategy(Strategy):
    """2X SPY TIPS Strategy."""

    def __init__(self):
        super(two_x_spy_tips_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._fast_sma_length = self.Param("FastSmaLength", 50) \
            .SetDisplay("Fast SMA", "Fast SMA period", "Indicators")
        self._slow_sma_length = self.Param("SlowSmaLength", 200) \
            .SetDisplay("Slow SMA", "Slow SMA period", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 15) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")

        self._fast_sma = None
        self._slow_sma = None
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(two_x_spy_tips_strategy, self).OnReseted()
        self._fast_sma = None
        self._slow_sma = None
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(two_x_spy_tips_strategy, self).OnStarted(time)

        self._fast_sma = SimpleMovingAverage()
        self._fast_sma.Length = int(self._fast_sma_length.Value)

        self._slow_sma = SimpleMovingAverage()
        self._slow_sma.Length = int(self._slow_sma_length.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._fast_sma, self._slow_sma, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._fast_sma)
            self.DrawIndicator(area, self._slow_sma)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return

        if not self._fast_sma.IsFormed or not self._slow_sma.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        price = float(candle.ClosePrice)
        fast_v = float(fast_val)
        slow_v = float(slow_val)
        cooldown = int(self._cooldown_bars.Value)

        if price > fast_v and price > slow_v and fast_v > slow_v and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif price < fast_v and price < slow_v and fast_v < slow_v and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif self.Position > 0 and price < fast_v:
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown
        elif self.Position < 0 and price > fast_v:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown

    def CreateClone(self):
        return two_x_spy_tips_strategy()
