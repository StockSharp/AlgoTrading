import clr

clr.AddReference("System.Drawing")
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from System.Drawing import Color
from StockSharp.Messages import UnitTypes
from StockSharp.Messages import Unit
from StockSharp.Messages import DataType
from StockSharp.Messages import ICandleMessage
from StockSharp.Messages import CandleStates
from StockSharp.Messages import Sides
from StockSharp.Algo.Indicators import DonchianChannels
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy

class donchian_macd_strategy(Strategy):
    """
    Strategy combining Donchian Channel breakout with MACD trend confirmation.
    """
    def __init__(self):
        super(donchian_macd_strategy, self).__init__()

        # Initialize internal state
        self._previousHighest = 0
        self._previousLowest = float("inf")
        self._previousMacd = 0
        self._previousSignal = 0
        self._entryPrice = None

        # Initialize strategy parameters
        self._donchianPeriod = self.Param("DonchianPeriod", 20) \
            .SetDisplay("Donchian Period", "Channel lookback period", "Indicators")

        self._macdFast = self.Param("MacdFast", 12) \
            .SetDisplay("MACD Fast Period", "Fast EMA period for MACD", "Indicators")

        self._macdSlow = self.Param("MacdSlow", 26) \
            .SetDisplay("MACD Slow Period", "Slow EMA period for MACD", "Indicators")

        self._macdSignal = self.Param("MacdSignal", 9) \
            .SetDisplay("MACD Signal Period", "Signal line period for MACD", "Indicators")

        self._stopLossPercent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop-Loss %", "Stop-loss percentage from entry price", "Risk Management")

        self._candleType = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def DonchianPeriod(self):
        """Donchian channel period."""
        return self._donchianPeriod.Value

    @DonchianPeriod.setter
    def DonchianPeriod(self, value):
        self._donchianPeriod.Value = value

    @property
    def MacdFast(self):
        """MACD fast period."""
        return self._macdFast.Value

    @MacdFast.setter
    def MacdFast(self, value):
        self._macdFast.Value = value

    @property
    def MacdSlow(self):
        """MACD slow period."""
        return self._macdSlow.Value

    @MacdSlow.setter
    def MacdSlow(self, value):
        self._macdSlow.Value = value

    @property
    def MacdSignal(self):
        """MACD signal period."""
        return self._macdSignal.Value

    @MacdSignal.setter
    def MacdSignal(self, value):
        self._macdSignal.Value = value

    @property
    def StopLossPercent(self):
        """Stop loss percentage."""
        return self._stopLossPercent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stopLossPercent.Value = value

    @property
    def CandleType(self):
        """Candle type for strategy."""
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    def OnReseted(self):
        super(donchian_macd_strategy, self).OnReseted()
        self._previousHighest = 0
        self._previousLowest = float('inf')
        self._previousMacd = 0
        self._previousSignal = 0
        self._entryPrice = None

    def OnStarted(self, time):
        super(donchian_macd_strategy, self).OnStarted(time)

        # Initialize indicators
        self._donchian = DonchianChannels()
        self._donchian.Length = self.DonchianPeriod

        self._macd = MovingAverageConvergenceDivergenceSignal()
        self._macd.Macd.ShortMa.Length = self.MacdFast
        self._macd.Macd.LongMa.Length = self.MacdSlow
        self._macd.SignalMa.Length = self.MacdSignal

        # Reset state variables
        self._previousHighest = 0
        self._previousLowest = float('inf')
        self._previousMacd = 0
        self._previousSignal = 0
        self._entryPrice = None

        # Create subscription and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(self._donchian, self._macd, self.ProcessCandle).Start()

        # Setup position protection
        self.StartProtection(Unit(0, UnitTypes.Absolute), Unit(self.StopLossPercent, UnitTypes.Percent))

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._donchian)
            self.DrawIndicator(area, self._macd)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, donchianValue, macdValue):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Wait until strategy and indicators are ready
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        macdTyped = macdValue
        signalValue = macdTyped.Signal
        macdDec = macdTyped.Macd

        # Check for breakouts with MACD trend confirmation
        # Long entry: Price breaks above Donchian high and MACD > Signal
        if candle.ClosePrice > self._previousHighest and self.Position <= 0 and macdDec > signalValue:
            # Cancel existing orders before entering new position
            self.CancelActiveOrders()

            # Enter long position
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)
            self._entryPrice = candle.ClosePrice

            self.LogInfo("Long entry signal: Price {0} broke above Donchian high {1} with MACD confirmation".format(candle.ClosePrice, self._previousHighest))
        # Short entry: Price breaks below Donchian low and MACD < Signal
        elif candle.ClosePrice < self._previousLowest and self.Position >= 0 and macdDec < signalValue:
            # Cancel existing orders before entering new position
            self.CancelActiveOrders()

            # Enter short position
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)
            self._entryPrice = candle.ClosePrice

            self.LogInfo("Short entry signal: Price {0} broke below Donchian low {1} with MACD confirmation".format(candle.ClosePrice, self._previousLowest))
        # MACD trend reversal exit
        elif ((self.Position > 0 and macdDec < signalValue and self._previousMacd > self._previousSignal) or
              (self.Position < 0 and macdDec > signalValue and self._previousMacd < self._previousSignal)):
            # Close position on MACD signal reversal
            self.ClosePosition()
            self._entryPrice = None

            self.LogInfo("Exit signal: MACD trend reversal. MACD: {0}, Signal: {1}".format(macdDec, signalValue))

        donchianTyped = donchianValue

        # Update previous values for next candle
        self._previousHighest = donchianTyped.UpperBand
        self._previousLowest = donchianTyped.LowerBand
        self._previousMacd = macdDec
        self._previousSignal = signalValue

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return donchian_macd_strategy()
