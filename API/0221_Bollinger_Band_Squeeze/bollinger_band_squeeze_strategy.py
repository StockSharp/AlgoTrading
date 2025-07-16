import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import DataType, UnitTypes, Unit
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import BollingerBands, AverageTrueRange, BollingerBandsValue
from StockSharp.Algo.Strategies import Strategy

class bollinger_band_squeeze_strategy(Strategy):
    """
    Bollinger Band Squeeze strategy.
    Trades when volatility decreases (bands squeeze) followed by a breakout.
    """

    def __init__(self):
        super(bollinger_band_squeeze_strategy, self).__init__()

        # Bollinger Bands period.
        self._bollingerPeriod = self.Param("BollingerPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Bollinger Period", "Period for Bollinger Bands calculation", "Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 30, 5)

        # Bollinger Bands multiplier.
        self._bollingerMultiplier = self.Param("BollingerMultiplier", 2.0) \
            .SetRange(0.1, float('inf')) \
            .SetDisplay("Bollinger Multiplier", "Standard deviation multiplier for Bollinger Bands", "Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(1.5, 3.0, 0.5)

        # Period for averaging Bollinger width.
        self._lookbackPeriod = self.Param("LookbackPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback Period", "Period for averaging Bollinger width", "Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 30, 5)

        # Candle type for strategy.
        self._candleType = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle type for strategy", "Common")

        # Internal state fields
        self._bollinger = None
        self._atr = None
        self._prevBollingerWidth = 0.0
        self._avgBollingerWidth = 0.0
        self._bollingerWidthSum = 0.0
        self._bollingerWidths = []

    @property
    def BollingerPeriod(self):
        """Bollinger Bands period."""
        return self._bollingerPeriod.Value

    @BollingerPeriod.setter
    def BollingerPeriod(self, value):
        self._bollingerPeriod.Value = value

    @property
    def BollingerMultiplier(self):
        """Bollinger Bands multiplier."""
        return self._bollingerMultiplier.Value

    @BollingerMultiplier.setter
    def BollingerMultiplier(self, value):
        self._bollingerMultiplier.Value = value

    @property
    def LookbackPeriod(self):
        """Period for averaging Bollinger width."""
        return self._lookbackPeriod.Value

    @LookbackPeriod.setter
    def LookbackPeriod(self, value):
        self._lookbackPeriod.Value = value

    @property
    def CandleType(self):
        """Candle type for strategy."""
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        super(bollinger_band_squeeze_strategy, self).OnStarted(time)

        # Initialize indicator
        self._bollinger = BollingerBands()
        self._bollinger.Length = self.BollingerPeriod
        self._bollinger.Width = self.BollingerMultiplier

        self._atr = AverageTrueRange()
        self._atr.Length = self.BollingerPeriod

        # Reset state
        self._prevBollingerWidth = 0.0
        self._avgBollingerWidth = 0.0
        self._bollingerWidthSum = 0.0
        self._bollingerWidths = []

        # Create subscription and bind indicator
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._bollinger, self._atr, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._bollinger)
            self.DrawOwnTrades(area)

        # Enable position protection
        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),  # No take profit
            stopLoss=Unit(2, UnitTypes.Absolute)     # Stop loss at 2*ATR
        )

    def ProcessCandle(self, candle, bollingerValue, atrValue):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        bollingerTyped = bollingerValue
        if not isinstance(bollingerTyped, BollingerBandsValue):
            bollingerTyped = BollingerBandsValue(bollingerTyped)

        upperBand = bollingerTyped.UpBand if isinstance(bollingerTyped.UpBand, float) or isinstance(bollingerTyped.UpBand, int) else None
        if upperBand is None:
            return

        lowerBand = bollingerTyped.LowBand if isinstance(bollingerTyped.LowBand, float) or isinstance(bollingerTyped.LowBand, int) else None
        if lowerBand is None:
            return

        atr = float(atrValue)

        # Calculate Bollinger width (upper - lower)
        bollingerWidth = upperBand - lowerBand

        # Track average Bollinger width over lookback period
        self._bollingerWidths.append(bollingerWidth)
        self._bollingerWidthSum += bollingerWidth

        if len(self._bollingerWidths) > self.LookbackPeriod:
            oldValue = self._bollingerWidths.pop(0)
            self._bollingerWidthSum -= oldValue

        if len(self._bollingerWidths) == self.LookbackPeriod:
            self._avgBollingerWidth = self._bollingerWidthSum / self.LookbackPeriod

            # Detect Bollinger Band squeeze (narrowing bands)
            isSqueeze = bollingerWidth < self._avgBollingerWidth

            # Breakout after squeeze
            if isSqueeze:
                # Upside breakout
                if candle.ClosePrice > upperBand and self.Position <= 0:
                    self.BuyMarket(self.Volume + Math.Abs(self.Position))
                # Downside breakout
                elif candle.ClosePrice < lowerBand and self.Position >= 0:
                    self.SellMarket(self.Volume + Math.Abs(self.Position))

        self._prevBollingerWidth = bollingerWidth

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return bollinger_band_squeeze_strategy()
