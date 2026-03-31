import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
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

        self._stddev = None
        self._atr = None
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(volatility_risk_premium_strategy, self).OnReseted()
        self._stddev = None
        self._atr = None
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(volatility_risk_premium_strategy, self).OnStarted2(time)
        self._stddev = StandardDeviation()
        self._stddev.Length = int(self._stddev_period.Value)
        self._atr = AverageTrueRange()
        self._atr.Length = int(self._atr_period.Value)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._stddev, self._atr, self._process_candle).Start()

    def _process_candle(self, candle, stddev_val, atr_val):
        if candle.State != CandleStates.Finished:
            return

        if not self._stddev.IsFormed or not self._atr.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        av = float(atr_val)
        if av <= 0:
            return

        vol_ratio = float(stddev_val) / av
        vol_thresh = float(self._vol_ratio_threshold.Value)
        cooldown = int(self._cooldown_bars.Value)

        if vol_ratio < vol_thresh and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif vol_ratio > vol_thresh * 1.5 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = cooldown

    def CreateClone(self):
        return volatility_risk_premium_strategy()
