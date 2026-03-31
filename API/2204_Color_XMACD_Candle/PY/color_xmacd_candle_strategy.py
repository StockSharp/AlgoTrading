import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergence, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class color_xmacd_candle_strategy(Strategy):
    def __init__(self):
        super(color_xmacd_candle_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 12) \
            .SetDisplay("Fast Period", "Fast EMA period", "MACD")
        self._slow_period = self.Param("SlowPeriod", 26) \
            .SetDisplay("Slow Period", "Slow EMA period", "MACD")
        self._signal_period = self.Param("SignalPeriod", 9) \
            .SetDisplay("Signal Period", "Signal line period", "MACD")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle type for calculations", "Common")
        self._signal_ma = None
        self._prev_hist = None
        self._prev_prev_hist = None

    @property
    def fast_period(self):
        return self._fast_period.Value

    @property
    def slow_period(self):
        return self._slow_period.Value

    @property
    def signal_period(self):
        return self._signal_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(color_xmacd_candle_strategy, self).OnReseted()
        self._signal_ma = None
        self._prev_hist = None
        self._prev_prev_hist = None

    def OnStarted2(self, time):
        super(color_xmacd_candle_strategy, self).OnStarted2(time)
        macd = MovingAverageConvergenceDivergence()
        macd.ShortMa.Length = self.fast_period
        macd.LongMa.Length = self.slow_period
        self._signal_ma = ExponentialMovingAverage()
        self._signal_ma.Length = self.signal_period
        self.Indicators.Add(self._signal_ma)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, macd)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, macd_value):
        if candle.State != CandleStates.Finished:
            return
        if not macd_value.IsFormed:
            return
        macd_line = float(macd_value)
        signal_result = self._signal_ma.Process(macd_value)
        if not signal_result.IsFormed:
            return
        signal_line = float(signal_result)
        hist = macd_line - signal_line
        if self._prev_hist is not None and self._prev_prev_hist is not None:
            was_rising = self._prev_hist > self._prev_prev_hist
            now_rising = hist > self._prev_hist
            if not was_rising and now_rising and self.Position <= 0:
                self.BuyMarket()
            elif was_rising and not now_rising and self.Position >= 0:
                self.SellMarket()
        self._prev_prev_hist = self._prev_hist
        self._prev_hist = hist

    def CreateClone(self):
        return color_xmacd_candle_strategy()
