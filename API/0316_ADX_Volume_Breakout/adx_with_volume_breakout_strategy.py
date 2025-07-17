import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import AverageDirectionalIndex, SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class adx_with_volume_breakout_strategy(Strategy):
    """Strategy based on ADX with Volume Breakout."""

    def __init__(self):
        super(adx_with_volume_breakout_strategy, self).__init__()

        # ADX period parameter.
        self._adx_period = self.Param("AdxPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ADX Period", "Period for ADX calculation", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(7, 28, 7)

        # ADX threshold parameter.
        self._adx_threshold = self.Param("AdxThreshold", 25.0) \
            .SetGreaterThanZero() \
            .SetDisplay("ADX Threshold", "Threshold for strong trend identification", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(15, 35, 5)

        # Volume average period parameter.
        self._volume_avg_period = self.Param("VolumeAvgPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Volume Avg Period", "Period for volume moving average", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5)

        # Volume threshold factor parameter.
        self._volume_threshold_factor = self.Param("VolumeThresholdFactor", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Volume Threshold Factor", "Factor for volume breakout detection", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(1.5, 3.0, 0.5)

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
    def VolumeAvgPeriod(self):
        return self._volume_avg_period.Value

    @VolumeAvgPeriod.setter
    def VolumeAvgPeriod(self, value):
        self._volume_avg_period.Value = value

    @property
    def VolumeThresholdFactor(self):
        return self._volume_threshold_factor.Value

    @VolumeThresholdFactor.setter
    def VolumeThresholdFactor(self, value):
        self._volume_threshold_factor.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        super(adx_with_volume_breakout_strategy, self).OnStarted(time)

        # Create indicators
        adx = AverageDirectionalIndex(); adx.Length = self.AdxPeriod
        volume_sma = SimpleMovingAverage(); volume_sma.Length = self.VolumeAvgPeriod
        volume_std_dev = StandardDeviation(); volume_std_dev.Length = self.VolumeAvgPeriod

        # Subscribe to candles and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)

        def process(candle, adx_value):
            if adx_value.MovingAverage is None:
                return
            adx_ma = adx_value.MovingAverage
            if adx_value.Dx is None or adx_value.Dx.Plus is None or adx_value.Dx.Minus is None:
                return
            dx = adx_value.Dx
            plus_di = dx.Plus
            minus_di = dx.Minus

            # Process volume indicators
            sma_val = to_float(process_float(volume_sma, candle.TotalVolume, candle.ServerTime, candle.State == CandleStates.Finished))
            std_dev_val = to_float(process_float(volume_std_dev, candle.TotalVolume, candle.ServerTime, candle.State == CandleStates.Finished))

            # Process the strategy logic
            self.ProcessStrategy(
                candle,
                adx_ma,
                plus_di,
                minus_di,
                candle.TotalVolume,
                sma_val,
                std_dev_val
            )

        subscription.BindEx(adx, process).Start()

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
    def ProcessStrategy(self, candle, adx, di_plus, di_minus, volume, volume_avg, volume_std_dev):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready for trading
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Check for strong trend
        is_strong_trend = adx > self.AdxThreshold

        # Check directional indicators
        is_bullish_trend = di_plus > di_minus
        is_bearish_trend = di_minus > di_plus

        # Check for volume breakout
        volume_threshold = volume_avg + (self.VolumeThresholdFactor * volume_std_dev)
        is_volume_breakout = volume > volume_threshold

        # Trading logic - only enter with strong trend and volume breakout
        if is_strong_trend and is_volume_breakout:
            if is_bullish_trend and self.Position <= 0:
                # Strong bullish trend with volume breakout - Go long
                self.CancelActiveOrders()

                # Calculate position size
                ord_volume = self.Volume + Math.Abs(self.Position)

                # Enter long position
                self.BuyMarket(ord_volume)
            elif is_bearish_trend and self.Position >= 0:
                # Strong bearish trend with volume breakout - Go short
                self.CancelActiveOrders()

                # Calculate position size
                ord_volume = self.Volume + Math.Abs(self.Position)

                # Enter short position
                self.SellMarket(ord_volume)

        # Exit logic - when ADX drops below threshold (trend weakens)
        if adx < 20:
            # Close position on trend weakening
            self.ClosePosition()

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return adx_with_volume_breakout_strategy()
