import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, UnitTypes, Unit, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class triple_ma_strategy(Strategy):
    """
    Strategy based on Triple Moving Average crossover.
    It enters long position when short MA > middle MA > long MA 
    and short position when short MA < middle MA < long MA.
    
    """
    
    def __init__(self):
        super(triple_ma_strategy, self).__init__()
        
        # Initialize strategy parameters
        self._short_ma_period = self.Param("ShortMaPeriod", 5) \
            .SetDisplay("Short MA Period", "Period for short moving average", "Indicators")
        
        self._middle_ma_period = self.Param("MiddleMaPeriod", 20) \
            .SetDisplay("Middle MA Period", "Period for middle moving average", "Indicators")
        
        self._long_ma_period = self.Param("LongMaPeriod", 50) \
            .SetDisplay("Long MA Period", "Period for long moving average", "Indicators")
        
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss (%)", "Stop loss as a percentage of entry price", "Risk parameters")
        
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        
        # Current state
        self._prev_is_short_above_middle = False

    @property
    def short_ma_period(self):
        """Period for short moving average."""
        return self._short_ma_period.Value

    @short_ma_period.setter
    def short_ma_period(self, value):
        self._short_ma_period.Value = value

    @property
    def middle_ma_period(self):
        """Period for middle moving average."""
        return self._middle_ma_period.Value

    @middle_ma_period.setter
    def middle_ma_period(self, value):
        self._middle_ma_period.Value = value

    @property
    def long_ma_period(self):
        """Period for long moving average."""
        return self._long_ma_period.Value

    @long_ma_period.setter
    def long_ma_period(self, value):
        self._long_ma_period.Value = value

    @property
    def stop_loss_percent(self):
        """Stop-loss percentage."""
        return self._stop_loss_percent.Value

    @stop_loss_percent.setter
    def stop_loss_percent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def candle_type(self):
        """Candle type."""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        """
        Resets internal state when strategy is reset.
        """
        super(triple_ma_strategy, self).OnReseted()
        self._prev_is_short_above_middle = False

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        
        :param time: The time when the strategy started.
        """
        super(triple_ma_strategy, self).OnStarted(time)

        # Create indicators
        short_ma = SimpleMovingAverage()
        short_ma.Length = self.short_ma_period
        
        middle_ma = SimpleMovingAverage()
        middle_ma.Length = self.middle_ma_period
        
        long_ma = SimpleMovingAverage()
        long_ma.Length = self.long_ma_period

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(short_ma, middle_ma, long_ma, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, short_ma)
            self.DrawIndicator(area, middle_ma)
            self.DrawIndicator(area, long_ma)
            self.DrawOwnTrades(area)

        # Start protection with stop loss
        self.StartProtection(
            takeProfit=None,
            stopLoss=Unit(self.stop_loss_percent, UnitTypes.Percent)
        )
    def ProcessCandle(self, candle, short_ma_value, middle_ma_value, long_ma_value):
        """
        Processes each finished candle and executes Triple MA trading logic.
        
        :param candle: The processed candle message.
        :param short_ma_value: The current value of the short MA.
        :param middle_ma_value: The current value of the middle MA.
        :param long_ma_value: The current value of the long MA.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Convert values to float for comparison
        short_ma = float(short_ma_value)
        middle_ma = float(middle_ma_value)
        long_ma = float(long_ma_value)

        # Check the MA alignments
        is_short_above_middle = short_ma > middle_ma
        is_middle_above_long = middle_ma > long_ma

        # Check for MA crossover
        is_short_crossed_middle = is_short_above_middle != self._prev_is_short_above_middle

        # Check for alignment conditions
        is_bullish_alignment = is_short_above_middle and is_middle_above_long
        is_bearish_alignment = not is_short_above_middle and not is_middle_above_long

        # Entry logic based on three MA alignment
        if is_bullish_alignment and self.Position <= 0:
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)
            self.LogInfo("Buy signal: Short MA={0} > Middle MA={1} > Long MA={2}".format(
                short_ma, middle_ma, long_ma))
        elif is_bearish_alignment and self.Position >= 0:
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)
            self.LogInfo("Sell signal: Short MA={0} < Middle MA={1} < Long MA={2}".format(
                short_ma, middle_ma, long_ma))
        # Exit logic based on short MA crossing middle MA
        elif is_short_crossed_middle:
            if not is_short_above_middle and self.Position > 0:
                self.SellMarket(self.Position)
                self.LogInfo("Exit long: Short MA crossed below Middle MA (Short={0}, Middle={1})".format(
                    short_ma, middle_ma))
            elif is_short_above_middle and self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
                self.LogInfo("Exit short: Short MA crossed above Middle MA (Short={0}, Middle={1})".format(
                    short_ma, middle_ma))

        # Update previous state
        self._prev_is_short_above_middle = is_short_above_middle

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return triple_ma_strategy()
