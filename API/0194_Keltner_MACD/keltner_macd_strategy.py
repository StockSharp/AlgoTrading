import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange, MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class keltner_macd_strategy(Strategy):
    """
    Strategy based on Keltner Channels and MACD.
    Enters long when price breaks above upper Keltner Channel with MACD > Signal.
    Enters short when price breaks below lower Keltner Channel with MACD < Signal.
    Exits when MACD crosses its signal line in the opposite direction.

    """

    def __init__(self):
        super(keltner_macd_strategy, self).__init__()

        # Initialize strategy parameters
        self._emaPeriod = self.Param("EmaPeriod", 20) \
            .SetDisplay("EMA Period", "Period for EMA calculation in Keltner Channel", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 30, 5)

        self._multiplier = self.Param("Multiplier", 2.0) \
            .SetDisplay("ATR Multiplier", "ATR multiplier for Keltner Channel bands", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(1.5, 3.0, 0.5)

        self._atrPeriod = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "Period for ATR calculation in Keltner Channel", "Indicators")

        self._macdFastPeriod = self.Param("MacdFastPeriod", 12) \
            .SetDisplay("MACD Fast Period", "Fast EMA period for MACD calculation", "Indicators") \
            .SetCanOptimize(True)

        self._macdSlowPeriod = self.Param("MacdSlowPeriod", 26) \
            .SetDisplay("MACD Slow Period", "Slow EMA period for MACD calculation", "Indicators") \
            .SetCanOptimize(True)

        self._macdSignalPeriod = self.Param("MacdSignalPeriod", 9) \
            .SetDisplay("MACD Signal Period", "Signal line period for MACD calculation", "Indicators") \
            .SetCanOptimize(True)

        self._atrMultiplier = self.Param("AtrMultiplier", 2.0) \
            .SetDisplay("Stop Loss ATR Multiplier", "ATR multiplier for stop loss calculation", "Risk Management")

        self._candleType = self.Param("CandleType", tf(15)) \
            .SetDisplay("Candle Type", "Timeframe of data for strategy", "General")

        self._stopLossPercent = self.Param("StopLossPercent", 1.0) \
            .SetNotNegative() \
            .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(0.5, 2.0, 0.5)

        # Indicators
        self._ema = None
        self._atr = None
        self._macd = None

        # Previous MACD values for cross detection
        self._prevMacd = 0.0
        self._prevSignal = 0.0

    @property
    def EmaPeriod(self):
        """EMA period for Keltner Channel middle line."""
        return self._emaPeriod.Value

    @EmaPeriod.setter
    def EmaPeriod(self, value):
        self._emaPeriod.Value = value

    @property
    def Multiplier(self):
        """ATR multiplier for Keltner Channel bands."""
        return self._multiplier.Value

    @Multiplier.setter
    def Multiplier(self, value):
        self._multiplier.Value = value

    @property
    def AtrPeriod(self):
        """ATR period for Keltner Channel bands."""
        return self._atrPeriod.Value

    @AtrPeriod.setter
    def AtrPeriod(self, value):
        self._atrPeriod.Value = value

    @property
    def MacdFastPeriod(self):
        """MACD fast EMA period."""
        return self._macdFastPeriod.Value

    @MacdFastPeriod.setter
    def MacdFastPeriod(self, value):
        self._macdFastPeriod.Value = value

    @property
    def MacdSlowPeriod(self):
        """MACD slow EMA period."""
        return self._macdSlowPeriod.Value

    @MacdSlowPeriod.setter
    def MacdSlowPeriod(self, value):
        self._macdSlowPeriod.Value = value

    @property
    def MacdSignalPeriod(self):
        """MACD signal line period."""
        return self._macdSignalPeriod.Value

    @MacdSignalPeriod.setter
    def MacdSignalPeriod(self, value):
        self._macdSignalPeriod.Value = value

    @property
    def AtrMultiplier(self):
        """ATR multiplier for stop loss calculation."""
        return self._atrMultiplier.Value

    @AtrMultiplier.setter
    def AtrMultiplier(self, value):
        self._atrMultiplier.Value = value

    @property
    def CandleType(self):
        """Candle type for strategy."""
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    @property
    def StopLossPercent(self):
        """Stop-loss percentage."""
        return self._stopLossPercent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stopLossPercent.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        """Called when the strategy starts."""
        super(keltner_macd_strategy, self).OnStarted(time)

        # Create indicators
        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.EmaPeriod

        self._atr = AverageTrueRange()
        self._atr.Length = self.AtrPeriod

        self._macd = MovingAverageConvergenceDivergenceSignal()
        self._macd.Macd.ShortMa.Length = self.MacdFastPeriod
        self._macd.Macd.LongMa.Length = self.MacdSlowPeriod
        self._macd.SignalMa.Length = self.MacdSignalPeriod

        # Reset previous values
        self._prevMacd = 0.0
        self._prevSignal = 0.0

        # Subscribe to candles and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(self._ema, self._atr, self._macd, self.ProcessCandle).Start()

        # Setup chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)

            macdArea = self.CreateChartArea()
            if macdArea is not None:
                self.DrawIndicator(macdArea, self._macd)

            self.DrawOwnTrades(area)

        self.StartProtection(
            takeProfit=Unit(0),
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Absolute)
        )
    def ProcessCandle(self, candle, ema_value, atr_value, macd_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        ema = to_float(ema_value)
        atr = to_float(atr_value)

        # Calculate Keltner Channels
        upperBand = ema + self.Multiplier * atr
        lowerBand = ema - self.Multiplier * atr

        macd_typed = macd_value
        if macd_typed.Macd is None or macd_typed.Signal is None:
            return

        macd = float(macd_typed.Macd)
        signal = float(macd_typed.Signal)

        # Detect MACD crosses
        macdCrossedAboveSignal = self._prevMacd <= self._prevSignal and macd > signal
        macdCrossedBelowSignal = self._prevMacd >= self._prevSignal and macd < signal

        # Check if strategy is ready for trading
        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prevMacd = macd
            self._prevSignal = signal
            return

        # Trading logic
        if candle.ClosePrice > upperBand and macd > signal and self.Position <= 0:
            # Price breaks above upper Keltner Channel with bullish MACD - go long
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif candle.ClosePrice < lowerBand and macd < signal and self.Position >= 0:
            # Price breaks below lower Keltner Channel with bearish MACD - go short
            self.SellMarket(self.Volume + Math.Abs(self.Position))

        # Exit logic based on MACD crosses
        if self.Position > 0 and macdCrossedBelowSignal:
            # Exit long position when MACD crosses below Signal
            self.ClosePosition()
        elif self.Position < 0 and macdCrossedAboveSignal:
            # Exit short position when MACD crosses above Signal
            self.ClosePosition()

        # Store current values for next candle
        self._prevMacd = macd
        self._prevSignal = signal

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return keltner_macd_strategy()
