import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class gap_fill_reversal_strategy(Strategy):
    """
    Gap Fill Reversal Strategy that trades gaps followed by reversal candles.
    It enters when a gap is followed by a candle in the opposite direction of the gap.
    """

    def __init__(self):
        super(gap_fill_reversal_strategy, self).__init__()

        # Initialize strategy parameters
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles for strategy calculation", "General")

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetRange(0.1, 5.0) \
            .SetDisplay("Stop Loss %", "Stop loss as percentage from entry price", "Risk Management")

        self._min_gap_percent = self.Param("MinGapPercent", 0.5) \
            .SetRange(0.1, 3.0) \
            .SetDisplay("Min Gap %", "Minimum gap size as percentage for trade signal", "Trading Parameters")

        # Internal candle storage
        self._previous_candle = None
        self._current_candle = None

    @property
    def candle_type(self):
        """Candle type and timeframe for strategy."""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    @property
    def stop_loss_percent(self):
        """Stop-loss percent from entry price."""
        return self._stop_loss_percent.Value

    @stop_loss_percent.setter
    def stop_loss_percent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def min_gap_percent(self):
        """Minimum gap size as percentage for trade signal."""
        return self._min_gap_percent.Value

    @min_gap_percent.setter
    def min_gap_percent(self, value):
        self._min_gap_percent.Value = value

    def OnReseted(self):
        """Resets internal state when strategy is reset."""
        super(gap_fill_reversal_strategy, self).OnReseted()
        self._previous_candle = None
        self._current_candle = None

    def OnStarted(self, time):
        """Called when the strategy starts."""
        super(gap_fill_reversal_strategy, self).OnStarted(time)

        # Reset candle storage
        self._previous_candle = None
        self._current_candle = None

        # Create subscription and bind to process candles
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.ProcessCandle).Start()

        # Setup protection with stop loss
        self.StartProtection(
            takeProfit=None,
            stopLoss=Unit(self.stop_loss_percent, UnitTypes.Percent),
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

        # Check for a gap
        has_gap_up = self._current_candle.OpenPrice > self._previous_candle.ClosePrice
        has_gap_down = self._current_candle.OpenPrice < self._previous_candle.ClosePrice

        # Calculate gap size as a percentage
        gap_size = 0.0
        if has_gap_up:
            gap_size = float((self._current_candle.OpenPrice - self._previous_candle.ClosePrice) / self._previous_candle.ClosePrice * 100)
        elif has_gap_down:
            gap_size = float((self._previous_candle.ClosePrice - self._current_candle.OpenPrice) / self._previous_candle.ClosePrice * 100)

        # Check if gap is large enough
        if gap_size < self.min_gap_percent:
            return

        # Check for a gap up followed by a bearish candle (potential reversal)
        is_gap_up_with_reversal = has_gap_up and self._current_candle.ClosePrice < self._current_candle.OpenPrice

        # Check for a gap down followed by a bullish candle (potential reversal)
        is_gap_down_with_reversal = has_gap_down and self._current_candle.ClosePrice > self._current_candle.OpenPrice

        # Check for long entry condition
        if is_gap_down_with_reversal and self.Position <= 0:
            self.LogInfo(f"Gap down of {gap_size:.2f}% with bullish reversal candle. Going long.")
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        # Check for short entry condition
        elif is_gap_up_with_reversal and self.Position >= 0:
            self.LogInfo(f"Gap up of {gap_size:.2f}% with bearish reversal candle. Going short.")
            self.SellMarket(self.Volume + Math.Abs(self.Position))
        # Check for exit conditions
        elif ((self.Position > 0 and candle.ClosePrice > self._previous_candle.ClosePrice) or
              (self.Position < 0 and candle.ClosePrice < self._previous_candle.ClosePrice)):
            self.LogInfo("Gap filled. Exiting position.")
            self.ClosePosition()

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return gap_fill_reversal_strategy()