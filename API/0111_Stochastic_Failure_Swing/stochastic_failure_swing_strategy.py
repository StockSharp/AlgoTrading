import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates, Sides
from StockSharp.Algo.Indicators import StochasticOscillator
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class stochastic_failure_swing_strategy(Strategy):
    """
    Strategy that trades based on Stochastic Oscillator Failure Swing pattern.
    A failure swing occurs when Stochastic reverses direction without crossing through centerline.
    """

    def __init__(self):
        super(stochastic_failure_swing_strategy, self).__init__()

        # Candle type and timeframe for the strategy.
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use for analysis", "General")

        # K period for Stochastic calculation.
        self._k_period = self.Param("KPeriod", 14) \
            .SetDisplay("K Period", "Period for %K line calculation", "Stochastic Settings") \
            .SetRange(5, 30)

        # D period for Stochastic calculation.
        self._d_period = self.Param("DPeriod", 3) \
            .SetDisplay("D Period", "Period for %D line calculation", "Stochastic Settings") \
            .SetRange(2, 10)

        # Slowing period for Stochastic calculation.
        self._slowing = self.Param("Slowing", 3) \
            .SetDisplay("Slowing", "Slowing period for Stochastic calculation", "Stochastic Settings") \
            .SetRange(1, 5)

        # Oversold level for Stochastic.
        self._oversold_level = self.Param("OversoldLevel", 20.0) \
            .SetDisplay("Oversold Level", "Stochastic level considered oversold", "Stochastic Settings") \
            .SetRange(10.0, 30.0)

        # Overbought level for Stochastic.
        self._overbought_level = self.Param("OverboughtLevel", 80.0) \
            .SetDisplay("Overbought Level", "Stochastic level considered overbought", "Stochastic Settings") \
            .SetRange(70.0, 90.0)

        # Stop-loss percentage from entry price.
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop-loss percentage from entry price", "Protection") \
            .SetRange(0.5, 5.0)

        # Internal fields
        self._stochastic = None
        self._prev_k_value = 0.0
        self._prev_prev_k_value = 0.0
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
    def k_period(self):
        """K period for Stochastic calculation."""
        return self._k_period.Value

    @k_period.setter
    def k_period(self, value):
        self._k_period.Value = value

    @property
    def d_period(self):
        """D period for Stochastic calculation."""
        return self._d_period.Value

    @d_period.setter
    def d_period(self, value):
        self._d_period.Value = value

    @property
    def slowing(self):
        """Slowing period for Stochastic calculation."""
        return self._slowing.Value

    @slowing.setter
    def slowing(self, value):
        self._slowing.Value = value

    @property
    def oversold_level(self):
        """Oversold level for Stochastic."""
        return self._oversold_level.Value

    @oversold_level.setter
    def oversold_level(self, value):
        self._oversold_level.Value = value

    @property
    def overbought_level(self):
        """Overbought level for Stochastic."""
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

    def OnStarted(self, time):
        """Called when the strategy starts."""
        super(stochastic_failure_swing_strategy, self).OnStarted(time)

        # Initialize indicators
        self._stochastic = StochasticOscillator()
        self._stochastic.K.Length = self.k_period
        self._stochastic.D.Length = self.d_period

        self._prev_k_value = 0.0
        self._prev_prev_k_value = 0.0
        self._in_position = False
        self._position_side = None

        # Create and setup subscription for candles
        subscription = self.SubscribeCandles(self.candle_type)

        # Bind indicator and processor
        subscription.BindEx(self._stochastic, self.ProcessCandle).Start()

        # Enable stop-loss protection
        self.StartProtection(
            takeProfit=Unit(0),
            stopLoss=Unit(self.stop_loss_percent, UnitTypes.Percent)
        )
        # Setup chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._stochastic)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, stochastic_value):
        """Processes each finished candle and executes Failure Swing logic."""
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Get current K value (we use %K for the strategy, not %D)
        k_value = float(stochastic_value.K) if hasattr(stochastic_value, 'K') else None
        if k_value is None:
            return

        # Need at least 3 Stochastic values to detect failure swing
        if self._prev_k_value == 0 or self._prev_prev_k_value == 0:
            self._prev_prev_k_value = self._prev_k_value
            self._prev_k_value = k_value
            return

        # Detect Bullish Failure Swing:
        # 1. Stochastic falls below oversold level
        # 2. Stochastic rises without crossing centerline
        # 3. Stochastic pulls back but stays above previous low
        # 4. Stochastic breaks above the high point of first rise
        is_bullish_failure_swing = (
            self._prev_prev_k_value < self.oversold_level and
            self._prev_k_value > self._prev_prev_k_value and
            k_value < self._prev_k_value and
            k_value > self._prev_prev_k_value
        )

        # Detect Bearish Failure Swing:
        # 1. Stochastic rises above overbought level
        # 2. Stochastic falls without crossing centerline
        # 3. Stochastic bounces up but stays below previous high
        # 4. Stochastic breaks below the low point of first decline
        is_bearish_failure_swing = (
            self._prev_prev_k_value > self.overbought_level and
            self._prev_k_value < self._prev_prev_k_value and
            k_value > self._prev_k_value and
            k_value < self._prev_prev_k_value
        )

        # Trading logic
        if is_bullish_failure_swing and not self._in_position:
            # Enter long position
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)

            self._in_position = True
            self._position_side = Sides.Buy

            self.LogInfo(
                f"Bullish Stochastic Failure Swing detected. %K values: {self._prev_prev_k_value:.2f} -> {self._prev_k_value:.2f} -> {k_value:.2f}. Long entry at {candle.ClosePrice}"
            )
        elif is_bearish_failure_swing and not self._in_position:
            # Enter short position
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)

            self._in_position = True
            self._position_side = Sides.Sell

            self.LogInfo(
                f"Bearish Stochastic Failure Swing detected. %K values: {self._prev_prev_k_value:.2f} -> {self._prev_k_value:.2f} -> {k_value:.2f}. Short entry at {candle.ClosePrice}"
            )

        # Exit conditions
        if self._in_position:
            # For long positions: exit when Stochastic crosses above 50
            if self._position_side == Sides.Buy and k_value > 50:
                self.SellMarket(Math.Abs(self.Position))
                self._in_position = False
                self._position_side = None

                self.LogInfo(
                    f"Exit signal for long position: Stochastic %K ({k_value:.2f}) crossed above 50. Closing at {candle.ClosePrice}"
                )
            # For short positions: exit when Stochastic crosses below 50
            elif self._position_side == Sides.Sell and k_value < 50:
                self.BuyMarket(Math.Abs(self.Position))
                self._in_position = False
                self._position_side = None

                self.LogInfo(
                    f"Exit signal for short position: Stochastic %K ({k_value:.2f}) crossed below 50. Closing at {candle.ClosePrice}"
                )

        # Update Stochastic values for next iteration
        self._prev_prev_k_value = self._prev_k_value
        self._prev_k_value = k_value

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return stochastic_failure_swing_strategy()
