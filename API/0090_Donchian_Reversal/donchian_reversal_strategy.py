import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import DonchianChannels
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class donchian_reversal_strategy(Strategy):
    """
    Donchian Reversal Strategy.
    Enters long when price bounces from the lower Donchian Channel band.
    Enters short when price bounces from the upper Donchian Channel band.

    """

    def __init__(self):
        super(donchian_reversal_strategy, self).__init__()

        # Initialize strategy parameters
        self._period = self.Param("Period", 20) \
            .SetDisplay("Period", "Period for Donchian Channel calculation", "Indicator Settings") \
            .SetOptimize(10, 40, 5) \
            .SetCanOptimize(True)

        self._stop_loss = self.Param("StopLoss", Unit(2, UnitTypes.Percent)) \
            .SetDisplay("Stop Loss", "Stop loss as percentage from entry price", "Risk Management") \
            .SetOptimize(1.0, 3.0, 0.5) \
            .SetCanOptimize(True)

        self._candle_type = self.Param("CandleType", tf(15)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Internal state
        self._previous_close = 0.0
        self._is_first_candle = True

    @property
    def Period(self):
        """Period for Donchian Channel calculation."""
        return self._period.Value

    @Period.setter
    def Period(self, value):
        self._period.Value = value

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

    def OnReseted(self):
        """
        Resets internal state when strategy is reset.
        """
        super(donchian_reversal_strategy, self).OnReseted()
        self._previous_close = 0.0
        self._is_first_candle = True

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        """
        super(donchian_reversal_strategy, self).OnStarted(time)

        # Enable position protection using stop-loss
        self.StartProtection(
            takeProfit=None,
            stopLoss=self.StopLoss,
            isStopTrailing=False,
            useMarketOrders=True
        )
        # Initialize state
        self._previous_close = 0.0
        self._is_first_candle = True

        # Create Donchian Channel indicator
        donchian = DonchianChannels()
        donchian.Length = self.Period

        # Create subscription
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind indicator and process candles
        subscription.BindEx(donchian, self.ProcessCandle).Start()

        # Setup chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, donchian)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, donchian_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # If this is the first candle, just store the close price
        if self._is_first_candle:
            self._previous_close = float(candle.ClosePrice)
            self._is_first_candle = False
            return

        if donchian_value.LowerBand is None or donchian_value.UpperBand is None:
            return

        lower_band = float(donchian_value.LowerBand)
        upper_band = float(donchian_value.UpperBand)

        # Check for price bounce from Donchian bands
        bounced_from_lower = self._previous_close < lower_band and candle.ClosePrice > lower_band
        bounced_from_upper = self._previous_close > upper_band and candle.ClosePrice < upper_band

        # Long entry: Price bounced from lower band
        if bounced_from_lower and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
            self.LogInfo(f"Long entry: Price bounced from lower Donchian band ({lower_band})")
        # Short entry: Price bounced from upper band
        elif bounced_from_upper and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))
            self.LogInfo(f"Short entry: Price bounced from upper Donchian band ({upper_band})")

        # Store current close price for next candle comparison
        self._previous_close = float(candle.ClosePrice)

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return donchian_reversal_strategy()
