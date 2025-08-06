import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, UnitTypes, Unit, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class cci_hook_reversal_strategy(Strategy):
    """
    CCI Hook Reversal Strategy.
    Enters long when CCI forms an upward hook from oversold conditions.
    Enters short when CCI forms a downward hook from overbought conditions.

    """

    def __init__(self):
        super(cci_hook_reversal_strategy, self).__init__()

        # Initialize strategy parameters
        self._cci_period = self.Param("CciPeriod", 20) \
            .SetDisplay("CCI Period", "Period for CCI calculation", "CCI Settings")

        self._oversold_level = self.Param("OversoldLevel", -100) \
            .SetDisplay("Oversold Level", "Oversold level for CCI", "CCI Settings")

        self._overbought_level = self.Param("OverboughtLevel", 100) \
            .SetDisplay("Overbought Level", "Overbought level for CCI", "CCI Settings")

        self._stop_loss_percent = self.Param("StopLoss", 2.0) \
            .SetDisplay("Stop Loss", "Stop loss as percentage from entry price", "Risk Management")

        self._candle_type = self.Param("CandleType", tf(15)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Previous CCI value
        self._prev_cci = 0.0

    @property
    def cci_period(self):
        """Period for CCI calculation."""
        return self._cci_period.Value

    @cci_period.setter
    def cci_period(self, value):
        self._cci_period.Value = value

    @property
    def oversold_level(self):
        """Oversold level for CCI."""
        return self._oversold_level.Value

    @oversold_level.setter
    def oversold_level(self, value):
        self._oversold_level.Value = value

    @property
    def overbought_level(self):
        """Overbought level for CCI."""
        return self._overbought_level.Value

    @overbought_level.setter
    def overbought_level(self, value):
        self._overbought_level.Value = value

    @property
    def stop_loss_percent(self):
        """Stop loss percentage from entry price."""
        return self._stop_loss_percent.Value

    @stop_loss_percent.setter
    def stop_loss_percent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def candle_type(self):
        """Type of candles to use."""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        """Resets internal state when strategy is reset."""
        super(cci_hook_reversal_strategy, self).OnReseted()
        self._prev_cci = 0.0

    def OnStarted(self, time):
        """Called when the strategy starts."""
        super(cci_hook_reversal_strategy, self).OnStarted(time)

        # Enable position protection using stop-loss
        self.StartProtection(
            takeProfit=None,
            stopLoss=Unit(self.stop_loss_percent, UnitTypes.Percent),
            isStopTrailing=False,
            useMarketOrders=True
        )
        # Create CCI indicator
        cci = CommodityChannelIndex()
        cci.Length = self.cci_period

        # Create subscription and bind indicator
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(cci, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, cci)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, cci_value):
        """Process candle with CCI value."""
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # If this is the first calculation, just store the value
        if self._prev_cci == 0:
            self._prev_cci = cci_value
            return

        # Check for CCI hooks
        oversold_hook_up = self._prev_cci < self.oversold_level and cci_value > self._prev_cci
        overbought_hook_down = self._prev_cci > self.overbought_level and cci_value < self._prev_cci

        # Long entry: CCI forms an upward hook from oversold
        if oversold_hook_up and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
            self.LogInfo("Long entry: CCI upward hook from oversold ({0} -> {1})".format(
                self._prev_cci, cci_value))
        # Short entry: CCI forms a downward hook from overbought
        elif overbought_hook_down and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))
            self.LogInfo("Short entry: CCI downward hook from overbought ({0} -> {1})".format(
                self._prev_cci, cci_value))

        # Exit conditions based on CCI crossing zero line
        if cci_value > 0 and self.Position < 0:
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo("Exit short: CCI crossed above zero ({0})".format(cci_value))
        elif cci_value < 0 and self.Position > 0:
            self.SellMarket(self.Position)
            self.LogInfo("Exit long: CCI crossed below zero ({0})".format(cci_value))

        # Update previous CCI value
        self._prev_cci = cci_value

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return cci_hook_reversal_strategy()
