import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StandardDeviation, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class volatility_risk_premium_strategy(Strategy):
    """Volatility risk premium strategy using realized vs implied volatility proxy."""

    def __init__(self):
        super(volatility_risk_premium_strategy, self).__init__()

        self._stddev_period = self.Param("StdDevPeriod", 20) \
            .SetDisplay("StdDev Period", "Period for realized volatility", "Parameters")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "Period for ATR calculation", "Parameters")
        self._vol_ratio_threshold = self.Param("VolRatioThreshold", 1.0) \
            .SetDisplay("Vol Ratio Threshold", "StdDev/ATR ratio threshold", "Parameters")
        self._cooldown_bars = self.Param("CooldownBars", 20) \
            .SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._cooldown_remaining = 0

    @property
    def StdDevPeriod(self):
        return self._stddev_period.Value
    @property
    def AtrPeriod(self):
        return self._atr_period.Value
    @property
    def VolRatioThreshold(self):
        return self._vol_ratio_threshold.Value
    @property
    def CooldownBars(self):
        return self._cooldown_bars.Value
    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(volatility_risk_premium_strategy, self).OnReseted()
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(volatility_risk_premium_strategy, self).OnStarted(time)
        stddev = StandardDeviation()
        stddev.Length = self.StdDevPeriod
        atr = AverageTrueRange()
        atr.Length = self.AtrPeriod
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(stddev, atr, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, stddev_val, atr_val):
        if candle.State != CandleStates.Finished:
            return
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        av = float(atr_val)
        if av <= 0:
            return

        vol_ratio = float(stddev_val) / av

        if vol_ratio < self.VolRatioThreshold and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self.CooldownBars
        elif vol_ratio > self.VolRatioThreshold * 1.5 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = self.CooldownBars

    def CreateClone(self):
        return volatility_risk_premium_strategy()
