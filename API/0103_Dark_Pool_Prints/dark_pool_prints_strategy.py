import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import DataType, UnitTypes, Unit
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageDirectionalIndex, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class dark_pool_prints_strategy(Strategy):
    """
    Strategy that detects unusually high volume (dark pool prints) and enters positions based on that.

    See more examples: https://github.com/StockSharp/AlgoTrading
    """

    def __init__(self):
        super(dark_pool_prints_strategy, self).__init__()

        # Candle type and timeframe for the strategy.
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use for analysis", "General")

        # Period for volume average calculation.
        self._volume_period = self.Param("VolumePeriod", 20) \
            .SetDisplay("Volume Period", "Period for volume average calculation", "Volume") \
            .SetRange(5, 50)

        # Multiplier to determine significant volume.
        # Volume > Average(Volume) * VolumeMultiplier is considered significant.
        self._volume_multiplier = self.Param("VolumeMultiplier", 2.0) \
            .SetDisplay("Volume Multiplier", "Trigger multiplier for volume significance", "Volume") \
            .SetRange(1.5, 5.0)

        # Period for moving average calculation.
        self._ma_period = self.Param("MaPeriod", 20) \
            .SetDisplay("MA Period", "Period for moving average calculation", "Trend") \
            .SetRange(5, 50)

        # ATR multiplier for stop-loss calculation.
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0) \
            .SetDisplay("ATR Multiplier", "Multiplier for ATR-based stop-loss", "Protection") \
            .SetRange(1.0, 5.0)

        self._ma = None
        self._volume_average = None
        self._adx = None  # To ensure we're in a trending market
        self._atr = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def VolumePeriod(self):
        return self._volume_period.Value

    @VolumePeriod.setter
    def VolumePeriod(self, value):
        self._volume_period.Value = value

    @property
    def VolumeMultiplier(self):
        return self._volume_multiplier.Value

    @VolumeMultiplier.setter
    def VolumeMultiplier(self, value):
        self._volume_multiplier.Value = value

    @property
    def MaPeriod(self):
        return self._ma_period.Value

    @MaPeriod.setter
    def MaPeriod(self, value):
        self._ma_period.Value = value

    @property
    def AtrMultiplier(self):
        return self._atr_multiplier.Value

    @AtrMultiplier.setter
    def AtrMultiplier(self, value):
        self._atr_multiplier.Value = value

    def OnStarted(self, time):
        super(dark_pool_prints_strategy, self).OnStarted(time)

        # Initialize indicators
        self._ma = SimpleMovingAverage()
        self._ma.Length = self.MaPeriod
        self._volume_average = SimpleMovingAverage()
        self._volume_average.Length = self.VolumePeriod
        self._adx = AverageDirectionalIndex()
        self._adx.Length = 14  # Standard ADX period
        self._atr = AverageTrueRange()
        self._atr.Length = 14  # Standard ATR period

        # Create subscription for candles
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind indicators and processor
        subscription.BindEx(self._ma, self._volume_average, self._adx, self._atr, self.ProcessCandle).Start()

        # Enable stop-loss protection
        self.StartProtection(
            takeProfit=Unit(0),
            stopLoss=Unit(self.AtrMultiplier, UnitTypes.Absolute)
        )
        # Setup chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ma)
            self.DrawIndicator(area, self._adx)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, ma, volume_avg, adx, atr):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Retrieve ADX moving average value
        try:
            if hasattr(adx, 'MovingAverage') and adx.MovingAverage is not None:
                adx_ma = float(adx.MovingAverage)
            else:
                adx_ma = float(adx)
        except:
            return

        ma_decimal = float(ma)

        # Check if we have a strong trend (ADX > 25)
        isStrongTrend = adx_ma > 25

        # Check if current volume is significantly higher than average
        isHighVolume = candle.TotalVolume > float(volume_avg) * self.VolumeMultiplier

        if not isHighVolume or not isStrongTrend:
            return

        # Determine if the candle is bullish or bearish
        isBullish = candle.ClosePrice > candle.OpenPrice

        # Determine if price is above or below the moving average
        isAboveMA = candle.ClosePrice > ma_decimal
        isBelowMA = candle.ClosePrice < ma_decimal

        # Entry rules for long or short positions
        if isBullish and isAboveMA and self.Position <= 0:
            # Bullish candle + high volume + price above MA = Long signal
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)

            self.LogInfo(f"Dark Pool Print detected. Bullish candle with volume {candle.TotalVolume} (avg: {volume_avg}). Buying at {candle.ClosePrice}")
        elif not isBullish and isBelowMA and self.Position >= 0:
            # Bearish candle + high volume + price below MA = Short signal
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)

            self.LogInfo(f"Dark Pool Print detected. Bearish candle with volume {candle.TotalVolume} (avg: {volume_avg}). Selling at {candle.ClosePrice}")

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return dark_pool_prints_strategy()

