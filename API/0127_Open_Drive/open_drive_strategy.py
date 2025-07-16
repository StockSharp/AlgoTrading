import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class open_drive_strategy(Strategy):
    """Implementation of Open Drive trading strategy.
    The strategy trades on strong gap openings relative to previous close."""

    def __init__(self):
        super(open_drive_strategy, self).__init__()

        # ATR multiplier for gap size.
        self._atr_multiplier = self.Param("AtrMultiplier", 1.0) \
            .SetDisplay("ATR Multiplier", "Multiplier for ATR to define gap size", "Strategy")

        # ATR period.
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "Period for ATR calculation", "Strategy")

        # Moving average period.
        self._ma_period = self.Param("MaPeriod", 20) \
            .SetDisplay("MA Period", "Moving average period for trend confirmation", "Strategy")

        # Candle type for strategy.
        self._candle_type = self.Param("CandleType", tf(15)) \
            .SetDisplay("Candle Type", "Type of candles for strategy", "Strategy")

        # Internal state
        self._prev_close_price = 0.0
        self._atr = None

    @property
    def atr_multiplier(self):
        """ATR multiplier for gap size."""
        return self._atr_multiplier.Value

    @atr_multiplier.setter
    def atr_multiplier(self, value):
        self._atr_multiplier.Value = value

    @property
    def atr_period(self):
        """ATR period."""
        return self._atr_period.Value

    @atr_period.setter
    def atr_period(self, value):
        self._atr_period.Value = value

    @property
    def ma_period(self):
        """Moving average period."""
        return self._ma_period.Value

    @ma_period.setter
    def ma_period(self, value):
        self._ma_period.Value = value

    @property
    def candle_type(self):
        """Candle type for strategy."""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        """Called when the strategy starts."""
        super(open_drive_strategy, self).OnStarted(time)

        self._prev_close_price = 0.0

        # Create indicators
        sma = SimpleMovingAverage()
        sma.Length = self.ma_period
        self._atr = AverageTrueRange()
        self._atr.Length = self.atr_period

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.candle_type)

        # We need to process both indicators with the same candle
        subscription.Bind(sma, self._atr, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawIndicator(area, self._atr)
            self.DrawOwnTrades(area)

        # Start position protection using ATR for stops
        self.StartProtection(
            takeProfit=Unit(0),
            stopLoss=Unit(2 * self.atr_multiplier, UnitTypes.Absolute),
            isStopTrailing=True
        )
    def ProcessCandle(self, candle, sma_value, atr_value):
        """Process candle and execute trading logic."""
        # Skip if we don't have the previous close price yet
        if self._prev_close_price == 0:
            self._prev_close_price = candle.ClosePrice
            return

        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Skip if strategy is not ready
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Calculate gap size compared to previous close
        gap = candle.OpenPrice - self._prev_close_price
        gap_size = Math.Abs(gap)

        # Check if we have a significant gap (> ATR * multiplier)
        if gap_size > atr_value * self.atr_multiplier:
            # Upward gap (Open > Previous Close) with price above MA = Buy
            if gap > 0 and candle.OpenPrice > sma_value and self.Position <= 0:
                volume = self.Volume + Math.Abs(self.Position)
                self.BuyMarket(volume)

                self.LogInfo("Buy signal on upward gap: Gap={0}, ATR={1}, OpenPrice={2}, PrevClose={3}, MA={4}, Volume={5}".format(
                    gap, atr_value, candle.OpenPrice, self._prev_close_price, sma_value, volume))
            # Downward gap (Open < Previous Close) with price below MA = Sell
            elif gap < 0 and candle.OpenPrice < sma_value and self.Position >= 0:
                volume = self.Volume + Math.Abs(self.Position)
                self.SellMarket(volume)

                self.LogInfo("Sell signal on downward gap: Gap={0}, ATR={1}, OpenPrice={2}, PrevClose={3}, MA={4}, Volume={5}".format(
                    gap, atr_value, candle.OpenPrice, self._prev_close_price, sma_value, volume))

        # Exit conditions
        if (self.Position > 0 and candle.ClosePrice < sma_value) or \
                (self.Position < 0 and candle.ClosePrice > sma_value):
            self.ClosePosition()
            self.LogInfo("Closing position on MA crossover: Position={0}, ClosePrice={1}, MA={2}".format(
                self.Position, candle.ClosePrice, sma_value))

        # Update previous close price for next candle
        self._prev_close_price = candle.ClosePrice

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return open_drive_strategy()
