import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import BollingerBands, CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class bollinger_cci_strategy(Strategy):
    """
    Implementation of strategy - Bollinger Bands + CCI.
    Buy when price is below lower Bollinger Band and CCI is below -100 (oversold).
    Sell when price is above upper Bollinger Band and CCI is above 100 (overbought).
    """

    def __init__(self):
        super(bollinger_cci_strategy, self).__init__()

        # Initialize strategy parameters
        self._bollinger_period = self.Param("BollingerPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Bollinger Period", "Period for Bollinger Bands", "Bollinger Parameters")

        self._bollinger_deviation = self.Param("BollingerDeviation", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Bollinger Deviation", "Deviation multiplier for Bollinger Bands", "Bollinger Parameters")

        self._cci_period = self.Param("CciPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("CCI Period", "Period for Commodity Channel Index", "CCI Parameters")

        self._cci_oversold = self.Param("CciOversold", -100) \
            .SetDisplay("CCI Oversold", "CCI level to consider market oversold", "CCI Parameters")

        self._cci_overbought = self.Param("CciOverbought", 100) \
            .SetDisplay("CCI Overbought", "CCI level to consider market overbought", "CCI Parameters")

        self._stop_loss = self.Param("StopLoss", Unit(2, UnitTypes.Absolute)) \
            .SetDisplay("Stop Loss", "Stop loss in ATR or value", "Risk Management")

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Candle type for strategy", "General")

    @property
    def BollingerPeriod(self):
        """Bollinger Bands period."""
        return self._bollinger_period.Value

    @BollingerPeriod.setter
    def BollingerPeriod(self, value):
        self._bollinger_period.Value = value

    @property
    def BollingerDeviation(self):
        """Bollinger Bands deviation multiplier."""
        return self._bollinger_deviation.Value

    @BollingerDeviation.setter
    def BollingerDeviation(self, value):
        self._bollinger_deviation.Value = value

    @property
    def CciPeriod(self):
        """CCI period."""
        return self._cci_period.Value

    @CciPeriod.setter
    def CciPeriod(self, value):
        self._cci_period.Value = value

    @property
    def CciOversold(self):
        """CCI oversold level."""
        return self._cci_oversold.Value

    @CciOversold.setter
    def CciOversold(self, value):
        self._cci_oversold.Value = value

    @property
    def CciOverbought(self):
        """CCI overbought level."""
        return self._cci_overbought.Value

    @CciOverbought.setter
    def CciOverbought(self, value):
        self._cci_overbought.Value = value

    @property
    def StopLoss(self):
        """Stop-loss value."""
        return self._stop_loss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stop_loss.Value = value

    @property
    def CandleType(self):
        """Candle type used for strategy."""
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        super(bollinger_cci_strategy, self).OnStarted(time)

        # Create indicators
        bollinger = BollingerBands()
        bollinger.Length = self.BollingerPeriod
        bollinger.Width = self.BollingerDeviation

        cci = CommodityChannelIndex()
        cci.Length = self.CciPeriod

        # Setup candle subscription
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind indicators to candles
        subscription.BindEx(bollinger, cci, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)

            # Create separate area for CCI
            cciArea = self.CreateChartArea()
            if cciArea is not None:
                self.DrawIndicator(cciArea, cci)

            self.DrawOwnTrades(area)

        # Start protective orders
        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=self.StopLoss
        )
    def ProcessCandle(self, candle, bollingerValue, cciValue):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # In this function we receive only the middle band value from the Bollinger Bands indicator
        # We need to calculate the upper and lower bands ourselves or get them directly from the indicator

        # Get Bollinger Bands values from the indicator
        bb = bollingerValue

        if bb.MovingAverage is None:
            return
        middleBand = float(bb.MovingAverage)

        if bb.UpBand is None:
            return
        upperBand = float(bb.UpBand)

        if bb.LowBand is None:
            return
        lowerBand = float(bb.LowBand)
        cciTyped = to_float(cciValue)

        # Current price
        price = float(candle.ClosePrice)

        self.LogInfo(
            "Candle: {0}, Close: {1}, Upper Band: {2}, Middle Band: {3}, Lower Band: {4}, CCI: {5}".format(
                candle.OpenTime, price, upperBand, middleBand, lowerBand, cciTyped))

        # Trading rules
        if price < lowerBand and cciTyped < self.CciOversold and self.Position <= 0:
            # Buy signal - price below lower band and CCI oversold
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)

            self.LogInfo(
                "Buy signal: Price below lower Bollinger Band and CCI oversold ({0} < {1}). Volume: {2}".format(
                    cciTyped, self.CciOversold, volume))
        elif price > upperBand and cciTyped > self.CciOverbought and self.Position >= 0:
            # Sell signal - price above upper band and CCI overbought
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)

            self.LogInfo(
                "Sell signal: Price above upper Bollinger Band and CCI overbought ({0} > {1}). Volume: {2}".format(
                    cciTyped, self.CciOverbought, volume))
        # Exit conditions
        elif price > middleBand and self.Position > 0:
            # Exit long position when price returns to the middle band
            self.SellMarket(Math.Abs(self.Position))
            self.LogInfo("Exit long: Price returned to middle band. Position: {0}".format(self.Position))
        elif price < middleBand and self.Position < 0:
            # Exit short position when price returns to the middle band
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo("Exit short: Price returned to middle band. Position: {0}".format(self.Position))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return bollinger_cci_strategy()
