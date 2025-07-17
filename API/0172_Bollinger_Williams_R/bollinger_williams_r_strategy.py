import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Sides
from StockSharp.Algo.Indicators import BollingerBands, WilliamsR, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class bollinger_williams_r_strategy(Strategy):
    """
    Strategy based on Bollinger Bands and Williams %R indicators.
    Enters long when price is at lower band and Williams %R is oversold (< -80)
    Enters short when price is at upper band and Williams %R is overbought (> -20)
    """

    def __init__(self):
        super(bollinger_williams_r_strategy, self).__init__()

        # Bollinger Bands period
        self._bollinger_period = self.Param("BollingerPeriod", 20) \
            .SetDisplay("Bollinger Period", "Period for Bollinger Bands", "Indicators")

        # Bollinger Bands deviation
        self._bollinger_deviation = self.Param("BollingerDeviation", 2.0) \
            .SetDisplay("Bollinger Deviation", "Deviation multiplier for Bollinger Bands", "Indicators")

        # Williams %R period
        self._williams_r_period = self.Param("WilliamsRPeriod", 14) \
            .SetDisplay("Williams %R Period", "Period for Williams %R indicator", "Indicators")

        # ATR period for stop-loss calculation
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "Period for ATR indicator for stop-loss", "Risk Management")

        # ATR multiplier for stop-loss
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0) \
            .SetDisplay("ATR Multiplier", "Multiplier for ATR-based stop-loss", "Risk Management")

        # Candle type for strategy calculation
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Timeframe for strategy", "General")

    @property
    def BollingerPeriod(self):
        return self._bollinger_period.Value

    @BollingerPeriod.setter
    def BollingerPeriod(self, value):
        self._bollinger_period.Value = value

    @property
    def BollingerDeviation(self):
        return self._bollinger_deviation.Value

    @BollingerDeviation.setter
    def BollingerDeviation(self, value):
        self._bollinger_deviation.Value = value

    @property
    def WilliamsRPeriod(self):
        return self._williams_r_period.Value

    @WilliamsRPeriod.setter
    def WilliamsRPeriod(self, value):
        self._williams_r_period.Value = value

    @property
    def AtrPeriod(self):
        return self._atr_period.Value

    @AtrPeriod.setter
    def AtrPeriod(self, value):
        self._atr_period.Value = value

    @property
    def AtrMultiplier(self):
        return self._atr_multiplier.Value

    @AtrMultiplier.setter
    def AtrMultiplier(self, value):
        self._atr_multiplier.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(bollinger_williams_r_strategy, self).OnStarted(time)

        # Create indicators
        bollinger = BollingerBands()
        bollinger.Length = self.BollingerPeriod
        bollinger.Width = self.BollingerDeviation

        williams_r = WilliamsR()
        williams_r.Length = self.WilliamsRPeriod

        atr = AverageTrueRange()
        atr.Length = self.AtrPeriod

        # Subscribe to candles and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)

        subscription.BindEx(bollinger, williams_r, atr, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)

            # Create a separate area for Williams %R
            williams_area = self.CreateChartArea()
            if williams_area is not None:
                self.DrawIndicator(williams_area, williams_r)

            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, bollinger_value, williams_r_value, atr_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Get additional values from Bollinger Bands

        if bollinger_value.MovingAverage is None:
            return
        middle_band = float(bollinger_value.MovingAverage)  # Middle band is returned by default

        if bollinger_value.UpBand is None:
            return
        upper_band = float(bollinger_value.UpBand)

        if bollinger_value.LowBand is None:
            return
        lower_band = float(bollinger_value.LowBand)

        # Current price (close of the candle)
        price = float(candle.ClosePrice)

        # Stop-loss size based on ATR
        stop_size = to_float(atr_value) * self.AtrMultiplier

        williams_value_dec = to_float(williams_r_value)

        # Trading logic
        if price <= lower_band and williams_value_dec < -80 and self.Position <= 0:
            # Buy signal: price at/below lower band and Williams %R oversold
            self.BuyMarket(self.Volume + Math.Abs(self.Position))

            # Set stop-loss
            stop_price = price - stop_size
            self.RegisterOrder(self.CreateOrder(Sides.Sell, stop_price, Math.Max(Math.Abs(self.Position + self.Volume), self.Volume)))
        elif price >= upper_band and williams_value_dec > -20 and self.Position >= 0:
            # Sell signal: price at/above upper band and Williams %R overbought
            self.SellMarket(self.Volume + Math.Abs(self.Position))

            # Set stop-loss
            stop_price = price + stop_size
            self.RegisterOrder(self.CreateOrder(Sides.Buy, stop_price, Math.Max(Math.Abs(self.Position + self.Volume), self.Volume)))
        # Exit conditions
        elif price >= middle_band and self.Position < 0:
            # Exit short position when price returns to middle band
            self.BuyMarket(Math.Abs(self.Position))
        elif price <= middle_band and self.Position > 0:
            # Exit long position when price returns to middle band
            self.SellMarket(self.Position)

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return bollinger_williams_r_strategy()

