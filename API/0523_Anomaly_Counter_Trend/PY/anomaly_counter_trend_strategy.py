import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RateOfChange
from StockSharp.Algo.Strategies import Strategy


class anomaly_counter_trend_strategy(Strategy):
    def __init__(self):
        super(anomaly_counter_trend_strategy, self).__init__()
        self._percentage_threshold = self.Param("PercentageThreshold", 1.0) \
            .SetDisplay("Percentage Threshold", "Minimum ROC to trigger counter trade", "Anomaly Detection")
        self._roc_length = self.Param("RocLength", 60) \
            .SetDisplay("ROC Length", "Rate of change lookback period", "Anomaly Detection")
        self._cooldown_bars = self.Param("CooldownBars", 200) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._bar_index = 0
        self._last_trade_bar = 0

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
        super(anomaly_counter_trend_strategy, self).OnReseted()
        self._bar_index = 0
        self._last_trade_bar = 0

    def OnStarted2(self, time):
        super(anomaly_counter_trend_strategy, self).OnStarted2(time)
        roc = RateOfChange()
        roc.Length = self._roc_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(roc, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, roc_val):
        if candle.State != CandleStates.Finished:
            return
        self._bar_index += 1
        roc_v = float(roc_val)
        threshold = float(self._percentage_threshold.Value)
        cooldown_ok = self._bar_index - self._last_trade_bar > self.cooldown_bars
        if roc_v >= threshold and self.Position >= 0 and cooldown_ok:
            self.SellMarket()
            self._last_trade_bar = self._bar_index
        elif roc_v <= -threshold and self.Position <= 0 and cooldown_ok:
            self.BuyMarket()
            self._last_trade_bar = self._bar_index

    def CreateClone(self):
        return anomaly_counter_trend_strategy()
