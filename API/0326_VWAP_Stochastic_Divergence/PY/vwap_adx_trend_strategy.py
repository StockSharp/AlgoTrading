import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import VolumeWeightedMovingAverage, AverageDirectionalIndex, DirectionalIndex
from StockSharp.Algo.Strategies import Strategy


class vwap_adx_trend_strategy(Strategy):
    """
    Strategy combining VWAP with ADX trend strength indicator.
    """

    def __init__(self):
        super(vwap_adx_trend_strategy, self).__init__()

        self._adx_period = self.Param("AdxPeriod", 14) \
            .SetDisplay("ADX Period", "Period for ADX and Directional Index calculations", "ADX") \
            .SetCanOptimize(True) \
            .SetOptimize(8, 20, 2)

        self._adx_threshold = self.Param("AdxThreshold", 25.0) \
            .SetDisplay("ADX Threshold", "ADX threshold for trend strength entry", "ADX") \
            .SetCanOptimize(True) \
            .SetOptimize(20.0, 40.0, 5.0)

        self._adx_exit_threshold = self.Param("AdxExitThreshold", 20.0) \
            .SetDisplay("ADX Exit Threshold", "ADX threshold for trend strength exit", "ADX") \
            .SetCanOptimize(True) \
            .SetOptimize(10.0, 25.0, 5.0)

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 4) \
            .SetNotNegative() \
            .SetDisplay("Signal Cooldown Bars", "Closed candles to wait before a new DI crossover entry", "General")

        self._vwap_value = 0.0
        self._adx_value = 0.0
        self._plus_di_value = 0.0
        self._minus_di_value = 0.0
        self._prev_plus_di = None
        self._prev_minus_di = None
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnReseted(self):
        super(vwap_adx_trend_strategy, self).OnReseted()
        self._vwap_value = 0.0
        self._adx_value = 0.0
        self._plus_di_value = 0.0
        self._minus_di_value = 0.0
        self._prev_plus_di = None
        self._prev_minus_di = None
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(vwap_adx_trend_strategy, self).OnStarted(time)

        vwap = VolumeWeightedMovingAverage()

        adx = AverageDirectionalIndex()
        adx.Length = int(self._adx_period.Value)

        di = DirectionalIndex()
        di.Length = int(self._adx_period.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(vwap, adx, di, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, vwap)
            self.DrawIndicator(area, adx)
            self.DrawOwnTrades(area)

        self.StartProtection(
            Unit(2, UnitTypes.Percent),
            Unit(2, UnitTypes.Percent)
        )

    def ProcessCandle(self, candle, vwap_value, adx_value, di_value):
        if candle.State != CandleStates.Finished:
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        if adx_value.MovingAverage is None:
            return

        adx = float(adx_value.MovingAverage)

        dx = adx_value.Dx
        if dx.Plus is None or dx.Minus is None:
            return

        plus_di = float(dx.Plus)
        minus_di = float(dx.Minus)

        self._vwap_value = float(vwap_value)
        self._adx_value = adx
        self._plus_di_value = plus_di
        self._minus_di_value = minus_di

        if self._prev_plus_di is None or self._prev_minus_di is None:
            self._prev_plus_di = self._plus_di_value
            self._prev_minus_di = self._minus_di_value
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_plus_di = self._plus_di_value
            self._prev_minus_di = self._minus_di_value
            return

        bullish_cross = self._prev_plus_di <= self._prev_minus_di and self._plus_di_value > self._minus_di_value
        bearish_cross = self._prev_plus_di >= self._prev_minus_di and self._minus_di_value > self._plus_di_value

        close_price = float(candle.ClosePrice)
        adx_threshold = float(self._adx_threshold.Value)
        adx_exit_threshold = float(self._adx_exit_threshold.Value)
        cooldown_bars = int(self._signal_cooldown_bars.Value)

        if self._cooldown_remaining == 0 and bullish_cross and close_price > self._vwap_value and self._adx_value > adx_threshold and self.Position <= 0:
            vol = self.Volume
            if self.Position < 0:
                vol = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(vol)
            self._cooldown_remaining = cooldown_bars
        elif self._cooldown_remaining == 0 and bearish_cross and close_price < self._vwap_value and self._adx_value > adx_threshold and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))
            self._cooldown_remaining = cooldown_bars
        elif self.Position > 0 and (self._adx_value < adx_exit_threshold or self._minus_di_value > self._plus_di_value):
            self.SellMarket(self.Position)
            self._cooldown_remaining = cooldown_bars
        elif self.Position < 0 and (self._adx_value < adx_exit_threshold or self._plus_di_value > self._minus_di_value):
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown_bars

        self._prev_plus_di = self._plus_di_value
        self._prev_minus_di = self._minus_di_value

    def CreateClone(self):
        return vwap_adx_trend_strategy()
