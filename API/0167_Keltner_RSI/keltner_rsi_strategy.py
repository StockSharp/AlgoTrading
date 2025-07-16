import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import DataType, UnitTypes, Unit
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, ATR, RSI
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class keltner_rsi_strategy(Strategy):
    """
    Strategy combining Keltner Channels and RSI indicators.
    Looks for mean reversion opportunities when price touches channel boundaries
    and RSI confirms oversold/overbought conditions.

    """

    def __init__(self):
        super(keltner_rsi_strategy, self).__init__()

        # Initialize strategy parameters
        self._emaPeriod = self.Param("EmaPeriod", 20) \
            .SetDisplay("EMA Period", "Period for EMA in Keltner Channels", "Indicators")

        self._atrPeriod = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "Period for ATR in Keltner Channels", "Indicators")

        self._atrMultiplier = self.Param("AtrMultiplier", 2.0) \
            .SetDisplay("ATR Multiplier", "Multiplier for ATR to set channel width", "Indicators")

        self._rsiPeriod = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "Period for RSI calculation", "Indicators")

        self._rsiOverboughtLevel = self.Param("RsiOverboughtLevel", 70) \
            .SetDisplay("RSI Overbought", "RSI level considered overbought", "Trading Levels")

        self._rsiOversoldLevel = self.Param("RsiOversoldLevel", 30) \
            .SetDisplay("RSI Oversold", "RSI level considered oversold", "Trading Levels")

        self._stopLossPercent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management")

        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Fields for indicators
        self._ema = None
        self._atr = None
        self._rsi = None

    @property
    def EmaPeriod(self):
        """EMA period for Keltner Channels."""
        return self._emaPeriod.Value

    @EmaPeriod.setter
    def EmaPeriod(self, value):
        self._emaPeriod.Value = value

    @property
    def AtrPeriod(self):
        """ATR period for Keltner Channels."""
        return self._atrPeriod.Value

    @AtrPeriod.setter
    def AtrPeriod(self, value):
        self._atrPeriod.Value = value

    @property
    def AtrMultiplier(self):
        """ATR multiplier for Keltner Channels width."""
        return self._atrMultiplier.Value

    @AtrMultiplier.setter
    def AtrMultiplier(self, value):
        self._atrMultiplier.Value = value

    @property
    def RsiPeriod(self):
        """Period for RSI calculation."""
        return self._rsiPeriod.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsiPeriod.Value = value

    @property
    def RsiOverboughtLevel(self):
        """RSI overbought level."""
        return self._rsiOverboughtLevel.Value

    @RsiOverboughtLevel.setter
    def RsiOverboughtLevel(self, value):
        self._rsiOverboughtLevel.Value = value

    @property
    def RsiOversoldLevel(self):
        """RSI oversold level."""
        return self._rsiOversoldLevel.Value

    @RsiOversoldLevel.setter
    def RsiOversoldLevel(self, value):
        self._rsiOversoldLevel.Value = value

    @property
    def StopLossPercent(self):
        """Stop loss percentage."""
        return self._stopLossPercent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stopLossPercent.Value = value

    @property
    def CandleType(self):
        """Candle type for strategy calculation."""
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.

        :param time: The time when the strategy started.
        """
        super(keltner_rsi_strategy, self).OnStarted(time)

        # Create indicators
        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.EmaPeriod
        self._atr = ATR()
        self._atr.Length = self.AtrPeriod
        self._rsi = RSI()
        self._rsi.Length = self.RsiPeriod

        # Create subscription
        subscription = self.SubscribeCandles(self.CandleType)

        # Use WhenCandlesFinished to process candles manually
        subscription.Bind(self._ema, self._atr, self._rsi, self.ProcessCandle).Start()

        # Enable stop-loss
        self.StartProtection(
            takeProfit=None,
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent),
            isStopTrailing=False,
            useMarketOrders=True
        )
        # Setup chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)

            # Add indicators to chart
            self.DrawIndicator(area, self._ema)

            # Create second area for RSI
            rsiArea = self.CreateChartArea()
            self.DrawIndicator(rsiArea, self._rsi)

            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, emaValue, atrValue, rsiValue):
        """
        Process candle and execute trading logic

        :param candle: The candle message.
        :param emaValue: EMA indicator value.
        :param atrValue: ATR indicator value.
        :param rsiValue: RSI indicator value.
        """
        # Skip if indicators are not formed yet
        if not self._ema.IsFormed or not self._atr.IsFormed or not self._rsi.IsFormed:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Calculate Keltner Channels
        upperBand = emaValue + (atrValue * self.AtrMultiplier)
        lowerBand = emaValue - (atrValue * self.AtrMultiplier)

        # Trading logic
        if candle.ClosePrice < lowerBand and rsiValue < self.RsiOversoldLevel and self.Position <= 0:
            # Price below lower Keltner band and RSI oversold - Buy
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)
        elif candle.ClosePrice > upperBand and rsiValue > self.RsiOverboughtLevel and self.Position >= 0:
            # Price above upper Keltner band and RSI overbought - Sell
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)
        elif self.Position > 0 and candle.ClosePrice > emaValue:
            # Exit long position when price crosses above EMA (middle band)
            self.SellMarket(Math.Abs(self.Position))
        elif self.Position < 0 and candle.ClosePrice < emaValue:
            # Exit short position when price crosses below EMA (middle band)
            self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return keltner_rsi_strategy()
