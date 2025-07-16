import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import DataType
from StockSharp.Messages import CandleStates
from StockSharp.Messages import Unit
from StockSharp.Messages import UnitTypes
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy

class macd_cci_strategy(Strategy):
    """
    Implementation of strategy - MACD + CCI.
    Buy when MACD is above Signal line and CCI is below -100 (oversold).
    Sell when MACD is below Signal line and CCI is above 100 (overbought).

    See more examples: https://github.com/StockSharp/AlgoTrading
    """

    def __init__(self):
        super(macd_cci_strategy, self).__init__()

        # Initialize strategy parameters
        self._fastPeriod = self.Param("FastPeriod", 12) \
            .SetDisplay("Fast Period", "Fast EMA period for MACD", "MACD Parameters")

        self._slowPeriod = self.Param("SlowPeriod", 26) \
            .SetDisplay("Slow Period", "Slow EMA period for MACD", "MACD Parameters")

        self._signalPeriod = self.Param("SignalPeriod", 9) \
            .SetDisplay("Signal Period", "Signal line period for MACD", "MACD Parameters")

        self._cciPeriod = self.Param("CciPeriod", 20) \
            .SetDisplay("CCI Period", "Period for Commodity Channel Index", "CCI Parameters")

        self._cciOversold = self.Param("CciOversold", -100.0) \
            .SetDisplay("CCI Oversold", "CCI level to consider market oversold", "CCI Parameters")

        self._cciOverbought = self.Param("CciOverbought", 100.0) \
            .SetDisplay("CCI Overbought", "CCI level to consider market overbought", "CCI Parameters")

        self._stopLoss = self.Param("StopLoss", Unit(2, UnitTypes.Percent)) \
            .SetDisplay("Stop Loss", "Stop loss percent or value", "Risk Management")

        self._candleType = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle type for strategy", "General")

    @property
    def FastPeriod(self):
        """MACD fast period."""
        return self._fastPeriod.Value

    @FastPeriod.setter
    def FastPeriod(self, value):
        self._fastPeriod.Value = value

    @property
    def SlowPeriod(self):
        """MACD slow period."""
        return self._slowPeriod.Value

    @SlowPeriod.setter
    def SlowPeriod(self, value):
        self._slowPeriod.Value = value

    @property
    def SignalPeriod(self):
        """MACD signal period."""
        return self._signalPeriod.Value

    @SignalPeriod.setter
    def SignalPeriod(self, value):
        self._signalPeriod.Value = value

    @property
    def CciPeriod(self):
        """CCI period."""
        return self._cciPeriod.Value

    @CciPeriod.setter
    def CciPeriod(self, value):
        self._cciPeriod.Value = value

    @property
    def CciOversold(self):
        """CCI oversold level."""
        return self._cciOversold.Value

    @CciOversold.setter
    def CciOversold(self, value):
        self._cciOversold.Value = value

    @property
    def CciOverbought(self):
        """CCI overbought level."""
        return self._cciOverbought.Value

    @CciOverbought.setter
    def CciOverbought(self, value):
        self._cciOverbought.Value = value

    @property
    def StopLoss(self):
        """Stop-loss value."""
        return self._stopLoss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stopLoss.Value = value

    @property
    def CandleType(self):
        """Candle type used for strategy."""
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    def OnStarted(self, time):
        """
        Called when the strategy starts.
        Creates indicators and subscriptions.
        """
        super(macd_cci_strategy, self).OnStarted(time)

        # Create indicators
        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self.FastPeriod
        macd.Macd.LongMa.Length = self.SlowPeriod
        macd.SignalMa.Length = self.SignalPeriod
        cci = CommodityChannelIndex()
        cci.Length = self.CciPeriod

        # Setup candle subscription
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind indicators to candles
        subscription.BindEx(macd, cci, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, macd)

            # Create separate area for CCI
            cciArea = self.CreateChartArea()
            if cciArea is not None:
                self.DrawIndicator(cciArea, cci)

            self.DrawOwnTrades(area)

        # Start protective orders
        self.StartProtection(Unit(0, UnitTypes.Absolute), self.StopLoss)

    def ProcessCandle(self, candle, macdValue, cciValue):
        """
        Processes a finished candle and performs trading logic.
        """
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Note: In this implementation, the MACD and signal values are obtained separately.
        # We need to extract both MACD and signal values to determine crossovers.
        # For demonstration, we'll access these values through a direct call to the indicator.
        # In a proper implementation, we should find a way to get these values through Bind parameter values.

        # Get MACD line and Signal line values
        # This approach is not ideal - in a proper implementation, these values should come from the Bind parameters
        macdTyped = macdValue
        macdLine = macdTyped.Macd  # The main MACD line
        signalLine = macdTyped.Signal  # Signal line

        # Determine if MACD is above or below signal line
        isMacdAboveSignal = macdLine > signalLine

        cciDec = cciValue.ToDecimal()

        self.LogInfo(
            "Candle: {0}, Close: {1}, MACD: {2}, Signal: {3}, MACD > Signal: {4}, CCI: {5}".format(
                candle.OpenTime, candle.ClosePrice, macdLine, signalLine, isMacdAboveSignal, cciDec))

        # Trading rules
        if isMacdAboveSignal and cciDec < self.CciOversold and self.Position <= 0:
            # Buy signal - MACD above signal line and CCI oversold
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)
            self.LogInfo(
                "Buy signal: MACD above Signal and CCI oversold ({0} < {1}). Volume: {2}".format(
                    cciDec, self.CciOversold, volume))
        elif not isMacdAboveSignal and cciDec > self.CciOverbought and self.Position >= 0:
            # Sell signal - MACD below signal line and CCI overbought
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)
            self.LogInfo(
                "Sell signal: MACD below Signal and CCI overbought ({0} > {1}). Volume: {2}".format(
                    cciDec, self.CciOverbought, volume))
        elif not isMacdAboveSignal and self.Position > 0:
            # Exit long position when MACD crosses below signal
            self.SellMarket(self.Position)
            self.LogInfo("Exit long: MACD crossed below Signal. Position: {0}".format(self.Position))
        elif isMacdAboveSignal and self.Position < 0:
            # Exit short position when MACD crosses above signal
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo("Exit short: MACD crossed above Signal. Position: {0}".format(self.Position))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return macd_cci_strategy()
