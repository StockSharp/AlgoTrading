import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import AverageDirectionalIndex, SimpleMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class adx_with_volume_breakout_strategy(Strategy):
    """
    Strategy based on ADX with a volume breakout confirmation.
    """

    def __init__(self):
        super(adx_with_volume_breakout_strategy, self).__init__()

        self._adx_period = self.Param("AdxPeriod", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("ADX Period", "Period for ADX calculation", "Indicators")

        self._adx_threshold = self.Param("AdxThreshold", 25.0) \
            .SetGreaterThanZero() \
            .SetDisplay("ADX Threshold", "Threshold for strong trend identification", "Indicators")

        self._volume_avg_period = self.Param("VolumeAvgPeriod", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Volume Avg Period", "Period for volume moving average", "Indicators")

        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 15) \
            .SetGreaterThanZero() \
            .SetDisplay("Signal Cooldown", "Bars to wait between signals", "Trading")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(adx_with_volume_breakout_strategy, self).OnReseted()
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(adx_with_volume_breakout_strategy, self).OnStarted2(time)

        adx = AverageDirectionalIndex()
        adx.Length = int(self._adx_period.Value)

        self._volume_sma = SimpleMovingAverage()
        self._volume_sma.Length = int(self._volume_avg_period.Value)
        self._cooldown_remaining = 0

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(adx, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

        self.StartProtection(
            Unit(2, UnitTypes.Percent),
            Unit(1, UnitTypes.Percent)
        )

    def _process_candle(self, candle, adx_value):
        if candle.State != CandleStates.Finished:
            return

        if not adx_value.IsFinal:
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        volume_avg_result = self._volume_sma.Process(DecimalIndicatorValue(self._volume_sma, candle.TotalVolume, candle.ServerTime))

        adx_typed = adx_value

        adx_ma = adx_typed.MovingAverage
        if adx_ma is None:
            return

        dx = adx_typed.Dx
        if dx is None:
            return

        plus_di = dx.Plus
        minus_di = dx.Minus
        if plus_di is None or minus_di is None:
            return

        adx_val = float(adx_ma)
        plus_di_val = float(plus_di)
        minus_di_val = float(minus_di)

        volume_average = float(volume_avg_result) if volume_avg_result.IsFormed else 0.0

        threshold = float(self._adx_threshold.Value)
        is_strong_trend = adx_val > threshold
        is_volume_breakout = volume_average <= 0.0 or float(candle.TotalVolume) >= volume_average
        is_bullish = plus_di_val > minus_di_val
        is_bearish = minus_di_val > plus_di_val

        if self._cooldown_remaining > 0:
            return

        if not is_strong_trend or not is_volume_breakout:
            return

        cd = int(self._signal_cooldown_bars.Value)

        if self.Position == 0:
            if is_bullish:
                self.BuyMarket()
                self._cooldown_remaining = cd
            elif is_bearish:
                self.SellMarket()
                self._cooldown_remaining = cd

    def CreateClone(self):
        return adx_with_volume_breakout_strategy()
