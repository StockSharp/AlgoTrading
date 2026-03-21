import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import DeMarker
from StockSharp.Algo.Strategies import Strategy


class color_metro_de_marker_strategy(Strategy):
    def __init__(self):
        super(color_metro_de_marker_strategy, self).__init__()
        self._de_marker_period = self.Param("DeMarkerPeriod", 7) \
            .SetDisplay("DeMarker Period", "Period of the DeMarker indicator", "Indicator")
        self._step_size_fast = self.Param("StepSizeFast", 5.0) \
            .SetDisplay("Fast Step", "Fast step size for MPlus line", "Indicator")
        self._step_size_slow = self.Param("StepSizeSlow", 15.0) \
            .SetDisplay("Slow Step", "Slow step size for MMinus line", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Time frame for candles", "General")
        self._fmin = 999999.0
        self._fmax = -999999.0
        self._smin = 999999.0
        self._smax = -999999.0
        self._ftrend = 0
        self._strend = 0
        self._prev_m_plus = 0.0
        self._prev_m_minus = 0.0
        self._is_first = True

    @property
    def de_marker_period(self):
        return self._de_marker_period.Value

    @property
    def step_size_fast(self):
        return self._step_size_fast.Value

    @property
    def step_size_slow(self):
        return self._step_size_slow.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(color_metro_de_marker_strategy, self).OnReseted()
        self._fmin = 999999.0
        self._fmax = -999999.0
        self._smin = 999999.0
        self._smax = -999999.0
        self._ftrend = 0
        self._strend = 0
        self._prev_m_plus = 0.0
        self._prev_m_minus = 0.0
        self._is_first = True

    def OnStarted(self, time):
        super(color_metro_de_marker_strategy, self).OnStarted(time)
        self._fmin = 999999.0
        self._fmax = -999999.0
        self._smin = 999999.0
        self._smax = -999999.0
        self._ftrend = 0
        self._strend = 0
        self._prev_m_plus = 0.0
        self._prev_m_minus = 0.0
        self._is_first = True

        de_marker = DeMarker()
        de_marker.Length = self.de_marker_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(de_marker, self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, de_marker)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, de_marker_val):
        if candle.State != CandleStates.Finished:
            return

        dm = float(de_marker_val) * 100.0
        step_fast = float(self.step_size_fast)
        step_slow = float(self.step_size_slow)

        fmax0 = dm + 2.0 * step_fast
        fmin0 = dm - 2.0 * step_fast

        if dm > self._fmax:
            self._ftrend = 1
        if dm < self._fmin:
            self._ftrend = -1

        if self._ftrend > 0 and fmin0 < self._fmin:
            fmin0 = self._fmin
        if self._ftrend < 0 and fmax0 > self._fmax:
            fmax0 = self._fmax

        smax0 = dm + 2.0 * step_slow
        smin0 = dm - 2.0 * step_slow

        if dm > self._smax:
            self._strend = 1
        if dm < self._smin:
            self._strend = -1

        if self._strend > 0 and smin0 < self._smin:
            smin0 = self._smin
        if self._strend < 0 and smax0 > self._smax:
            smax0 = self._smax

        m_plus = fmin0 + step_fast if self._ftrend > 0 else fmax0 - step_fast
        m_minus = smin0 + step_slow if self._strend > 0 else smax0 - step_slow

        if not self._is_first:
            if self._prev_m_plus > self._prev_m_minus and m_plus <= m_minus:
                if self.Position < 0:
                    self.BuyMarket()
                if self.Position <= 0:
                    self.BuyMarket()
            elif self._prev_m_plus < self._prev_m_minus and m_plus >= m_minus:
                if self.Position > 0:
                    self.SellMarket()
                if self.Position >= 0:
                    self.SellMarket()

        self._prev_m_plus = m_plus
        self._prev_m_minus = m_minus
        self._fmin = fmin0
        self._fmax = fmax0
        self._smin = smin0
        self._smax = smax0
        self._is_first = False

    def CreateClone(self):
        return color_metro_de_marker_strategy()
