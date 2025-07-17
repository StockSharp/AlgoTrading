import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, UnitTypes, Unit, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, StochasticOscillator
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class rsi_stochastic_strategy(Strategy):
    """
    Strategy that combines RSI and Stochastic Oscillator for double confirmation
    of oversold and overbought conditions.

    """

    def __init__(self):
        super(rsi_stochastic_strategy, self).__init__()

        # Initialize strategy parameters
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Period", "Period of the RSI indicator", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(7, 21, 7)

        self._rsi_oversold = self.Param("RsiOversold", 30.0) \
            .SetNotNegative() \
            .SetDisplay("RSI Oversold", "RSI level considered oversold", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(20.0, 40.0, 5.0)

        self._rsi_overbought = self.Param("RsiOverbought", 70.0) \
            .SetNotNegative() \
            .SetDisplay("RSI Overbought", "RSI level considered overbought", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(60.0, 80.0, 5.0)

        self._stoch_period = self.Param("StochPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("Stochastic Period", "Period of the Stochastic Oscillator", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(5, 20, 5)

        self._stoch_k = self.Param("StochK", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Stochastic %K", "Smoothing of the %K line", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(1, 5, 1)

        self._stoch_d = self.Param("StochD", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Stochastic %D", "Smoothing of the %D line", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(1, 5, 1)

        self._stoch_oversold = self.Param("StochOversold", 20.0) \
            .SetNotNegative() \
            .SetDisplay("Stochastic Oversold", "Level considered oversold", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(10.0, 30.0, 5.0)

        self._stoch_overbought = self.Param("StochOverbought", 80.0) \
            .SetNotNegative() \
            .SetDisplay("Stochastic Overbought", "Level considered overbought", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(70.0, 90.0, 5.0)

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop loss as percentage of entry price", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def rsi_period(self):
        """RSI period."""
        return self._rsi_period.Value

    @rsi_period.setter
    def rsi_period(self, value):
        self._rsi_period.Value = value

    @property
    def rsi_oversold(self):
        """RSI oversold level."""
        return self._rsi_oversold.Value

    @rsi_oversold.setter
    def rsi_oversold(self, value):
        self._rsi_oversold.Value = value

    @property
    def rsi_overbought(self):
        """RSI overbought level."""
        return self._rsi_overbought.Value

    @rsi_overbought.setter
    def rsi_overbought(self, value):
        self._rsi_overbought.Value = value

    @property
    def stoch_period(self):
        """Stochastic period."""
        return self._stoch_period.Value

    @stoch_period.setter
    def stoch_period(self, value):
        self._stoch_period.Value = value

    @property
    def stoch_k(self):
        """Stochastic %K period."""
        return self._stoch_k.Value

    @stoch_k.setter
    def stoch_k(self, value):
        self._stoch_k.Value = value

    @property
    def stoch_d(self):
        """Stochastic %D period."""
        return self._stoch_d.Value

    @stoch_d.setter
    def stoch_d(self, value):
        self._stoch_d.Value = value

    @property
    def stoch_oversold(self):
        """Stochastic oversold level."""
        return self._stoch_oversold.Value

    @stoch_oversold.setter
    def stoch_oversold(self, value):
        self._stoch_oversold.Value = value

    @property
    def stoch_overbought(self):
        """Stochastic overbought level."""
        return self._stoch_overbought.Value

    @stoch_overbought.setter
    def stoch_overbought(self, value):
        self._stoch_overbought.Value = value

    @property
    def stop_loss_percent(self):
        """Stop loss percentage."""
        return self._stop_loss_percent.Value

    @stop_loss_percent.setter
    def stop_loss_percent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def candle_type(self):
        """Candle type for strategy calculation."""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.

        :param time: The time when the strategy started.
        """
        super(rsi_stochastic_strategy, self).OnStarted(time)

        # Create indicators
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period

        stochastic = StochasticOscillator()
        stochastic.K.Length = self.stoch_k
        stochastic.D.Length = self.stoch_d

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(rsi, stochastic, self.ProcessIndicators).Start()

        # Setup position protection
        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(self.stop_loss_percent, UnitTypes.Percent)
        )
        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, rsi)
            self.DrawIndicator(area, stochastic)
            self.DrawOwnTrades(area)

    def ProcessIndicators(self, candle, rsi_value, stoch_value):
        """
        Process indicator values.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        rsi = to_float(rsi_value)
        stoch_k = stoch_value.K

        # Long entry: double confirmation of oversold condition
        if rsi < self.rsi_oversold and stoch_k < self.stoch_oversold and self.Position <= 0:
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)
        # Short entry: double confirmation of overbought condition
        elif rsi > self.rsi_overbought and stoch_k > self.stoch_overbought and self.Position >= 0:
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)
        # Long exit: RSI returns to neutral zone
        elif self.Position > 0 and rsi > 50:
            self.SellMarket(Math.Abs(self.Position))
        # Short exit: RSI returns to neutral zone
        elif self.Position < 0 and rsi < 50:
            self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return rsi_stochastic_strategy()
