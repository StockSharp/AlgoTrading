import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class keltner_seasonal_strategy(Strategy):
    """
    Strategy that trades based on Keltner Channel breakouts with seasonal bias filter.
    Enters position when price breaks Keltner Channel with seasonal bias confirmation.
    """

    def __init__(self):
        super(keltner_seasonal_strategy, self).__init__()

        # Strategy parameter: EMA period for Keltner Channel.
        self._emaPeriod = self.Param("EmaPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("EMA Period", "Period for EMA in Keltner Channel", "Indicator Settings")

        # Strategy parameter: ATR period for Keltner Channel.
        self._atrPeriod = self.Param("AtrPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "Period for ATR in Keltner Channel", "Indicator Settings")

        # Strategy parameter: ATR multiplier for Keltner Channel.
        self._atrMultiplier = self.Param("AtrMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Multiplier", "Multiplier for ATR to set channel width", "Indicator Settings")

        # Strategy parameter: Seasonal strength threshold.
        self._seasonalThreshold = self.Param("SeasonalThreshold", 0.5) \
            .SetDisplay("Seasonal Threshold", "Minimum seasonal strength to consider for trading", "Seasonal Settings")

        # Strategy parameter: Candle type.
        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        # Initialize seasonal data dictionary
        self._monthlyReturns = {}
        self._currentSeasonalStrength = 0.0

        # Initialize seasonal returns (this would typically be loaded from historical data)
        # These are example values - in a real implementation, these would be calculated from historical data
        self.InitializeSeasonalData()

    @property
    def ema_period(self):
        """Strategy parameter: EMA period for Keltner Channel."""
        return self._emaPeriod.Value

    @ema_period.setter
    def ema_period(self, value):
        self._emaPeriod.Value = value

    @property
    def atr_period(self):
        """Strategy parameter: ATR period for Keltner Channel."""
        return self._atrPeriod.Value

    @atr_period.setter
    def atr_period(self, value):
        self._atrPeriod.Value = value

    @property
    def atr_multiplier(self):
        """Strategy parameter: ATR multiplier for Keltner Channel."""
        return self._atrMultiplier.Value

    @atr_multiplier.setter
    def atr_multiplier(self, value):
        self._atrMultiplier.Value = value

    @property
    def seasonal_threshold(self):
        """Strategy parameter: Seasonal strength threshold."""
        return self._seasonalThreshold.Value

    @seasonal_threshold.setter
    def seasonal_threshold(self, value):
        self._seasonalThreshold.Value = value

    @property
    def candle_type(self):
        """Strategy parameter: Candle type."""
        return self._candleType.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candleType.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnReseted(self):
        super(keltner_seasonal_strategy, self).OnReseted()
        self._currentSeasonalStrength = 0.0

    def OnStarted(self, time):
        super(keltner_seasonal_strategy, self).OnStarted(time)

        # Initialize seasonal strength for current month
        self.UpdateSeasonalStrength(time)

        # Create indicators for Keltner Channel
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_period
        atr = AverageTrueRange()
        atr.Length = self.atr_period

        # Create subscription for candles
        subscription = self.SubscribeCandles(self.candle_type)

        # Bind indicators to subscription and start
        subscription.Bind(ema, atr, self.ProcessKeltner).Start()

        # Add chart visualization
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

        # Start position protection with ATR-based stop-loss
        self.StartProtection(
            takeProfit=Unit(0),
            stopLoss=Unit(self.atr_multiplier, UnitTypes.Absolute)
        )
    def ProcessKeltner(self, candle, ema_value, atr_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Check if we need to update seasonal strength (month changed)
        candle_month = candle.OpenTime.Month
        current_month = self.CurrentTime.Month
        if candle_month != current_month:
            self.UpdateSeasonalStrength(self.CurrentTime)

        # Calculate Keltner Channel bands
        upper_band = ema_value + atr_value * self.atr_multiplier
        lower_band = ema_value - atr_value * self.atr_multiplier

        # Check for breakout signals with seasonal filter
        if self._currentSeasonalStrength > self.seasonal_threshold:
            # Strong positive seasonal bias
            if candle.ClosePrice > upper_band and self.Position <= 0:
                # Breakout above upper band - Buy signal
                self.LogInfo("Buy signal: Breakout above Keltner upper band ({0}) with positive seasonal bias ({1})".format(
                    upper_band, self._currentSeasonalStrength))
                self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif self._currentSeasonalStrength < -self.seasonal_threshold:
            # Strong negative seasonal bias
            if candle.ClosePrice < lower_band and self.Position >= 0:
                # Breakout below lower band - Sell signal
                self.LogInfo("Sell signal: Breakout below Keltner lower band ({0}) with negative seasonal bias ({1})".format(
                    lower_band, self._currentSeasonalStrength))
                self.SellMarket(self.Volume + Math.Abs(self.Position))

        # Exit rules based on middle line reversion
        if (self.Position > 0 and candle.ClosePrice < ema_value) or \
           (self.Position < 0 and candle.ClosePrice > ema_value):
            self.LogInfo("Exit signal: Price reverted to EMA ({0})".format(ema_value))
            self.ClosePosition()

    def InitializeSeasonalData(self):
        # Example seasonal bias by month (positive values favor longs, negative values favor shorts)
        # This would typically be calculated from historical data
        self._monthlyReturns[1] = 0.8   # January - Strong bullish
        self._monthlyReturns[2] = 0.3   # February - Mildly bullish
        self._monthlyReturns[3] = 0.6   # March - Moderately bullish
        self._monthlyReturns[4] = 0.7   # April - Moderately bullish
        self._monthlyReturns[5] = 0.2   # May - Mildly bullish
        self._monthlyReturns[6] = -0.3  # June - Mildly bearish
        self._monthlyReturns[7] = -0.1  # July - Neutral to mildly bearish
        self._monthlyReturns[8] = -0.4  # August - Moderately bearish
        self._monthlyReturns[9] = -0.8  # September - Strong bearish
        self._monthlyReturns[10] = 0.1  # October - Neutral to mildly bullish
        self._monthlyReturns[11] = 0.9  # November - Strong bullish
        self._monthlyReturns[12] = 0.7  # December - Moderately bullish

    def UpdateSeasonalStrength(self, time):
        month = time.Month
        if month in self._monthlyReturns:
            self._currentSeasonalStrength = self._monthlyReturns[month]
            self.LogInfo("Updated seasonal strength for month {0}: {1}".format(month, self._currentSeasonalStrength))
        else:
            self._currentSeasonalStrength = 0
            self.LogInfo("No seasonal data found for month {0}, setting neutral bias".format(month))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return keltner_seasonal_strategy()
