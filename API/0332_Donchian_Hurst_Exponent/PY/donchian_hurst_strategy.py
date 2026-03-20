import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import DonchianChannels, FractalDimension
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class donchian_hurst_strategy(Strategy):
    """
    Strategy that trades based on Donchian Channel breakouts with Hurst Exponent filter.
    """

    def __init__(self):
        super(donchian_hurst_strategy, self).__init__()

        self._donchian_period = self.Param("DonchianPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Donchian Period", "Period for Donchian Channel indicator", "Indicator Settings")

        self._hurst_period = self.Param("HurstPeriod", 100) \
            .SetGreaterThanZero() \
            .SetDisplay("Hurst Period", "Period for Hurst Exponent calculation", "Indicator Settings")

        self._hurst_threshold = self.Param("HurstThreshold", 0.45) \
            .SetDisplay("Hurst Threshold", "Minimum Hurst Exponent value for trend persistence", "Indicator Settings")

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop Loss percentage from entry price", "Risk Management")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(2))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._hurst_value = 0.0
        self._previous_upper = None
        self._previous_lower = None
        self._previous_middle = None

    @property
    def DonchianPeriod(self):
        return self._donchian_period.Value

    @DonchianPeriod.setter
    def DonchianPeriod(self, value):
        self._donchian_period.Value = value

    @property
    def HurstPeriod(self):
        return self._hurst_period.Value

    @HurstPeriod.setter
    def HurstPeriod(self, value):
        self._hurst_period.Value = value

    @property
    def HurstThreshold(self):
        return self._hurst_threshold.Value

    @HurstThreshold.setter
    def HurstThreshold(self, value):
        self._hurst_threshold.Value = value

    @property
    def StopLossPercent(self):
        return self._stop_loss_percent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(donchian_hurst_strategy, self).OnReseted()
        self._hurst_value = 0.0
        self._previous_upper = None
        self._previous_lower = None
        self._previous_middle = None

    def OnStarted(self, time):
        super(donchian_hurst_strategy, self).OnStarted(time)

        donchian = DonchianChannels()
        donchian.Length = self.DonchianPeriod

        fractal_dimension = FractalDimension()
        fractal_dimension.Length = self.HurstPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(donchian, fractal_dimension, self.ProcessIndicators).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, donchian)
            self.DrawOwnTrades(area)

        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent)
        )

    def ProcessIndicators(self, candle, donchian_value, fractal_dimension_value):
        if candle.State != CandleStates.Finished:
            return

        fractal_dim = float(fractal_dimension_value)
        self._hurst_value = 2.0 - fractal_dim

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if donchian_value.UpperBand is None or donchian_value.LowerBand is None or donchian_value.Middle is None:
            return

        upper = float(donchian_value.UpperBand)
        lower = float(donchian_value.LowerBand)
        middle = float(donchian_value.Middle)

        if self._previous_upper is None or self._previous_lower is None or self._previous_middle is None:
            self._previous_upper = upper
            self._previous_lower = lower
            self._previous_middle = middle
            return

        close_price = float(candle.ClosePrice)

        if self._hurst_value > self.HurstThreshold:
            if close_price > self._previous_upper and self.Position <= 0:
                self.BuyMarket(self.Volume + abs(self.Position))
            elif close_price < self._previous_lower and self.Position >= 0:
                self.SellMarket(self.Volume + abs(self.Position))

        if self.Position > 0 and close_price < self._previous_middle:
            self.SellMarket(self.Position)
        elif self.Position < 0 and close_price > self._previous_middle:
            self.BuyMarket(abs(self.Position))

        self._previous_upper = upper
        self._previous_lower = lower
        self._previous_middle = middle

    def CreateClone(self):
        return donchian_hurst_strategy()
