import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal, WilliamsR
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class macd_williams_r_strategy(Strategy):
    """
    Strategy based on MACD and Williams %R indicators.
    Enters long when MACD > Signal and Williams %R is oversold (< -80)
    Enters short when MACD < Signal and Williams %R is overbought (> -20)

    See more examples: https://github.com/StockSharp/AlgoTrading
    """

    def __init__(self):
        super(macd_williams_r_strategy, self).__init__()

        # Initialize strategy parameters
        self._macd_fast = self.Param("MacdFast", 12) \
            .SetDisplay("MACD Fast Period", "Fast EMA period for MACD", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(8, 16, 2)

        self._macd_slow = self.Param("MacdSlow", 26) \
            .SetDisplay("MACD Slow Period", "Slow EMA period for MACD", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(20, 30, 2)

        self._macd_signal = self.Param("MacdSignal", 9) \
            .SetDisplay("MACD Signal Period", "Signal line period for MACD", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(7, 12, 1)

        self._williams_r_period = self.Param("WilliamsRPeriod", 14) \
            .SetDisplay("Williams %R Period", "Period for Williams %R indicator", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 20, 2)

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss as percentage of entry price", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Timeframe for strategy", "General")

    @property
    def macd_fast(self):
        """MACD fast period"""
        return self._macd_fast.Value

    @macd_fast.setter
    def macd_fast(self, value):
        self._macd_fast.Value = value

    @property
    def macd_slow(self):
        """MACD slow period"""
        return self._macd_slow.Value

    @macd_slow.setter
    def macd_slow(self, value):
        self._macd_slow.Value = value

    @property
    def macd_signal(self):
        """MACD signal period"""
        return self._macd_signal.Value

    @macd_signal.setter
    def macd_signal(self, value):
        self._macd_signal.Value = value

    @property
    def williams_r_period(self):
        """Williams %R period"""
        return self._williams_r_period.Value

    @williams_r_period.setter
    def williams_r_period(self, value):
        self._williams_r_period.Value = value

    @property
    def stop_loss_percent(self):
        """Stop-loss percentage"""
        return self._stop_loss_percent.Value

    @stop_loss_percent.setter
    def stop_loss_percent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def candle_type(self):
        """Candle type for strategy calculation"""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.

        :param time: The time when the strategy started.
        """
        super(macd_williams_r_strategy, self).OnStarted(time)

        # Create indicators
        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self.macd_fast
        macd.Macd.LongMa.Length = self.macd_slow
        macd.SignalMa.Length = self.macd_signal
        williams_r = WilliamsR()
        williams_r.Length = self.williams_r_period

        # Enable position protection with stop-loss
        self.StartProtection(
            takeProfit=Unit(0),
            stopLoss=Unit(self.stop_loss_percent, UnitTypes.Percent)
        )
        # Subscribe to candles and bind indicators
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, williams_r, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, macd)

            # Create a separate area for Williams %R
            williams_area = self.CreateChartArea()
            if williams_area is not None:
                self.DrawIndicator(williams_area, williams_r)

            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, macd_value, williams_r_value):
        """
        Processes each finished candle and executes trading logic.

        :param candle: The candle message.
        :param macd_value: The current MACD value with signal line.
        :param williams_r_value: The current Williams %R value.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Get additional values from MACD (signal line)
        macd_line = float(macd_value.Macd)
        signal_line = float(macd_value.Signal)
        williams_r = float(williams_r_value)

        # Trading logic
        if macd_line > signal_line:  # MACD above signal line - bullish
            if williams_r < -80 and self.Position <= 0:  # Oversold condition
                # Buy signal
                self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif macd_line < signal_line:  # MACD below signal line - bearish
            if williams_r > -20 and self.Position >= 0:  # Overbought condition
                # Sell signal
                self.SellMarket(self.Volume + Math.Abs(self.Position))
            elif self.Position > 0:  # Already long, exit on MACD crossing down
                # Exit long position
                self.SellMarket(self.Position)
        elif macd_line > signal_line and self.Position < 0:  # Already short, exit on MACD crossing up
            # Exit short position
            self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return macd_williams_r_strategy()
