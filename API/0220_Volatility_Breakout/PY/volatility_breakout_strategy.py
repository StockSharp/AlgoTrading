import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, UnitTypes, Unit, ICandleMessage, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class volatility_breakout_strategy(Strategy):
    """
    Volatility Breakout strategy. Enters trades when price breaks out from average price with volatility threshold.
    """

    def __init__(self):
        super(volatility_breakout_strategy, self).__init__()

        # Period for SMA and ATR calculations.
        self._period_param = self.Param("Period", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Period", "Period for SMA and ATR", "Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5)

        # Volatility multiplier for breakout threshold.
        self._multiplier_param = self.Param("Multiplier", 2.0) \
            .SetRange(0.1, float('inf')) \
            .SetDisplay("Multiplier", "Volatility multiplier for breakout threshold", "Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        # Candle type for strategy.
        self._candle_type_param = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Candle type for strategy", "Common")

        # Indicators and state variables
        self._sma = None
        self._atr = None
        self._prev_sma = 0.0
        self._prev_atr = 0.0

    @property
    def Period(self):
        """Period for SMA and ATR calculations."""
        return self._period_param.Value

    @Period.setter
    def Period(self, value):
        self._period_param.Value = value

    @property
    def Multiplier(self):
        """Volatility multiplier for breakout threshold."""
        return self._multiplier_param.Value

    @Multiplier.setter
    def Multiplier(self, value):
        self._multiplier_param.Value = value

    @property
    def CandleType(self):
        """Candle type for strategy."""
        return self._candle_type_param.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type_param.Value = value

    def GetWorkingSecurities(self):
        """See base class for details."""
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(volatility_breakout_strategy, self).OnReseted()
        self._sma = None
        self._atr = None
        self._prev_sma = 0.0
        self._prev_atr = 0.0

    def OnStarted(self, time):
        """Called when the strategy starts."""
        super(volatility_breakout_strategy, self).OnStarted(time)

        # Create indicators
        self._sma = SimpleMovingAverage()
        self._sma.Length = self.Period
        self._atr = AverageTrueRange()
        self._atr.Length = self.Period

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._sma, self._atr, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._sma)
            self.DrawOwnTrades(area)

        # Enable position protection
        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(self.Multiplier, UnitTypes.Absolute)
        )
    def ProcessCandle(self, candle, sma_value, atr_value):
        """Process candle with SMA and ATR values."""
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Save values for the next candle
        current_sma = sma_value
        current_atr = atr_value

        # Skip first candle after indicators become formed
        if self._prev_sma == 0 or self._prev_atr == 0:
            self._prev_sma = current_sma
            self._prev_atr = current_atr
            return

        # Calculate volatility threshold
        threshold = self.Multiplier * current_atr

        # Check for long setup - price breaks above SMA + threshold
        if candle.ClosePrice > current_sma + threshold and self.Position <= 0:
            # Close any short position and open long
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        # Check for short setup - price breaks below SMA - threshold
        elif candle.ClosePrice < current_sma - threshold and self.Position >= 0:
            # Close any long position and open short
            self.SellMarket(self.Volume + Math.Abs(self.Position))

        # Update previous values for next candle
        self._prev_sma = current_sma
        self._prev_atr = current_atr

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return volatility_breakout_strategy()

