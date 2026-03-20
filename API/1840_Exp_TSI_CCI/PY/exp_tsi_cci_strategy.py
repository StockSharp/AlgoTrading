import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import TrueStrengthIndex, CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy


class exp_tsi_cci_strategy(Strategy):
    def __init__(self):
        super(exp_tsi_cci_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cci_period = self.Param("CciPeriod", 14) \
            .SetDisplay("CCI Period", "CCI calculation period", "CCI")
        self._min_tsi_spread = self.Param("MinTsiSpread", 2.0) \
            .SetDisplay("Min TSI Spread", "Minimum TSI-signal spread", "Signal")
        self._min_cci_magnitude = self.Param("MinCciMagnitude", 50.0) \
            .SetDisplay("Min CCI", "Minimum absolute CCI confirmation", "Signal")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Completed candles to wait after a signal", "Signal")
        self._prev_tsi = 0.0
        self._prev_signal = 0.0
        self._initialized = False
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def cci_period(self):
        return self._cci_period.Value

    @property
    def min_tsi_spread(self):
        return self._min_tsi_spread.Value

    @property
    def min_cci_magnitude(self):
        return self._min_cci_magnitude.Value

    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    def OnReseted(self):
        super(exp_tsi_cci_strategy, self).OnReseted()
        self._prev_tsi = 0.0
        self._prev_signal = 0.0
        self._initialized = False
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(exp_tsi_cci_strategy, self).OnStarted(time)
        tsi = TrueStrengthIndex()
        cci = CommodityChannelIndex()
        cci.Length = self.cci_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(tsi, cci, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, tsi)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, tsi_value, cci_value):
        if candle.State != CandleStates.Finished:
            return
        if not tsi_value.IsFinal or not cci_value.IsFinal:
            return
        tsi_val = tsi_value.Tsi
        signal_val = tsi_value.Signal
        if tsi_val is None or signal_val is None:
            return
        tsi_val = float(tsi_val)
        signal_val = float(signal_val)
        cci = float(cci_value.ToDecimal())
        if not self._initialized:
            self._prev_tsi = tsi_val
            self._prev_signal = signal_val
            self._initialized = True
            return
        cross_up = self._prev_tsi <= self._prev_signal and tsi_val > signal_val
        cross_down = self._prev_tsi >= self._prev_signal and tsi_val < signal_val
        spread = abs(tsi_val - signal_val)
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
        min_tsi = float(self.min_tsi_spread)
        min_cci = float(self.min_cci_magnitude)
        if cross_up and spread >= min_tsi and cci >= min_cci and self._cooldown_remaining == 0 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self.cooldown_bars
        elif cross_down and spread >= min_tsi and cci <= -min_cci and self._cooldown_remaining == 0 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = self.cooldown_bars
        self._prev_tsi = tsi_val
        self._prev_signal = signal_val

    def CreateClone(self):
        return exp_tsi_cci_strategy()
