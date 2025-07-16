import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *


class macd_rsi_strategy(Strategy):
    """
    Strategy that combines MACD and RSI indicators to identify potential trading opportunities.
    It looks for trend direction with MACD and enters on extreme RSI values in the trend direction.

    See more examples: https://github.com/StockSharp/AlgoTrading
    """

    def __init__(self):
        super(macd_rsi_strategy, self).__init__()

        # Initialize strategy parameters
        self._candle_type = self.Param("CandleType", tf(15)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._macd_fast = self.Param("MacdFast", 12) \
            .SetRange(5, 30) \
            .SetDisplay("MACD Fast", "Fast period for MACD calculation", "MACD Settings") \
            .SetCanOptimize(True)

        self._macd_slow = self.Param("MacdSlow", 26) \
            .SetRange(10, 50) \
            .SetDisplay("MACD Slow", "Slow period for MACD calculation", "MACD Settings") \
            .SetCanOptimize(True)

        self._macd_signal = self.Param("MacdSignal", 9) \
            .SetRange(3, 20) \
            .SetDisplay("MACD Signal", "Signal period for MACD calculation", "MACD Settings") \
            .SetCanOptimize(True)

        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetRange(5, 30) \
            .SetDisplay("RSI Period", "Period for RSI calculation", "RSI Settings") \
            .SetCanOptimize(True)

        self._rsi_oversold = self.Param("RsiOversold", 30) \
            .SetRange(10, 40) \
            .SetDisplay("RSI Oversold", "RSI oversold level", "RSI Settings") \
            .SetCanOptimize(True)

        self._rsi_overbought = self.Param("RsiOverbought", 70) \
            .SetRange(60, 90) \
            .SetDisplay("RSI Overbought", "RSI overbought level", "RSI Settings") \
            .SetCanOptimize(True)

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetRange(0.5, 5.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management")

    @property
    def candle_type(self):
        """Data type for candles."""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    @property
    def macd_fast(self):
        """Fast period for MACD calculation."""
        return self._macd_fast.Value

    @macd_fast.setter
    def macd_fast(self, value):
        self._macd_fast.Value = value

    @property
    def macd_slow(self):
        """Slow period for MACD calculation."""
        return self._macd_slow.Value

    @macd_slow.setter
    def macd_slow(self, value):
        self._macd_slow.Value = value

    @property
    def macd_signal(self):
        """Signal period for MACD calculation."""
        return self._macd_signal.Value

    @macd_signal.setter
    def macd_signal(self, value):
        self._macd_signal.Value = value

    @property
    def rsi_period(self):
        """Period for RSI calculation."""
        return self._rsi_period.Value

    @rsi_period.setter
    def rsi_period(self, value):
        self._rsi_period.Value = value

    @property
    def rsi_oversold(self):
        """RSI oversold level."""
        return self._rsi_oversold.Value

    @rsi_oversold.setter
    def rsi_oversold(self, value):
        self._rsi_oversold.Value = value

    @property
    def rsi_overbought(self):
        """RSI overbought level."""
        return self._rsi_overbought.Value

    @rsi_overbought.setter
    def rsi_overbought(self, value):
        self._rsi_overbought.Value = value

    @property
    def stop_loss_percent(self):
        """Stop loss percentage from entry price."""
        return self._stop_loss_percent.Value

    @stop_loss_percent.setter
    def stop_loss_percent(self, value):
        self._stop_loss_percent.Value = value

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.

        :param time: The time when the strategy started.
        """
        super(macd_rsi_strategy, self).OnStarted(time)

        # Set up stop loss protection
        self.StartProtection(
            Unit(0),  # No take profit
            Unit(self.stop_loss_percent, UnitTypes.Percent)  # Stop loss based on parameter
        )

        # Create indicators
        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self.macd_fast
        macd.Macd.LongMa.Length = self.macd_slow
        macd.SignalMa.Length = self.macd_signal
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period

        # Create candle subscription
        subscription = self.SubscribeCandles(self.candle_type)

        # When both indicators are ready, process the candle
        subscription.BindEx(macd, rsi, self.ProcessCandle).Start()

        # Set up chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)

            # Draw MACD in a separate area
            macd_area = self.CreateChartArea()
            self.DrawIndicator(macd_area, macd)

            # Draw RSI in a separate area
            rsi_area = self.CreateChartArea()
            self.DrawIndicator(rsi_area, rsi)

            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, macd_value, rsi_value):
        """
        Process incoming candle with MACD and RSI values.

        :param candle: Candle to process.
        :param macd_value: MACD line value.
        :param rsi_value: RSI value.
        """
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        macd_dec = macd_value.Macd
        signal_value = macd_value.Signal
        rsi_dec = to_float(rsi_value)

        # Trading logic: Combine MACD trend with RSI extreme values

        # MACD above signal line indicates uptrend
        is_uptrend = macd_dec > signal_value

        # Check for entry conditions
        if is_uptrend and rsi_dec < self.rsi_oversold:
            # Bullish trend with oversold RSI - Long signal
            if self.Position <= 0:
                self.BuyMarket(self.Volume + Math.Abs(self.Position))
                self.LogInfo(
                    "Buy signal: MACD uptrend ({0:F4} > {1:F4}) with oversold RSI ({2:F2})".format(
                        macd_dec, signal_value, rsi_dec))
        elif not is_uptrend and rsi_dec > self.rsi_overbought:
            # Bearish trend with overbought RSI - Short signal
            if self.Position >= 0:
                self.SellMarket(self.Volume + Math.Abs(self.Position))
                self.LogInfo(
                    "Sell signal: MACD downtrend ({0:F4} < {1:F4}) with overbought RSI ({2:F2})".format(
                        macd_dec, signal_value, rsi_dec))

        # Check for exit conditions
        if self.Position > 0 and not is_uptrend:
            # Exit long when MACD crosses below signal line
            self.SellMarket(Math.Abs(self.Position))
            self.LogInfo(
                "Exit long: MACD crossed below signal ({0:F4} < {1:F4})".format(macd_dec, signal_value))
        elif self.Position < 0 and is_uptrend:
            # Exit short when MACD crosses above signal line
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo(
                "Exit short: MACD crossed above signal ({0:F4} > {1:F4})".format(macd_dec, signal_value))

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return macd_rsi_strategy()
