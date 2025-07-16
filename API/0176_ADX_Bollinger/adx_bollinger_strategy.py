import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Sides
from StockSharp.Algo.Indicators import AverageDirectionalIndex, BollingerBands, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class adx_bollinger_strategy(Strategy):
    """
    Strategy based on ADX and Bollinger Bands indicators.
    Enters long when ADX > 25 and price breaks above upper Bollinger band
    Enters short when ADX > 25 and price breaks below lower Bollinger band

    See more examples: https://github.com/StockSharp/AlgoTrading
    """

    def __init__(self):
        super(adx_bollinger_strategy, self).__init__()

        # Initialize strategy parameters
        self._adx_period = self.Param("AdxPeriod", 14) \
            .SetDisplay("ADX Period", "Period for ADX indicator", "Indicators")

        self._bollinger_period = self.Param("BollingerPeriod", 20) \
            .SetDisplay("Bollinger Period", "Period for Bollinger Bands", "Indicators")

        self._bollinger_deviation = self.Param("BollingerDeviation", 2.0) \
            .SetDisplay("Bollinger Deviation", "Deviation multiplier for Bollinger Bands", "Indicators")

        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "Period for ATR indicator for stop-loss", "Risk Management")

        self._atr_multiplier = self.Param("AtrMultiplier", 2.0) \
            .SetDisplay("ATR Multiplier", "Multiplier for ATR-based stop-loss", "Risk Management")

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Timeframe for strategy", "General")

    @property
    def adx_period(self):
        """ADX period."""
        return self._adx_period.Value

    @adx_period.setter
    def adx_period(self, value):
        self._adx_period.Value = value

    @property
    def bollinger_period(self):
        """Bollinger Bands period."""
        return self._bollinger_period.Value

    @bollinger_period.setter
    def bollinger_period(self, value):
        self._bollinger_period.Value = value

    @property
    def bollinger_deviation(self):
        """Bollinger Bands deviation."""
        return self._bollinger_deviation.Value

    @bollinger_deviation.setter
    def bollinger_deviation(self, value):
        self._bollinger_deviation.Value = value

    @property
    def atr_period(self):
        """ATR period for stop-loss calculation."""
        return self._atr_period.Value

    @atr_period.setter
    def atr_period(self, value):
        self._atr_period.Value = value

    @property
    def atr_multiplier(self):
        """ATR multiplier for stop-loss."""
        return self._atr_multiplier.Value

    @atr_multiplier.setter
    def atr_multiplier(self, value):
        self._atr_multiplier.Value = value

    @property
    def candle_type(self):
        """Candle type for strategy calculation."""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.

        :param time: The time when the strategy started.
        """
        super(adx_bollinger_strategy, self).OnStarted(time)

        # Create indicators
        adx = AverageDirectionalIndex()
        adx.Length = self.adx_period

        bollinger = BollingerBands()
        bollinger.Length = self.bollinger_period
        bollinger.Width = self.bollinger_deviation

        atr = AverageTrueRange()
        atr.Length = self.atr_period

        # Subscribe to candles and bind indicators
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(adx, bollinger, atr, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)

            # Create a separate area for ADX
            adx_area = self.CreateChartArea()
            if adx_area is not None:
                self.DrawIndicator(adx_area, adx)

            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, adx_value, bollinger_value, atr_value):
        """
        Process candle and execute trading logic

        :param candle: The candle message.
        :param adx_value: The ADX value.
        :param bollinger_value: The Bollinger Bands value.
        :param atr_value: The ATR value.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Get additional values from Bollinger Bands
        bollinger_typed = bollinger_value

        upper_band = bollinger_typed.UpBand
        lower_band = bollinger_typed.LowBand
        middle_band = (upper_band - lower_band) / 2 + lower_band

        # Current price (close of the candle)
        price = candle.ClosePrice

        # Stop-loss size based on ATR
        stop_size = float(atr_value) * self.atr_multiplier

        adx_typed = adx_value

        # Trading logic
        if adx_typed.MovingAverage > 25:  # Strong trend
            if price > upper_band and self.Position <= 0:
                # Buy signal: price above upper Bollinger band with strong trend
                self.BuyMarket(self.Volume + Math.Abs(self.Position))

                # Set stop-loss
                stop_price = price - stop_size
                self.RegisterOrder(self.CreateOrder(Sides.Sell, stop_price, max(Math.Abs(self.Position + self.Volume), self.Volume)))
            elif price < lower_band and self.Position >= 0:
                # Sell signal: price below lower Bollinger band with strong trend
                self.SellMarket(self.Volume + Math.Abs(self.Position))

                # Set stop-loss
                stop_price = price + stop_size
                self.RegisterOrder(self.CreateOrder(Sides.Buy, stop_price, max(Math.Abs(self.Position + self.Volume), self.Volume)))
        elif adx_typed.MovingAverage < 20:
            # Trend is weakening - close any position
            if self.Position > 0:
                self.SellMarket(self.Position)
            elif self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
        elif price < middle_band and self.Position > 0:
            # Exit long position when price returns to middle band
            self.SellMarket(self.Position)
        elif price > middle_band and self.Position < 0:
            # Exit short position when price returns to middle band
            self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return adx_bollinger_strategy()
