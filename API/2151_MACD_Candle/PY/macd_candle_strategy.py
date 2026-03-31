import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class macd_candle_strategy(Strategy):
    def __init__(self):
        super(macd_candle_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 12) \
            .SetDisplay("Fast EMA", "Fast EMA period", "Indicator")
        self._slow_length = self.Param("SlowLength", 26) \
            .SetDisplay("Slow EMA", "Slow EMA period", "Indicator")
        self._signal_length = self.Param("SignalLength", 9) \
            .SetDisplay("Signal", "Signal period", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle type for indicators", "General")
        self._macd_open = None
        self._macd_close = None
        self._previous_color = None

    @property
    def fast_length(self):
        return self._fast_length.Value

    @property
    def slow_length(self):
        return self._slow_length.Value

    @property
    def signal_length(self):
        return self._signal_length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(macd_candle_strategy, self).OnReseted()
        self._previous_color = None
        self._macd_open = None
        self._macd_close = None

    def OnStarted2(self, time):
        super(macd_candle_strategy, self).OnStarted2(time)
        self._macd_open = MovingAverageConvergenceDivergenceSignal()
        self._macd_open.Macd.ShortMa.Length = self.fast_length
        self._macd_open.Macd.LongMa.Length = self.slow_length
        self._macd_open.SignalMa.Length = self.signal_length
        self._macd_close = MovingAverageConvergenceDivergenceSignal()
        self._macd_close.Macd.ShortMa.Length = self.fast_length
        self._macd_close.Macd.LongMa.Length = self.slow_length
        self._macd_close.SignalMa.Length = self.signal_length
        self.Indicators.Add(self._macd_open)
        self.Indicators.Add(self._macd_close)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        open_input = DecimalIndicatorValue(self._macd_open, candle.OpenPrice, candle.OpenTime)
        open_input.IsFinal = True
        close_input = DecimalIndicatorValue(self._macd_close, candle.ClosePrice, candle.OpenTime)
        close_input.IsFinal = True
        open_value = self._macd_open.Process(open_input)
        close_value = self._macd_close.Process(close_input)
        open_macd = open_value.Macd
        close_macd = close_value.Macd
        if open_macd is None or close_macd is None:
            return
        open_macd = float(open_macd)
        close_macd = float(close_macd)
        if open_macd < close_macd:
            color = 2.0
        elif open_macd > close_macd:
            color = 0.0
        else:
            color = 1.0
        if self._previous_color is None:
            self._previous_color = color
            return
        if color == 2.0 and self._previous_color < 2.0:
            if self.Position < 0:
                self.BuyMarket()
            if self.Position <= 0:
                self.BuyMarket()
        elif color == 0.0 and self._previous_color > 0.0:
            if self.Position > 0:
                self.SellMarket()
            if self.Position >= 0:
                self.SellMarket()
        self._previous_color = color

    def CreateClone(self):
        return macd_candle_strategy()
