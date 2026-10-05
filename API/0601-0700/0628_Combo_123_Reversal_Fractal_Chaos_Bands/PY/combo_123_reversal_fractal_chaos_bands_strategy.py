import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StochasticK, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class combo_123_reversal_fractal_chaos_bands_strategy(Strategy):
    """
    Combo 123 Reversal and Fractal Chaos Bands strategy.
    The 123 reversal is long after a close below the previous one followed by a close above it while the slow stochastic %K is
    below %D and above Level, and short in the mirrored case. The fractal chaos bands hold the high of the last up fractal and
    the low of the last down fractal, a fractal being a bar whose high (low) exceeds the Pattern bars on each side.
    A long needs the 123 long signal with a close above the upper band, a short the 123 short signal with a close below the lower
    band, and an opposite signal reverses the position.
    """

    def __init__(self):
        super(combo_123_reversal_fractal_chaos_bands_strategy, self).__init__()
        self._length = self.Param("Length", 15).SetGreaterThanZero().SetDisplay("Length", "Stochastic lookback", "123 Reversal")
        self._k_smoothing = self.Param("KSmoothing", 1).SetGreaterThanZero().SetDisplay("K Smoothing", "Smoothing of %K", "123 Reversal")
        self._d_length = self.Param("DLength", 3).SetGreaterThanZero().SetDisplay("D Length", "Length of %D", "123 Reversal")
        self._level = self.Param("Level", 50.0).SetDisplay("Level", "Stochastic level separating the 123 signals", "123 Reversal")
        self._pattern = self.Param("Pattern", 1).SetGreaterThanZero().SetDisplay("Pattern", "Bars on each side of a fractal", "Fractal Chaos Bands")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._k_smoother = None
        self._d_average = None
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._highs = []
        self._lows = []
        self._close1 = None
        self._close2 = None
        self._upper_band = None
        self._lower_band = None

    def OnReseted(self):
        super(combo_123_reversal_fractal_chaos_bands_strategy, self).OnReseted()
        self._reset_state()
        self._k_smoother = None
        self._d_average = None

    def OnStarted2(self, time):
        super(combo_123_reversal_fractal_chaos_bands_strategy, self).OnStarted2(time)

        self._reset_state()

        raw_k = StochasticK()
        raw_k.Length = self._length.Value
        self._k_smoother = SimpleMovingAverage()
        self._k_smoother.Length = self._k_smoothing.Value
        self._d_average = SimpleMovingAverage()
        self._d_average.Length = self._d_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(raw_k, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, raw_k)

    def _process_candle(self, candle, raw_k_value):
        if candle.State != CandleStates.Finished:
            return

        self._update_fractals(candle)

        close = candle.ClosePrice
        close1 = self._close1
        close2 = self._close2
        self._close2 = self._close1
        self._close1 = close

        if not raw_k_value.IsFormed:
            return

        k_result = process_value(self._k_smoother, raw_k_value.GetValue[Decimal](None), candle.OpenTime, True)
        if not k_result.IsFormed:
            return
        fast = k_result.GetValue[Decimal](None)

        d_result = process_value(self._d_average, fast, candle.OpenTime, True)
        if not d_result.IsFormed:
            return
        slow = d_result.GetValue[Decimal](None)

        if close1 is None or close2 is None or self._upper_band is None or self._lower_band is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        level = Decimal(self._level.Value)
        reversal_long = close2 < close1 and close > close1 and fast < slow and fast > level
        reversal_short = close2 > close1 and close < close1 and fast > slow and fast < level

        if reversal_long and close > self._upper_band and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif reversal_short and close < self._lower_band and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def _update_fractals(self, candle):
        pattern = self._pattern.Value
        window = pattern * 2 + 1

        self._highs.append(candle.HighPrice)
        self._lows.append(candle.LowPrice)

        if len(self._highs) > window:
            self._highs.pop(0)
            self._lows.pop(0)

        if len(self._highs) < window:
            return

        # The fractal bar sits in the middle of the window, Pattern bars back.
        center_high = self._highs[pattern]
        center_low = self._lows[pattern]
        is_up = True
        is_down = True

        for i in range(window):
            if i == pattern:
                continue
            if self._highs[i] >= center_high:
                is_up = False
            if self._lows[i] <= center_low:
                is_down = False

        if is_up:
            self._upper_band = center_high
        if is_down:
            self._lower_band = center_low

    def CreateClone(self):
        return combo_123_reversal_fractal_chaos_bands_strategy()
