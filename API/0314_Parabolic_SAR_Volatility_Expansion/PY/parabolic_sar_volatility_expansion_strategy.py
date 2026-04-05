import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ParabolicSar, AverageTrueRange, SimpleMovingAverage, StandardDeviation, CandleIndicatorValue
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

class parabolic_sar_volatility_expansion_strategy(Strategy):
    """
    Trend-following strategy that activates Parabolic SAR signals only when ATR expands above its recent regime.
    """

    def __init__(self):
        super(parabolic_sar_volatility_expansion_strategy, self).__init__()

        self._sar_af = self.Param("SarAf", 0.02) \
            .SetRange(0.001, 1.0) \
            .SetDisplay("SAR AF", "Parabolic SAR acceleration factor", "Indicators")

        self._sar_max_af = self.Param("SarMaxAf", 0.2) \
            .SetRange(0.01, 2.0) \
            .SetDisplay("SAR Max AF", "Parabolic SAR maximum acceleration factor", "Indicators")

        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetRange(2, 100) \
            .SetDisplay("ATR Period", "Period for ATR calculation", "Indicators")

        self._volatility_expansion_factor = self.Param("VolatilityExpansionFactor", 1.6) \
            .SetRange(0.1, 10.0) \
            .SetDisplay("Volatility Expansion Factor", "Factor for volatility expansion detection", "Signals")

        self._cooldown_bars = self.Param("CooldownBars", 84) \
            .SetRange(1, 500) \
            .SetDisplay("Cooldown Bars", "Bars to wait after each order", "Risk")

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetRange(0.5, 10.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(parabolic_sar_volatility_expansion_strategy, self).OnReseted()
        self._cooldown = 0

    def OnStarted2(self, time):
        super(parabolic_sar_volatility_expansion_strategy, self).OnStarted2(time)

        parabolic_sar = ParabolicSar()
        parabolic_sar.Acceleration = Decimal(self._sar_af.Value)
        parabolic_sar.AccelerationMax = Decimal(self._sar_max_af.Value)

        atr_period = int(self._atr_period.Value)
        self._atr = AverageTrueRange()
        self._atr.Length = atr_period
        self._atr_sma = SimpleMovingAverage()
        self._atr_sma.Length = atr_period
        self._atr_std_dev = StandardDeviation()
        self._atr_std_dev.Length = atr_period
        self._cooldown = 0

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(parabolic_sar, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, parabolic_sar)
            self.DrawIndicator(area, self._atr)
            self.DrawOwnTrades(area)

        self.StartProtection(
            Unit(0, UnitTypes.Absolute),
            Unit(self._stop_loss_percent.Value, UnitTypes.Percent),
            False
        )

    def _process_candle(self, candle, sar_value):
        if candle.State != CandleStates.Finished:
            return

        atr_result = self._atr.Process(CandleIndicatorValue(self._atr, candle))
        atr_value = float(atr_result)

        atr_sma_value = float(process_float(self._atr_sma, Decimal(atr_value), candle.OpenTime, True))

        atr_std_dev_value = float(process_float(self._atr_std_dev, Decimal(atr_value), candle.OpenTime, True))

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if not self._atr.IsFormed or not self._atr_sma.IsFormed or not self._atr_std_dev.IsFormed:
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        vef = float(self._volatility_expansion_factor.Value)
        volatility_threshold = atr_sma_value + vef * atr_std_dev_value
        is_volatility_expanding = atr_value >= volatility_threshold

        price = float(candle.ClosePrice)
        sar_val = float(sar_value)
        is_above_sar = price > sar_val
        is_below_sar = price < sar_val

        cd = int(self._cooldown_bars.Value)

        if self.Position == 0:
            if is_volatility_expanding and is_above_sar:
                self.BuyMarket()
                self._cooldown = cd
            elif is_volatility_expanding and is_below_sar:
                self.SellMarket()
                self._cooldown = cd
            return

        if self.Position > 0 and (not is_above_sar or not is_volatility_expanding):
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown = cd
        elif self.Position < 0 and (not is_below_sar or not is_volatility_expanding):
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown = cd

    def CreateClone(self):
        return parabolic_sar_volatility_expansion_strategy()
