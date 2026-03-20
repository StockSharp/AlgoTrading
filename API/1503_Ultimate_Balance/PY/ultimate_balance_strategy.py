import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class ultimate_balance_strategy(Strategy):
    def __init__(self):
        super(ultimate_balance_strategy, self).__init__()
        self._overbought_level = self.Param("OverboughtLevel", 70) \
            .SetDisplay("Overbought Level", "Overbought threshold", "General")
        self._oversold_level = self.Param("OversoldLevel", 30) \
            .SetDisplay("Oversold Level", "Oversold threshold", "General")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "RSI period", "General")
        self._smooth_length = self.Param("SmoothLength", 9) \
            .SetDisplay("Smooth Length", "Smoothing period for oscillator", "General")
        self._enable_short = self.Param("EnableShort", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Enable Short", "Allow short positions", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_rsi = 0.0
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._cooldown = 0

    @property
    def overbought_level(self):
        return self._overbought_level.Value

    @property
    def oversold_level(self):
        return self._oversold_level.Value

    @property
    def rsi_length(self):
        return self._rsi_length.Value

    @property
    def smooth_length(self):
        return self._smooth_length.Value

    @property
    def enable_short(self):
        return self._enable_short.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ultimate_balance_strategy, self).OnReseted()
        self._prev_rsi = 0.0
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._cooldown = 0

    def OnStarted(self, time):
        super(ultimate_balance_strategy, self).OnStarted(time)
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_length
        ema_fast = ExponentialMovingAverage()
        ema_fast.Length = 8
        ema_slow = ExponentialMovingAverage()
        ema_slow.Length = 21
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, ema_fast, ema_slow, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema_fast)
            self.DrawIndicator(area, ema_slow)
            self.DrawOwnTrades(area)

    def on_process(self, candle, rsi_val, ema_fast, ema_slow):
        if candle.State != CandleStates.Finished:
            return
        rsi_val = float(rsi_val)
        ema_fast = float(ema_fast)
        ema_slow = float(ema_slow)
        if self._prev_rsi == 0 or self._prev_fast == 0 or self._prev_slow == 0:
            self._prev_rsi = rsi_val
            self._prev_fast = ema_fast
            self._prev_slow = ema_slow
            return
        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_rsi = rsi_val
            self._prev_fast = ema_fast
            self._prev_slow = ema_slow
            return
        hist = ema_fast - ema_slow
        hist_up = hist > 0
        hist_down = hist < 0
        rsi_cross_up = self._prev_rsi <= 50 and rsi_val > 50
        rsi_cross_down = self._prev_rsi >= 50 and rsi_val < 50
        if self.Position > 0 and rsi_cross_down:
            self.SellMarket()
            self._cooldown = 80
        elif self.Position < 0 and rsi_cross_up:
            self.BuyMarket()
            self._cooldown = 80
        if self.Position == 0:
            if rsi_cross_up and hist_up:
                self.BuyMarket()
                self._cooldown = 80
            elif rsi_cross_down and hist_down:
                self.SellMarket()
                self._cooldown = 80
        self._prev_rsi = rsi_val
        self._prev_fast = ema_fast
        self._prev_slow = ema_slow

    def CreateClone(self):
        return ultimate_balance_strategy()
