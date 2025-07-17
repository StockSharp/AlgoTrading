import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Sides
from StockSharp.Algo.Indicators import BollingerBands, StochasticOscillator, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class bollinger_stochastic_strategy(Strategy):
    """
    Strategy that combines Bollinger Bands and Stochastic oscillator to identify
    potential mean-reversion trading opportunities when price is at extremes.

    """

    def __init__(self):
        super(bollinger_stochastic_strategy, self).__init__()

        # Initialize strategy parameters
        self._candle_type = self.Param("CandleType", tf(15)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._bollinger_period = self.Param("BollingerPeriod", 20) \
            .SetDisplay("BB Period", "Period for Bollinger Bands calculation", "Bollinger Settings")

        self._bollinger_deviation = self.Param("BollingerDeviation", 2.0) \
            .SetDisplay("BB Deviation", "Standard deviation multiplier for Bollinger Bands", "Bollinger Settings")

        self._stoch_period = self.Param("StochPeriod", 14) \
            .SetDisplay("Stoch Period", "Period for Stochastic oscillator calculation", "Stochastic Settings")

        self._stoch_k = self.Param("StochK", 3) \
            .SetDisplay("Stoch %K", "K period for Stochastic oscillator", "Stochastic Settings")

        self._stoch_d = self.Param("StochD", 3) \
            .SetDisplay("Stoch %D", "D period for Stochastic oscillator", "Stochastic Settings")

        self._stoch_oversold = self.Param("StochOversold", 20) \
            .SetDisplay("Oversold Level", "Stochastic oversold level", "Stochastic Settings")

        self._stoch_overbought = self.Param("StochOverbought", 80) \
            .SetDisplay("Overbought Level", "Stochastic overbought level", "Stochastic Settings")

        self._atr_multiplier = self.Param("AtrMultiplier", 2.0) \
            .SetDisplay("ATR Multiplier", "ATR multiplier for stop-loss calculation", "Risk Management")

    @property
    def candle_type(self):
        """Data type for candles."""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    @property
    def bollinger_period(self):
        """Period for Bollinger Bands calculation."""
        return self._bollinger_period.Value

    @bollinger_period.setter
    def bollinger_period(self, value):
        self._bollinger_period.Value = value

    @property
    def bollinger_deviation(self):
        """Standard deviation multiplier for Bollinger Bands."""
        return self._bollinger_deviation.Value

    @bollinger_deviation.setter
    def bollinger_deviation(self, value):
        self._bollinger_deviation.Value = value

    @property
    def stoch_period(self):
        """Period for Stochastic oscillator calculation."""
        return self._stoch_period.Value

    @stoch_period.setter
    def stoch_period(self, value):
        self._stoch_period.Value = value

    @property
    def stoch_k(self):
        """K period for Stochastic oscillator."""
        return self._stoch_k.Value

    @stoch_k.setter
    def stoch_k(self, value):
        self._stoch_k.Value = value

    @property
    def stoch_d(self):
        """D period for Stochastic oscillator."""
        return self._stoch_d.Value

    @stoch_d.setter
    def stoch_d(self, value):
        self._stoch_d.Value = value

    @property
    def stoch_oversold(self):
        """Stochastic oversold level."""
        return self._stoch_oversold.Value

    @stoch_oversold.setter
    def stoch_oversold(self, value):
        self._stoch_oversold.Value = value

    @property
    def stoch_overbought(self):
        """Stochastic overbought level."""
        return self._stoch_overbought.Value

    @stoch_overbought.setter
    def stoch_overbought(self, value):
        self._stoch_overbought.Value = value

    @property
    def atr_multiplier(self):
        """ATR multiplier for stop-loss calculation."""
        return self._atr_multiplier.Value

    @atr_multiplier.setter
    def atr_multiplier(self, value):
        self._atr_multiplier.Value = value

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.
        """
        super(bollinger_stochastic_strategy, self).OnStarted(time)

        # Initialize indicators
        self._bollinger = BollingerBands()
        self._bollinger.Length = self.bollinger_period
        self._bollinger.Width = self.bollinger_deviation

        self._stochastic = StochasticOscillator()
        self._stochastic.K.Length = self.stoch_period
        self._stochastic.D.Length = self.stoch_d

        self._atr = AverageTrueRange()
        self._atr.Length = 14

        # Create candle subscription
        subscription = self.SubscribeCandles(self.candle_type)

        # Bind the indicators and candle processor
        subscription.BindEx(self._bollinger, self._stochastic, self._atr, self.ProcessCandle).Start()

        # Set up chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._bollinger)

            # Draw Stochastic in a separate area
            stoch_area = self.CreateChartArea()
            self.DrawIndicator(stoch_area, self._stochastic)

            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, bollinger_value, stochastic_value, atr_value):
        """
        Process incoming candle with indicator values.

        :param candle: Candle to process.
        :param bollinger_value: Bollinger Bands value.
        :param stochastic_value: Stochastic oscillator value.
        :param atr_value: ATR value.
        """
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Extract values from indicators
        bb = bollinger_value

        if bb.MovingAverage is None:
            return
        middle_band = float(bb.MovingAverage)

        if bb.UpBand is None:
            return
        upper_band = float(bb.UpBand)

        if bb.LowBand is None:
            return
        lower_band = float(bb.LowBand)

        k = stochastic_value.K
        d = stochastic_value.D

        atr_val = to_float(atr_value)

        # Calculate stop loss distance based on ATR
        stop_loss_distance = atr_val * self.atr_multiplier

        # Trading logic for long positions
        if candle.ClosePrice < lower_band and k < self.stoch_oversold:
            # Price below lower Bollinger Band and Stochastic in oversold region - Long signal
            if self.Position <= 0:
                self.BuyMarket(self.Volume + Math.Abs(self.Position))
                self.LogInfo("Buy signal: Price ({0}) below lower BB ({1:F4}) with oversold Stochastic ({2:F2})".format(
                    candle.ClosePrice, lower_band, k))

                # Set stop loss
                stop_price = float(candle.ClosePrice - stop_loss_distance)
                self.RegisterOrder(self.CreateOrder(Sides.Sell, stop_price, max(Math.Abs(self.Position + self.Volume), self.Volume)))

        # Trading logic for short positions
        elif candle.ClosePrice > upper_band and k > self.stoch_overbought:
            # Price above upper Bollinger Band and Stochastic in overbought region - Short signal
            if self.Position >= 0:
                self.SellMarket(self.Volume + Math.Abs(self.Position))
                self.LogInfo("Sell signal: Price ({0}) above upper BB ({1:F4}) with overbought Stochastic ({2:F2})".format(
                    candle.ClosePrice, upper_band, k))

                # Set stop loss
                stop_price = float(candle.ClosePrice + stop_loss_distance)
                self.RegisterOrder(self.CreateOrder(Sides.Buy, stop_price, max(Math.Abs(self.Position + self.Volume), self.Volume)))

        # Exit conditions
        if self.Position > 0:
            # Exit long when price crosses above middle band
            if candle.ClosePrice > middle_band:
                self.SellMarket(Math.Abs(self.Position))
                self.LogInfo("Exit long: Price ({0}) crossed above middle BB ({1:F4})".format(
                    candle.ClosePrice, middle_band))
        elif self.Position < 0:
            # Exit short when price crosses below middle band
            if candle.ClosePrice < middle_band:
                self.BuyMarket(Math.Abs(self.Position))
                self.LogInfo("Exit short: Price ({0}) crossed below middle BB ({1:F4})".format(
                    candle.ClosePrice, middle_band))

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return bollinger_stochastic_strategy()