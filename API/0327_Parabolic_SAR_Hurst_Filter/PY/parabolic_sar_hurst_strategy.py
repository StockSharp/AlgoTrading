import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ParabolicSar, HurstExponent, CandleIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class parabolic_sar_hurst_strategy(Strategy):
    """
    Parabolic SAR with Hurst Filter Strategy.
    Enters a position when price crosses SAR and Hurst exponent indicates a persistent trend.
    """

    def __init__(self):
        super(parabolic_sar_hurst_strategy, self).__init__()

        self._sar_acceleration_factor = self.Param("SarAccelerationFactor", 0.02) \
            .SetRange(0.01, 0.2) \
            .SetDisplay("SAR Acceleration Factor", "Initial acceleration factor for Parabolic SAR", "SAR Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(0.01, 0.1, 0.01)

        self._sar_max_acceleration_factor = self.Param("SarMaxAccelerationFactor", 0.2) \
            .SetRange(0.05, 0.5) \
            .SetDisplay("SAR Max Acceleration Factor", "Maximum acceleration factor for Parabolic SAR", "SAR Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(0.1, 0.3, 0.05)

        self._hurst_period = self.Param("HurstPeriod", 100) \
            .SetRange(20, 200) \
            .SetDisplay("Hurst Period", "Period for Hurst exponent calculation", "Hurst Settings") \
            .SetCanOptimize(True) \
            .SetOptimize(50, 150, 25)

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 4) \
            .SetNotNegative() \
            .SetDisplay("Signal Cooldown Bars", "Closed candles to wait before a new SAR crossover entry", "General")

        self._parabolic_sar = None
        self._hurst_indicator = None
        self._prev_sar_value = 0.0
        self._hurst_value = 0.5
        self._prev_price_above_sar = None
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnReseted(self):
        super(parabolic_sar_hurst_strategy, self).OnReseted()
        self._parabolic_sar = None
        self._hurst_indicator = None
        self._prev_sar_value = 0.0
        self._hurst_value = 0.5
        self._prev_price_above_sar = None
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(parabolic_sar_hurst_strategy, self).OnStarted2(time)

        self._parabolic_sar = ParabolicSar()
        self._parabolic_sar.Acceleration = Decimal(self._sar_acceleration_factor.Value)
        self._parabolic_sar.AccelerationMax = Decimal(self._sar_max_acceleration_factor.Value)

        self._hurst_indicator = HurstExponent()
        self._hurst_indicator.Length = int(self._hurst_period.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.ProcessCandle).Start()

        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(1, UnitTypes.Percent)
        )

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._parabolic_sar)
            self.DrawIndicator(area, self._hurst_indicator)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        sar_val = self._parabolic_sar.Process(CandleIndicatorValue(self._parabolic_sar, candle))
        hurst_val = self._hurst_indicator.Process(CandleIndicatorValue(self._hurst_indicator, candle))

        if not self._parabolic_sar.IsFormed or not self._hurst_indicator.IsFormed or sar_val.IsEmpty or hurst_val.IsEmpty:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        sar_price = float(sar_val)
        self._hurst_value = float(hurst_val)
        current_sar_value = sar_price
        price_above_sar = float(candle.ClosePrice) > sar_price

        if self._prev_price_above_sar is None or self._prev_sar_value == 0.0:
            self._prev_sar_value = current_sar_value
            self._prev_price_above_sar = price_above_sar
            return

        cooldown_bars = int(self._signal_cooldown_bars.Value)

        if self._hurst_value > 0.55:
            bullish_cross = not self._prev_price_above_sar and price_above_sar
            bearish_cross = self._prev_price_above_sar and not price_above_sar

            if self._cooldown_remaining == 0 and bullish_cross and self.Position <= 0:
                vol = self.Volume
                if self.Position < 0:
                    vol = self.Volume + Math.Abs(self.Position)
                self.BuyMarket(vol)
                self._cooldown_remaining = cooldown_bars
            elif self._cooldown_remaining == 0 and bearish_cross and self.Position >= 0:
                vol = self.Volume
                if self.Position > 0:
                    vol = self.Volume + Math.Abs(self.Position)
                self.SellMarket(vol)
                self._cooldown_remaining = cooldown_bars
        else:
            if self.Position > 0:
                self.SellMarket(self.Position)
            elif self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))

        self._prev_sar_value = current_sar_value
        self._prev_price_above_sar = price_above_sar

    def CreateClone(self):
        return parabolic_sar_hurst_strategy()
