import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ParabolicSar, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class genie_strategy(Strategy):
    def __init__(self):
        super(genie_strategy, self).__init__()
        self._take_profit = self.Param("TakeProfit", 500.0) \
            .SetDisplay("Take Profit", "Take profit distance", "Protection")
        self._trailing_stop = self.Param("TrailingStop", 200.0) \
            .SetDisplay("Trailing Stop", "Trailing stop distance", "Protection")
        self._sar_step = self.Param("SarStep", 0.02) \
            .SetDisplay("SAR Step", "Acceleration factor", "Indicator")
        self._adx_period = self.Param("AdxPeriod", 14) \
            .SetDisplay("Momentum Period", "Period for momentum confirmation", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._cooldown_bars = self.Param("CooldownBars", 4) \
            .SetDisplay("Cooldown Bars", "Completed candles to wait after a position change", "Trading")
        self._rsi_long_level = self.Param("RsiLongLevel", 55.0) \
            .SetDisplay("RSI Long", "Minimum RSI level for long entries", "Filters")
        self._rsi_short_level = self.Param("RsiShortLevel", 45.0) \
            .SetDisplay("RSI Short", "Maximum RSI level for short entries", "Filters")
        self._prev_sar = 0.0
        self._prev_candle = None
        self._cooldown_remaining = 0

    @property
    def take_profit(self):
        return self._take_profit.Value
    @property
    def trailing_stop(self):
        return self._trailing_stop.Value
    @property
    def sar_step(self):
        return self._sar_step.Value
    @property
    def adx_period(self):
        return self._adx_period.Value
    @property
    def candle_type(self):
        return self._candle_type.Value
    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value
    @property
    def rsi_long_level(self):
        return self._rsi_long_level.Value
    @property
    def rsi_short_level(self):
        return self._rsi_short_level.Value

    def OnReseted(self):
        super(genie_strategy, self).OnReseted()
        self._prev_sar = 0.0
        self._prev_candle = None
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(genie_strategy, self).OnStarted2(time)
        sar = ParabolicSar()
        sar.AccelerationStep = float(self.sar_step)
        sar.AccelerationMax = 0.2
        rsi = RelativeStrengthIndex()
        rsi.Length = self.adx_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(sar, rsi, self.process_candle).Start()
        self.StartProtection(
            Unit(float(self.take_profit), UnitTypes.Absolute),
            Unit(float(self.trailing_stop), UnitTypes.Absolute),
            isStopTrailing=True,
            useMarketOrders=True)
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sar)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, sar_value, rsi_value):
        if candle.State != CandleStates.Finished or not sar_value.IsFinal or not rsi_value.IsFinal:
            return
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
        sar_current = float(sar_value)
        rsi = float(rsi_value)
        if self._prev_candle is None:
            self._prev_sar = sar_current
            self._prev_candle = candle
            return
        prev_close = float(self._prev_candle.ClosePrice)
        close = float(candle.ClosePrice)
        rsi_long = float(self.rsi_long_level)
        rsi_short = float(self.rsi_short_level)
        sell_condition = self._cooldown_remaining == 0 and \
            self._prev_sar < prev_close and \
            sar_current > close and \
            rsi <= rsi_short
        buy_condition = self._cooldown_remaining == 0 and \
            self._prev_sar > prev_close and \
            sar_current < close and \
            rsi >= rsi_long
        if self.Position == 0:
            if sell_condition:
                self.SellMarket()
                self._cooldown_remaining = self.cooldown_bars
            elif buy_condition:
                self.BuyMarket()
                self._cooldown_remaining = self.cooldown_bars
        elif self.Position > 0 and float(self._prev_candle.OpenPrice) > prev_close:
            self.SellMarket()
            self._cooldown_remaining = self.cooldown_bars
        elif self.Position < 0 and float(self._prev_candle.OpenPrice) < prev_close:
            self.BuyMarket()
            self._cooldown_remaining = self.cooldown_bars
        self._prev_sar = sar_current
        self._prev_candle = candle

    def CreateClone(self):
        return genie_strategy()
