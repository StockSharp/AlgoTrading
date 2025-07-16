import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from System import Math
from StockSharp.Messages import DataType
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import SuperTrend, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class supertrend_rsi_strategy(Strategy):
    """
    Implementation of strategy - Supertrend + RSI.
    Buy when price is above Supertrend and RSI is below 30 (oversold).
    Sell when price is below Supertrend and RSI is above 70 (overbought).
    """

    def __init__(self):
        super(supertrend_rsi_strategy, self).__init__()

        # Strategy parameters
        self._supertrend_period = self.Param("SupertrendPeriod", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Supertrend Period", "Period for ATR in Supertrend", "Supertrend Parameters")

        self._supertrend_multiplier = self.Param("SupertrendMultiplier", 3.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Supertrend Multiplier", "Multiplier for ATR in Supertrend", "Supertrend Parameters")

        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Period", "Period for relative strength index", "RSI Parameters")

        self._rsi_oversold = self.Param("RsiOversold", 30.0) \
            .SetRange(1, 100) \
            .SetDisplay("RSI Oversold", "RSI level to consider market oversold", "RSI Parameters")

        self._rsi_overbought = self.Param("RsiOverbought", 70.0) \
            .SetRange(1, 100) \
            .SetDisplay("RSI Overbought", "RSI level to consider market overbought", "RSI Parameters")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle type for strategy", "General")

        # Indicators
        self._supertrend = None
        self._rsi = None

    @property
    def supertrend_period(self):
        """Supertrend period."""
        return self._supertrend_period.Value

    @supertrend_period.setter
    def supertrend_period(self, value):
        self._supertrend_period.Value = value

    @property
    def supertrend_multiplier(self):
        """Supertrend multiplier."""
        return self._supertrend_multiplier.Value

    @supertrend_multiplier.setter
    def supertrend_multiplier(self, value):
        self._supertrend_multiplier.Value = value

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
    def candle_type(self):
        """Candle type used for strategy."""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnStarted(self, time):
        super(supertrend_rsi_strategy, self).OnStarted(time)

        # Create indicators
        self._supertrend = SuperTrend()
        self._supertrend.Length = self.supertrend_period
        self._supertrend.Multiplier = self.supertrend_multiplier

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.rsi_period

        # Setup candle subscription
        subscription = self.SubscribeCandles(self.candle_type)

        # Bind both indicators to the candle feed
        subscription.Bind(self._supertrend, self._rsi, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._supertrend)

            # Create separate area for RSI
            rsi_area = self.CreateChartArea()
            if rsi_area is not None:
                self.DrawIndicator(rsi_area, self._rsi)

            self.DrawOwnTrades(area)

        # Using Supertrend for dynamic stop-loss
        # (the strategy design already includes the dynamic stop-loss mechanism
        # through the Supertrend indicator crossovers)

    def ProcessCandle(self, candle, supertrend_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        self.LogInfo(
            "Candle: {0}, Close: {1}, Supertrend: {2}, RSI: {3}".format(
                candle.OpenTime, candle.ClosePrice, supertrend_value, rsi_value))

        # Trading rules
        trend = 1 if candle.ClosePrice > supertrend_value else -1  # 1 = uptrend, -1 = downtrend

        if trend > 0 and rsi_value < self.rsi_oversold and self.Position <= 0:
            # Buy signal - price above Supertrend (uptrend) and RSI is oversold
            volume = self.Volume + Math.Abs(self.Position)
            self.BuyMarket(volume)

            self.LogInfo(
                "Buy signal: Uptrend (Price > Supertrend) and RSI oversold ({0} < {1}). Volume: {2}".format(
                    rsi_value, self.rsi_oversold, volume))
        elif trend < 0 and rsi_value > self.rsi_overbought and self.Position >= 0:
            # Sell signal - price below Supertrend (downtrend) and RSI is overbought
            volume = self.Volume + Math.Abs(self.Position)
            self.SellMarket(volume)

            self.LogInfo(
                "Sell signal: Downtrend (Price < Supertrend) and RSI overbought ({0} > {1}). Volume: {2}".format(
                    rsi_value, self.rsi_overbought, volume))
        # Exit conditions are handled by Supertrend crossovers
        elif trend < 0 and self.Position > 0:
            # Exit long position when trend turns down
            self.SellMarket(Math.Abs(self.Position))
            self.LogInfo(
                "Exit long: Trend turned down (Price < Supertrend). Position: {0}".format(self.Position))
        elif trend > 0 and self.Position < 0:
            # Exit short position when trend turns up
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo(
                "Exit short: Trend turned up (Price > Supertrend). Position: {0}".format(self.Position))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return supertrend_rsi_strategy()
