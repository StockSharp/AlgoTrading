import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, SuperTrend, BollingerBands
from StockSharp.Algo.Strategies import Strategy


class two_mars_okx_strategy(Strategy):
    def __init__(self):
        super(two_mars_okx_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._basis_length = self.Param("BasisLength", 30) \
            .SetGreaterThanZero() \
            .SetDisplay("Basis MA Length", "Basis EMA period", "MA")
        self._signal_length = self.Param("SignalLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Signal MA Length", "Signal EMA period", "MA")
        self._supertrend_period = self.Param("SupertrendPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("SuperTrend Period", "SuperTrend ATR period", "Trend")
        self._supertrend_multiplier = self.Param("SupertrendMultiplier", 4.0) \
            .SetDisplay("SuperTrend Multiplier", "SuperTrend ATR multiplier", "Trend")
        self._bb_length = self.Param("BbLength", 30) \
            .SetGreaterThanZero() \
            .SetDisplay("BB Length", "Bollinger Bands period", "BB")
        self._bb_width = self.Param("BbWidth", 3.0) \
            .SetDisplay("BB Width", "Bollinger Bands width", "BB")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")
        self._prev_basis = 0.0
        self._prev_signal = 0.0
        self._has_prev = False
        self._entry_price = 0.0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value
    @property
    def basis_length(self):
        return self._basis_length.Value
    @property
    def signal_length(self):
        return self._signal_length.Value
    @property
    def supertrend_period(self):
        return self._supertrend_period.Value
    @property
    def supertrend_multiplier(self):
        return self._supertrend_multiplier.Value
    @property
    def bb_length(self):
        return self._bb_length.Value
    @property
    def bb_width(self):
        return self._bb_width.Value
    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    def OnReseted(self):
        super(two_mars_okx_strategy, self).OnReseted()
        self._prev_basis = 0.0
        self._prev_signal = 0.0
        self._has_prev = False
        self._entry_price = 0.0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(two_mars_okx_strategy, self).OnStarted(time)
        self._basis_ma = ExponentialMovingAverage()
        self._basis_ma.Length = self.basis_length
        self._signal_ma = ExponentialMovingAverage()
        self._signal_ma.Length = self.signal_length
        self._supertrend = SuperTrend()
        self._supertrend.Length = self.supertrend_period
        self._supertrend.Multiplier = self.supertrend_multiplier
        self._bb = BollingerBands()
        self._bb.Length = self.bb_length
        self._bb.Width = self.bb_width

        subscription = self.SubscribeCandles(self.candle_type)
        subscription \
            .BindEx(self._basis_ma, self._signal_ma, self._supertrend, self._bb, self.OnProcess) \
            .Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._basis_ma)
            self.DrawIndicator(area, self._signal_ma)
            self.DrawIndicator(area, self._bb)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, basis_val, signal_val, st_val, bb_val):
        if candle.State != CandleStates.Finished:
            return
        if not self._basis_ma.IsFormed or not self._signal_ma.IsFormed or not self._supertrend.IsFormed or not self._bb.IsFormed:
            return
        if basis_val.IsEmpty or signal_val.IsEmpty or st_val.IsEmpty or bb_val.IsEmpty:
            return

        basis = float(basis_val)
        signal = float(signal_val)
        uptrend = st_val.IsUpTrend
        upper = bb_val.UpBand
        lower = bb_val.LowBand
        if upper is None or lower is None:
            return
        upper = float(upper)
        lower = float(lower)

        if not self._has_prev:
            self._prev_basis = basis
            self._prev_signal = signal
            self._has_prev = True
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_basis = basis
            self._prev_signal = signal
            return

        cross_up = self._prev_signal < self._prev_basis and signal >= basis
        cross_down = self._prev_signal > self._prev_basis and signal <= basis

        if cross_up and uptrend and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(abs(self.Position))
            self.BuyMarket()
            self._entry_price = float(candle.ClosePrice)
            self._cooldown_remaining = self.cooldown_bars
        elif cross_down and not uptrend and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(abs(self.Position))
            self.SellMarket()
            self._entry_price = float(candle.ClosePrice)
            self._cooldown_remaining = self.cooldown_bars
        elif self.Position > 0 and float(candle.ClosePrice) >= upper:
            self.SellMarket(abs(self.Position))
            self._cooldown_remaining = self.cooldown_bars
        elif self.Position < 0 and float(candle.ClosePrice) <= lower:
            self.BuyMarket(abs(self.Position))
            self._cooldown_remaining = self.cooldown_bars

        self._prev_basis = basis
        self._prev_signal = signal

    def CreateClone(self):
        return two_mars_okx_strategy()
