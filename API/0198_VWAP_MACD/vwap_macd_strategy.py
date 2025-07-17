import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal, VolumeWeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class vwap_macd_strategy(Strategy):
    """
    Strategy based on VWAP and MACD.
    Enters long when price is above VWAP and MACD > Signal.
    Enters short when price is below VWAP and MACD < Signal.
    Exits when MACD crosses its signal line in the opposite direction.
    """

    def __init__(self):
        super(vwap_macd_strategy, self).__init__()

        # MACD fast EMA period.
        self._macd_fast_period = self.Param("MacdFastPeriod", 12) \
            .SetDisplay("MACD Fast Period", "Fast EMA period for MACD calculation", "Indicators") \
            .SetCanOptimize(True)

        # MACD slow EMA period.
        self._macd_slow_period = self.Param("MacdSlowPeriod", 26) \
            .SetDisplay("MACD Slow Period", "Slow EMA period for MACD calculation", "Indicators") \
            .SetCanOptimize(True)

        # MACD signal line period.
        self._macd_signal_period = self.Param("MacdSignalPeriod", 9) \
            .SetDisplay("MACD Signal Period", "Signal line period for MACD calculation", "Indicators") \
            .SetCanOptimize(True)

        # Stop loss percentage value.
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss (%)", "Stop loss percentage from entry price", "Risk Management")

        # Candle type for strategy.
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Timeframe of data for strategy", "General")

        self._macd = None
        self._vwap = None
        self._prev_macd = 0
        self._prev_signal = 0

    @property
    def macd_fast_period(self):
        return self._macd_fast_period.Value

    @macd_fast_period.setter
    def macd_fast_period(self, value):
        self._macd_fast_period.Value = value

    @property
    def macd_slow_period(self):
        return self._macd_slow_period.Value

    @macd_slow_period.setter
    def macd_slow_period(self, value):
        self._macd_slow_period.Value = value

    @property
    def macd_signal_period(self):
        return self._macd_signal_period.Value

    @macd_signal_period.setter
    def macd_signal_period(self, value):
        self._macd_signal_period.Value = value

    @property
    def stop_loss_percent(self):
        return self._stop_loss_percent.Value

    @stop_loss_percent.setter
    def stop_loss_percent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(vwap_macd_strategy, self).OnStarted(time)

        # Create MACD indicator
        self._macd = MovingAverageConvergenceDivergenceSignal()
        self._macd.Macd.ShortMa.Length = self.macd_fast_period
        self._macd.Macd.LongMa.Length = self.macd_slow_period
        self._macd.SignalMa.Length = self.macd_signal_period
        self._vwap = VolumeWeightedMovingAverage()
        self._vwap.Length = self.macd_signal_period

        # Initialize variables
        self._prev_macd = 0
        self._prev_signal = 0

        # Enable position protection
        self.StartProtection(
            takeProfit=Unit(self.stop_loss_percent, UnitTypes.Percent),
            stopLoss=Unit(self.stop_loss_percent, UnitTypes.Percent)
        )
        # Create subscription
        subscription = self.SubscribeCandles(self.candle_type)

        # Process candles with MACD
        subscription.BindEx(self._macd, self.ProcessCandle).Start()

        # Setup chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

            # MACD in separate area
            macd_area = self.CreateChartArea()
            if macd_area is not None:
                self.DrawIndicator(macd_area, self._macd)

    def ProcessCandle(self, candle, macd_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Get VWAP value (calculated per day)
        vwap = to_float(process_candle(self._vwap, candle))

        macd_typed = macd_value

        # Check if MACD and Signal values are available
        if macd_typed.Macd is None or macd_typed.Signal is None:
            return

        # Extract MACD and Signal values
        macd = float(macd_typed.Macd)
        signal = float(macd_typed.Signal)

        # Detect MACD crosses
        macd_crossed_above_signal = self._prev_macd <= self._prev_signal and macd > signal
        macd_crossed_below_signal = self._prev_macd >= self._prev_signal and macd < signal

        # Check if strategy is ready for trading
        if not self.IsFormedAndOnlineAndAllowTrading():
            # Store current values for next candle
            self._prev_macd = macd
            self._prev_signal = signal
            return

        # Trading logic
        if candle.ClosePrice > vwap and macd > signal and self.Position <= 0:
            # Price above VWAP with bullish MACD - go long
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif candle.ClosePrice < vwap and macd < signal and self.Position >= 0:
            # Price below VWAP with bearish MACD - go short
            self.SellMarket(self.Volume + Math.Abs(self.Position))

        # Exit logic based on MACD crosses
        if self.Position > 0 and macd_crossed_below_signal:
            # Exit long position when MACD crosses below Signal
            self.ClosePosition()
        elif self.Position < 0 and macd_crossed_above_signal:
            # Exit short position when MACD crosses above Signal
            self.ClosePosition()

        # Store current values for next candle
        self._prev_macd = macd
        self._prev_signal = signal

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return vwap_macd_strategy()
