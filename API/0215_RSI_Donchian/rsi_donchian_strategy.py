import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import RelativeStrengthIndex, Highest, Lowest
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class rsi_donchian_strategy(Strategy):
    """
    Strategy based on RSI and Donchian Channel indicators.
    Enters long when RSI is below 30 (oversold) and price breaks above Donchian high.
    Enters short when RSI is above 70 (overbought) and price breaks below Donchian low.
    Uses middle line of Donchian Channel for exit signals.

    """

    def __init__(self):
        super(rsi_donchian_strategy, self).__init__()

        # RSI period parameter.
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Period", "Period for RSI calculation", "Indicators") \
            .SetCanOptimize(True)

        # Donchian Channel period parameter.
        self._donchian_period = self.Param("DonchianPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Donchian Period", "Period for Donchian Channel calculation", "Indicators") \
            .SetCanOptimize(True)

        # Stop-loss percentage parameter.
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop-loss %", "Stop-loss as percentage of entry price", "Risk Management") \
            .SetCanOptimize(True)

        # Candle type parameter.
        self._candle_type = self.Param("CandleType", tf(15)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._rsi = None
        self._highest_high = None
        self._lowest_low = None
        self._previous_rsi = 0
        self._donchian_high = 0
        self._donchian_low = 0
        self._donchian_middle = 0
        self._current_rsi = 0

    @property
    def RsiPeriod(self):
        """RSI period parameter."""
        return self._rsi_period.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsi_period.Value = value

    @property
    def DonchianPeriod(self):
        """Donchian Channel period parameter."""
        return self._donchian_period.Value

    @DonchianPeriod.setter
    def DonchianPeriod(self, value):
        self._donchian_period.Value = value

    @property
    def StopLossPercent(self):
        """Stop-loss percentage parameter."""
        return self._stop_loss_percent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def CandleType(self):
        """Candle type parameter."""
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        """Return the security and candle type this strategy works with."""
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        """Called when the strategy starts."""
        super(rsi_donchian_strategy, self).OnStarted(time)

        self._previous_rsi = 0
        self._donchian_high = 0
        self._donchian_low = 0
        self._donchian_middle = 0
        self._current_rsi = 0

        # Initialize indicators
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.RsiPeriod

        self._highest_high = Highest()
        self._highest_high.Length = self.DonchianPeriod

        self._lowest_low = Lowest()
        self._lowest_low.Length = self.DonchianPeriod

        # Create candles subscription
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind indicators
        subscription.Bind(self._rsi, self._highest_high, self._lowest_low, self.ProcessIndicators).Start()

        # Enable position protection with stop-loss
        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent)
        )
        # Setup chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._rsi)
            self.DrawOwnTrades(area)

    def ProcessIndicators(self, candle, rsi_value, highest_value, lowest_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Save previous RSI value
        self._previous_rsi = self._current_rsi

        # Get current RSI value
        self._current_rsi = float(rsi_value)

        # Update Donchian high value
        self._donchian_high = float(highest_value)

        # Update Donchian low value
        self._donchian_low = float(lowest_value)

        # Calculate Donchian middle line
        self._donchian_middle = (self._donchian_high + self._donchian_low) / 2

        # Process trading logic after all indicators are updated
        self.ProcessTradingLogic(candle)

    def ProcessTradingLogic(self, candle):
        # Skip if strategy is not ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Skip if not all indicators are initialized
        if self._donchian_high == 0 or self._donchian_low == 0 or self._current_rsi == 0:
            return

        # Trading signals
        is_rsi_oversold = self._current_rsi < 30
        is_rsi_overbought = self._current_rsi > 70
        is_price_breaking_higher = candle.ClosePrice > self._donchian_high
        is_price_breaking_lower = candle.ClosePrice < self._donchian_low

        # Long signal: RSI < 30 (oversold) and price breaks above Donchian high
        if is_rsi_oversold and is_price_breaking_higher:
            if self.Position <= 0:
                self.BuyMarket(self.Volume + Math.Abs(self.Position))
                self.LogInfo("Long Entry: RSI({0:F2}) < 30 && Price({1}) > Donchian High({2})".format(
                    self._current_rsi, candle.ClosePrice, self._donchian_high))
        # Short signal: RSI > 70 (overbought) and price breaks below Donchian low
        elif is_rsi_overbought and is_price_breaking_lower:
            if self.Position >= 0:
                self.SellMarket(self.Volume + Math.Abs(self.Position))
                self.LogInfo("Short Entry: RSI({0:F2}) > 70 && Price({1}) < Donchian Low({2})".format(
                    self._current_rsi, candle.ClosePrice, self._donchian_low))
        # Exit signals based on Donchian middle line
        elif ((self.Position > 0 and candle.ClosePrice < self._donchian_middle) or
              (self.Position < 0 and candle.ClosePrice > self._donchian_middle)):
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
                self.LogInfo("Exit Long: Price({0}) < Donchian Middle({1})".format(
                    candle.ClosePrice, self._donchian_middle))
            elif self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
                self.LogInfo("Exit Short: Price({0}) > Donchian Middle({1})".format(
                    candle.ClosePrice, self._donchian_middle))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return rsi_donchian_strategy()

