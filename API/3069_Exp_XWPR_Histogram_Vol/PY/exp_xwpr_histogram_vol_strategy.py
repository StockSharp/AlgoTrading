import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import DecimalIndicatorValue, SimpleMovingAverage, WilliamsR
from StockSharp.Algo.Strategies import Strategy


class exp_xwpr_histogram_vol_strategy(Strategy):
    def __init__(self):
        super(exp_xwpr_histogram_vol_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._wpr_period = self.Param("WprPeriod", 7) \
            .SetDisplay("WPR Period", "Williams %R lookback", "Indicator")
        self._smoothing_length = self.Param("SmoothingLength", 5) \
            .SetDisplay("Smoothing", "Smoothing length", "Indicator")
        self._high_level2 = self.Param("HighLevel2", 17.0) \
            .SetDisplay("High Level 2", "Strong bullish zone", "Indicator")
        self._low_level2 = self.Param("LowLevel2", -17.0) \
            .SetDisplay("Low Level 2", "Strong bearish zone", "Indicator")
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 48) \
            .SetDisplay("Signal Cooldown", "Bars to wait after a new entry", "Trading")

        self._wpr = None
        self._hist_sma = None
        self._vol_sma = None
        self._prev_color = None
        self._cooldown_remaining = 0

    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def WprPeriod(self):
        return self._wpr_period.Value
    @property
    def SmoothingLength(self):
        return self._smoothing_length.Value
    @property
    def HighLevel2(self):
        return self._high_level2.Value
    @property
    def LowLevel2(self):
        return self._low_level2.Value
    @property
    def SignalCooldownBars(self):
        return self._signal_cooldown_bars.Value

    def OnReseted(self):
        super(exp_xwpr_histogram_vol_strategy, self).OnReseted()
        self._wpr = None
        self._hist_sma = None
        self._vol_sma = None
        self._prev_color = None
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(exp_xwpr_histogram_vol_strategy, self).OnStarted(time)
        self._prev_color = None
        self._cooldown_remaining = 0

        self._wpr = WilliamsR()
        self._wpr.Length = self.WprPeriod
        self._hist_sma = SimpleMovingAverage()
        self._hist_sma.Length = self.SmoothingLength
        self._vol_sma = SimpleMovingAverage()
        self._vol_sma.Length = self.SmoothingLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _on_process(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        wpr_result = self._wpr.Process(candle)
        if not wpr_result.IsFormed:
            return

        wpr = float(wpr_result.ToDecimal())
        volume = float(candle.TotalVolume) if float(candle.TotalVolume) > 0.0 else 1.0
        hist_raw = (wpr + 50.0) * volume

        hist_input = DecimalIndicatorValue(self._hist_sma, hist_raw, candle.OpenTime)
        hist_input.IsFinal = True
        hist_smoothed = self._hist_sma.Process(hist_input)

        vol_input = DecimalIndicatorValue(self._vol_sma, volume, candle.OpenTime)
        vol_input.IsFinal = True
        vol_smoothed = self._vol_sma.Process(vol_input)

        if not hist_smoothed.IsFormed or not vol_smoothed.IsFormed:
            return

        baseline = float(vol_smoothed.ToDecimal())
        if baseline == 0.0:
            return

        hist = float(hist_smoothed.ToDecimal())
        strong_bull_level = float(self.HighLevel2) * baseline
        strong_bear_level = float(self.LowLevel2) * baseline

        if hist >= strong_bull_level:
            color = 0
        elif hist <= strong_bear_level:
            color = 4
        else:
            color = 2

        if self._prev_color is None:
            self._prev_color = color
            return

        previous_color = self._prev_color
        self._prev_color = color

        if self._cooldown_remaining > 0:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if previous_color != 0 and color == 0 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self.SignalCooldownBars
        elif previous_color != 4 and color == 4 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = self.SignalCooldownBars

    def CreateClone(self):
        return exp_xwpr_histogram_vol_strategy()
