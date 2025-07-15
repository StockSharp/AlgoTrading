import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import DataType, Unit, UnitTypes
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal, VolumeWeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy

class macd_vwap_strategy(Strategy):
    """
    Strategy based on MACD and VWAP indicators.
    Enters long when MACD > Signal and price > VWAP
    Enters short when MACD < Signal and price < VWAP
    """

    def __init__(self):
        super(macd_vwap_strategy, self).__init__()

        # MACD fast period
        self._macd_fast = self.Param("MacdFast", 12) \
            .SetDisplay("MACD Fast Period", "Fast EMA period for MACD", "Indicators")

        # MACD slow period
        self._macd_slow = self.Param("MacdSlow", 26) \
            .SetDisplay("MACD Slow Period", "Slow EMA period for MACD", "Indicators")

        # MACD signal period
        self._macd_signal = self.Param("MacdSignal", 9) \
            .SetDisplay("MACD Signal Period", "Signal line period for MACD", "Indicators")

        # Stop-loss percentage
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss as percentage of entry price", "Risk Management")

        # Candle type for strategy calculation
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Timeframe for strategy", "General")

    @property
    def macd_fast(self):
        return self._macd_fast.Value

    @macd_fast.setter
    def macd_fast(self, value):
        self._macd_fast.Value = value

    @property
    def macd_slow(self):
        return self._macd_slow.Value

    @macd_slow.setter
    def macd_slow(self, value):
        self._macd_slow.Value = value

    @property
    def macd_signal(self):
        return self._macd_signal.Value

    @macd_signal.setter
    def macd_signal(self, value):
        self._macd_signal.Value = value

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
        super(macd_vwap_strategy, self).OnStarted(time)

        # Create indicators
        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self.macd_fast
        macd.Macd.LongMa.Length = self.macd_slow
        macd.SignalMa.Length = self.macd_signal
        vwap = VolumeWeightedMovingAverage()

        # Enable position protection with stop-loss
        self.StartProtection(Unit(0), Unit(self.stop_loss_percent, UnitTypes.Percent))

        # Subscribe to candles and bind indicators
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, vwap, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, macd)
            self.DrawIndicator(area, vwap)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, macd_value, vwap_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Get additional values from MACD (signal line)
        macd_typed = macd_value
        macd_line = macd_typed.Macd
        signal_line = macd_typed.Signal
        vwap = vwap_value.ToDecimal()

        # Current price (close of the candle)
        price = candle.ClosePrice

        # Trading logic
        if macd_line > signal_line and price > vwap and self.Position <= 0:
            # Buy signal: MACD above signal and price above VWAP
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif macd_line < signal_line and price < vwap and self.Position >= 0:
            # Sell signal: MACD below signal and price below VWAP
            self.SellMarket(self.Volume + Math.Abs(self.Position))
        # Exit conditions
        elif macd_line < signal_line and self.Position > 0:
            # Exit long position when MACD crosses below signal
            self.SellMarket(self.Position)
        elif macd_line > signal_line and self.Position < 0:
            # Exit short position when MACD crosses above signal
            self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return macd_vwap_strategy()
