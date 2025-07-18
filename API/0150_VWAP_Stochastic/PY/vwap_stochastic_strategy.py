import clr

clr.AddReference("System.Drawing")
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import UnitTypes, Unit, DataType, CandleStates
from StockSharp.Algo.Indicators import VolumeWeightedMovingAverage, StochasticOscillator
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class vwap_stochastic_strategy(Strategy):
    """
    Strategy combining VWAP and Stochastic indicators.
    Buys when price is below VWAP and Stochastic is oversold.
    Sells when price is above VWAP and Stochastic is overbought.

    """

    def __init__(self):
        super(vwap_stochastic_strategy, self).__init__()

        # Initialize strategy parameters
        self._stochPeriod = self.Param("StochPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("Stoch Period", "Period for Stochastic calculation", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(7, 21, 7)

        self._stochKPeriod = self.Param("StochKPeriod", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Stoch %K", "Smoothing period for %K line", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(1, 5, 1)

        self._stochDPeriod = self.Param("StochDPeriod", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Stoch %D", "Smoothing period for %D line", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(1, 5, 1)

        self._overboughtLevel = self.Param("OverboughtLevel", 80.0) \
            .SetRange(50, 95) \
            .SetDisplay("Overbought Level", "Level considered overbought", "Trading Levels") \
            .SetCanOptimize(True) \
            .SetOptimize(70, 90, 5)

        self._oversoldLevel = self.Param("OversoldLevel", 20.0) \
            .SetRange(5, 50) \
            .SetDisplay("Oversold Level", "Level considered oversold", "Trading Levels") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 30, 5)

        self._stopLossPercent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 5.0, 0.5)

        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def StochPeriod(self):
        return self._stochPeriod.Value

    @StochPeriod.setter
    def StochPeriod(self, value):
        self._stochPeriod.Value = value

    @property
    def StochKPeriod(self):
        return self._stochKPeriod.Value

    @StochKPeriod.setter
    def StochKPeriod(self, value):
        self._stochKPeriod.Value = value

    @property
    def StochDPeriod(self):
        return self._stochDPeriod.Value

    @StochDPeriod.setter
    def StochDPeriod(self, value):
        self._stochDPeriod.Value = value

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
    def StopLossPercent(self):
        return self._stopLossPercent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stopLossPercent.Value = value

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
        super(vwap_stochastic_strategy, self).OnStarted(time)

        # Create indicators
        vwap = VolumeWeightedMovingAverage()
        stochastic = StochasticOscillator()
        stochastic.K.Length = self.StochKPeriod
        stochastic.D.Length = self.StochDPeriod

        # Create subscription
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind indicators to candles
        subscription.BindEx(vwap, stochastic, self.ProcessCandle).Start()

        # Enable stop-loss and take-profit protection
        self.StartProtection(
            takeProfit=None,
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent),
            isStopTrailing=True
        )
        # Setup chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, vwap)

            # Create second area for stochastic
            stochArea = self.CreateChartArea()
            self.DrawIndicator(stochArea, stochastic)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, vwapValue, stochValue):
        """
        Skip unfinished candles and apply trading logic using VWAP and Stochastic.

        :param candle: The candle message.
        :param vwapValue: The VWAP indicator value.
        :param stochValue: The Stochastic indicator value.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        kValue = stochValue.K

        vwapDec = float(vwapValue)

        # Trading logic
        if candle.ClosePrice < vwapDec and kValue < self.OversoldLevel and self.Position <= 0:
            # Price below VWAP and stochastic shows oversold - Buy
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)
        elif candle.ClosePrice > vwapDec and kValue > self.OverboughtLevel and self.Position >= 0:
            # Price above VWAP and stochastic shows overbought - Sell
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)
        elif self.Position > 0 and candle.ClosePrice > vwapDec:
            # Exit long position when price crosses above VWAP
            self.SellMarket(Math.Abs(self.Position))
        elif self.Position < 0 and candle.ClosePrice < vwapDec:
            # Exit short position when price crosses below VWAP
            self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return vwap_stochastic_strategy()
