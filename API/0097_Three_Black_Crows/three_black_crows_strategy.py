import clr

clr.AddReference("System.Drawing")
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import UnitTypes
from StockSharp.Messages import Unit
from StockSharp.Messages import DataType
from StockSharp.Messages import ICandleMessage
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import SMA
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class three_black_crows_strategy(Strategy):
    """
    Three Black Crows Strategy.
    Three Black Crows is the bearish counterpart to Three White Soldiers, consisting of three long down candles after an advance.
    The pattern suggests that sellers have seized control as each close lands near the session low.
    This strategy initiates a short position once the third crow appears, expecting momentum to continue lower.
    It can also be used to exit longs that were opened by other systems if the pattern forms at resistance.
    Risk is managed with a tight percent stop above the pattern high, and trades exit if price closes back above that level.

    See more examples: https://github.com/StockSharp/AlgoTrading
    """

    def __init__(self):
        super(three_black_crows_strategy, self).__init__()

        # Initialize internal state
        self._first_candle = None
        self._second_candle = None
        self._current_candle = None

        # Initialize strategy parameters
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles for strategy calculation", "General")

        self._stop_loss_percent = self.Param("StopLossPercent", 1.0) \
            .SetRange(0.1, 5.0) \
            .SetDisplay("Stop Loss %", "Stop loss as percentage above high of pattern", "Risk Management")

        self._ma_length = self.Param("MaLength", 20) \
            .SetRange(10, 50) \
            .SetDisplay("MA Length", "Period of moving average for exit signal", "Indicators")

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
    def MaLength(self):
        """Moving average length for exit signal."""
        return self._ma_length.Value

    @MaLength.setter
    def MaLength(self, value):
        self._ma_length.Value = value

    def OnReseted(self):
        """Resets internal state when strategy is reset."""
        super(three_black_crows_strategy, self).OnReseted()
        self._first_candle = None
        self._second_candle = None
        self._current_candle = None

    def OnStarted(self, time):
        """Called when the strategy starts."""
        super(three_black_crows_strategy, self).OnStarted(time)

        # Reset candle storage
        self._first_candle = None
        self._second_candle = None
        self._current_candle = None

        # Create a simple moving average indicator for exit signal
        ma = SMA()
        ma.Length = self.MaLength

        # Create subscription and bind to process candles
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ma, self.ProcessCandle).Start()

        # Setup protection with stop loss
        self.StartProtection(
            takeProfit=None,
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent)
        )
        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, ma_value):
        """Process each finished candle and execute trading logic."""
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Shift candles
        self._first_candle = self._second_candle
        self._second_candle = self._current_candle
        self._current_candle = candle

        # Check if we have enough candles to analyze
        if self._first_candle is None or self._second_candle is None or self._current_candle is None:
            return

        # Check for "Three Black Crows" pattern
        is_black_crows = (
            self._first_candle.OpenPrice > self._first_candle.ClosePrice and
            self._second_candle.OpenPrice > self._second_candle.ClosePrice and
            self._current_candle.OpenPrice > self._current_candle.ClosePrice and
            self._current_candle.ClosePrice < self._second_candle.ClosePrice and
            self._second_candle.ClosePrice < self._first_candle.ClosePrice
        )

        # Check for short entry condition
        if is_black_crows and self.Position == 0:
            self.LogInfo("Three Black Crows pattern detected. Going short.")
            self.SellMarket(self.Volume)
        # Check for exit condition
        elif self.Position < 0 and candle.ClosePrice > ma_value:
            self.LogInfo("Price rose above MA. Exiting short position.")
            self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return three_black_crows_strategy()

