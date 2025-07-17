import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class supertrend_volume_strategy(Strategy):
    """
    Strategy that combines the Supertrend indicator with volume analysis to identify
    strong trend-following trading opportunities.

    """

    def __init__(self):
        super(supertrend_volume_strategy, self).__init__()

        # Initialize strategy parameters
        self._candleType = self.Param("CandleType", tf(15)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._supertrendPeriod = self.Param("SupertrendPeriod", 10) \
            .SetRange(5, 30) \
            .SetDisplay("Supertrend Period", "Period for Supertrend ATR calculation", "Supertrend Settings") \
            .SetCanOptimize(True)

        self._supertrendMultiplier = self.Param("SupertrendMultiplier", 3.0) \
            .SetRange(1.0, 5.0) \
            .SetDisplay("Supertrend Multiplier", "Multiplier for Supertrend ATR calculation", "Supertrend Settings") \
            .SetCanOptimize(True)

        self._volumePeriod = self.Param("VolumePeriod", 20) \
            .SetRange(5, 50) \
            .SetDisplay("Volume Period", "Period for volume moving average calculation", "Volume Settings") \
            .SetCanOptimize(True)

        self._volumeThreshold = self.Param("VolumeThreshold", 1.5) \
            .SetRange(1.0, 3.0) \
            .SetDisplay("Volume Threshold", "Volume threshold multiplier for volume confirmation", "Volume Settings") \
            .SetCanOptimize(True)

        # Internal variables for indicators
        self._atr = None
        self._volumeSma = None

        # Additional variables for Supertrend calculation
        self._upperBand = None
        self._lowerBand = None
        self._supertrend = None
        self._isBullish = None

    @property
    def CandleType(self):
        """Data type for candles."""
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    @property
    def SupertrendPeriod(self):
        """Period for Supertrend ATR calculation."""
        return self._supertrendPeriod.Value

    @SupertrendPeriod.setter
    def SupertrendPeriod(self, value):
        self._supertrendPeriod.Value = value

    @property
    def SupertrendMultiplier(self):
        """Multiplier for Supertrend ATR calculation."""
        return self._supertrendMultiplier.Value

    @SupertrendMultiplier.setter
    def SupertrendMultiplier(self, value):
        self._supertrendMultiplier.Value = value

    @property
    def VolumePeriod(self):
        """Period for volume moving average calculation."""
        return self._volumePeriod.Value

    @VolumePeriod.setter
    def VolumePeriod(self, value):
        self._volumePeriod.Value = value

    @property
    def VolumeThreshold(self):
        """Volume threshold multiplier for volume confirmation."""
        return self._volumeThreshold.Value

    @VolumeThreshold.setter
    def VolumeThreshold(self, value):
        self._volumeThreshold.Value = value

    def OnStarted(self, time):
        """
        Called when the strategy starts. Initializes indicators and subscriptions.

        :param time: The time when the strategy started.
        """
        super(supertrend_volume_strategy, self).OnStarted(time)

        # Initialize indicators
        self._atr = AverageTrueRange()
        self._atr.Length = self.SupertrendPeriod
        self._volumeSma = SimpleMovingAverage()
        self._volumeSma.Length = self.VolumePeriod

        # Reset Supertrend variables
        self._upperBand = None
        self._lowerBand = None
        self._supertrend = None
        self._isBullish = None

        # Create candle subscription
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind the indicators and candle processor
        subscription.Bind(self._atr, self.ProcessCandle).Start()

        # Set up chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)

            # ATR area
            atrArea = self.CreateChartArea()
            self.DrawIndicator(atrArea, self._atr)

            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, atrValue):
        """
        Process incoming candle with ATR value.

        :param candle: Candle to process.
        :param atrValue: ATR value.
        """
        if candle.State != CandleStates.Finished:
            return

        volumeSmaValue = process_float(self._volumeSma, candle.TotalVolume, candle.ServerTime, candle.State == CandleStates.Finished)

        if not self.IsFormedAndOnlineAndAllowTrading() or not self._atr.IsFormed or not self._volumeSma.IsFormed:
            return

        # Calculate Supertrend
        self.CalculateSupertrend(candle, atrValue)

        if self._supertrend is None or self._isBullish is None:
            return

        # Check if current volume is above threshold compared to average volume
        volumeConfirmation = candle.TotalVolume > volumeSmaValue * self.VolumeThreshold

        # Trading logic
        if volumeConfirmation:
            if self._isBullish and candle.ClosePrice > self._supertrend:
                # Bullish Supertrend with volume confirmation - Long signal
                if self.Position <= 0:
                    self.BuyMarket(self.Volume + Math.Abs(self.Position))
                    self.LogInfo(f"Buy signal: Bullish Supertrend ({self._supertrend:.4f}) with volume confirmation ({candle.TotalVolume} > {volumeSmaValue * self.VolumeThreshold})")
            elif (not self._isBullish) and candle.ClosePrice < self._supertrend:
                # Bearish Supertrend with volume confirmation - Short signal
                if self.Position >= 0:
                    self.SellMarket(self.Volume + Math.Abs(self.Position))
                    self.LogInfo(f"Sell signal: Bearish Supertrend ({self._supertrend:.4f}) with volume confirmation ({candle.TotalVolume} > {volumeSmaValue * self.VolumeThreshold})")

        # Exit logic
        if (self.Position > 0 and not self._isBullish and candle.ClosePrice < self._supertrend) or \
           (self.Position < 0 and self._isBullish and candle.ClosePrice > self._supertrend):
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
                self.LogInfo(f"Exit long: Supertrend turned bearish ({self._supertrend:.4f})")
            elif self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
                self.LogInfo(f"Exit short: Supertrend turned bullish ({self._supertrend:.4f})")

    def CalculateSupertrend(self, candle, atrValue):
        """
        Calculate Supertrend indicator values.

        :param candle: Candle to process.
        :param atrValue: ATR value.
        """
        basicPrice = float((candle.HighPrice + candle.LowPrice) / 2)

        newUpperBand = basicPrice + (self.SupertrendMultiplier * atrValue)
        newLowerBand = basicPrice - (self.SupertrendMultiplier * atrValue)

        if self._upperBand is None or self._lowerBand is None or self._supertrend is None or self._isBullish is None:
            self._upperBand = newUpperBand
            self._lowerBand = newLowerBand
            self._supertrend = newUpperBand
            self._isBullish = False
            return

        # Update upper band
        if newUpperBand < self._upperBand or candle.ClosePrice > self._upperBand:
            self._upperBand = newUpperBand

        # Update lower band
        if newLowerBand > self._lowerBand or candle.ClosePrice < self._lowerBand:
            self._lowerBand = newLowerBand

        # Update Supertrend and trend direction
        if self._supertrend == self._upperBand:
            # Previous trend was bearish
            if candle.ClosePrice > self._upperBand:
                # Trend changed to bullish
                self._supertrend = self._lowerBand
                self._isBullish = True
            else:
                # Trend remains bearish
                self._supertrend = self._upperBand
                self._isBullish = False
        else:
            # Previous trend was bullish
            if candle.ClosePrice < self._lowerBand:
                # Trend changed to bearish
                self._supertrend = self._upperBand
                self._isBullish = False
            else:
                # Trend remains bullish
                self._supertrend = self._lowerBand
                self._isBullish = True

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return supertrend_volume_strategy()