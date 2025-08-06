import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import WilliamsR
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class williams_r_hook_reversal_strategy(Strategy):
    """
    Williams %R Hook Reversal Strategy.
    Enters long when Williams %R forms an upward hook from oversold conditions.
    Enters short when Williams %R forms a downward hook from overbought conditions.
    """

    def __init__(self):
        super(williams_r_hook_reversal_strategy, self).__init__()

        # Initialize strategy parameters
        self._willRPeriod = self.Param("WillRPeriod", 14) \
            .SetDisplay("Williams %R Period", "Period for Williams %R calculation", "Williams %R Settings") \
            .SetRange(7, 21) \
            .SetCanOptimize(True)

        self._oversoldLevel = self.Param("OversoldLevel", -80.0) \
            .SetDisplay("Oversold Level", "Oversold level for Williams %R (typically -80)", "Williams %R Settings") \
            .SetRange(-90.0, -70.0) \
            .SetCanOptimize(True)

        self._overboughtLevel = self.Param("OverboughtLevel", -20.0) \
            .SetDisplay("Overbought Level", "Overbought level for Williams %R (typically -20)", "Williams %R Settings") \
            .SetRange(-30.0, -10.0) \
            .SetCanOptimize(True)

        self._exitLevel = self.Param("ExitLevel", -50.0) \
            .SetDisplay("Exit Level", "Exit level for Williams %R (neutral zone)", "Williams %R Settings") \
            .SetRange(-60.0, -40.0) \
            .SetCanOptimize(True)

        self._stopLoss = self.Param("StopLoss", Unit(2, UnitTypes.Percent)) \
            .SetDisplay("Stop Loss", "Stop loss as percentage from entry price", "Risk Management") \
            .SetRange(1.0, 3.0) \
            .SetCanOptimize(True)

        self._candleType = self.Param("CandleType", tf(15)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Internal state
        self._prevWillR = 0.0

    @property
    def WillRPeriod(self):
        """Period for Williams %R calculation."""
        return self._willRPeriod.Value

    @WillRPeriod.setter
    def WillRPeriod(self, value):
        self._willRPeriod.Value = value

    @property
    def OversoldLevel(self):
        """Oversold level for Williams %R (typically -80)."""
        return self._oversoldLevel.Value

    @OversoldLevel.setter
    def OversoldLevel(self, value):
        self._oversoldLevel.Value = value

    @property
    def OverboughtLevel(self):
        """Overbought level for Williams %R (typically -20)."""
        return self._overboughtLevel.Value

    @OverboughtLevel.setter
    def OverboughtLevel(self, value):
        self._overboughtLevel.Value = value

    @property
    def ExitLevel(self):
        """Exit level for Williams %R (neutral zone)."""
        return self._exitLevel.Value

    @ExitLevel.setter
    def ExitLevel(self, value):
        self._exitLevel.Value = value

    @property
    def StopLoss(self):
        """Stop loss percentage from entry price."""
        return self._stopLoss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stopLoss.Value = value

    @property
    def CandleType(self):
        """Type of candles to use."""
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    def GetWorkingSecurities(self):
        """!! REQUIRED !! Returns securities for strategy."""
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        """Resets internal state when strategy is reset."""
        super(williams_r_hook_reversal_strategy, self).OnReseted()
        self._prevWillR = 0.0

    def OnStarted(self, time):
        super(williams_r_hook_reversal_strategy, self).OnStarted(time)

        # Enable position protection using stop-loss
        self.StartProtection(
            takeProfit=None,
            stopLoss=self.StopLoss,
            isStopTrailing=False,
            useMarketOrders=True
        )

        # Create Williams %R indicator
        williams_r = WilliamsR()
        williams_r.Length = self.WillRPeriod

        # Create subscription
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind indicator and process candles
        subscription.Bind(williams_r, self.ProcessCandle).Start()

        # Setup chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, williams_r)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, will_r_value):
        """Process candle with Williams %R value."""
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # If this is the first calculation, just store the value
        if self._prevWillR == 0:
            self._prevWillR = will_r_value
            return

        # Check for Williams %R hooks
        oversold_hook_up = self._prevWillR < self.OversoldLevel and will_r_value > self._prevWillR
        overbought_hook_down = self._prevWillR > self.OverboughtLevel and will_r_value < self._prevWillR

        # Long entry: Williams %R forms an upward hook from oversold
        if oversold_hook_up and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
            self.LogInfo("Long entry: Williams %R upward hook from oversold ({0} -> {1})".format(self._prevWillR, will_r_value))
        # Short entry: Williams %R forms a downward hook from overbought
        elif overbought_hook_down and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))
            self.LogInfo("Short entry: Williams %R downward hook from overbought ({0} -> {1})".format(self._prevWillR, will_r_value))

        # Exit conditions based on Williams %R reaching neutral zone
        if will_r_value > self.ExitLevel and self.Position < 0:
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo("Exit short: Williams %R reached neutral zone ({0} > {1})".format(will_r_value, self.ExitLevel))
        elif will_r_value < self.ExitLevel and self.Position > 0:
            self.SellMarket(self.Position)
            self.LogInfo("Exit long: Williams %R reached neutral zone ({0} < {1})".format(will_r_value, self.ExitLevel))

        # Update previous Williams %R value
        self._prevWillR = will_r_value

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return williams_r_hook_reversal_strategy()
