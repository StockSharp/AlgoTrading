import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import AverageDirectionalIndex, VolumeWeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class vwap_adx_trend_strength_strategy(Strategy):
    """
    Strategy based on VWAP with ADX Trend Strength.
    """

    def __init__(self):
        super(vwap_adx_trend_strength_strategy, self).__init__()

        # ADX period parameter.
        self._adx_period = self.Param("AdxPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ADX Period", "Period for ADX calculation", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(7, 28, 7)

        # ADX threshold parameter.
        self._adx_threshold = self.Param("AdxThreshold", 25.0) \
            .SetRange(10.0, float('inf')) \
            .SetDisplay("ADX Threshold", "Threshold for strong trend identification", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(15, 35, 5)

        # Candle type parameter.
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def AdxPeriod(self):
        return self._adx_period.Value

    @AdxPeriod.setter
    def AdxPeriod(self, value):
        self._adx_period.Value = value

    @property
    def AdxThreshold(self):
        return self._adx_threshold.Value

    @AdxThreshold.setter
    def AdxThreshold(self, value):
        self._adx_threshold.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        super(vwap_adx_trend_strength_strategy, self).OnStarted(time)

        # Create indicators
        adx = AverageDirectionalIndex()
        adx.Length = self.AdxPeriod

        vwap = VolumeWeightedMovingAverage()

        # Subscribe to candles and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)

        subscription.BindEx(adx, vwap, self.ProcessCandle).Start()

        # Setup chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, adx)
            self.DrawOwnTrades(area)

        # Setup position protection
        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(1, UnitTypes.Percent)
        )
    def ProcessCandle(self, candle, adx_value, vwap_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready for trading
        if not self.IsFormedAndOnlineAndAllowTrading():
            return


        # Extract values from ADX composite indicator
        adx_ma = adx_value.MovingAverage  # ADX value
        di_plus = adx_value.Dx.Plus  # +DI value
        di_minus = adx_value.Dx.Minus  # -DI value

        # Get VWAP
        vwap_dec = float(vwap_value)

        # Check for strong trend
        is_strong_trend = adx_ma > self.AdxThreshold

        # Check directional indicators
        is_bullish_trend = di_plus > di_minus
        is_bearish_trend = di_minus > di_plus

        # Check VWAP position
        is_above_vwap = candle.ClosePrice > vwap_dec
        is_below_vwap = candle.ClosePrice < vwap_dec

        # Trading logic
        if is_strong_trend and is_bullish_trend and is_above_vwap and self.Position <= 0:
            # Strong bullish trend above VWAP - Go long
            self.CancelActiveOrders()

            # Calculate position size
            volume = self.Volume + Math.Abs(self.Position)

            # Enter long position
            self.BuyMarket(volume)
        elif is_strong_trend and is_bearish_trend and is_below_vwap and self.Position >= 0:
            # Strong bearish trend below VWAP - Go short
            self.CancelActiveOrders()

            # Calculate position size
            volume = self.Volume + Math.Abs(self.Position)

            # Enter short position
            self.SellMarket(volume)

        # Exit logic - when ADX drops below threshold (trend weakens)
        if adx_ma < 20:
            # Close position
            self.ClosePosition()

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return vwap_adx_trend_strength_strategy()
