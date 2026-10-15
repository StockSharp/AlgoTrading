import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, RelativeStrengthIndex, WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class fibonacci_bands_strategy(Strategy):
    def __init__(self):
        super(fibonacci_bands_strategy, self).__init__()
        self._ma_length = self.Param("MaLength", 50) \
            .SetDisplay("MA Length", "Moving average length", "General")
        self._fib3 = self.Param("Fib3", 1.618) \
            .SetDisplay("Fib Level 3", "Fibonacci level 3", "Levels")
        self._kc_multiplier = self.Param("KcMultiplier", 2) \
            .SetDisplay("KC Multiplier", "Keltner multiplier", "Keltner")
        self._kc_length = self.Param("KcLength", 14) \
            .SetDisplay("KC Length", "ATR length", "Keltner")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "RSI length", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe for candles", "General")
        self._prev_src = 0.0
        self._prev_ma = 0.0
        self._prev_fb_upper3 = 0.0
        self._prev_fb_lower3 = 0.0
        self._is_ready = False

    @property
    def ma_length(self):
        return self._ma_length.Value

    @property
    def fib3(self):
        return self._fib3.Value

    @property
    def kc_multiplier(self):
        return self._kc_multiplier.Value

    @property
    def kc_length(self):
        return self._kc_length.Value

    @property
    def rsi_length(self):
        return self._rsi_length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(fibonacci_bands_strategy, self).OnReseted()
        self._prev_src = 0.0
        self._prev_ma = 0.0
        self._prev_fb_upper3 = 0.0
        self._prev_fb_lower3 = 0.0
        self._is_ready = False

    def OnStarted2(self, time):
        super(fibonacci_bands_strategy, self).OnStarted2(time)
        wma = WeightedMovingAverage()
        wma.Length = self.ma_length
        atr = AverageTrueRange()
        atr.Length = self.kc_length
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(wma, atr, rsi, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, wma)
            self.DrawOwnTrades(area)

    def on_process(self, candle, ma_val, atr_val, rsi_val):
        if candle.State != CandleStates.Finished:
            return
        src = (candle.HighPrice + candle.LowPrice) / 2
        if not self._is_ready:
            self._prev_src = src
            self._prev_ma = ma_val
            self._prev_fb_upper3 = ma_val
            self._prev_fb_lower3 = ma_val
            self._is_ready = True
            return
        kc_upper = ma_val + self.kc_multiplier * atr_val
        kc_lower = ma_val - self.kc_multiplier * atr_val
        fb_upper3 = ma_val + self.fib3 * (kc_upper - ma_val)
        fb_lower3 = ma_val - self.fib3 * (ma_val - kc_lower)
        long_cond = self._prev_src <= self._prev_fb_upper3 and src > fb_upper3 and rsi_val > 55
        short_cond = self._prev_src >= self._prev_fb_lower3 and src < fb_lower3 and rsi_val < 45
        if long_cond and self.Position <= 0:
            self.BuyMarket()
        elif short_cond and self.Position >= 0:
            self.SellMarket()
        # Exit on MA cross
        if self.Position > 0 and src < ma_val:
            self.SellMarket()
        elif self.Position < 0 and src > ma_val:
            self.BuyMarket()
        self._prev_src = src
        self._prev_ma = ma_val
        self._prev_fb_upper3 = fb_upper3
        self._prev_fb_lower3 = fb_lower3

    def CreateClone(self):
        return fibonacci_bands_strategy()
