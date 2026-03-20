import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import Highest, Lowest, AverageTrueRange, SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *


class donchian_volatility_contraction_strategy(Strategy):
    """
    Breakout strategy that waits for Donchian channel contraction before trading a break of the previous channel.
    """

    def __init__(self):
        super(donchian_volatility_contraction_strategy, self).__init__()

        self._donchian_period = self.Param("DonchianPeriod", 20) \
            .SetDisplay("Donchian Period", "Period for the Donchian channel", "Indicators")

        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "Period for the ATR", "Indicators")

        self._volatility_factor = self.Param("VolatilityFactor", 0.8) \
            .SetDisplay("Volatility Factor", "Standard deviation multiplier for contraction detection", "Signals")

        self._cooldown_bars = self.Param("CooldownBars", 72) \
            .SetDisplay("Cooldown Bars", "Bars to wait after each order", "Risk")

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._donchian_high = None
        self._donchian_low = None
        self._atr = None
        self._width_average = None
        self._width_std_dev = None
        self._previous_high = 0.0
        self._previous_low = 0.0
        self._previous_width = 0.0
        self._width_average_value = 0.0
        self._width_std_dev_value = 0.0
        self._is_initialized = False
        self._cooldown = 0

    @property
    def DonchianPeriod(self):
        return self._donchian_period.Value

    @DonchianPeriod.setter
    def DonchianPeriod(self, value):
        self._donchian_period.Value = value

    @property
    def AtrPeriod(self):
        return self._atr_period.Value

    @AtrPeriod.setter
    def AtrPeriod(self, value):
        self._atr_period.Value = value

    @property
    def VolatilityFactor(self):
        return self._volatility_factor.Value

    @VolatilityFactor.setter
    def VolatilityFactor(self, value):
        self._volatility_factor.Value = value

    @property
    def CooldownBars(self):
        return self._cooldown_bars.Value

    @CooldownBars.setter
    def CooldownBars(self, value):
        self._cooldown_bars.Value = value

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
        super(donchian_volatility_contraction_strategy, self).OnReseted()
        self._donchian_high = None
        self._donchian_low = None
        self._atr = None
        self._width_average = None
        self._width_std_dev = None
        self._previous_high = 0.0
        self._previous_low = 0.0
        self._previous_width = 0.0
        self._width_average_value = 0.0
        self._width_std_dev_value = 0.0
        self._is_initialized = False
        self._cooldown = 0

    def OnStarted(self, time):
        super(donchian_volatility_contraction_strategy, self).OnStarted(time)

        self._donchian_high = Highest()
        self._donchian_high.Length = self.DonchianPeriod
        self._donchian_low = Lowest()
        self._donchian_low.Length = self.DonchianPeriod
        self._atr = AverageTrueRange()
        self._atr.Length = self.AtrPeriod
        self._width_average = SimpleMovingAverage()
        self._width_average.Length = self.DonchianPeriod
        self._width_std_dev = StandardDeviation()
        self._width_std_dev.Length = self.DonchianPeriod
        self._is_initialized = False
        self._cooldown = 0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._donchian_high, self._donchian_low, self._atr, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._atr)
            self.DrawOwnTrades(area)

        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent)
        )

    def ProcessCandle(self, candle, donchian_high_val, donchian_low_val, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._donchian_high.IsFormed or not self._donchian_low.IsFormed or not self._atr.IsFormed:
            return

        dh = float(donchian_high_val)
        dl = float(donchian_low_val)
        atr_val = float(atr_value)

        if not self._is_initialized:
            self._previous_high = dh
            self._previous_low = dl
            self._previous_width = dh - dl
            self._width_average_value = float(process_float(self._width_average, self._previous_width, candle.OpenTime, True))
            self._width_std_dev_value = float(process_float(self._width_std_dev, self._previous_width, candle.OpenTime, True))
            self._is_initialized = True
            return

        if not self._width_average.IsFormed or not self._width_std_dev.IsFormed:
            self._previous_high = dh
            self._previous_low = dl
            self._previous_width = dh - dl
            self._width_average_value = float(process_float(self._width_average, self._previous_width, candle.OpenTime, True))
            self._width_std_dev_value = float(process_float(self._width_std_dev, self._previous_width, candle.OpenTime, True))
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self.UpdateChannelStatistics(candle, dh, dl)
            return

        price = float(candle.ClosePrice)
        channel_middle = (self._previous_high + self._previous_low) / 2.0

        step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None:
            step = float(self.Security.PriceStep)
        if step <= 0:
            step = 1.0

        vol_threshold = max(self._width_average_value - self.VolatilityFactor * self._width_std_dev_value, step)
        is_contracted = self._previous_width <= vol_threshold

        if self.Position == 0:
            if is_contracted and price >= self._previous_high + atr_val * 0.05:
                self.BuyMarket()
                self._cooldown = self.CooldownBars
            elif is_contracted and price <= self._previous_low - atr_val * 0.05:
                self.SellMarket()
                self._cooldown = self.CooldownBars
        elif self.Position > 0:
            if price <= channel_middle:
                self.SellMarket(abs(self.Position))
                self._cooldown = self.CooldownBars
        elif self.Position < 0:
            if price >= channel_middle:
                self.BuyMarket(abs(self.Position))
                self._cooldown = self.CooldownBars

        self.UpdateChannelStatistics(candle, dh, dl)

    def UpdateChannelStatistics(self, candle, dh, dl):
        self._previous_high = dh
        self._previous_low = dl
        self._previous_width = dh - dl
        self._width_average_value = float(process_float(self._width_average, self._previous_width, candle.OpenTime, True))
        self._width_std_dev_value = float(process_float(self._width_std_dev, self._previous_width, candle.OpenTime, True))

    def CreateClone(self):
        return donchian_volatility_contraction_strategy()
