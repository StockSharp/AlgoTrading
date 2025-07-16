import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SuperTrend, StochasticOscillator
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class supertrend_stochastic_strategy(Strategy):
    """
    Supertrend + Stochastic strategy.
    Strategy enters trades when Supertrend indicates trend direction and Stochastic confirms with oversold/overbought conditions.

    See more examples: https://github.com/StockSharp/AlgoTrading
    """

    def __init__(self):
        super(supertrend_stochastic_strategy, self).__init__()

        # Initialize strategy parameters
        self._supertrendPeriod = self.Param("SupertrendPeriod", 10) \
            .SetDisplay("Supertrend Period", "Supertrend ATR period length", "Supertrend") \
            .SetCanOptimize(True) \
            .SetOptimize(5, 20, 1)

        self._supertrendMultiplier = self.Param("SupertrendMultiplier", 3.0) \
            .SetDisplay("Supertrend Multiplier", "Supertrend ATR multiplier", "Supertrend") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 5.0, 0.5)

        self._stochPeriod = self.Param("StochPeriod", 14) \
            .SetDisplay("Stochastic Period", "Stochastic oscillator period", "Stochastic") \
            .SetCanOptimize(True) \
            .SetOptimize(5, 30, 5)

        self._stochK = self.Param("StochK", 3) \
            .SetDisplay("Stochastic %K", "Stochastic %K period", "Stochastic") \
            .SetCanOptimize(True) \
            .SetOptimize(1, 10, 1)

        self._stochD = self.Param("StochD", 3) \
            .SetDisplay("Stochastic %D", "Stochastic %D period", "Stochastic") \
            .SetCanOptimize(True) \
            .SetOptimize(1, 10, 1)

        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._stopLossPercent = self.Param("StopLossPercent", 1.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(0.5, 2.0, 0.5)

        # Indicators
        self._supertrend = None
        self._stochastic = None

    @property
    def SupertrendPeriod(self):
        """Supertrend period."""
        return self._supertrendPeriod.Value

    @SupertrendPeriod.setter
    def SupertrendPeriod(self, value):
        self._supertrendPeriod.Value = value

    @property
    def SupertrendMultiplier(self):
        """Supertrend multiplier."""
        return self._supertrendMultiplier.Value

    @SupertrendMultiplier.setter
    def SupertrendMultiplier(self, value):
        self._supertrendMultiplier.Value = value

    @property
    def StochPeriod(self):
        """Stochastic period."""
        return self._stochPeriod.Value

    @StochPeriod.setter
    def StochPeriod(self, value):
        self._stochPeriod.Value = value

    @property
    def StochK(self):
        """Stochastic %K period."""
        return self._stochK.Value

    @StochK.setter
    def StochK(self, value):
        self._stochK.Value = value

    @property
    def StochD(self):
        """Stochastic %D period."""
        return self._stochD.Value

    @StochD.setter
    def StochD(self, value):
        self._stochD.Value = value

    @property
    def CandleType(self):
        """Candle type."""
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    @property
    def StopLossPercent(self):
        """Stop-loss percentage."""
        return self._stopLossPercent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stopLossPercent.Value = value

    def OnStarted(self, time):
        """Called when the strategy starts."""
        super(supertrend_stochastic_strategy, self).OnStarted(time)

        # Create indicators
        self._supertrend = SuperTrend()
        self._supertrend.Length = self.SupertrendPeriod
        self._supertrend.Multiplier = self.SupertrendMultiplier

        self._stochastic = StochasticOscillator()
        self._stochastic.K.Length = self.StochK
        self._stochastic.D.Length = self.StochD

        # Subscribe to candles and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(self._supertrend, self._stochastic, self.ProcessCandle).Start()

        # Setup chart
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._supertrend)

            secondArea = self.CreateChartArea()
            if secondArea is not None:
                self.DrawIndicator(secondArea, self._stochastic)

            self.DrawOwnTrades(area)

        self.StartProtection(
            Unit(),
            Unit(self.StopLossPercent, UnitTypes.Percent)
        )

    def ProcessCandle(self, candle, supertrend_value, stochastic_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Get indicator values
        supertrend_line = float(supertrend_value)
        is_bullish = getattr(supertrend_value, 'IsUpTrend', True)
        is_bearish = not is_bullish

        if not hasattr(stochastic_value, 'K'):
            return
        stochK = float(stochastic_value.K)

        is_above_supertrend = candle.ClosePrice > supertrend_line
        is_below_supertrend = candle.ClosePrice < supertrend_line

        # Trading logic:
        # Buy when price is above Supertrend line (bullish) and Stochastic shows oversold condition
        if is_above_supertrend and is_bullish and stochK < 20 and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
            self.LogInfo(f"Long entry: Price={candle.ClosePrice}, Supertrend={supertrend_line}, Stochastic %K={stochK}")
        # Sell when price is below Supertrend line (bearish) and Stochastic shows overbought condition
        elif is_below_supertrend and is_bearish and stochK > 80 and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))
            self.LogInfo(f"Short entry: Price={candle.ClosePrice}, Supertrend={supertrend_line}, Stochastic %K={stochK}")
        # Exit long position when price falls below Supertrend line
        elif self.Position > 0 and is_below_supertrend:
            self.SellMarket(Math.Abs(self.Position))
            self.LogInfo(f"Long exit: Price={candle.ClosePrice}, Below Supertrend={supertrend_line}")
        # Exit short position when price rises above Supertrend line
        elif self.Position < 0 and is_above_supertrend:
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo(f"Short exit: Price={candle.ClosePrice}, Above Supertrend={supertrend_line}")

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return supertrend_stochastic_strategy()
