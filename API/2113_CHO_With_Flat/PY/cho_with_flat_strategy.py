import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, BollingerBands, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class cho_with_flat_strategy(Strategy):
    def __init__(self):
        super(cho_with_flat_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for analysis", "General")
        self._fast_period = self.Param("FastPeriod", 3) \
            .SetDisplay("Fast Period", "Fast EMA period", "Indicator")
        self._slow_period = self.Param("SlowPeriod", 10) \
            .SetDisplay("Slow Period", "Slow EMA period", "Indicator")
        self._signal_period = self.Param("SignalPeriod", 9) \
            .SetDisplay("Signal Period", "Signal line EMA period", "Indicator")
        self._bollinger_period = self.Param("BollingerPeriod", 20) \
            .SetDisplay("Bollinger Period", "Period for Bollinger Bands", "Flat Filter")
        self._std_deviation = self.Param("StdDeviation", 2.0) \
            .SetDisplay("Std Deviation", "Deviation for Bollinger Bands", "Flat Filter")
        self._flat_threshold = self.Param("FlatThreshold", 0.005) \
            .SetDisplay("Flat Threshold", "Minimum band width ratio to detect trending", "Flat Filter")
        self._fast_ema = None
        self._slow_ema = None
        self._signal_ema = None
        self._prev_osc = 0.0
        self._prev_signal = 0.0
        self._is_initialized = False

    @property
    def candle_type(self):
        return self._candle_type.Value

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
    def bollinger_period(self):
        return self._bollinger_period.Value

    @property
    def std_deviation(self):
        return self._std_deviation.Value

    @property
    def flat_threshold(self):
        return self._flat_threshold.Value

    def OnReseted(self):
        super(cho_with_flat_strategy, self).OnReseted()
        self._fast_ema = None
        self._slow_ema = None
        self._signal_ema = None
        self._prev_osc = 0.0
        self._prev_signal = 0.0
        self._is_initialized = False

    def OnStarted2(self, time):
        super(cho_with_flat_strategy, self).OnStarted2(time)
        self._fast_ema = ExponentialMovingAverage()
        self._fast_ema.Length = self.fast_period
        self._slow_ema = ExponentialMovingAverage()
        self._slow_ema.Length = self.slow_period
        self._signal_ema = ExponentialMovingAverage()
        self._signal_ema.Length = self.signal_period
        self.Indicators.Add(self._fast_ema)
        self.Indicators.Add(self._slow_ema)
        self.Indicators.Add(self._signal_ema)
        bollinger = BollingerBands()
        bollinger.Length = self.bollinger_period
        bollinger.Width = self.std_deviation
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bollinger, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, bb_value):
        if candle.State != CandleStates.Finished:
            return
        if bb_value.UpBand is None or bb_value.LowBand is None or bb_value.MovingAverage is None:
            return
        upper_band = float(bb_value.UpBand)
        lower_band = float(bb_value.LowBand)
        middle_band = float(bb_value.MovingAverage)
        div_f = DecimalIndicatorValue(self._fast_ema, candle.ClosePrice, candle.OpenTime)
        div_f.IsFinal = True
        fast_result = self._fast_ema.Process(div_f)
        div_s = DecimalIndicatorValue(self._slow_ema, candle.ClosePrice, candle.OpenTime)
        div_s.IsFinal = True
        slow_result = self._slow_ema.Process(div_s)
        if not fast_result.IsFormed or not slow_result.IsFormed:
            return
        osc_value = float(fast_result) - float(slow_result)
        div_sig = DecimalIndicatorValue(self._signal_ema, osc_value, candle.OpenTime)
        div_sig.IsFinal = True
        sig_result = self._signal_ema.Process(div_sig)
        if not sig_result.IsFormed:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        signal_value = float(sig_result)
        if not self._is_initialized:
            self._prev_osc = osc_value
            self._prev_signal = signal_value
            self._is_initialized = True
            return
        band_width = upper_band - lower_band
        if middle_band != 0 and (band_width / middle_band) < float(self.flat_threshold):
            self._prev_osc = osc_value
            self._prev_signal = signal_value
            return
        was_above = self._prev_osc > self._prev_signal
        is_above = osc_value > signal_value
        if not was_above and is_above:
            if self.Position <= 0:
                self.BuyMarket()
        elif was_above and not is_above:
            if self.Position >= 0:
                self.SellMarket()
        self._prev_osc = osc_value
        self._prev_signal = signal_value

    def CreateClone(self):
        return cho_with_flat_strategy()
