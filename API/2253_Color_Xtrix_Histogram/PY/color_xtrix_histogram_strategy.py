import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import TripleExponentialMovingAverage, RateOfChange, ExponentialMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class color_xtrix_histogram_strategy(Strategy):
    def __init__(self):
        super(color_xtrix_histogram_strategy, self).__init__()
        self._trix_length = self.Param("TrixLength", 5) \
            .SetDisplay("TRIX Length", "Length for base triple EMA", "Indicators")
        self._smooth_length = self.Param("SmoothLength", 5) \
            .SetDisplay("Smooth Length", "Length for additional smoothing", "Indicators")
        self._momentum_period = self.Param("MomentumPeriod", 1) \
            .SetDisplay("Momentum Period", "Period for rate of change", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._triple_ema = None
        self._roc = None
        self._smoother = None
        self._prev1 = None
        self._prev2 = None

    @property
    def trix_length(self):
        return self._trix_length.Value

    @property
    def smooth_length(self):
        return self._smooth_length.Value

    @property
    def momentum_period(self):
        return self._momentum_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(color_xtrix_histogram_strategy, self).OnReseted()
        self._triple_ema = None
        self._roc = None
        self._smoother = None
        self._prev1 = None
        self._prev2 = None

    def OnStarted(self, time):
        super(color_xtrix_histogram_strategy, self).OnStarted(time)
        self._prev1 = None
        self._prev2 = None
        self._triple_ema = TripleExponentialMovingAverage()
        self._triple_ema.Length = self.trix_length
        self._roc = RateOfChange()
        self._roc.Length = self.momentum_period
        self._smoother = ExponentialMovingAverage()
        self._smoother.Length = self.smooth_length
        self.Indicators.Add(self._triple_ema)
        self.Indicators.Add(self._roc)
        self.Indicators.Add(self._smoother)
        warmup = ExponentialMovingAverage()
        warmup.Length = self.trix_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(warmup, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, warmup_val):
        if candle.State != CandleStates.Finished:
            return
        t = candle.ServerTime
        close_price = float(candle.ClosePrice)
        if close_price <= 0:
            return
        log_close = math.log(close_price)
        input_val = DecimalIndicatorValue(self._triple_ema, log_close, t)
        input_val.IsFinal = True
        ema_result = self._triple_ema.Process(input_val)
        if not self._triple_ema.IsFormed:
            return
        ema_val = float(ema_result.GetValue[float]())
        input_roc = DecimalIndicatorValue(self._roc, ema_val, t)
        input_roc.IsFinal = True
        roc_result = self._roc.Process(input_roc)
        if not self._roc.IsFormed:
            return
        roc_val = float(roc_result.GetValue[float]())
        input_smooth = DecimalIndicatorValue(self._smoother, roc_val, t)
        input_smooth.IsFinal = True
        smooth_result = self._smoother.Process(input_smooth)
        if not self._smoother.IsFormed:
            return
        trix = float(smooth_result.GetValue[float]())
        if self._prev1 is None or self._prev2 is None:
            self._prev2 = self._prev1
            self._prev1 = trix
            return
        was_down = self._prev1 < self._prev2
        is_up = trix > self._prev1
        was_up = self._prev1 > self._prev2
        is_down = trix < self._prev1
        if was_down and is_up and self.Position <= 0:
            self.BuyMarket()
        elif was_up and is_down and self.Position >= 0:
            self.SellMarket()
        self._prev2 = self._prev1
        self._prev1 = trix

    def CreateClone(self):
        return color_xtrix_histogram_strategy()
