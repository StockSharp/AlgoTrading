import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import AverageTrueRange, VolumeWeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class vwap_mean_reversion_strategy(Strategy):
    """
    VWAP Mean Reversion Strategy.
    Enter when price deviates from VWAP by a certain ATR multiple.
    Exit when price returns to VWAP.
    """

    def __init__(self):
        super(vwap_mean_reversion_strategy, self).__init__()

        # Initialize strategy parameters
        self._k_param = self.Param("K", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Multiplier", "ATR multiplier for entry distance from VWAP", "Strategy Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 4.0, 0.5)

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "Strategy Parameters")

        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "ATR indicator period", "Strategy Parameters") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 20, 2)

        # Internal indicators
        self._atr = None
        self._vwap = None
        self._current_atr = 0
        self._current_vwap = 0

    @property
    def K(self):
        """ATR multiplier for entry."""
        return self._k_param.Value

    @K.setter
    def K(self, value):
        self._k_param.Value = value

    @property
    def CandleType(self):
        """Type of candles to use."""
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def AtrPeriod(self):
        """ATR period."""
        return self._atr_period.Value

    @AtrPeriod.setter
    def AtrPeriod(self, value):
        self._atr_period.Value = value

    def GetWorkingSecurities(self):
        """!! REQUIRED!! Override to return securities used by the strategy."""
        return [(self.Security, self.CandleType)]

    def OnStarted(self, time):
        """Set up indicators, subscriptions and protection."""
        super(vwap_mean_reversion_strategy, self).OnStarted(time)

        self._current_atr = 0
        self._current_vwap = 0

        # Create indicators
        self._atr = AverageTrueRange()
        self._atr.Length = self.AtrPeriod
        self._vwap = VolumeWeightedMovingAverage()
        self._vwap.Length = self.AtrPeriod

        # Create subscription for candles
        subscription = self.SubscribeCandles(self.CandleType)

        # Bind indicators to candles
        subscription.Bind(self._atr, self.ProcessATR).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._atr)
            self.DrawOwnTrades(area)

        # Enable position protection
        self.StartProtection(
            takeProfit=Unit(5, UnitTypes.Percent),
            stopLoss=Unit(2, UnitTypes.Percent)
        )
    def ProcessATR(self, candle, atr_value):
        if candle.State != CandleStates.Finished:
            return

        self._current_vwap = process_candle(self._vwap, candle)
        self._current_vwap = float(self._current_vwap) if self._current_vwap is not None else 0

        self._current_atr = atr_value
        self.ProcessStrategy(candle.ClosePrice)

    def ProcessStrategy(self, current_price):
        # Check if strategy is ready for trading
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Skip if we don't have valid VWAP or ATR yet
        if self._current_vwap <= 0 or self._current_atr <= 0:
            return

        # Calculate distance to VWAP
        upper_band = self._current_vwap + self.K * self._current_atr
        lower_band = self._current_vwap - self.K * self._current_atr

        self.LogInfo(
            "Current Price: {0}, VWAP: {1}, Upper: {2}, Lower: {3}".format(
                current_price, self._current_vwap, upper_band, lower_band))

        # Entry logic
        if self.Position == 0:
            # Long Entry: Price is below lower band
            if current_price < lower_band:
                # Buy when price is too low compared to VWAP
                self.LogInfo(
                    "Buy Signal - Price ({0}) < Lower Band ({1})".format(current_price, lower_band))
                self.BuyMarket(self.Volume)
            # Short Entry: Price is above upper band
            elif current_price > upper_band:
                # Sell when price is too high compared to VWAP
                self.LogInfo(
                    "Sell Signal - Price ({0}) > Upper Band ({1})".format(current_price, upper_band))
                self.SellMarket(self.Volume)
        # Exit logic
        elif self.Position > 0 and current_price > self._current_vwap:
            # Exit Long: Price returned to VWAP
            self.LogInfo(
                "Exit Long - Price ({0}) > VWAP ({1})".format(current_price, self._current_vwap))
            self.SellMarket(Math.Abs(self.Position))
        elif self.Position < 0 and current_price < self._current_vwap:
            # Exit Short: Price returned to VWAP
            self.LogInfo(
                "Exit Short - Price ({0}) < VWAP ({1})".format(current_price, self._current_vwap))
            self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return vwap_mean_reversion_strategy()

