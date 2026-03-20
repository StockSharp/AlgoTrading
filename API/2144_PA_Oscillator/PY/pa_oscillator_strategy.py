import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class pa_oscillator_strategy(Strategy):
    def __init__(self):
        super(pa_oscillator_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 12) \
            .SetDisplay("Fast EMA Length", "Fast EMA period", "Indicators")
        self._slow_length = self.Param("SlowLength", 26) \
            .SetDisplay("Slow EMA Length", "Slow EMA period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for strategy", "General")
        self._prev_macd = None
        self._prev_color = None
        self._prev_prev_color = None

    @property
    def fast_length(self):
        return self._fast_length.Value

    @property
    def slow_length(self):
        return self._slow_length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(pa_oscillator_strategy, self).OnReseted()
        self._prev_macd = None
        self._prev_color = None
        self._prev_prev_color = None

    def OnStarted(self, time):
        super(pa_oscillator_strategy, self).OnStarted(time)
        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self.fast_length
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self.slow_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_ema, slow_ema, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_ema)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return
        fast = float(fast)
        slow = float(slow)
        macd = fast - slow
        if self._prev_macd is None:
            self._prev_macd = macd
            self._prev_color = 1
            self._prev_prev_color = 1
            return
        osc = macd - self._prev_macd
        if osc > 0:
            color = 0
        elif osc < 0:
            color = 2
        else:
            color = 1
        if self._prev_prev_color == 0 and self._prev_color > 0:
            if self.Position < 0:
                self.BuyMarket()
            if self.Position <= 0:
                self.BuyMarket()
        elif self._prev_prev_color == 2 and self._prev_color < 2:
            if self.Position > 0:
                self.SellMarket()
            if self.Position >= 0:
                self.SellMarket()
        self._prev_macd = macd
        self._prev_prev_color = self._prev_color
        self._prev_color = color

    def CreateClone(self):
        return pa_oscillator_strategy()
