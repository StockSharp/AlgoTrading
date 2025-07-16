import clr

clr.AddReference("System.Drawing")
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from System.Drawing import Color
from StockSharp.Messages import UnitTypes
from StockSharp.Messages import Unit
from StockSharp.Messages import DataType
from StockSharp.Messages import ICandleMessage
from StockSharp.Messages import CandleStates
from StockSharp.Messages import Sides
from StockSharp.Algo.Indicators import StochasticOscillator
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class stochastic_overbought_oversold_strategy(Strategy):
    """
    Strategy based on Stochastic Oscillator's overbought/oversold conditions.
    
    See more examples: https://github.com/StockSharp/AlgoTrading
    """
    def __init__(self):
        super(stochastic_overbought_oversold_strategy, self).__init__()

        # Initialize strategy parameters
        self._stochPeriod = self.Param("StochPeriod", 14) \
            .SetDisplay("Stochastic Period", "Period for Stochastic oscillator calculation", "Indicators")

        self._kPeriod = self.Param("KPeriod", 3) \
            .SetDisplay("K Period", "Smoothing period for Stochastic %K line", "Indicators")

        self._dPeriod = self.Param("DPeriod", 3) \
            .SetDisplay("D Period", "Smoothing period for Stochastic %D line", "Indicators")

        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def StochPeriod(self):
        return self._stochPeriod.Value

    @StochPeriod.setter
    def StochPeriod(self, value):
        self._stochPeriod.Value = value

    @property
    def KPeriod(self):
        return self._kPeriod.Value

    @KPeriod.setter
    def KPeriod(self, value):
        self._kPeriod.Value = value

    @property
    def DPeriod(self):
        return self._dPeriod.Value

    @DPeriod.setter
    def DPeriod(self, value):
        self._dPeriod.Value = value

    @property
    def CandleType(self):
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(stochastic_overbought_oversold_strategy, self).OnStarted(time)

        # Create Stochastic oscillator
        stochastic = StochasticOscillator()
        stochastic.K.Length = self.KPeriod
        stochastic.D.Length = self.DPeriod

        # Subscribe to candles and bind Stochastic indicator
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(stochastic, self.ProcessCandle).Start()

        # Setup stop loss/take profit protection
        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(2, UnitTypes.Percent)
        )
        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, stochastic)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, stochValue):
        """
        Process candle and execute trading logic
        
        :param candle: The candle message.
        :param stochValue: The Stochastic oscillator value.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        stochTyped = stochValue
        kValue = stochTyped.K
        dValue = stochTyped.D

        self.LogInfo("Stochastic %K: {0}, %D: {1}".format(kValue, dValue))

        if kValue < 20 and self.Position <= 0:
            # Oversold condition - Buy
            self.LogInfo("Oversold condition detected. K: {0}, D: {1}".format(kValue, dValue))
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif kValue > 80 and self.Position >= 0:
            # Overbought condition - Sell
            self.LogInfo("Overbought condition detected. K: {0}, D: {1}".format(kValue, dValue))
            self.SellMarket(self.Volume + Math.Abs(self.Position))
        elif kValue > 50 and self.Position > 0:
            # Exit long position when Stochastic moves back above 50
            self.LogInfo("Exiting long position. K: {0}".format(kValue))
            self.SellMarket(self.Position)
        elif kValue < 50 and self.Position < 0:
            # Exit short position when Stochastic moves back below 50
            self.LogInfo("Exiting short position. K: {0}".format(kValue))
            self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return stochastic_overbought_oversold_strategy()