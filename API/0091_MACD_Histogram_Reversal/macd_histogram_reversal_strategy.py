import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceHistogram
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class macd_histogram_reversal_strategy(Strategy):
    """
    MACD Histogram Reversal Strategy.
    Enters long when MACD histogram crosses above zero.
    Enters short when MACD histogram crosses below zero.

    """

    def __init__(self):
        super(macd_histogram_reversal_strategy, self).__init__()

        # Initialize strategy parameters
        self._fastPeriod = self.Param("FastPeriod", 12) \
            .SetDisplay("Fast Period", "Fast period for MACD calculation", "MACD Settings") \
            .SetRange(8, 16) \
            .SetCanOptimize(True)

        self._slowPeriod = self.Param("SlowPeriod", 26) \
            .SetDisplay("Slow Period", "Slow period for MACD calculation", "MACD Settings") \
            .SetRange(20, 30) \
            .SetCanOptimize(True)

        self._signalPeriod = self.Param("SignalPeriod", 9) \
            .SetDisplay("Signal Period", "Signal period for MACD calculation", "MACD Settings") \
            .SetRange(7, 13) \
            .SetCanOptimize(True)

        self._stopLoss = self.Param("StopLoss", Unit(2, UnitTypes.Percent)) \
            .SetDisplay("Stop Loss", "Stop loss as percentage from entry price", "Risk Management") \
            .SetRange(1.0, 3.0) \
            .SetCanOptimize(True)

        self._candleType = self.Param("CandleType", tf(15)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Previous MACD histogram value
        self._prevHistogram = None

    @property
    def FastPeriod(self):
        """Fast period for MACD calculation."""
        return self._fastPeriod.Value

    @FastPeriod.setter
    def FastPeriod(self, value):
        self._fastPeriod.Value = value

    @property
    def SlowPeriod(self):
        """Slow period for MACD calculation."""
        return self._slowPeriod.Value

    @SlowPeriod.setter
    def SlowPeriod(self, value):
        self._slowPeriod.Value = value

    @property
    def SignalPeriod(self):
        """Signal period for MACD calculation."""
        return self._signalPeriod.Value

    @SignalPeriod.setter
    def SignalPeriod(self, value):
        self._signalPeriod.Value = value

    @property
    def StopLoss(self):
        """Stop loss percentage from entry price."""
        return self._stopLoss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stopLoss.Value = value

    @property
    def CandleType(self):
        """Type of candles to use."""
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    def OnReseted(self):
        """Resets internal state when strategy is reset."""
        super(macd_histogram_reversal_strategy, self).OnReseted()
        self._prevHistogram = None

    def OnStarted(self, time):
        """Called when the strategy starts."""
        super(macd_histogram_reversal_strategy, self).OnStarted(time)

        # Enable position protection using stop-loss
        self.StartProtection(
            takeProfit=None,
            stopLoss=self.StopLoss
        )
        # Initialize state
        self._prevHistogram = None

        # Create MACD histogram indicator
        macdHistogram = MovingAverageConvergenceDivergenceHistogram()
        macdHistogram.Macd.ShortMa.Length = self.FastPeriod
        macdHistogram.Macd.LongMa.Length = self.SlowPeriod
        macdHistogram.SignalMa.Length = self.SignalPeriod

        # Create subscription
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind indicator and process candles
        subscription.BindEx(macdHistogram, self.ProcessCandle).Start()

        # Setup chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, macdHistogram)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, macdValue):
        """Process candle with MACD histogram value."""
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        macd = macdValue.Macd
        signal = macdValue.Signal

        # If this is the first calculation, just store the value
        if self._prevHistogram is None:
            self._prevHistogram = macd
            return

        # Check for zero-line crossovers
        crossedAboveZero = self._prevHistogram < 0 and macd > 0
        crossedBelowZero = self._prevHistogram > 0 and macd < 0

        # Long entry: MACD histogram crossed above zero
        if crossedAboveZero and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
            self.LogInfo("Long entry: MACD histogram crossed above zero ({0} -> {1})".format(
                self._prevHistogram, macd))
        # Short entry: MACD histogram crossed below zero
        elif crossedBelowZero and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))
            self.LogInfo("Short entry: MACD histogram crossed below zero ({0} -> {1})".format(
                self._prevHistogram, macd))

        # Update previous value
        self._prevHistogram = macd

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return macd_histogram_reversal_strategy()
