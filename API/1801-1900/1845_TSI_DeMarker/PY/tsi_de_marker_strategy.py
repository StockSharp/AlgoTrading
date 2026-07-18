import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import TrueStrengthIndex, DeMarker
from StockSharp.Algo.Strategies import Strategy


class tsi_de_marker_strategy(Strategy):
    def __init__(self):
        super(tsi_de_marker_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for strategy", "General")
        self._demarker_period = self.Param("DemarkerPeriod", 14) \
            .SetDisplay("DeMarker Period", "Period for DeMarker", "Indicators")
        self._tsi_spread = self.Param("TsiSpread", 2.0) \
            .SetDisplay("TSI Spread", "Minimum spread between TSI and its signal line", "Filters")
        self._long_demarker_limit = self.Param("LongDeMarkerLimit", 0.55) \
            .SetDisplay("Long DeMarker", "Maximum DeMarker for long entries", "Filters")
        self._short_demarker_limit = self.Param("ShortDeMarkerLimit", 0.45) \
            .SetDisplay("Short DeMarker", "Minimum DeMarker for short entries", "Filters")
        self._cooldown_bars = self.Param("CooldownBars", 6) \
            .SetDisplay("Cooldown Bars", "Completed candles to wait after a position change", "Trading")
        self._prev_tsi = None
        self._prev_signal = None
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def demarker_period(self):
        return self._demarker_period.Value

    @property
    def tsi_spread(self):
        return self._tsi_spread.Value

    @property
    def long_demarker_limit(self):
        return self._long_demarker_limit.Value

    @property
    def short_demarker_limit(self):
        return self._short_demarker_limit.Value

    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    def OnReseted(self):
        super(tsi_de_marker_strategy, self).OnReseted()
        self._prev_tsi = None
        self._prev_signal = None
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(tsi_de_marker_strategy, self).OnStarted2(time)
        tsi = TrueStrengthIndex()
        demarker = DeMarker()
        demarker.Length = self.demarker_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(tsi, demarker, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, tsi)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, tsi_value, demarker_value):
        if candle.State != CandleStates.Finished or not tsi_value.IsFinal or not demarker_value.IsFinal:
            return
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
        tsi_pair = tsi_value
        tsi_val = tsi_pair.Tsi
        signal_val = tsi_pair.Signal
        if tsi_val is None or signal_val is None:
            return
        tsi_val = float(tsi_val)
        signal_val = float(signal_val)
        demarker = float(demarker_value)
        if self._prev_tsi is None or self._prev_signal is None:
            self._prev_tsi = tsi_val
            self._prev_signal = signal_val
            return
        tsi_spread_val = float(self.tsi_spread)
        cross_up = self._prev_tsi <= self._prev_signal and tsi_val > signal_val and abs(tsi_val - signal_val) >= tsi_spread_val
        cross_down = self._prev_tsi >= self._prev_signal and tsi_val < signal_val and abs(tsi_val - signal_val) >= tsi_spread_val
        if self._cooldown_remaining == 0:
            if cross_up and demarker <= float(self.long_demarker_limit) and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
                self._cooldown_remaining = self.cooldown_bars
            elif cross_down and demarker >= float(self.short_demarker_limit) and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()
                self._cooldown_remaining = self.cooldown_bars
        self._prev_tsi = tsi_val
        self._prev_signal = signal_val

    def CreateClone(self):
        return tsi_de_marker_strategy()
