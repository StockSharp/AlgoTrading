import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class karpenko_channel_strategy(Strategy):
    def __init__(self):
        super(karpenko_channel_strategy, self).__init__()
        self._basic_ma = self.Param("BasicMa", 20) \
            .SetDisplay("Base MA", "Length of base moving average", "Parameters")
        self._cooldown_bars = self.Param("CooldownBars", 8) \
            .SetDisplay("Cooldown Bars", "Completed candles to wait after a signal", "Signal")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_close = 0.0
        self._prev_ma = 0.0
        self._initialized = False
        self._cooldown_remaining = 0

    @property
    def basic_ma(self):
        return self._basic_ma.Value

    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(karpenko_channel_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_ma = 0.0
        self._initialized = False
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(karpenko_channel_strategy, self).OnStarted2(time)
        sma = SimpleMovingAverage()
        sma.Length = self.basic_ma
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, ma_value):
        if candle.State != CandleStates.Finished:
            return
        ma_value = float(ma_value)
        close = float(candle.ClosePrice)
        if not self._initialized:
            self._prev_close = close
            self._prev_ma = ma_value
            self._initialized = True
            return
        cross_up = self._prev_close <= self._prev_ma and close > ma_value
        cross_down = self._prev_close >= self._prev_ma and close < ma_value
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
        if cross_up and self._cooldown_remaining == 0 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self.cooldown_bars
        elif cross_down and self._cooldown_remaining == 0 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = self.cooldown_bars
        self._prev_close = close
        self._prev_ma = ma_value

    def CreateClone(self):
        return karpenko_channel_strategy()
