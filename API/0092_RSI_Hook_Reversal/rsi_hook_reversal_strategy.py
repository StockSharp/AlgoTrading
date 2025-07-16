import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class rsi_hook_reversal_strategy(Strategy):
    """
    RSI Hook Reversal Strategy.
    Enters long when RSI forms an upward hook from oversold conditions.
    Enters short when RSI forms a downward hook from overbought conditions.

    See more examples: https://github.com/StockSharp/AlgoTrading
    """

    def __init__(self):
        super(rsi_hook_reversal_strategy, self).__init__()

        # Initialize strategy parameters
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "Period for RSI calculation", "RSI Settings")
        self._oversold_level = self.Param("OversoldLevel", 30) \
            .SetDisplay("Oversold Level", "Oversold level for RSI", "RSI Settings")
        self._overbought_level = self.Param("OverboughtLevel", 70) \
            .SetDisplay("Overbought Level", "Overbought level for RSI", "RSI Settings")
        self._exit_level = self.Param("ExitLevel", 50) \
            .SetDisplay("Exit Level", "Exit level for RSI (neutral zone)", "RSI Settings")
        self._stop_loss = self.Param("StopLoss", Unit(2, UnitTypes.Percent)) \
            .SetDisplay("Stop Loss", "Stop loss as percentage from entry price", "Risk Management")
        self._candle_type = self.Param("CandleType", tf(15)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Previous RSI value
        self._prev_rsi = 0.0

    @property
    def rsi_period(self):
        """Period for RSI calculation."""
        return self._rsi_period.Value

    @rsi_period.setter
    def rsi_period(self, value):
        self._rsi_period.Value = value

    @property
    def oversold_level(self):
        """Oversold level for RSI."""
        return self._oversold_level.Value

    @oversold_level.setter
    def oversold_level(self, value):
        self._oversold_level.Value = value

    @property
    def overbought_level(self):
        """Overbought level for RSI."""
        return self._overbought_level.Value

    @overbought_level.setter
    def overbought_level(self, value):
        self._overbought_level.Value = value

    @property
    def exit_level(self):
        """Exit level for RSI (neutral zone)."""
        return self._exit_level.Value

    @exit_level.setter
    def exit_level(self, value):
        self._exit_level.Value = value

    @property
    def stop_loss(self):
        """Stop loss percentage from entry price."""
        return self._stop_loss.Value

    @stop_loss.setter
    def stop_loss(self, value):
        self._stop_loss.Value = value

    @property
    def candle_type(self):
        """Type of candles to use."""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        """Resets internal state when strategy is reset."""
        super(rsi_hook_reversal_strategy, self).OnReseted()
        self._prev_rsi = 0.0

    def OnStarted(self, time):
        """Called when the strategy starts. Sets up indicators, subscriptions, and charting."""
        super(rsi_hook_reversal_strategy, self).OnStarted(time)

        # Enable position protection using stop-loss
        self.StartProtection(
            takeProfit=None,
            stopLoss=self.stop_loss,
            isStopTrailing=False,
            useMarketOrders=True
        )
        # Initialize previous RSI value
        self._prev_rsi = 0.0

        # Create RSI indicator
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period

        # Create subscription
        subscription = self.SubscribeCandles(self.candle_type)

        # Bind indicator and process candles
        subscription.Bind(rsi, self.ProcessCandle).Start()

        # Setup chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, rsi_value):
        """Process candle with RSI value."""
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # If this is the first calculation, just store the value
        if self._prev_rsi == 0:
            self._prev_rsi = rsi_value
            return

        # Check for RSI hooks
        oversold_hook_up = self._prev_rsi < self.oversold_level and rsi_value > self._prev_rsi
        overbought_hook_down = self._prev_rsi > self.overbought_level and rsi_value < self._prev_rsi

        # Long entry: RSI forms an upward hook from oversold
        if oversold_hook_up and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
            self.LogInfo(f"Long entry: RSI upward hook from oversold ({self._prev_rsi} -> {rsi_value})")
        # Short entry: RSI forms a downward hook from overbought
        elif overbought_hook_down and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))
            self.LogInfo(f"Short entry: RSI downward hook from overbought ({self._prev_rsi} -> {rsi_value})")

        # Exit conditions based on RSI reaching neutral zone
        if rsi_value > self.exit_level and self.Position < 0:
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo(f"Exit short: RSI reached neutral zone ({rsi_value} > {self.exit_level})")
        elif rsi_value < self.exit_level and self.Position > 0:
            self.SellMarket(self.Position)
            self.LogInfo(f"Exit long: RSI reached neutral zone ({rsi_value} < {self.exit_level})")

        # Update previous RSI value
        self._prev_rsi = rsi_value

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return rsi_hook_reversal_strategy()
