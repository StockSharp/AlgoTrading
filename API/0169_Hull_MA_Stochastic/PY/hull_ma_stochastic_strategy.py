import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import HullMovingAverage, StochasticOscillator, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class hull_ma_stochastic_strategy(Strategy):
    """
    Hull Moving Average + Stochastic Oscillator strategy.
    Strategy enters when HMA trend direction changes with Stochastic confirming oversold/overbought conditions.

    """

    def __init__(self):
        super(hull_ma_stochastic_strategy, self).__init__()

        # Initialize strategy parameters
        self._hmaPeriod = self.Param("HmaPeriod", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("HMA Period", "Hull Moving Average period", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(4, 30, 2)

        self._stochPeriod = self.Param("StochPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("Stochastic Period", "Stochastic oscillator period", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(5, 30, 5)

        self._stochK = self.Param("StochK", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Stochastic %K", "Stochastic %K period", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(1, 10, 1)

        self._stochD = self.Param("StochD", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Stochastic %D", "Stochastic %D period", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(1, 10, 1)

        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._stopLossPercent = self.Param("StopLossPercent", 1.0) \
            .SetNotNegative() \
            .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(0.5, 2.0, 0.5)

        # Previous HMA value for trend detection
        self._prev_hma_value = 0.0

    @property
    def hma_period(self):
        """Hull Moving Average period."""
        return self._hmaPeriod.Value

    @hma_period.setter
    def hma_period(self, value):
        self._hmaPeriod.Value = value

    @property
    def stoch_period(self):
        """Stochastic period."""
        return self._stochPeriod.Value

    @stoch_period.setter
    def stoch_period(self, value):
        self._stochPeriod.Value = value

    @property
    def stoch_k(self):
        """Stochastic %K period."""
        return self._stochK.Value

    @stoch_k.setter
    def stoch_k(self, value):
        self._stochK.Value = value

    @property
    def stoch_d(self):
        """Stochastic %D period."""
        return self._stochD.Value

    @stoch_d.setter
    def stoch_d(self, value):
        self._stochD.Value = value

    @property
    def candle_type(self):
        """Candle type."""
        return self._candleType.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candleType.Value = value

    @property
    def stop_loss_percent(self):
        """Stop-loss percentage."""
        return self._stopLossPercent.Value

    @stop_loss_percent.setter
    def stop_loss_percent(self, value):
        self._stopLossPercent.Value = value

    def OnReseted(self):
        """Resets internal state when strategy is reset."""
        super(hull_ma_stochastic_strategy, self).OnReseted()
        self._prev_hma_value = 0.0

    def OnStarted(self, time):
        """Called when the strategy starts. Sets up indicators, subscriptions, and charting."""
        super(hull_ma_stochastic_strategy, self).OnStarted(time)

        # Initialize the previous HMA value
        self._prev_hma_value = 0.0

        # Create indicators
        self._hma = HullMovingAverage()
        self._hma.Length = self.hma_period

        self._stochastic = StochasticOscillator()
        self._stochastic.K.Length = self.stoch_k
        self._stochastic.D.Length = self.stoch_d

        self._atr = AverageTrueRange()
        self._atr.Length = 14

        # Subscribe to candles and bind indicators
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._hma, self._stochastic, self._atr, self.ProcessCandle).Start()

        # Setup chart
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._hma)

            second_area = self.CreateChartArea()
            if second_area is not None:
                self.DrawIndicator(second_area, self._stochastic)

            self.DrawOwnTrades(area)

        # Start protection
        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(self.stop_loss_percent, UnitTypes.Percent)
        )
    def ProcessCandle(self, candle, hma_value, stoch_value, atr_value):
        """Processes each finished candle and executes trading logic."""
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Get indicator values
        hma = float(hma_value)

        if stoch_value.K is None:
            return
        stoch_k = float(stoch_value.K)

        atr = float(atr_value)

        # Skip first candle after initialization
        if self._prev_hma_value == 0:
            self._prev_hma_value = hma
            return

        # Detect HMA trend direction
        hma_increasing = hma > self._prev_hma_value
        hma_decreasing = hma < self._prev_hma_value

        # Trading logic:
        # Buy when HMA starts increasing (trend changes up) and Stochastic shows oversold condition
        if hma_increasing and not hma_decreasing and stoch_k < 20 and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
            self.LogInfo("Long entry: Price={0}, HMA={1}, Prev HMA={2}, Stochastic %K={3}".format(
                candle.ClosePrice, hma, self._prev_hma_value, stoch_k))
        # Sell when HMA starts decreasing (trend changes down) and Stochastic shows overbought condition
        elif hma_decreasing and not hma_increasing and stoch_k > 80 and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))
            self.LogInfo("Short entry: Price={0}, HMA={1}, Prev HMA={2}, Stochastic %K={3}".format(
                candle.ClosePrice, hma, self._prev_hma_value, stoch_k))
        # Exit when HMA trend changes direction
        elif self.Position > 0 and hma_decreasing:
            self.SellMarket(Math.Abs(self.Position))
            self.LogInfo("Long exit: Price={0}, HMA={1}, Prev HMA={2}".format(
                candle.ClosePrice, hma, self._prev_hma_value))
        elif self.Position < 0 and hma_increasing:
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo("Short exit: Price={0}, HMA={1}, Prev HMA={2}".format(
                candle.ClosePrice, hma, self._prev_hma_value))

        # Save current HMA value for next candle
        self._prev_hma_value = hma

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return hull_ma_stochastic_strategy()
