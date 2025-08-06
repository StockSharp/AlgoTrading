import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, ICandleMessage, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class tweezer_bottom_strategy(Strategy):
    """
    Strategy based on "Tweezer Bottom" candlestick pattern.
    This pattern forms when two candlesticks have nearly identical lows, with the first
    being bearish and the second being bullish, indicating a potential reversal.
    """

    def __init__(self):
        super(tweezer_bottom_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles for strategy calculation", "General")

        self._stop_loss_percent = self.Param("StopLossPercent", 1.0) \
            .SetRange(0.1, 5.0) \
            .SetDisplay("Stop Loss %", "Stop loss as percentage below low", "Risk Management")

        self._low_tolerance_percent = self.Param("LowTolerancePercent", 0.1) \
            .SetRange(0.05, 1.0) \
            .SetDisplay("Low Tolerance %", "Maximum percentage difference between lows", "Pattern Parameters")

        self._previous_candle = None
        self._current_candle = None
        self._entry_price = 0.0

    @property
    def CandleType(self):
        """Candle type and timeframe for strategy."""
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def StopLossPercent(self):
        """Stop-loss percent from entry price."""
        return self._stop_loss_percent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def LowTolerancePercent(self):
        """Tolerance percentage for comparing low prices."""
        return self._low_tolerance_percent.Value

    @LowTolerancePercent.setter
    def LowTolerancePercent(self, value):
        self._low_tolerance_percent.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        """Resets internal state when strategy is reset."""
        super(tweezer_bottom_strategy, self).OnReseted()
        self._previous_candle = None
        self._current_candle = None
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(tweezer_bottom_strategy, self).OnStarted(time)

        # Create subscription and bind to process candles
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

        # Setup protection with stop loss
        self.StartProtection(
            takeProfit=None,
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent),
            isStopTrailing=False
        )
        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Shift candles
        self._previous_candle = self._current_candle
        self._current_candle = candle

        if self._previous_candle is None:
            return

        # Check for Tweezer Bottom pattern
        is_tweezer_bottom = self.IsTweezerBottom(self._previous_candle, self._current_candle)

        # Check for entry condition
        if is_tweezer_bottom and self.Position == 0:
            self.LogInfo("Tweezer Bottom pattern detected. Going long.")
            self.BuyMarket(self.Volume)
            self._entry_price = float(candle.ClosePrice)
        # Check for exit condition
        elif self.Position > 0 and candle.HighPrice > self._entry_price:
            self.LogInfo("Price exceeded entry high. Taking profit.")
            self.SellMarket(Math.Abs(self.Position))

    def IsTweezerBottom(self, candle1, candle2):
        # First candle must be bearish (close < open)
        if candle1.ClosePrice >= candle1.OpenPrice:
            return False

        # Second candle must be bullish (close > open)
        if candle2.ClosePrice <= candle2.OpenPrice:
            return False

        # Calculate the tolerance range for low comparisons
        low_tolerance = candle1.LowPrice * (self.LowTolerancePercent / 100.0)

        # Low prices must be approximately equal
        lows_are_equal = abs(candle1.LowPrice - candle2.LowPrice) <= low_tolerance
        if not lows_are_equal:
            return False

        return True

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return tweezer_bottom_strategy()
