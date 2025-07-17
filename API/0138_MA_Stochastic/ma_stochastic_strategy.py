import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, StochasticOscillator
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class ma_stochastic_strategy(Strategy):
    """
    Strategy that combines Moving Average and Stochastic Oscillator.
    Enters positions when price is above MA and Stochastic shows oversold conditions (for longs)
    or when price is below MA and Stochastic shows overbought conditions (for shorts).
    """

    def __init__(self):
        super(ma_stochastic_strategy, self).__init__()

        # Initialize strategy parameters
        self._ma_period = self.Param("MaPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("MA Period", "Period of the Moving Average", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5)

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
            .SetOptimize(1.0, 5.0, 1.0)

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def MaPeriod(self):
        """Moving Average period."""
        return self._ma_period.Value

    @MaPeriod.setter
    def MaPeriod(self, value):
        self._ma_period.Value = value

    @property
    def StochPeriod(self):
        """Stochastic period."""
        return self._stoch_period.Value

    @StochPeriod.setter
    def StochPeriod(self, value):
        self._stoch_period.Value = value

    @property
    def StochK(self):
        """Stochastic %K period."""
        return self._stoch_k.Value

    @StochK.setter
    def StochK(self, value):
        self._stoch_k.Value = value

    @property
    def StochD(self):
        """Stochastic %D period."""
        return self._stoch_d.Value

    @StochD.setter
    def StochD(self, value):
        self._stoch_d.Value = value

    @property
    def StochOversold(self):
        """Stochastic oversold level."""
        return self._stoch_oversold.Value

    @StochOversold.setter
    def StochOversold(self, value):
        self._stoch_oversold.Value = value

    @property
    def StochOverbought(self):
        """Stochastic overbought level."""
        return self._stoch_overbought.Value

    @StochOverbought.setter
    def StochOverbought(self, value):
        self._stoch_overbought.Value = value

    @property
    def StopLossPercent(self):
        """Stop loss percentage."""
        return self._stop_loss_percent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def CandleType(self):
        """Candle type for strategy calculation."""
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(ma_stochastic_strategy, self).OnStarted(time)

        # Create indicators
        ma = SimpleMovingAverage()
        ma.Length = self.MaPeriod

        stochastic = StochasticOscillator()
        stochastic.K.Length = self.StochK
        stochastic.D.Length = self.StochD

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(ma, stochastic, self.ProcessCandle).Start()

        # Setup position protection
        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent)
        )
        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma)
            self.DrawIndicator(area, stochastic)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, ma_value, stoch_value):
        """Process candles and indicator values."""
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        ma_dec = to_float(ma_value)
        stoch_k_value = stoch_value.K

        # Long entry: price above MA and Stochastic is oversold
        if candle.ClosePrice > ma_dec and stoch_k_value < self.StochOversold and self.Position <= 0:
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)
        # Short entry: price below MA and Stochastic is overbought
        elif candle.ClosePrice < ma_dec and stoch_k_value > self.StochOverbought and self.Position >= 0:
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)
        # Long exit: price falls below MA
        elif self.Position > 0 and candle.ClosePrice < ma_dec:
            self.SellMarket(Math.Abs(self.Position))
        # Short exit: price rises above MA
        elif self.Position < 0 and candle.ClosePrice > ma_dec:
            self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return ma_stochastic_strategy()
