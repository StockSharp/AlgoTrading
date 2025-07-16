import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, UnitTypes, Unit, CandleStates
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class midday_reversal_strategy(Strategy):
    """
    Implementation of Midday Reversal trading strategy.
    The strategy trades on price reversals that occur around noon (12:00).

    See more examples: https://github.com/StockSharp/AlgoTrading
    """

    def __init__(self):
        super(midday_reversal_strategy, self).__init__()

        # Initialize strategy parameters
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetNotNegative() \
            .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Protection")

        self._candle_type = self.Param("CandleType", tf(30)) \
            .SetDisplay("Candle Type", "Type of candles for strategy", "Strategy")

        # Previous candle closing prices
        self._prev_candle_close = 0.0
        self._prev_prev_candle_close = 0.0

    @property
    def stop_loss_percent(self):
        """Stop loss percentage from entry price."""
        return self._stop_loss_percent.Value

    @stop_loss_percent.setter
    def stop_loss_percent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def candle_type(self):
        """Candle type for strategy."""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnStarted(self, time):
        super(midday_reversal_strategy, self).OnStarted(time)

        self._prev_candle_close = 0.0
        self._prev_prev_candle_close = 0.0

        # Create subscription
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

        # Start position protection
        self.StartProtection(
            takeProfit=Unit(0),  # No take profit
            stopLoss=Unit(self.stop_loss_percent, UnitTypes.Percent),
        )

    def ProcessCandle(self, candle):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Skip if strategy is not ready
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        hour = candle.OpenTime.Hour
        is_noon = hour == 12  # Check if the candle is around noon (12:00)

        # Initialize price history first
        if self._prev_candle_close == 0:
            self._prev_candle_close = candle.ClosePrice
            return

        if self._prev_prev_candle_close == 0:
            self._prev_prev_candle_close = self._prev_candle_close
            self._prev_candle_close = candle.ClosePrice
            return

        # Check for midday reversal conditions
        if is_noon:
            is_bullish_candle = candle.ClosePrice > candle.OpenPrice
            is_bearish_candle = candle.ClosePrice < candle.OpenPrice
            was_price_decreasing = self._prev_candle_close < self._prev_prev_candle_close
            was_price_increasing = self._prev_candle_close > self._prev_prev_candle_close

            # Buy signal: Previous decrease followed by a bullish candle at noon
            if was_price_decreasing and is_bullish_candle and self.Position <= 0:
                volume = self.Volume + Math.Abs(self.Position)
                self.BuyMarket(volume)

                self.LogInfo(
                    "Buy signal at midday reversal: Time={0}, PrevDown={1}, BullishCandle={2}, ClosePrice={3}, Volume={4}".format(
                        candle.OpenTime, was_price_decreasing, is_bullish_candle, candle.ClosePrice, volume
                    )
                )
            # Sell signal: Previous increase followed by a bearish candle at noon
            elif was_price_increasing and is_bearish_candle and self.Position >= 0:
                volume = self.Volume + Math.Abs(self.Position)
                self.SellMarket(volume)

                self.LogInfo(
                    "Sell signal at midday reversal: Time={0}, PrevUp={1}, BearishCandle={2}, ClosePrice={3}, Volume={4}".format(
                        candle.OpenTime, was_price_increasing, is_bearish_candle, candle.ClosePrice, volume
                    )
                )

        # Exit condition - close at 15:00
        if hour == 15 and self.Position != 0:
            self.ClosePosition()
            self.LogInfo(
                "Closing position at 15:00: Time={0}, Position={1}".format(
                    candle.OpenTime, self.Position
                )
            )

        # Update price history
        self._prev_prev_candle_close = self._prev_candle_close
        self._prev_candle_close = candle.ClosePrice

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return midday_reversal_strategy()
