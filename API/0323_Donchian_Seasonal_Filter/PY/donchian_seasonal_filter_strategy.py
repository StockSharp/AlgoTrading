import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import DonchianChannels
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class donchian_seasonal_filter_strategy(Strategy):
    """
    Strategy based on Donchian Channels with seasonal filter.
    """

    class Month:
        """Enumeration for months of the year."""
        January = 1
        February = 2
        March = 3
        April = 4
        May = 5
        June = 6
        July = 7
        August = 8
        September = 9
        October = 10
        November = 11
        December = 12

    def __init__(self):
        super(donchian_seasonal_filter_strategy, self).__init__()

        self._donchian_period = self.Param("DonchianPeriod", 20) \
            .SetDisplay("Donchian Period", "Donchian Channel period", "Donchian") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5)

        self._seasonal_threshold = self.Param("SeasonalThreshold", 0.5) \
            .SetDisplay("Seasonal Threshold", "Seasonal strength threshold for entry", "Seasonal") \
            .SetCanOptimize(True) \
            .SetOptimize(0.2, 1.0, 0.1)

        self._candle_type = self.Param("CandleType", tf(15)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._donchian = None
        self._is_long_position = False
        self._is_short_position = False

        # Seasonal data storage
        self._monthly_returns = {}

        # Simulated 5 years of data
        self._seasonal_data_count = 5

        # Current values
        self._upper_band = 0.0
        self._lower_band = 0.0
        self._middle_band = 0.0
        self._seasonal_strength = 0.0

        # Initialize monthly returns with neutral values
        for month in range(1, 13):
            self._monthly_returns[month] = 0.0

        # Simulated historical seasonal data (in a real strategy, this would come from analysis of historical data)
        # These are example values that suggest certain months tend to be bullish or bearish
        self._monthly_returns[self.Month.January] = 0.8
        self._monthly_returns[self.Month.February] = 0.3
        self._monthly_returns[self.Month.March] = 0.6
        self._monthly_returns[self.Month.April] = 0.9
        self._monthly_returns[self.Month.May] = 0.2
        self._monthly_returns[self.Month.June] = -0.4
        self._monthly_returns[self.Month.July] = -0.2
        self._monthly_returns[self.Month.August] = -0.7
        self._monthly_returns[self.Month.September] = -0.9
        self._monthly_returns[self.Month.October] = -0.1
        self._monthly_returns[self.Month.November] = 0.5
        self._monthly_returns[self.Month.December] = 0.7

    @property
    def DonchianPeriod(self):
        """Donchian Channel period."""
        return self._donchian_period.Value

    @DonchianPeriod.setter
    def DonchianPeriod(self, value):
        self._donchian_period.Value = value

    @property
    def SeasonalThreshold(self):
        """Seasonal strength threshold for entry."""
        return self._seasonal_threshold.Value

    @SeasonalThreshold.setter
    def SeasonalThreshold(self, value):
        self._seasonal_threshold.Value = value

    @property
    def CandleType(self):
        """Candle type to use for the strategy."""
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(donchian_seasonal_filter_strategy, self).OnReseted()
        self._is_long_position = False
        self._is_short_position = False
        self._upper_band = 0.0
        self._middle_band = 0.0
        self._lower_band = 0.0
        self._seasonal_strength = 0.0

    def OnStarted(self, time):
        super(donchian_seasonal_filter_strategy, self).OnStarted(time)

        # Create Donchian Channel indicator
        self._donchian = DonchianChannels()
        self._donchian.Length = self.DonchianPeriod

        # Create subscription and bind indicator
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(self._donchian, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._donchian)
            self.DrawOwnTrades(area)

        # Setup position protection
        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(2, UnitTypes.Percent)
        )
    def ProcessCandle(self, candle, donchian_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Extract Donchian Channel values
        if (
            donchian_value.UpperBand is None
            or donchian_value.LowerBand is None
            or donchian_value.Middle is None
        ):
            return
        upper_band = float(donchian_value.UpperBand)
        lower_band = float(donchian_value.LowerBand)
        middle_band = float(donchian_value.Middle)

        # Save current Donchian Channel values
        self._upper_band = upper_band
        self._middle_band = middle_band
        self._lower_band = lower_band

        # Calculate seasonal strength for current month
        self.UpdateSeasonalStrength(candle.OpenTime)

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Trading logic
        # Buy when price breaks above upper band and seasonal strength is positive (above threshold)
        if candle.ClosePrice > self._upper_band and self._seasonal_strength > self.SeasonalThreshold and self.Position <= 0:
            self.BuyMarket(self.Volume)
            self.LogInfo(f"Buy Signal: Price {candle.ClosePrice:.2f} > Upper Band {self._upper_band:.2f}, Seasonal Strength {self._seasonal_strength:.2f}")
            self._is_long_position = True
            self._is_short_position = False
        # Sell when price breaks below lower band and seasonal strength is negative (below negative threshold)
        elif candle.ClosePrice < self._lower_band and self._seasonal_strength < -self.SeasonalThreshold and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))
            self.LogInfo(f"Sell Signal: Price {candle.ClosePrice:.2f} < Lower Band {self._lower_band:.2f}, Seasonal Strength {self._seasonal_strength:.2f}")
            self._is_long_position = False
            self._is_short_position = True
        # Exit long position when price falls below middle band
        elif self._is_long_position and candle.ClosePrice < self._middle_band:
            self.SellMarket(self.Position)
            self.LogInfo(f"Exit Long: Price {candle.ClosePrice:.2f} fell below Middle Band {self._middle_band:.2f}")
            self._is_long_position = False
        # Exit short position when price rises above middle band
        elif self._is_short_position and candle.ClosePrice > self._middle_band:
            self.BuyMarket(Math.Abs(self.Position))
            self.LogInfo(f"Exit Short: Price {candle.ClosePrice:.2f} rose above Middle Band {self._middle_band:.2f}")
            self._is_short_position = False

    def UpdateSeasonalStrength(self, time):
        # Get current month
        current_month = time.Month

        # Get historical return for this month
        self._seasonal_strength = self._monthly_returns.get(current_month, 0.0)

        # Log seasonal information at the beginning of each month
        if time.Day == 1:
            self.LogInfo(f"Monthly Seasonal Data: {current_month} has historical strength of {self._seasonal_strength:.2f} over {self._seasonal_data_count} years")

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return donchian_seasonal_filter_strategy()
