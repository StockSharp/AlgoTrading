import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import Momentum, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *


class seasonality_adjusted_momentum_strategy(Strategy):
    """
    Momentum strategy that allows longs or shorts only when the current month historically supports that seasonal bias.
    """

    def __init__(self):
        super(seasonality_adjusted_momentum_strategy, self).__init__()

        self._momentum_period = self.Param("MomentumPeriod", 14) \
            .SetDisplay("Momentum Period", "Period for the momentum indicator", "Indicators")

        self._seasonality_threshold = self.Param("SeasonalityThreshold", 0.2) \
            .SetDisplay("Seasonality Threshold", "Minimum absolute seasonality strength required for entries", "Signals")

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")

        self._cooldown_bars = self.Param("CooldownBars", 120) \
            .SetDisplay("Cooldown Bars", "Bars to wait after each order", "Risk")

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles for the strategy", "General")

        self._seasonal_strength_by_month = {}
        self._momentum = None
        self._momentum_average = None
        self._cooldown = 0

        self.InitializeSeasonalityData()

    @property
    def MomentumPeriod(self):
        return self._momentum_period.Value

    @MomentumPeriod.setter
    def MomentumPeriod(self, value):
        self._momentum_period.Value = value

    @property
    def SeasonalityThreshold(self):
        return self._seasonality_threshold.Value

    @SeasonalityThreshold.setter
    def SeasonalityThreshold(self, value):
        self._seasonality_threshold.Value = value

    @property
    def StopLossPercent(self):
        return self._stop_loss_percent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def CooldownBars(self):
        return self._cooldown_bars.Value

    @CooldownBars.setter
    def CooldownBars(self, value):
        self._cooldown_bars.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def InitializeSeasonalityData(self):
        self._seasonal_strength_by_month[1] = 0.8
        self._seasonal_strength_by_month[2] = 0.2
        self._seasonal_strength_by_month[3] = 0.5
        self._seasonal_strength_by_month[4] = 0.7
        self._seasonal_strength_by_month[5] = 0.3
        self._seasonal_strength_by_month[6] = -0.2
        self._seasonal_strength_by_month[7] = -0.3
        self._seasonal_strength_by_month[8] = -0.4
        self._seasonal_strength_by_month[9] = -0.7
        self._seasonal_strength_by_month[10] = 0.4
        self._seasonal_strength_by_month[11] = 0.6
        self._seasonal_strength_by_month[12] = 0.9

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(seasonality_adjusted_momentum_strategy, self).OnReseted()
        self._momentum = None
        self._momentum_average = None
        self._cooldown = 0
        self._seasonal_strength_by_month.clear()
        self.InitializeSeasonalityData()

    def OnStarted(self, time):
        super(seasonality_adjusted_momentum_strategy, self).OnStarted(time)

        self._momentum = Momentum()
        self._momentum.Length = self.MomentumPeriod
        self._momentum_average = SimpleMovingAverage()
        self._momentum_average.Length = self.MomentumPeriod
        self._cooldown = 0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._momentum, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._momentum)
            self.DrawIndicator(area, self._momentum_average)
            self.DrawOwnTrades(area)

        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent)
        )

    def ProcessCandle(self, candle, momentum_value):
        if candle.State != CandleStates.Finished:
            return

        momentum_val = float(momentum_value)

        momentum_avg_result = process_float(self._momentum_average, momentum_val, candle.OpenTime, True)
        momentum_avg_val = float(momentum_avg_result)

        if not self._momentum.IsFormed or not self._momentum_average.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        seasonal_strength = self._seasonal_strength_by_month.get(candle.OpenTime.Month, 0.0)
        allow_long = seasonal_strength >= self.SeasonalityThreshold
        allow_short = seasonal_strength <= -self.SeasonalityThreshold
        bullish_momentum = momentum_val > momentum_avg_val
        bearish_momentum = momentum_val < momentum_avg_val

        if self.Position > 0:
            if not allow_long or bearish_momentum:
                self.SellMarket(abs(self.Position))
                self._cooldown = self.CooldownBars
            return

        if self.Position < 0:
            if not allow_short or bullish_momentum:
                self.BuyMarket(abs(self.Position))
                self._cooldown = self.CooldownBars
            return

        if allow_long and bullish_momentum:
            self.BuyMarket()
            self._cooldown = self.CooldownBars
        elif allow_short and bearish_momentum:
            self.SellMarket()
            self._cooldown = self.CooldownBars

    def CreateClone(self):
        return seasonality_adjusted_momentum_strategy()
