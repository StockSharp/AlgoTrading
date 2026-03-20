import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageTrueRange, HurstExponent
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *


class hurst_volatility_filter_strategy(Strategy):
    """
    Strategy using the Hurst exponent to identify mean-reversion markets
    with an ATR-based volatility filter to confirm entry signals.
    """

    def __init__(self):
        super(hurst_volatility_filter_strategy, self).__init__()

        self._hurst_period = self.Param("HurstPeriod", 80) \
            .SetDisplay("Hurst Period", "Period for the Hurst exponent", "Indicators")

        self._ma_period = self.Param("MAPeriod", 20) \
            .SetDisplay("MA Period", "Period for the moving average", "Indicators")

        self._atr_period = self.Param("ATRPeriod", 14) \
            .SetDisplay("ATR Period", "Period for the ATR", "Indicators")

        self._hurst_threshold = self.Param("HurstThreshold", 0.7) \
            .SetDisplay("Hurst Threshold", "Maximum Hurst value allowed for entries", "Signals")

        self._deviation_atr_multiplier = self.Param("DeviationAtrMultiplier", 0.5) \
            .SetDisplay("Deviation ATR", "Minimum ATR multiple required for entry", "Signals")

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")

        self._cooldown_bars = self.Param("CooldownBars", 90) \
            .SetDisplay("Cooldown Bars", "Bars to wait after each order", "Risk")

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._sma = None
        self._atr = None
        self._hurst_exponent = None
        self._atr_average = None
        self._cooldown = 0

    @property
    def HurstPeriod(self):
        return self._hurst_period.Value

    @HurstPeriod.setter
    def HurstPeriod(self, value):
        self._hurst_period.Value = value

    @property
    def MAPeriod(self):
        return self._ma_period.Value

    @MAPeriod.setter
    def MAPeriod(self, value):
        self._ma_period.Value = value

    @property
    def ATRPeriod(self):
        return self._atr_period.Value

    @ATRPeriod.setter
    def ATRPeriod(self, value):
        self._atr_period.Value = value

    @property
    def HurstThreshold(self):
        return self._hurst_threshold.Value

    @HurstThreshold.setter
    def HurstThreshold(self, value):
        self._hurst_threshold.Value = value

    @property
    def DeviationAtrMultiplier(self):
        return self._deviation_atr_multiplier.Value

    @DeviationAtrMultiplier.setter
    def DeviationAtrMultiplier(self, value):
        self._deviation_atr_multiplier.Value = value

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

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(hurst_volatility_filter_strategy, self).OnReseted()
        self._sma = None
        self._atr = None
        self._hurst_exponent = None
        self._atr_average = None
        self._cooldown = 0

    def OnStarted(self, time):
        super(hurst_volatility_filter_strategy, self).OnStarted(time)

        self._sma = SimpleMovingAverage()
        self._sma.Length = self.MAPeriod
        self._atr = AverageTrueRange()
        self._atr.Length = self.ATRPeriod
        self._hurst_exponent = HurstExponent()
        self._hurst_exponent.Length = self.HurstPeriod
        self._atr_average = SimpleMovingAverage()
        self._atr_average.Length = max(self.ATRPeriod * 2, 10)
        self._cooldown = 0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._sma, self._atr, self._hurst_exponent, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._sma)
            self.DrawIndicator(area, self._atr)
            self.DrawIndicator(area, self._hurst_exponent)
            self.DrawOwnTrades(area)

        self.StartProtection(
            takeProfit=Unit(0, UnitTypes.Absolute),
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent)
        )

    def ProcessCandle(self, candle, sma_value, atr_value, hurst_value):
        if candle.State != CandleStates.Finished:
            return

        sma_val = float(sma_value)
        atr_val = float(atr_value)
        hurst_val = float(hurst_value)

        # Process ATR through average
        atr_avg_result = process_float(self._atr_average, atr_val, candle.OpenTime, True)
        atr_average_value = float(atr_avg_result)

        if not self._sma.IsFormed or not self._atr.IsFormed or not self._hurst_exponent.IsFormed or not self._atr_average.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        price = float(candle.ClosePrice)
        deviation = price - sma_val
        required_deviation = atr_val * self.DeviationAtrMultiplier
        is_mean_reversion_regime = hurst_val <= self.HurstThreshold
        is_quiet_volatility = atr_val <= atr_average_value * 1.5

        if self.Position == 0:
            if not is_mean_reversion_regime or not is_quiet_volatility:
                return

            if deviation <= -required_deviation:
                self.BuyMarket()
                self._cooldown = self.CooldownBars
            elif deviation >= required_deviation:
                self.SellMarket()
                self._cooldown = self.CooldownBars
            return

        if self.Position > 0 and (price >= sma_val or deviation >= -atr_val * 0.2 or not is_mean_reversion_regime):
            self.SellMarket(abs(self.Position))
            self._cooldown = self.CooldownBars
        elif self.Position < 0 and (price <= sma_val or deviation <= atr_val * 0.2 or not is_mean_reversion_regime):
            self.BuyMarket(abs(self.Position))
            self._cooldown = self.CooldownBars

    def CreateClone(self):
        return hurst_volatility_filter_strategy()
