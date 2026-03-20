import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class macd_aggressive_scalp_simple_strategy(Strategy):
    def __init__(self):
        super(macd_aggressive_scalp_simple_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 12) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast", "MACD fast", "MACD")
        self._slow_length = self.Param("SlowLength", 26) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow", "MACD slow", "MACD")
        self._ema_length = self.Param("EmaLength", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("EMA", "EMA trend filter", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Candles", "General")
        self._prev_macd = 0.0
        self._initialized = False
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(macd_aggressive_scalp_simple_strategy, self).OnReseted()
        self._prev_macd = 0.0
        self._initialized = False
        self._cooldown = 0

    def OnStarted(self, time):
        super(macd_aggressive_scalp_simple_strategy, self).OnStarted(time)
        self._prev_macd = 0.0
        self._initialized = False
        self._cooldown = 0
        self._ema_fast = ExponentialMovingAverage()
        self._ema_fast.Length = self._fast_length.Value
        self._ema_slow = ExponentialMovingAverage()
        self._ema_slow.Length = self._slow_length.Value
        self._ema_filter = ExponentialMovingAverage()
        self._ema_filter.Length = self._ema_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._ema_fast, self._ema_slow, self._ema_filter, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ema_fast)
            self.DrawIndicator(area, self._ema_slow)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, fast, slow, ema):
        if candle.State != CandleStates.Finished:
            return
        if not self._ema_fast.IsFormed or not self._ema_slow.IsFormed or not self._ema_filter.IsFormed:
            return
        fv = float(fast)
        sv = float(slow)
        ev = float(ema)
        macd_line = fv - sv
        if not self._initialized:
            self._prev_macd = macd_line
            self._initialized = True
            return
        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_macd = macd_line
            return
        close = float(candle.ClosePrice)
        cross_up = self._prev_macd <= 0.0 and macd_line > 0.0
        cross_down = self._prev_macd >= 0.0 and macd_line < 0.0
        if cross_up and close >= ev and self.Position <= 0:
            self.BuyMarket()
            self._cooldown = 5
        elif cross_down and close <= ev and self.Position >= 0:
            self.SellMarket()
            self._cooldown = 5
        self._prev_macd = macd_line

    def CreateClone(self):
        return macd_aggressive_scalp_simple_strategy()
