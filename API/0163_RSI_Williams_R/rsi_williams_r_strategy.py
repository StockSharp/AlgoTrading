import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, WilliamsR
from StockSharp.Algo.Strategies import Strategy


class rsi_williams_r_strategy(Strategy):
    """
    Implementation of strategy #163 - RSI + Williams %R.
    Buy when RSI is below 30 and Williams %R is below -80 (double oversold condition).
    Sell when RSI is above 70 and Williams %R is above -20 (double overbought condition).

    See more examples: https://github.com/StockSharp/AlgoTrading
    """

    def __init__(self):
        super(rsi_williams_r_strategy, self).__init__()

        # Initialize strategy parameters
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Period", "Period for Relative Strength Index", "RSI Parameters")

        self._rsi_oversold = self.Param("RsiOversold", 30.0) \
            .SetRange(1, 100) \
            .SetDisplay("RSI Oversold", "RSI level to consider market oversold", "RSI Parameters")

        self._rsi_overbought = self.Param("RsiOverbought", 70.0) \
            .SetRange(1, 100) \
            .SetDisplay("RSI Overbought", "RSI level to consider market overbought", "RSI Parameters")

        self._williams_r_period = self.Param("WilliamsRPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("Williams %R Period", "Period for Williams %R", "Williams %R Parameters")

        self._williams_r_oversold = self.Param("WilliamsROversold", -80.0) \
            .SetRange(-100, 0) \
            .SetDisplay("Williams %R Oversold", "Williams %R level to consider market oversold", "Williams %R Parameters")

        self._williams_r_overbought = self.Param("WilliamsROverbought", -20.0) \
            .SetRange(-100, 0) \
            .SetDisplay("Williams %R Overbought", "Williams %R level to consider market overbought", "Williams %R Parameters")

        self._stop_loss = self.Param("StopLoss", Unit(2, UnitTypes.Percent)) \
            .SetDisplay("Stop Loss", "Stop loss percent or value", "Risk Management")

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5).TimeFrame()) \
            .SetDisplay("Candle Type", "Candle type for strategy", "General")

    @property
    def rsi_period(self):
        """RSI period."""
        return self._rsi_period.Value

    @rsi_period.setter
    def rsi_period(self, value):
        self._rsi_period.Value = value

    @property
    def rsi_oversold(self):
        """RSI oversold level."""
        return self._rsi_oversold.Value

    @rsi_oversold.setter
    def rsi_oversold(self, value):
        self._rsi_oversold.Value = value

    @property
    def rsi_overbought(self):
        """RSI overbought level."""
        return self._rsi_overbought.Value

    @rsi_overbought.setter
    def rsi_overbought(self, value):
        self._rsi_overbought.Value = value

    @property
    def williams_r_period(self):
        """Williams %R period."""
        return self._williams_r_period.Value

    @williams_r_period.setter
    def williams_r_period(self, value):
        self._williams_r_period.Value = value

    @property
    def williams_r_oversold(self):
        """Williams %R oversold level (usually below -80)."""
        return self._williams_r_oversold.Value

    @williams_r_oversold.setter
    def williams_r_oversold(self, value):
        self._williams_r_oversold.Value = value

    @property
    def williams_r_overbought(self):
        """Williams %R overbought level (usually above -20)."""
        return self._williams_r_overbought.Value

    @williams_r_overbought.setter
    def williams_r_overbought(self, value):
        self._williams_r_overbought.Value = value

    @property
    def stop_loss(self):
        """Stop-loss value."""
        return self._stop_loss.Value

    @stop_loss.setter
    def stop_loss(self, value):
        self._stop_loss.Value = value

    @property
    def candle_type(self):
        """Candle type used for strategy."""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        """
        Called when the strategy starts. Creates indicators, subscriptions, and charting.

        :param time: The time when the strategy started.
        """
        super(rsi_williams_r_strategy, self).OnStarted(time)

        # Create indicators
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period
        williams_r = WilliamsR()
        williams_r.Length = self.williams_r_period

        # Setup candle subscription
        subscription = self.SubscribeCandles(self.candle_type)

        # Bind indicators to candles
        subscription.Bind(rsi, williams_r, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)

            # Create separate area for oscillators
            oscillator_area = self.CreateChartArea()
            if oscillator_area is not None:
                self.DrawIndicator(oscillator_area, rsi)
                self.DrawIndicator(oscillator_area, williams_r)

            self.DrawOwnTrades(area)

        # Start protective orders
        self.StartProtection(Unit(0, UnitTypes.Absolute), self.stop_loss)

    def ProcessCandle(self, candle, rsi_value, williams_r_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        self.LogInfo("Candle: {0}, Close: {1}, RSI: {2} , Williams %R: {3}".format(
            candle.OpenTime, candle.ClosePrice, rsi_value, williams_r_value))

        # Trading rules
        if rsi_value < self.rsi_oversold and williams_r_value < self.williams_r_oversold and self.Position <= 0:
            # Buy signal - double oversold condition
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)

            self.LogInfo("Buy signal: Double oversold condition - RSI: {0} < {1} and Williams %R: {2} < {3}. Volume: {4}".format(
                rsi_value, self.rsi_oversold, williams_r_value, self.williams_r_oversold, volume))
        elif rsi_value > self.rsi_overbought and williams_r_value > self.williams_r_overbought and self.Position >= 0:
            # Sell signal - double overbought condition
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)

            self.LogInfo("Sell signal: Double overbought condition - RSI: {0} > {1} and Williams %R: {2} > {3}. Volume: {4}".format(
                rsi_value, self.rsi_overbought, williams_r_value, self.williams_r_overbought, volume))
        # Exit conditions
        elif rsi_value > 50 and self.Position > 0:
            # Exit long position when RSI returns to neutral zone
            self.SellMarket(self.Position)
            self.LogInfo("Exit long: RSI returned to neutral zone ({0} > 50). Position: {1}".format(
                rsi_value, self.Position))
        elif rsi_value < 50 and self.Position < 0:
            # Exit short position when RSI returns to neutral zone
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo("Exit short: RSI returned to neutral zone ({0} < 50). Position: {1}".format(
                rsi_value, self.Position))

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return rsi_williams_r_strategy()
