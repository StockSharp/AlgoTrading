import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class multi_timeframe_macd_strategy(Strategy):
    def __init__(self):
        super(multi_timeframe_macd_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 12) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast", "Fast EMA", "MACD")
        self._slow_length = self.Param("SlowLength", 26) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow", "Slow EMA", "MACD")
        self._trend_length = self.Param("TrendLength", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("Trend", "Trend EMA period", "Trend")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._initialized = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(multi_timeframe_macd_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._initialized = False

    def OnStarted2(self, time):
        super(multi_timeframe_macd_strategy, self).OnStarted2(time)
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._initialized = False
        self._fast_ema = ExponentialMovingAverage()
        self._fast_ema.Length = self._fast_length.Value
        self._slow_ema = ExponentialMovingAverage()
        self._slow_ema.Length = self._slow_length.Value
        self._trend_ema = ExponentialMovingAverage()
        self._trend_ema.Length = self._trend_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._fast_ema, self._slow_ema, self._trend_ema, self.OnProcess).Start()

    def OnProcess(self, candle, fast_val, slow_val, trend_val):
        if candle.State != CandleStates.Finished:
            return
        if not self._fast_ema.IsFormed or not self._slow_ema.IsFormed or not self._trend_ema.IsFormed:
            return
        fv = float(fast_val)
        sv = float(slow_val)
        tv = float(trend_val)
        close = float(candle.ClosePrice)
        if not self._initialized:
            self._prev_fast = fv
            self._prev_slow = sv
            self._initialized = True
            return
        macd = fv - sv
        prev_macd = self._prev_fast - self._prev_slow
        if prev_macd <= 0.0 and macd > 0.0 and close > tv and self.Position <= 0:
            self.BuyMarket()
        elif prev_macd >= 0.0 and macd < 0.0 and close < tv and self.Position > 0:
            self.SellMarket()
        self._prev_fast = fv
        self._prev_slow = sv

    def CreateClone(self):
        return multi_timeframe_macd_strategy()
