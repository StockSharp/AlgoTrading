import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import VolumeWeightedMovingAverage, AverageDirectionalIndex, DirectionalIndex
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *


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

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._vwap = None
        self._adx = None
        self._di = None

        self._vwap_value = 0.0
        self._adx_value = 0.0
        self._plus_di_value = 0.0
        self._minus_di_value = 0.0

    @property
    def adx_period(self):
        """ADX period for trend strength calculation."""
        return self._adx_period.Value

    @adx_period.setter
    def adx_period(self, value):
        self._adx_period.Value = value

    @property
    def adx_threshold(self):
        """ADX threshold for trend strength entry."""
        return self._adx_threshold.Value

    @adx_threshold.setter
    def adx_threshold(self, value):
        self._adx_threshold.Value = value

    @property
    def adx_exit_threshold(self):
        """ADX threshold for trend strength exit."""
        return self._adx_exit_threshold.Value

    @adx_exit_threshold.setter
    def adx_exit_threshold(self, value):
        self._adx_exit_threshold.Value = value

    @property
    def candle_type(self):
        """Candle type to use for the strategy."""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnStarted(self, time):
        super(vwap_adx_trend_strategy, self).OnStarted(time)

        self._vwap_value = 0.0
        self._adx_value = 0.0
        self._plus_di_value = 0.0
        self._minus_di_value = 0.0

        # Create indicators
        self._vwap = VolumeWeightedMovingAverage()

        self._adx = AverageDirectionalIndex()
        self._adx.Length = self.adx_period

        self._di = DirectionalIndex()
        self._di.Length = self.adx_period

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.candle_type)

        subscription.BindEx(self._vwap, self._adx, self._di, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._vwap)
            self.DrawIndicator(area, self._adx)
            self.DrawOwnTrades(area)

        # Setup position protection
        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(2, UnitTypes.Percent)
        )
    def ProcessCandle(self, candle, vwap_value, adx_value, di_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        if adx_value.MovingAverage is None:
            return
        adx = adx_value.MovingAverage

        dx = adx_value.Dx
        if dx.Plus is None or dx.Minus is None:
            return
        plus_di = dx.Plus
        minus_di = dx.Minus

        # Extract values from indicators
        self._vwap_value = float(vwap_value)
        self._adx_value = adx
        self._plus_di_value = plus_di  # +DI
        self._minus_di_value = minus_di  # -DI

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Log current values
        self.LogInfo(
            f"VWAP: {self._vwap_value:.2f}, ADX: {self._adx_value:.2f}, +DI: {self._plus_di_value:.2f}, -DI: {self._minus_di_value:.2f}")

        # Trading logic
        # Buy when price is above VWAP, ADX > threshold, and +DI > -DI (strong uptrend)
        if (candle.ClosePrice > self._vwap_value and
                self._adx_value > self.adx_threshold and
                self._plus_di_value > self._minus_di_value and
                self.Position <= 0):
            self.BuyMarket(self.Volume)
            self.LogInfo(
                f"Buy Signal: Price {candle.ClosePrice:.2f} > VWAP {self._vwap_value:.2f}, ADX {self._adx_value:.2f} > {self.adx_threshold}, +DI {self._plus_di_value:.2f} > -DI {self._minus_di_value:.2f}")
        # Sell when price is below VWAP, ADX > threshold, and -DI > +DI (strong downtrend)
        elif (candle.ClosePrice < self._vwap_value and
              self._adx_value > self.adx_threshold and
              self._minus_di_value > self._plus_di_value and
              self.Position >= 0):
            self.SellMarket(self.Volume + Math.Abs(self.Position))
            self.LogInfo(
                f"Sell Signal: Price {candle.ClosePrice:.2f} < VWAP {self._vwap_value:.2f}, ADX {self._adx_value:.2f} > {self.adx_threshold}, -DI {self._minus_di_value:.2f} > +DI {self._plus_di_value:.2f}")
        # Exit long position when ADX weakens below exit threshold or -DI crosses above +DI
        elif self.Position > 0 and (self._adx_value < self.adx_exit_threshold or self._minus_di_value > self._plus_di_value):
            self.SellMarket(self.Position)
            self.LogInfo(
                f"Exit Long: ADX {self._adx_value:.2f} < {self.adx_exit_threshold} or -DI {self._minus_di_value:.2f} > +DI {self._plus_di_value:.2f}")
        # Exit short position when ADX weakens below exit threshold or +DI crosses above -DI
        elif self.Position < 0 and (self._adx_value < self.adx_exit_threshold or self._plus_di_value > self._minus_di_value):
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo(
                f"Exit Short: ADX {self._adx_value:.2f} < {self.adx_exit_threshold} or +DI {self._plus_di_value:.2f} > -DI {self._minus_di_value:.2f}")

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return vwap_adx_trend_strategy()
