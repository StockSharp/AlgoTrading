import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import TrueStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class adaptive_smi_ergodic_strategy(Strategy):
    def __init__(self):
        super(adaptive_smi_ergodic_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._first_length = self.Param("FirstLength", 25) \
            .SetGreaterThanZero() \
            .SetDisplay("First Length", "First smoothing length for TSI", "TSI")
        self._second_length = self.Param("SecondLength", 13) \
            .SetGreaterThanZero() \
            .SetDisplay("Second Length", "Second smoothing length for TSI", "TSI")
        self._signal_length = self.Param("SignalLength", 7) \
            .SetGreaterThanZero() \
            .SetDisplay("Signal Length", "Signal EMA length", "TSI")
        self._oversold_threshold = self.Param("OversoldThreshold", -10.0) \
            .SetDisplay("Oversold Threshold", "Oversold level for TSI", "TSI")
        self._overbought_threshold = self.Param("OverboughtThreshold", 10.0) \
            .SetDisplay("Overbought Threshold", "Overbought level for TSI", "TSI")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")
        self._previous_tsi = 0.0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    @cooldown_bars.setter
    def cooldown_bars(self, value):
        self._cooldown_bars.Value = value

    def OnReseted(self):
        super(adaptive_smi_ergodic_strategy, self).OnReseted()
        self._previous_tsi = 0.0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(adaptive_smi_ergodic_strategy, self).OnStarted(time)
        tsi = TrueStrengthIndex()
        tsi.FirstLength = self._first_length.Value
        tsi.SecondLength = self._second_length.Value
        tsi.SignalLength = self._signal_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(tsi, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, tsi)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, tsi_value):
        if candle.State != CandleStates.Finished:
            return
        tv = tsi_value
        tsi_val = tv.Tsi
        signal_val = tv.Signal
        if tsi_val is None or signal_val is None:
            return
        tsi_val = float(tsi_val)
        signal_val = float(signal_val)
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._previous_tsi = tsi_val
            return
        oversold = float(self._oversold_threshold.Value)
        overbought = float(self._overbought_threshold.Value)
        cross_above_oversold = self._previous_tsi <= oversold and tsi_val > oversold
        cross_below_overbought = self._previous_tsi >= overbought and tsi_val < overbought
        if cross_above_oversold and tsi_val > signal_val and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self.cooldown_bars
        elif cross_below_overbought and tsi_val < signal_val and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = self.cooldown_bars
        self._previous_tsi = tsi_val

    def CreateClone(self):
        return adaptive_smi_ergodic_strategy()
