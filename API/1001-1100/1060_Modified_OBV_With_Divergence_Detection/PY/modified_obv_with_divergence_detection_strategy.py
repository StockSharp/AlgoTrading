import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import (OnBalanceVolume, SimpleMovingAverage, ExponentialMovingAverage,
                                        WeightedMovingAverage, SmoothedMovingAverage)
from StockSharp.Algo.Strategies import Strategy

MA_SIMPLE = 0
MA_EXPONENTIAL = 1
MA_WEIGHTED = 2
MA_SMOOTHED = 3


class modified_obv_with_divergence_detection_strategy(Strategy):
    """
    Modified OBV with divergence detection.
    On-Balance Volume is smoothed by a selectable moving average (OBV-M) and a signal line is an EMA of OBV-M.
    A cross of OBV-M above the signal goes long and a cross below goes short, reversing the opposite position.
    Regular and hidden divergences between price and OBV-M are found with five-bar fractals and only logged.
    """

    def __init__(self):
        super(modified_obv_with_divergence_detection_strategy, self).__init__()
        self._ma_type = self.Param("MaType", MA_EXPONENTIAL).SetDisplay("MA Type", "Moving average type used to smooth OBV (0 Simple, 1 Exponential, 2 Weighted, 3 Smoothed)", "Indicators")
        self._obv_ma_length = self.Param("ObvMaLength", 7).SetGreaterThanZero().SetDisplay("OBV MA Length", "Length of the OBV smoothing average", "Indicators")
        self._signal_length = self.Param("SignalLength", 10).SetGreaterThanZero().SetDisplay("Signal Length", "Length of the signal line EMA", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._obv_ma = None
        self._signal_ma = None
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_obvm = None
        self._prev_signal = None
        self._osc_window = []
        self._high_window = []
        self._low_window = []
        self._last_top_osc = None
        self._last_top_price = None
        self._last_bottom_osc = None
        self._last_bottom_price = None

    def OnReseted(self):
        super(modified_obv_with_divergence_detection_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(modified_obv_with_divergence_detection_strategy, self).OnStarted2(time)

        self._reset_state()

        obv = OnBalanceVolume()
        self._obv_ma = self._create_ma(int(self._ma_type.Value), self._obv_ma_length.Value)
        self._signal_ma = ExponentialMovingAverage()
        self._signal_ma.Length = self._signal_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(obv, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, self._obv_ma)
                self.DrawIndicator(oscillators, self._signal_ma)

    def _create_ma(self, ma_type, length):
        if ma_type == MA_SIMPLE:
            ma = SimpleMovingAverage()
        elif ma_type == MA_WEIGHTED:
            ma = WeightedMovingAverage()
        elif ma_type == MA_SMOOTHED:
            ma = SmoothedMovingAverage()
        else:
            ma = ExponentialMovingAverage()
        ma.Length = length
        return ma

    def _process_candle(self, candle, obv_value):
        if candle.State != CandleStates.Finished:
            return

        obvm_value = self._obv_ma.Process(obv_value)
        if not self._obv_ma.IsFormed:
            return

        obvm = float(obvm_value)
        signal_value = self._signal_ma.Process(obvm_value)

        self._detect_divergence(candle, obvm)

        if not self._signal_ma.IsFormed:
            return

        signal = float(signal_value)
        prev_obvm = self._prev_obvm
        prev_signal = self._prev_signal
        self._prev_obvm = obvm
        self._prev_signal = signal

        if prev_obvm is None or prev_signal is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        cross_up = prev_obvm <= prev_signal and obvm > signal
        cross_down = prev_obvm >= prev_signal and obvm < signal

        if cross_up and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif cross_down and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def _detect_divergence(self, candle, obvm):
        self._osc_window.append(obvm)
        self._high_window.append(float(candle.HighPrice))
        self._low_window.append(float(candle.LowPrice))
        if len(self._osc_window) > 5:
            self._osc_window.pop(0)
            self._high_window.pop(0)
            self._low_window.pop(0)
        if len(self._osc_window) < 5:
            return

        # The fractal is confirmed two bars after its middle bar.
        w = self._osc_window
        mid = w[2]

        if mid > w[0] and mid > w[1] and mid > w[3] and mid > w[4]:
            price = self._high_window[2]
            if self._last_top_osc is not None and self._last_top_price is not None:
                if price > self._last_top_price and mid < self._last_top_osc:
                    self.LogInfo("Regular bearish divergence at {0}.".format(candle.OpenTime))
                elif price < self._last_top_price and mid > self._last_top_osc:
                    self.LogInfo("Hidden bearish divergence at {0}.".format(candle.OpenTime))
            self._last_top_osc = mid
            self._last_top_price = price

        if mid < w[0] and mid < w[1] and mid < w[3] and mid < w[4]:
            price = self._low_window[2]
            if self._last_bottom_osc is not None and self._last_bottom_price is not None:
                if price < self._last_bottom_price and mid > self._last_bottom_osc:
                    self.LogInfo("Regular bullish divergence at {0}.".format(candle.OpenTime))
                elif price > self._last_bottom_price and mid < self._last_bottom_osc:
                    self.LogInfo("Hidden bullish divergence at {0}.".format(candle.OpenTime))
            self._last_bottom_osc = mid
            self._last_bottom_price = price

    def CreateClone(self):
        return modified_obv_with_divergence_detection_strategy()
