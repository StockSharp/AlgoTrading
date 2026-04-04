import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import WilliamsR, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *
from StockSharp.Algo.Indicators import DecimalIndicatorValue


class williams_r_breakout_strategy(Strategy):
    """
    Strategy that trades on Williams %R breakouts.
    When Williams %R crosses above the overbought level or below the oversold level,
    it enters position in the corresponding direction. Exits when Williams %R
    crosses back through its moving average.
    """

    def __init__(self):
        super(williams_r_breakout_strategy, self).__init__()

        self._williamsRPeriod = self.Param("WilliamsRPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("Williams %R Period", "Period for Williams %R indicator", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 30, 2)

        self._avgPeriod = self.Param("AvgPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Average Period", "Period for Williams %R average calculation", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5)

        self._overboughtLevel = self.Param("OverboughtLevel", -10.0) \
            .SetDisplay("Overbought Level", "Williams %R overbought threshold", "Indicators")

        self._oversoldLevel = self.Param("OversoldLevel", -90.0) \
            .SetDisplay("Oversold Level", "Williams %R oversold threshold", "Indicators")

        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._stopLoss = self.Param("StopLoss", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop Loss percentage", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 5.0, 0.5)

        self._prevInitialized = False
        self._prevWilliamsRValue = 0
        self._prevWilliamsRAvgValue = 0
        self._cooldown = 0
        self._williamsR = None
        self._williamsRAverage = None

    @property
    def WilliamsRPeriod(self):
        return self._williamsRPeriod.Value

    @WilliamsRPeriod.setter
    def WilliamsRPeriod(self, value):
        self._williamsRPeriod.Value = value

    @property
    def AvgPeriod(self):
        return self._avgPeriod.Value

    @AvgPeriod.setter
    def AvgPeriod(self, value):
        self._avgPeriod.Value = value

    @property
    def OverboughtLevel(self):
        return self._overboughtLevel.Value

    @OverboughtLevel.setter
    def OverboughtLevel(self, value):
        self._overboughtLevel.Value = value

    @property
    def OversoldLevel(self):
        return self._oversoldLevel.Value

    @OversoldLevel.setter
    def OversoldLevel(self, value):
        self._oversoldLevel.Value = value

    @property
    def CandleType(self):
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    @property
    def StopLoss(self):
        return self._stopLoss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stopLoss.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(williams_r_breakout_strategy, self).OnReseted()
        self._prevInitialized = False
        self._prevWilliamsRValue = 0
        self._prevWilliamsRAvgValue = 0
        self._cooldown = 0

    def OnStarted2(self, time):
        super(williams_r_breakout_strategy, self).OnStarted2(time)

        # Create indicators
        self._williamsR = WilliamsR()
        self._williamsR.Length = self.WilliamsRPeriod
        self._williamsRAverage = SimpleMovingAverage()
        self._williamsRAverage.Length = self.AvgPeriod

        # Create subscription and bind Williams %R
        subscription = self.SubscribeCandles(self.CandleType)

        subscription.BindEx(self._williamsR, self.ProcessCandle).Start()

        # Enable stop loss protection
        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(self.StopLoss, UnitTypes.Percent)
        )

        # Create chart area for visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._williamsR)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, wrValue):
        if candle.State != CandleStates.Finished:
            return

        if not wrValue.IsFinal:
            return

        wrVal = float(wrValue)

        # Feed WR value through SMA to get the average (must set IsFinal for buffer to accumulate)
        avgInput = DecimalIndicatorValue(self._williamsRAverage, wrVal, candle.ServerTime)
        avgInput.IsFinal = True
        avgResult = self._williamsRAverage.Process(avgInput)

        if not self._williamsRAverage.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        currentWilliamsRAvg = float(avgResult)

        if not self._prevInitialized:
            self._prevWilliamsRValue = wrVal
            self._prevWilliamsRAvgValue = currentWilliamsRAvg
            self._prevInitialized = True
            return

        # Cooldown between trades (minimum bars between signals)
        if self._cooldown > 0:
            self._cooldown -= 1
            self._prevWilliamsRValue = wrVal
            self._prevWilliamsRAvgValue = currentWilliamsRAvg
            return

        cooldownBars = 100

        # Williams %R breakout detection using crossover of extreme levels
        # Williams %R crossing above overbought level from below = bullish breakout
        if self._prevWilliamsRValue <= self.OverboughtLevel and wrVal > self.OverboughtLevel and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._cooldown = cooldownBars
        # Williams %R crossing below oversold level from above = bearish breakout
        elif self._prevWilliamsRValue >= self.OversoldLevel and wrVal < self.OversoldLevel and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._cooldown = cooldownBars
        # Exit long when Williams %R drops below the midpoint (-50)
        elif self.Position > 0 and self._prevWilliamsRValue >= -50.0 and wrVal < -50.0:
            self.SellMarket(abs(self.Position))
            self._cooldown = cooldownBars
        # Exit short when Williams %R rises above the midpoint (-50)
        elif self.Position < 0 and self._prevWilliamsRValue <= -50.0 and wrVal > -50.0:
            self.BuyMarket(abs(self.Position))
            self._cooldown = cooldownBars

        # Update previous values
        self._prevWilliamsRValue = wrVal
        self._prevWilliamsRAvgValue = currentWilliamsRAvg

    def CreateClone(self):
        return williams_r_breakout_strategy()
