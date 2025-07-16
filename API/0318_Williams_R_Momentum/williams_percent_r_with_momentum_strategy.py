import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *
from StockSharp.Algo.Indicators import WilliamsR, Momentum, SimpleMovingAverage

class williams_percent_r_with_momentum_strategy(Strategy):
    """
    Strategy based on Williams %R with Momentum filter.
    """

    def __init__(self):
        super(williams_percent_r_with_momentum_strategy, self).__init__()

        # Williams %R period parameter.
        self._williamsRPeriod = self.Param("WilliamsRPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("Williams %R Period", "Period for Williams %R calculation", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(5, 30, 5)

        # Momentum period parameter.
        self._momentumPeriod = self.Param("MomentumPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("Momentum Period", "Period for Momentum calculation", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(5, 30, 5)

        # Williams %R oversold level parameter.
        self._williamsROversold = self.Param("WilliamsROversold", -80) \
            .SetDisplay("Williams %R Oversold", "Williams %R oversold level", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(-90, -70, 5)

        # Williams %R overbought level parameter.
        self._williamsROverbought = self.Param("WilliamsROverbought", -20) \
            .SetDisplay("Williams %R Overbought", "Williams %R overbought level", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(-30, -10, 5)

        # Candle type parameter.
        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def WilliamsRPeriod(self):
        """Williams %R period parameter."""
        return self._williamsRPeriod.Value

    @WilliamsRPeriod.setter
    def WilliamsRPeriod(self, value):
        self._williamsRPeriod.Value = value

    @property
    def MomentumPeriod(self):
        """Momentum period parameter."""
        return self._momentumPeriod.Value

    @MomentumPeriod.setter
    def MomentumPeriod(self, value):
        self._momentumPeriod.Value = value

    @property
    def WilliamsROversold(self):
        """Williams %R oversold level parameter."""
        return self._williamsROversold.Value

    @WilliamsROversold.setter
    def WilliamsROversold(self, value):
        self._williamsROversold.Value = value

    @property
    def WilliamsROverbought(self):
        """Williams %R overbought level parameter."""
        return self._williamsROverbought.Value

    @WilliamsROverbought.setter
    def WilliamsROverbought(self, value):
        self._williamsROverbought.Value = value

    @property
    def CandleType(self):
        """Candle type parameter."""
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    def GetWorkingSecurities(self):
        return [
            (self.Security, self.CandleType)
        ]

    def OnStarted(self, time):
        super(williams_percent_r_with_momentum_strategy, self).OnStarted(time)

        # Create indicators
        williamsR = WilliamsR()
        williamsR.Length = self.WilliamsRPeriod
        momentum = Momentum()
        momentum.Length = self.MomentumPeriod
        momentumSma = SimpleMovingAverage()
        momentumSma.Length = self.MomentumPeriod

        # Subscribe to candles and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)

        def on_process(candle, williamsRValue, momentumValue):
            # Calculate momentum average
            momentumAvg = to_float(momentumSma.Process(momentumValue, candle.ServerTime, candle.State == CandleStates.Finished))

            # Process the strategy logic
            self.ProcessStrategy(candle, williamsRValue, momentumValue, momentumAvg)

        subscription.Bind(williamsR, momentum, on_process).Start()

        # Setup chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, williamsR)
            self.DrawIndicator(area, momentum)
            self.DrawOwnTrades(area)

        # Setup position protection
        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(1, UnitTypes.Percent)
        )

    def ProcessStrategy(self, candle, williamsRValue, momentumValue, momentumAvg):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready for trading
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Check momentum - rising or falling
        isMomentumRising = momentumValue > momentumAvg

        # Trading logic
        if williamsRValue < self.WilliamsROversold and isMomentumRising and self.Position <= 0:
            # Williams %R oversold with rising momentum - Go long
            self.CancelActiveOrders()

            # Calculate position size
            volume = self.Volume + Math.Abs(self.Position)

            # Enter long position
            self.BuyMarket(volume)
        elif williamsRValue > self.WilliamsROverbought and not isMomentumRising and self.Position >= 0:
            # Williams %R overbought with falling momentum - Go short
            self.CancelActiveOrders()

            # Calculate position size
            volume = self.Volume + Math.Abs(self.Position)

            # Enter short position
            self.SellMarket(volume)

        # Exit logic - when Williams %R crosses the middle (-50) level
        if (self.Position > 0 and williamsRValue > -50) or (self.Position < 0 and williamsRValue < -50):
            # Close position
            self.ClosePosition()

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return williams_percent_r_with_momentum_strategy()
