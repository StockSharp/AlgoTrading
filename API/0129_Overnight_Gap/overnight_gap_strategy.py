import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class overnight_gap_strategy(Strategy):
    """
    Implementation of Overnight Gap trading strategy.
    The strategy trades on gaps between the current open and previous close prices.

    See more examples: https://github.com/StockSharp/AlgoTrading
    """

    def __init__(self):
        super(overnight_gap_strategy, self).__init__()

        # Initialize strategy parameters
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Protection")

        self._ma_period = self.Param("MaPeriod", 20) \
            .SetDisplay("MA Period", "Moving average period for trend confirmation", "Strategy")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromDays(1))) \
            .SetDisplay("Candle Type", "Type of candles for strategy", "Strategy")

        # Internal state
        self._prev_close_price = 0.0

    @property
    def stop_loss_percent(self):
        """Stop loss percentage from entry price."""
        return self._stop_loss_percent.Value

    @stop_loss_percent.setter
    def stop_loss_percent(self, value):
        self._stop_loss_percent.Value = value

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

    def OnReseted(self):
        """Resets internal state when strategy is reset."""
        super(overnight_gap_strategy, self).OnReseted()
        self._prev_close_price = 0.0

    def OnStarted(self, time):
        """Called when the strategy starts. Sets up indicators, subscriptions, and charting.

        :param time: The time when the strategy started.
        """
        super(overnight_gap_strategy, self).OnStarted(time)

        self._prev_close_price = 0.0

        # Create indicators
        sma = SimpleMovingAverage()
        sma.Length = self.ma_period

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

        # Start position protection
        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),  # No take profit
            stopLoss=Unit(self.stop_loss_percent, UnitTypes.Percent),
        )

    def ProcessCandle(self, candle, ma_value):
        """Processes each finished candle and executes trading logic.

        :param candle: The processed candle message.
        :param ma_value: The value of the moving average.
        """
        # Skip if we don't have previous close price yet
        if self._prev_close_price == 0:
            self._prev_close_price = candle.ClosePrice
            return

        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Skip if strategy is not ready
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Calculate gap
        gap = candle.OpenPrice - self._prev_close_price
        is_gap_up = gap > 0
        is_gap_down = gap < 0

        # Upward gap with price above MA = Buy
        if is_gap_up and candle.OpenPrice > ma_value and self.Position <= 0:
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)
            self.LogInfo(
                "Buy signal on upward gap: Gap={0}, OpenPrice={1}, PrevClose={2}, MA={3}, Volume={4}".format(
                    gap, candle.OpenPrice, self._prev_close_price, ma_value, volume))
        # Downward gap with price below MA = Sell
        elif is_gap_down and candle.OpenPrice < ma_value and self.Position >= 0:
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)
            self.LogInfo(
                "Sell signal on downward gap: Gap={0}, OpenPrice={1}, PrevClose={2}, MA={3}, Volume={4}".format(
                    gap, candle.OpenPrice, self._prev_close_price, ma_value, volume))

        # Exit condition - Gap fill (price returns to previous close)
        if ((self.Position > 0 and candle.LowPrice <= self._prev_close_price) or
                (self.Position < 0 and candle.HighPrice >= self._prev_close_price)):
            self.ClosePosition()
            self.LogInfo(
                "Closing position on gap fill: Position={0}, PrevClose={1}".format(
                    self.Position, self._prev_close_price))

        # Update previous close price for next candle
        self._prev_close_price = candle.ClosePrice

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return overnight_gap_strategy()
