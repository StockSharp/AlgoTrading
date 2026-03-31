import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, SuperTrend, BollingerBands, IndicatorHelper
from StockSharp.Algo.Strategies import Strategy


class two_mars_okx_strategy(Strategy):
    """2Mars OKX Strategy."""

    def __init__(self):
        super(two_mars_okx_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._basis_length = self.Param("BasisLength", 30) \
            .SetDisplay("Basis MA Length", "Basis EMA period", "MA")
        self._signal_length = self.Param("SignalLength", 20) \
            .SetDisplay("Signal MA Length", "Signal EMA period", "MA")
        self._supertrend_period = self.Param("SupertrendPeriod", 20) \
            .SetDisplay("SuperTrend Period", "SuperTrend ATR period", "Trend")
        self._supertrend_multiplier = self.Param("SupertrendMultiplier", 4.0) \
            .SetDisplay("SuperTrend Multiplier", "SuperTrend ATR multiplier", "Trend")
        self._bb_length = self.Param("BbLength", 30) \
            .SetDisplay("BB Length", "Bollinger Bands period", "BB")
        self._bb_width = self.Param("BbWidth", 3.0) \
            .SetDisplay("BB Width", "Bollinger Bands width", "BB")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")

        self._basis_ma = None
        self._signal_ma = None
        self._supertrend = None
        self._bb = None
        self._prev_basis = 0.0
        self._prev_signal = 0.0
        self._has_prev = False
        self._entry_price = 0.0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(two_mars_okx_strategy, self).OnReseted()
        self._basis_ma = None
        self._signal_ma = None
        self._supertrend = None
        self._bb = None
        self._prev_basis = 0.0
        self._prev_signal = 0.0
        self._has_prev = False
        self._entry_price = 0.0
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(two_mars_okx_strategy, self).OnStarted2(time)

        self._basis_ma = ExponentialMovingAverage()
        self._basis_ma.Length = int(self._basis_length.Value)

        self._signal_ma = ExponentialMovingAverage()
        self._signal_ma.Length = int(self._signal_length.Value)

        self._supertrend = SuperTrend()
        self._supertrend.Length = int(self._supertrend_period.Value)
        self._supertrend.Multiplier = float(self._supertrend_multiplier.Value)

        self._bb = BollingerBands()
        self._bb.Length = int(self._bb_length.Value)
        self._bb.Width = float(self._bb_width.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._basis_ma, self._signal_ma, self._supertrend, self._bb, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._basis_ma)
            self.DrawIndicator(area, self._signal_ma)
            self.DrawIndicator(area, self._bb)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, basis_val, signal_val, st_val, bb_val):
        if candle.State != CandleStates.Finished:
            return

        if not self._basis_ma.IsFormed or not self._signal_ma.IsFormed or not self._supertrend.IsFormed or not self._bb.IsFormed:
            return

        if basis_val.IsEmpty or signal_val.IsEmpty or st_val.IsEmpty or bb_val.IsEmpty:
            return

        basis = float(IndicatorHelper.ToDecimal(basis_val))
        signal = float(IndicatorHelper.ToDecimal(signal_val))
        uptrend = st_val.IsUpTrend

        if bb_val.UpBand is None or bb_val.LowBand is None:
            return

        upper = float(bb_val.UpBand)
        lower = float(bb_val.LowBand)

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_basis = basis
            self._prev_signal = signal
            self._has_prev = True
            return

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

        cooldown = int(self._cooldown_bars.Value)
        cross_up = self._prev_signal < self._prev_basis and signal >= basis
        cross_down = self._prev_signal > self._prev_basis and signal <= basis

        if cross_up and uptrend and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._entry_price = float(candle.ClosePrice)
            self._cooldown_remaining = cooldown
        elif cross_down and not uptrend and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._entry_price = float(candle.ClosePrice)
            self._cooldown_remaining = cooldown
        elif self.Position > 0 and float(candle.ClosePrice) >= upper:
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown
        elif self.Position < 0 and float(candle.ClosePrice) <= lower:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown

        self._prev_basis = basis
        self._prev_signal = signal

    def CreateClone(self):
        return two_mars_okx_strategy()
