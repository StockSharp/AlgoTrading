import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
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

    def OnReseted(self):
        super(adaptive_smi_ergodic_strategy, self).OnReseted()
        self._previous_tsi = 0.0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(adaptive_smi_ergodic_strategy, self).OnStarted(time)

        self._previous_tsi = 0.0

        tsi = TrueStrengthIndex()
        tsi.FirstLength = int(self._first_length.Value)
        tsi.SecondLength = int(self._second_length.Value)
        tsi.SignalLength = int(self._signal_length.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(tsi, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, tsi)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, tsi_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if tsi_value.Tsi is None or tsi_value.Signal is None:
            return

        tsi_val = float(tsi_value.Tsi)
        signal_val = float(tsi_value.Signal)

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._previous_tsi = tsi_val
            return

        oversold = float(self._oversold_threshold.Value)
        overbought = float(self._overbought_threshold.Value)
        cooldown = int(self._cooldown_bars.Value)

        cross_above_oversold = self._previous_tsi <= oversold and tsi_val > oversold
        cross_below_overbought = self._previous_tsi >= overbought and tsi_val < overbought

        if cross_above_oversold and tsi_val > signal_val and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif cross_below_overbought and tsi_val < signal_val and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = cooldown

        self._previous_tsi = tsi_val

    def CreateClone(self):
        return adaptive_smi_ergodic_strategy()
