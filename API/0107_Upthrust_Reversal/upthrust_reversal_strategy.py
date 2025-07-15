import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import DataType
from StockSharp.Messages import CandleStates
from StockSharp.Messages import Unit
from StockSharp.Messages import UnitTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage, Highest
from StockSharp.Algo.Strategies import Strategy


class upthrust_reversal_strategy(Strategy):
    """
    Strategy based on Upthrust Reversal pattern, which occurs when price makes a new high above resistance
    but immediately reverses and closes below the resistance level, indicating a bearish reversal.

    See more examples: https://github.com/StockSharp/AlgoTrading
    """

    def __init__(self):
        super(upthrust_reversal_strategy, self).__init__()

        # Initialize strategy parameters
        self._candleType = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use for analysis", "General")

        self._lookbackPeriod = self.Param("LookbackPeriod", 20) \
            .SetDisplay("Lookback Period", "Period for resistance level detection", "Range") \
            .SetRange(5, 50)

        self._maPeriod = self.Param("MaPeriod", 20) \
            .SetDisplay("MA Period", "Period for moving average calculation", "Trend") \
            .SetRange(5, 50)

        self._stopLossPercent = self.Param("StopLossPercent", 1.0) \
            .SetDisplay("Stop Loss %", "Stop-loss percentage from entry price", "Protection") \
            .SetRange(0.5, 3.0)

        # Internal indicators
        self._ma = None
        self._highest = None

        # State variable to store last highest value
        self._last_highest_value = 0.0

    @property
    def CandleType(self):
        """Candle type and timeframe for the strategy."""
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    @property
    def LookbackPeriod(self):
        """Period for high range detection."""
        return self._lookbackPeriod.Value

    @LookbackPeriod.setter
    def LookbackPeriod(self, value):
        self._lookbackPeriod.Value = value

    @property
    def MaPeriod(self):
        """Period for moving average calculation."""
        return self._maPeriod.Value

    @MaPeriod.setter
    def MaPeriod(self, value):
        self._maPeriod.Value = value

    @property
    def StopLossPercent(self):
        """Stop-loss percentage from entry price."""
        return self._stopLossPercent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stopLossPercent.Value = value

    def OnReseted(self):
        """Resets internal state when strategy is reset."""
        super(upthrust_reversal_strategy, self).OnReseted()
        self._last_highest_value = 0.0

    def OnStarted(self, time):
        """Called when the strategy starts."""
        super(upthrust_reversal_strategy, self).OnStarted(time)

        self._last_highest_value = 0

        # Initialize indicators
        self._ma = SimpleMovingAverage()
        self._ma.Length = self.MaPeriod
        self._highest = Highest()
        self._highest.Length = self.LookbackPeriod

        # Create and setup subscription for candles
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind indicators and processor
        subscription.Bind(self._ma, self._highest, self.ProcessCandle).Start()

        # Enable stop-loss protection
        self.StartProtection(Unit(0), Unit(self.StopLossPercent, UnitTypes.Percent))

        # Setup chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ma)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, ma_value, highest_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Store the last highest value
        self._last_highest_value = highest_value

        # Determine candle characteristics
        is_bearish = candle.ClosePrice < candle.OpenPrice
        pierces_above_resistance = candle.HighPrice > self._last_highest_value
        close_below_resistance = candle.ClosePrice < self._last_highest_value

        # Upthrust pattern:
        # 1. Price spikes above recent high (resistance level)
        # 2. But closes below the resistance level (bearish rejection)
        if pierces_above_resistance and close_below_resistance and is_bearish:
            # Enter short position only if we're not already short
            if self.Position >= 0:
                volume = self.Volume + Math.Abs(self.Position)
                self.SellMarket(volume)

                self.LogInfo(
                    f"Upthrust Reversal detected. Resistance level: {self._last_highest_value}, High: {candle.HighPrice}. Short entry at {candle.ClosePrice}"
                )

        # Exit conditions
        if self.Position < 0:
            # Exit when price falls below the moving average (take profit)
            if candle.ClosePrice < ma_value:
                self.BuyMarket(Math.Abs(self.Position))

                self.LogInfo(
                    f"Exit signal: Price below MA. Closed short position at {candle.ClosePrice}"
                )

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return upthrust_reversal_strategy()

