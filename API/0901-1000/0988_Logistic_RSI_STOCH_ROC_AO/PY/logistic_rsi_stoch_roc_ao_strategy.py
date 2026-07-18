import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, RateOfChange
from StockSharp.Algo.Strategies import Strategy


class logistic_rsi_stoch_roc_ao_strategy(Strategy):
    def __init__(self):
        super(logistic_rsi_stoch_roc_ao_strategy, self).__init__()
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "RSI period", "General")
        self._roc_length = self.Param("RocLength", 9) \
            .SetDisplay("ROC Length", "ROC period", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Candles", "General")
        self._cooldown_bars = self.Param("CooldownBars", 20) \
            .SetDisplay("Cooldown Bars", "Min bars between signals", "General")
        self._prev_signal = None
        self._bars_since_signal = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(logistic_rsi_stoch_roc_ao_strategy, self).OnReseted()
        self._prev_signal = None
        self._bars_since_signal = 0

    def OnStarted2(self, time):
        super(logistic_rsi_stoch_roc_ao_strategy, self).OnStarted2(time)
        self._prev_signal = None
        self._bars_since_signal = 0
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self._rsi_length.Value
        self._roc = RateOfChange()
        self._roc.Length = self._roc_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._rsi, self._roc, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, rsi_val, roc_val):
        if candle.State != CandleStates.Finished:
            return
        self._bars_since_signal += 1
        if not self._rsi.IsFormed or not self._roc.IsFormed:
            return
        rv = float(rsi_val)
        rcv = float(roc_val)
        rsi_norm = rv / 100.0 - 0.5
        if rcv > 0:
            roc_sign = 0.5
        elif rcv < 0:
            roc_sign = -0.5
        else:
            roc_sign = 0.0
        signal = rsi_norm + roc_sign
        if self._prev_signal is not None and self._bars_since_signal >= self._cooldown_bars.Value:
            prev = self._prev_signal
            cross_up = prev <= 0.0 and signal > 0.0
            cross_down = prev >= 0.0 and signal < 0.0
            if cross_up and self.Position <= 0:
                self.BuyMarket(self.Volume + abs(self.Position))
                self._bars_since_signal = 0
            elif cross_down and self.Position >= 0:
                self.SellMarket(self.Volume + abs(self.Position))
                self._bars_since_signal = 0
        self._prev_signal = signal

    def CreateClone(self):
        return logistic_rsi_stoch_roc_ao_strategy()
