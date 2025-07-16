import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import RelativeStrengthIndex, SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *


class rsi_dynamic_overbought_oversold_strategy(Strategy):
    """
    Strategy based on RSI with dynamic overbought/oversold levels.

    See more examples: https://github.com/StockSharp/AlgoTrading
    """

    def __init__(self):
        super(rsi_dynamic_overbought_oversold_strategy, self).__init__()

        # Initialize strategy parameters
        self._rsiPeriod = self.Param("RsiPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Period", "Period for RSI calculation", "Indicator Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(7, 21, 7)

        self._movingAvgPeriod = self.Param("MovingAvgPeriod", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("MA Period", "Period for moving average of RSI and price", "Indicator Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(20, 100, 10)

        self._stdDevMultiplier = self.Param("StdDevMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("StdDev Multiplier", "Multiplier for standard deviation to define overbought/oversold levels", "Strategy Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._stopLossPercent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Strategy Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles for strategy", "General")

        self._rsiSma = None
        self._rsiStdDev = None

    @property
    def RsiPeriod(self):
        """Period for RSI calculation."""
        return self._rsiPeriod.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsiPeriod.Value = value

    @property
    def MovingAvgPeriod(self):
        """Period for moving average and standard deviation calculation."""
        return self._movingAvgPeriod.Value

    @MovingAvgPeriod.setter
    def MovingAvgPeriod(self, value):
        self._movingAvgPeriod.Value = value

    @property
    def StdDevMultiplier(self):
        """Multiplier for standard deviation to define dynamic levels."""
        return self._stdDevMultiplier.Value

    @StdDevMultiplier.setter
    def StdDevMultiplier(self, value):
        self._stdDevMultiplier.Value = value

    @property
    def StopLossPercent(self):
        """Stop loss percentage."""
        return self._stopLossPercent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stopLossPercent.Value = value

    @property
    def CandleType(self):
        """Candle type parameter."""
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    def OnStarted(self, time):
        """Called when the strategy starts."""
        super(rsi_dynamic_overbought_oversold_strategy, self).OnStarted(time)

        # Create indicators
        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod
        self._rsiSma = SimpleMovingAverage()
        self._rsiSma.Length = self.MovingAvgPeriod
        self._rsiStdDev = StandardDeviation()
        self._rsiStdDev.Length = self.MovingAvgPeriod
        priceSma = SimpleMovingAverage()
        priceSma.Length = self.MovingAvgPeriod

        # Create subscription
        subscription = self.SubscribeCandles(self.CandleType)

        # Create RSI and price SMA processing
        subscription.Bind(rsi, priceSma, self.ProcessCandle).Start()

        # Enable position protection with percentage stop-loss
        self.StartProtection(
            Unit(0),  # we'll handle exits in the strategy logic
            Unit(self.StopLossPercent, UnitTypes.Percent),
            useMarketOrders=True
        )

        # Setup chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, rsi)
            self.DrawIndicator(area, priceSma)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, rsiValue, priceSmaValue):
        """Process candle with RSI value and price SMA."""
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        smaValue = self._rsiSma.Process(rsiValue, candle.ServerTime, candle.State == CandleStates.Finished)
        stdDevValue = self._rsiStdDev.Process(rsiValue, candle.ServerTime, candle.State == CandleStates.Finished)

        # Get values from indicators
        rsiSmaValue = to_float(smaValue)
        rsiStdDevValue = to_float(stdDevValue)

        # Get the indicator containers using container names

        # Calculate dynamic overbought/oversold levels
        dynamicOverbought = rsiSmaValue + self.StdDevMultiplier * rsiStdDevValue
        dynamicOversold = rsiSmaValue - self.StdDevMultiplier * rsiStdDevValue

        # Make sure levels are within RSI range (0-100)
        dynamicOverbought = Math.Min(dynamicOverbought, 90.0)
        dynamicOversold = Math.Max(dynamicOversold, 10.0)

        # Log current values
        self.LogInfo(f"RSI: {rsiValue}, MA: {priceSmaValue}, DynamicOverbought: {dynamicOverbought}, DynamicOversold: {dynamicOversold}")

        # Define entry conditions
        longEntryCondition = rsiValue < dynamicOversold and candle.ClosePrice > priceSmaValue and self.Position <= 0
        shortEntryCondition = rsiValue > dynamicOverbought and candle.ClosePrice < priceSmaValue and self.Position >= 0

        # Define exit conditions
        longExitCondition = rsiValue > 50 and self.Position > 0
        shortExitCondition = rsiValue < 50 and self.Position < 0

        # Execute trading logic
        if longEntryCondition:
            positionSize = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(positionSize)
            self.LogInfo(f"Long entry: Price={candle.ClosePrice}, RSI={rsiValue}, Oversold={dynamicOversold}")
        elif shortEntryCondition:
            positionSize = self.Volume + Math.Abs(self.Position)
            self.SellMarket(positionSize)
            self.LogInfo(f"Short entry: Price={candle.ClosePrice}, RSI={rsiValue}, Overbought={dynamicOverbought}")
        elif longExitCondition:
            self.SellMarket(Math.Abs(self.Position))
            self.LogInfo(f"Long exit: Price={candle.ClosePrice}, RSI={rsiValue}")
        elif shortExitCondition:
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo(f"Short exit: Price={candle.ClosePrice}, RSI={rsiValue}")

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return rsi_dynamic_overbought_oversold_strategy()
