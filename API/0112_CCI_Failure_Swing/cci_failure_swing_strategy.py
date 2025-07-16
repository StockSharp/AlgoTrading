import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates, Sides
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class cci_failure_swing_strategy(Strategy):
    """
    Strategy that trades based on CCI (Commodity Channel Index) Failure Swing pattern.
    A failure swing occurs when CCI reverses direction without crossing through centerline.

    See more examples: https://github.com/StockSharp/AlgoTrading
    """

    def __init__(self):
        super(cci_failure_swing_strategy, self).__init__()

        # Initialize strategy parameters
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use for analysis", "General")

        self._cci_period = self.Param("CciPeriod", 20) \
            .SetDisplay("CCI Period", "Period for CCI calculation", "CCI Settings") \
            .SetRange(5, 50)

        self._oversold_level = self.Param("OversoldLevel", -100.0) \
            .SetDisplay("Oversold Level", "CCI level considered oversold", "CCI Settings") \
            .SetRange(-200.0, -50.0)

        self._overbought_level = self.Param("OverboughtLevel", 100.0) \
            .SetDisplay("Overbought Level", "CCI level considered overbought", "CCI Settings") \
            .SetRange(50.0, 200.0)

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop-loss percentage from entry price", "Protection") \
            .SetRange(0.5, 5.0)

        # Internal state
        self._prev_cci_value = 0.0
        self._prev_prev_cci_value = 0.0
        self._in_position = False
        self._position_side = None

    @property
    def candle_type(self):
        """Candle type and timeframe for the strategy."""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

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
        """Stop-loss percentage from entry price."""
        return self._stop_loss_percent.Value

    @stop_loss_percent.setter
    def stop_loss_percent(self, value):
        self._stop_loss_percent.Value = value

    def OnReseted(self):
        """
        Resets internal state when strategy is reset.
        """
        super(cci_failure_swing_strategy, self).OnReseted()
        self._prev_cci_value = 0.0
        self._prev_prev_cci_value = 0.0
        self._in_position = False
        self._position_side = None

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.

        :param time: The time when the strategy started.
        """
        super(cci_failure_swing_strategy, self).OnStarted(time)

        # Initialize indicators
        self._cci = CommodityChannelIndex()
        self._cci.Length = self.cci_period

        self._prev_cci_value = 0.0
        self._prev_prev_cci_value = 0.0
        self._in_position = False
        self._position_side = None

        # Create and setup subscription for candles
        subscription = self.SubscribeCandles(self.candle_type)

        # Bind indicator and processor
        subscription.Bind(self._cci, self.ProcessCandle).Start()

        # Enable stop-loss protection
        self.StartProtection(Unit(0), Unit(self.stop_loss_percent, UnitTypes.Percent))

        # Setup chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._cci)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, cci_value):
        """
        Processes each finished candle and executes CCI Failure Swing logic.

        :param candle: The processed candle message.
        :param cci_value: The current value of the CCI indicator.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Need at least 3 CCI values to detect failure swing
        if self._prev_cci_value == 0 or self._prev_prev_cci_value == 0:
            self._prev_prev_cci_value = self._prev_cci_value
            self._prev_cci_value = cci_value
            return

        # Detect Bullish Failure Swing:
        # 1. CCI falls below oversold level
        # 2. CCI rises without crossing centerline
        # 3. CCI pulls back but stays above previous low
        # 4. CCI breaks above the high point of first rise
        is_bullish_failure_swing = (self._prev_prev_cci_value < self.oversold_level and
                                    self._prev_cci_value > self._prev_prev_cci_value and
                                    cci_value < self._prev_cci_value and
                                    cci_value > self._prev_prev_cci_value)

        # Detect Bearish Failure Swing:
        # 1. CCI rises above overbought level
        # 2. CCI falls without crossing centerline
        # 3. CCI bounces up but stays below previous high
        # 4. CCI breaks below the low point of first decline
        is_bearish_failure_swing = (self._prev_prev_cci_value > self.overbought_level and
                                     self._prev_cci_value < self._prev_prev_cci_value and
                                     cci_value > self._prev_cci_value and
                                     cci_value < self._prev_prev_cci_value)

        # Trading logic
        if is_bullish_failure_swing and not self._in_position:
            # Enter long position
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)

            self._in_position = True
            self._position_side = Sides.Buy

            self.LogInfo("Bullish CCI Failure Swing detected. CCI values: {0:F2} -> {1:F2} -> {2:F2}. Long entry at {3}".format(
                float(self._prev_prev_cci_value), float(self._prev_cci_value), float(cci_value), candle.ClosePrice))

        elif is_bearish_failure_swing and not self._in_position:
            # Enter short position
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)

            self._in_position = True
            self._position_side = Sides.Sell

            self.LogInfo("Bearish CCI Failure Swing detected. CCI values: {0:F2} -> {1:F2} -> {2:F2}. Short entry at {3}".format(
                float(self._prev_prev_cci_value), float(self._prev_cci_value), float(cci_value), candle.ClosePrice))

        # Exit conditions
        if self._in_position:
            # For long positions: exit when CCI crosses above 0
            if self._position_side == Sides.Buy and cci_value > 0:
                self.SellMarket(Math.Abs(self.Position))
                self._in_position = False
                self._position_side = None

                self.LogInfo("Exit signal for long position: CCI ({0:F2}) crossed above 0. Closing at {1}".format(
                    float(cci_value), candle.ClosePrice))
            # For short positions: exit when CCI crosses below 0
            elif self._position_side == Sides.Sell and cci_value < 0:
                self.BuyMarket(Math.Abs(self.Position))
                self._in_position = False
                self._position_side = None

                self.LogInfo("Exit signal for short position: CCI ({0:F2}) crossed below 0. Closing at {1}".format(
                    float(cci_value), candle.ClosePrice))

        # Update CCI values for next iteration
        self._prev_prev_cci_value = self._prev_cci_value
        self._prev_cci_value = cci_value

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return cci_failure_swing_strategy()
