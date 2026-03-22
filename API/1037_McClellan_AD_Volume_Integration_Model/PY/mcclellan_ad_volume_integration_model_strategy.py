import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class mcclellan_ad_volume_integration_model_strategy(Strategy):
    def __init__(self):
        super(mcclellan_ad_volume_integration_model_strategy, self).__init__()
        self._ema_short_length = self.Param("EmaShortLength", 19) \
            .SetGreaterThanZero() \
            .SetDisplay("Short EMA Length", "EMA period for short term", "Indicators")
        self._ema_long_length = self.Param("EmaLongLength", 38) \
            .SetGreaterThanZero() \
            .SetDisplay("Long EMA Length", "EMA period for long term", "Indicators")
        self._osc_threshold_long = self.Param("OscThresholdLong", -96.0) \
            .SetDisplay("Long Entry Threshold", "Oscillator level for long entry", "Trading")
        self._exit_periods = self.Param("ExitPeriods", 13) \
            .SetGreaterThanZero() \
            .SetDisplay("Exit After Bars", "Bars to hold position", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._entry_bar = -1
        self._bar_index = 0
        self._previous_oscillator = 0.0
        self._has_prev_oscillator = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(mcclellan_ad_volume_integration_model_strategy, self).OnReseted()
        self._entry_bar = -1
        self._bar_index = 0
        self._previous_oscillator = 0.0
        self._has_prev_oscillator = False

    def OnStarted(self, time):
        super(mcclellan_ad_volume_integration_model_strategy, self).OnStarted(time)
        self._entry_bar = -1
        self._bar_index = 0
        self._previous_oscillator = 0.0
        self._has_prev_oscillator = False
        self._ema_short = ExponentialMovingAverage()
        self._ema_short.Length = self._ema_short_length.Value
        self._ema_long = ExponentialMovingAverage()
        self._ema_long.Length = self._ema_long_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return
        ad_line = float(candle.ClosePrice) - float(candle.OpenPrice)
        vol = float(candle.TotalVolume)
        if vol == 0.0:
            vol = 1.0
        weighted_ad = ad_line * vol
        short_res = self._ema_short.Process(DecimalIndicatorValue(self._ema_short, weighted_ad, candle.OpenTime))
        long_res = self._ema_long.Process(DecimalIndicatorValue(self._ema_long, weighted_ad, candle.OpenTime))
        short_val = float(short_res)
        long_val = float(long_res)
        oscillator = short_val - long_val
        threshold = float(self._osc_threshold_long.Value)
        long_entry = self._has_prev_oscillator and self._previous_oscillator < threshold and oscillator > threshold
        if long_entry and self.Position <= 0:
            self.BuyMarket()
            self._entry_bar = self._bar_index
        exit_periods = self._exit_periods.Value
        if self._entry_bar >= 0 and self.Position > 0 and self._bar_index - self._entry_bar >= exit_periods:
            self.SellMarket()
            self._entry_bar = -1
        self._previous_oscillator = oscillator
        self._has_prev_oscillator = True
        self._bar_index += 1

    def CreateClone(self):
        return mcclellan_ad_volume_integration_model_strategy()
