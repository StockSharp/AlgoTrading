import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit
from StockSharp.Algo.Indicators import SimpleMovingAverage, StandardDeviation, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *


class volatility_cluster_breakout_strategy(Strategy):
    """
    Strategy based on breakouts during high volatility clusters.
    """

    def __init__(self):
        """Initialize a new instance of :class:`volatility_cluster_breakout_strategy`."""
        super(volatility_cluster_breakout_strategy, self).__init__()

        # Period for price average and standard deviation calculation.
        self._price_avg_period = self.Param("PriceAvgPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Price Average Period", "Period for calculating price average and standard deviation", "Strategy Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5)

        # Period for ATR calculation.
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "Period for calculating Average True Range", "Strategy Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(7, 21, 7)

        # Standard deviation multiplier for breakout threshold.
        self._std_dev_multiplier = self.Param("StdDevMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("StdDev Multiplier", "Multiplier for standard deviation to determine breakout levels", "Strategy Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        # Stop-loss multiplier relative to ATR.
        self._stop_multiplier = self.Param("StopMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop ATR Multiplier", "ATR multiplier for stop-loss", "Strategy Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 3.0, 0.5)

        # Candle type parameter.
        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles for strategy", "General")

        self._atr_avg = None

    @property
    def price_avg_period(self):
        """Period for price average and standard deviation calculation."""
        return self._price_avg_period.Value

    @price_avg_period.setter
    def price_avg_period(self, value):
        self._price_avg_period.Value = value

    @property
    def atr_period(self):
        """Period for ATR calculation."""
        return self._atr_period.Value

    @atr_period.setter
    def atr_period(self, value):
        self._atr_period.Value = value

    @property
    def std_dev_multiplier(self):
        """Standard deviation multiplier for breakout threshold."""
        return self._std_dev_multiplier.Value

    @std_dev_multiplier.setter
    def std_dev_multiplier(self, value):
        self._std_dev_multiplier.Value = value

    @property
    def stop_multiplier(self):
        """Stop-loss multiplier relative to ATR."""
        return self._stop_multiplier.Value

    @stop_multiplier.setter
    def stop_multiplier(self, value):
        self._stop_multiplier.Value = value

    @property
    def candle_type(self):
        """Candle type parameter."""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        """Return security and timeframe used by the strategy."""
        return [(self.Security, self.candle_type)]

    def OnStarted(self, time):
        """Called when the strategy starts."""
        super(volatility_cluster_breakout_strategy, self).OnStarted(time)

        # Create indicators
        sma = SimpleMovingAverage()
        sma.Length = self.price_avg_period
        std_dev = StandardDeviation()
        std_dev.Length = self.price_avg_period
        atr = AverageTrueRange()
        atr.Length = self.atr_period
        self._atr_avg = SimpleMovingAverage()
        self._atr_avg.Length = self.atr_period

        # Create subscription
        subscription = self.SubscribeCandles(self.candle_type)

        # Bind indicators to subscription
        subscription.Bind(sma, std_dev, atr, self.ProcessCandle).Start()

        # Enable position protection with dynamic stops
        self.StartProtection(
            takeProfit=Unit(0),  # We'll handle exits in the strategy logic
            stopLoss=Unit(0),    # We'll handle stops in the strategy logic
            useMarketOrders=True
        )

        # Setup chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawIndicator(area, atr)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, sma_value, std_dev_value, atr_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        atr_avg_val = process_float(self._atr_avg, atr_value, candle.ServerTime, candle.State == CandleStates.Finished)

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Calculate breakout levels
        upper_level = sma_value + self.std_dev_multiplier * std_dev_value
        lower_level = sma_value - self.std_dev_multiplier * std_dev_value

        # Check if we're in high volatility cluster
        is_high_volatility = atr_value > sma_value * 0.01  # ATR > 1% of price as simplification

        # Exit conditions based on volatility
        exit_condition = not is_high_volatility

        # Entry conditions
        long_entry_condition = candle.ClosePrice > upper_level and is_high_volatility and self.Position <= 0
        short_entry_condition = candle.ClosePrice < lower_level and is_high_volatility and self.Position >= 0

        # Execute trading logic
        if exit_condition:
            # Exit positions when volatility drops
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
                self.LogInfo("Long exit on volatility drop: Price={0}, ATR={1}".format(candle.ClosePrice, atr_value))
            elif self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
                self.LogInfo("Short exit on volatility drop: Price={0}, ATR={1}".format(candle.ClosePrice, atr_value))
        elif long_entry_condition:
            # Calculate position size
            position_size = self.Volume + Math.Abs(self.Position)

            # Calculate stop loss level
            stop_price = candle.ClosePrice - atr_value * self.stop_multiplier

            # Enter long position
            self.BuyMarket(position_size)

            self.LogInfo("Long entry: Price={0}, Upper={1}, ATR={2}, Stop={3}".format(candle.ClosePrice, upper_level, atr_value, stop_price))
        elif short_entry_condition:
            # Calculate position size
            position_size = self.Volume + Math.Abs(self.Position)

            # Calculate stop loss level
            stop_price = candle.ClosePrice + atr_value * self.stop_multiplier

            # Enter short position
            self.SellMarket(position_size)

            self.LogInfo("Short entry: Price={0}, Lower={1}, ATR={2}, Stop={3}".format(candle.ClosePrice, lower_level, atr_value, stop_price))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return volatility_cluster_breakout_strategy()
