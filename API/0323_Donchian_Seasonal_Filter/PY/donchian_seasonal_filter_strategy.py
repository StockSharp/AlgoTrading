import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import DonchianChannels
from StockSharp.Algo.Strategies import Strategy


class donchian_seasonal_filter_strategy(Strategy):
    """
    Strategy based on Donchian Channels with seasonal filter.
    """

    def __init__(self):
        super(donchian_seasonal_filter_strategy, self).__init__()

        self._donchian_period = self.Param("DonchianPeriod", 40) \
            .SetDisplay("Donchian Period", "Donchian Channel period", "Donchian") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5)

        self._seasonal_threshold = self.Param("SeasonalThreshold", 0.5) \
            .SetDisplay("Seasonal Threshold", "Seasonal strength threshold for entry", "Seasonal") \
            .SetCanOptimize(True) \
            .SetOptimize(0.2, 1.0, 0.1)

        self._seasonal_data_count = self.Param("SeasonalDataCount", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Seasonal Years", "Years of seasonal data", "Seasonal") \
            .SetCanOptimize(True) \
            .SetOptimize(1, 10, 1)

        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 12) \
            .SetDisplay("Signal Cooldown Bars", "Closed candles to wait before a new breakout entry", "General")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._donchian = None
        self._monthly_returns = {}
        self._seasonal_strength = 0.0
        self._previous_upper_band = None
        self._previous_lower_band = None
        self._previous_middle_band = None
        self._previous_close_price = None
        self._cooldown_remaining = 0

        # Initialize monthly returns
        self._monthly_returns[1] = 0.8
        self._monthly_returns[2] = 0.3
        self._monthly_returns[3] = 0.6
        self._monthly_returns[4] = 0.9
        self._monthly_returns[5] = 0.2
        self._monthly_returns[6] = -0.4
        self._monthly_returns[7] = -0.2
        self._monthly_returns[8] = -0.7
        self._monthly_returns[9] = -0.9
        self._monthly_returns[10] = -0.1
        self._monthly_returns[11] = 0.5
        self._monthly_returns[12] = 0.7

    @property
    def DonchianPeriod(self):
        return self._donchian_period.Value

    @DonchianPeriod.setter
    def DonchianPeriod(self, value):
        self._donchian_period.Value = value

    @property
    def SeasonalThreshold(self):
        return self._seasonal_threshold.Value

    @SeasonalThreshold.setter
    def SeasonalThreshold(self, value):
        self._seasonal_threshold.Value = value

    @property
    def SeasonalDataCount(self):
        return self._seasonal_data_count.Value

    @SeasonalDataCount.setter
    def SeasonalDataCount(self, value):
        self._seasonal_data_count.Value = value

    @property
    def SignalCooldownBars(self):
        return self._signal_cooldown_bars.Value

    @SignalCooldownBars.setter
    def SignalCooldownBars(self, value):
        self._signal_cooldown_bars.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(donchian_seasonal_filter_strategy, self).OnReseted()
        self._donchian = None
        self._seasonal_strength = 0.0
        self._previous_upper_band = None
        self._previous_lower_band = None
        self._previous_middle_band = None
        self._previous_close_price = None
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(donchian_seasonal_filter_strategy, self).OnStarted(time)

        self._donchian = DonchianChannels()
        self._donchian.Length = self.DonchianPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(self._donchian, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._donchian)
            self.DrawOwnTrades(area)

        self.StartProtection(
            Unit(2, UnitTypes.Percent),
            Unit(2, UnitTypes.Percent)
        )

    def ProcessCandle(self, candle, donchian_value):
        if candle.State != CandleStates.Finished:
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        if donchian_value.UpperBand is None or donchian_value.LowerBand is None or donchian_value.Middle is None:
            return

        upper_band = float(donchian_value.UpperBand)
        lower_band = float(donchian_value.LowerBand)
        middle_band = float(donchian_value.Middle)

        self.UpdateSeasonalStrength(candle.OpenTime)

        if self._previous_upper_band is None or self._previous_lower_band is None or self._previous_middle_band is None or self._previous_close_price is None:
            self._previous_upper_band = upper_band
            self._previous_lower_band = lower_band
            self._previous_middle_band = middle_band
            self._previous_close_price = float(candle.ClosePrice)
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._previous_upper_band = upper_band
            self._previous_lower_band = lower_band
            self._previous_middle_band = middle_band
            self._previous_close_price = float(candle.ClosePrice)
            return

        close_price = float(candle.ClosePrice)

        if self.Position > 0 and close_price < self._previous_middle_band:
            self.SellMarket(self.Position)
            self._cooldown_remaining = self.SignalCooldownBars
        elif self.Position < 0 and close_price > self._previous_middle_band:
            self.BuyMarket(abs(self.Position))
            self._cooldown_remaining = self.SignalCooldownBars
        elif self._cooldown_remaining == 0 and \
             self._previous_close_price <= self._previous_upper_band and \
             close_price > self._previous_upper_band and \
             self._seasonal_strength > self.SeasonalThreshold and \
             self.Position <= 0:
            vol = self.Volume
            if self.Position < 0:
                vol = self.Volume + abs(self.Position)
            self.BuyMarket(vol)
            self._cooldown_remaining = self.SignalCooldownBars
        elif self._cooldown_remaining == 0 and \
             self._previous_close_price >= self._previous_lower_band and \
             close_price < self._previous_lower_band and \
             self._seasonal_strength < -self.SeasonalThreshold and \
             self.Position >= 0:
            vol = self.Volume
            if self.Position > 0:
                vol = self.Volume + self.Position
            self.SellMarket(vol)
            self._cooldown_remaining = self.SignalCooldownBars

        self._previous_upper_band = upper_band
        self._previous_lower_band = lower_band
        self._previous_middle_band = middle_band
        self._previous_close_price = close_price

    def UpdateSeasonalStrength(self, time):
        current_month = time.Month
        self._seasonal_strength = self._monthly_returns.get(current_month, 0.0)

    def CreateClone(self):
        return donchian_seasonal_filter_strategy()
