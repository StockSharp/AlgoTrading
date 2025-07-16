import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import StochasticOscillator, StochasticOscillatorValue
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class stochastic_hook_reversal_strategy(Strategy):
    """
    Stochastic Hook Reversal Strategy.
    Enters long when %K forms an upward hook from oversold conditions.
    Enters short when %K forms a downward hook from overbought conditions.

    """

    def __init__(self):
        super(stochastic_hook_reversal_strategy, self).__init__()

        # Initialize strategy parameters
        self._stoch_period = self.Param("StochPeriod", 14) \
            .SetDisplay("Stochastic Period", "Period for Stochastic calculation", "Stochastic Settings")

        self._k_period = self.Param("KPeriod", 3) \
            .SetDisplay("K Period", "%K Period for Stochastic calculation", "Stochastic Settings")

        self._d_period = self.Param("DPeriod", 3) \
            .SetDisplay("D Period", "%D Period for Stochastic calculation", "Stochastic Settings")

        self._oversold_level = self.Param("OversoldLevel", 20) \
            .SetDisplay("Oversold Level", "Oversold level for Stochastic", "Stochastic Settings")

        self._overbought_level = self.Param("OverboughtLevel", 80) \
            .SetDisplay("Overbought Level", "Overbought level for Stochastic", "Stochastic Settings")

        self._exit_level = self.Param("ExitLevel", 50) \
            .SetDisplay("Exit Level", "Exit level for Stochastic (neutral zone)", "Stochastic Settings")

        self._stop_loss = self.Param("StopLoss", Unit(2, UnitTypes.Percent)) \
            .SetDisplay("Stop Loss", "Stop loss as percentage from entry price", "Risk Management")

        self._candle_type = self.Param("CandleType", tf(15)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Previous %K value
        self._prev_k = 0.0

    @property
    def StochPeriod(self):
        """Period for Stochastic calculation."""
        return self._stoch_period.Value

    @StochPeriod.setter
    def StochPeriod(self, value):
        self._stoch_period.Value = value

    @property
    def KPeriod(self):
        """%K Period for Stochastic calculation."""
        return self._k_period.Value

    @KPeriod.setter
    def KPeriod(self, value):
        self._k_period.Value = value

    @property
    def DPeriod(self):
        """%D Period for Stochastic calculation."""
        return self._d_period.Value

    @DPeriod.setter
    def DPeriod(self, value):
        self._d_period.Value = value

    @property
    def OversoldLevel(self):
        """Oversold level for Stochastic."""
        return self._oversold_level.Value

    @OversoldLevel.setter
    def OversoldLevel(self, value):
        self._oversold_level.Value = value

    @property
    def OverboughtLevel(self):
        """Overbought level for Stochastic."""
        return self._overbought_level.Value

    @OverboughtLevel.setter
    def OverboughtLevel(self, value):
        self._overbought_level.Value = value

    @property
    def ExitLevel(self):
        """Exit level for Stochastic (neutral zone)."""
        return self._exit_level.Value

    @ExitLevel.setter
    def ExitLevel(self, value):
        self._exit_level.Value = value

    @property
    def StopLoss(self):
        """Stop loss percentage from entry price."""
        return self._stop_loss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stop_loss.Value = value

    @property
    def CandleType(self):
        """Type of candles to use."""
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        """
        Called when the strategy starts.
        """
        super(stochastic_hook_reversal_strategy, self).OnStarted(time)

        # Enable position protection using stop-loss
        self.StartProtection(
            takeProfit=None,
            stopLoss=self.StopLoss
        )
        # Initialize previous K value
        self._prev_k = 0.0

        # Create Stochastic oscillator
        stoch = StochasticOscillator()
        stoch.K.Length = self.KPeriod
        stoch.D.Length = self.DPeriod

        # Create subscription
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind indicator and process candles
        subscription.BindEx(stoch, self.ProcessCandle).Start()

        # Setup chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, stoch)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, stoch_value):
        """
        Process candle with Stochastic values.

        :param candle: Candle.
        :param stoch_value: Stochastic %K value.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Extract %K value
        if isinstance(stoch_value, StochasticOscillatorValue):
            if stoch_value.K is None:
                return
            stoch_k = float(stoch_value.K)
        else:
            try:
                stoch_k = float(stoch_value)
            except:
                return

        # If this is the first calculation, just store the value
        if self._prev_k == 0:
            self._prev_k = stoch_k
            return

        # Check for Stochastic hooks
        oversold_hook_up = self._prev_k < self.OversoldLevel and stoch_k > self._prev_k
        overbought_hook_down = self._prev_k > self.OverboughtLevel and stoch_k < self._prev_k

        # Long entry: %K forms an upward hook from oversold
        if oversold_hook_up and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
            self.LogInfo("Long entry: Stochastic %K upward hook from oversold ({0} -> {1})", self._prev_k, stoch_k)
        # Short entry: %K forms a downward hook from overbought
        elif overbought_hook_down and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))
            self.LogInfo("Short entry: Stochastic %K downward hook from overbought ({0} -> {1})", self._prev_k, stoch_k)

        # Exit conditions based on Stochastic reaching neutral zone
        if stoch_k > self.ExitLevel and self.Position < 0:
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo("Exit short: Stochastic %K reached neutral zone ({0} > {1})", stoch_k, self.ExitLevel)
        elif stoch_k < self.ExitLevel and self.Position > 0:
            self.SellMarket(self.Position)
            self.LogInfo("Exit long: Stochastic %K reached neutral zone ({0} < {1})", stoch_k, self.ExitLevel)

        # Update previous K value
        self._prev_k = stoch_k

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return stochastic_hook_reversal_strategy()
