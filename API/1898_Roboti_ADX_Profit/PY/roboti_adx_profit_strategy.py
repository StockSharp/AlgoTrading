import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import DirectionalIndex
from StockSharp.Algo.Strategies import Strategy


class roboti_adx_profit_strategy(Strategy):
    def __init__(self):
        super(roboti_adx_profit_strategy, self).__init__()
        self._dmi_period = self.Param("DmiPeriod", 14) \
            .SetDisplay("DMI Period", "Period for Directional Movement Index", "Indicators") \
            .SetOptimize(10, 30, 2)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type and timeframe of candles", "General")
        self._trailing_stop_percent = self.Param("TrailingStopPercent", 1.0) \
            .SetDisplay("Trailing Stop %", "Trailing stop as percent", "Risk Management") \
            .SetGreaterThanZero() \
            .SetOptimize(0.5, 5.0, 0.5)
        self._cooldown_bars = self.Param("CooldownBars", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Cooldown Bars", "Minimum number of bars between entries", "Risk Management")

        self._prev_plus = 0.0
        self._prev_minus = 0.0
        self._bars_since_trade = 0

    @property
    def dmi_period(self):
        return self._dmi_period.Value
    @property
    def candle_type(self):
        return self._candle_type.Value
    @property
    def trailing_stop_percent(self):
        return self._trailing_stop_percent.Value
    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    def OnReseted(self):
        super(roboti_adx_profit_strategy, self).OnReseted()
        self._prev_plus = 0.0
        self._prev_minus = 0.0
        self._bars_since_trade = self.cooldown_bars

    def OnStarted2(self, time):
        super(roboti_adx_profit_strategy, self).OnStarted2(time)
        dmi = DirectionalIndex()
        dmi.Length = self.dmi_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(dmi, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, dmi)
            self.DrawOwnTrades(area)
        self.StartProtection(
            None,
            Unit(float(self.trailing_stop_percent), UnitTypes.Percent),
            True)

    def process_candle(self, candle, dmi_value):
        if candle.State != CandleStates.Finished:
            return

        plus_val = dmi_value.Plus
        minus_val = dmi_value.Minus
        if plus_val is None or minus_val is None:
            return

        plus = float(plus_val)
        minus = float(minus_val)
        self._bars_since_trade += 1

        buy_signal = self._prev_plus <= self._prev_minus and plus > minus
        sell_signal = self._prev_plus >= self._prev_minus and minus > plus

        if buy_signal and self.Position <= 0 and self._bars_since_trade >= self.cooldown_bars:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._bars_since_trade = 0
        elif sell_signal and self.Position >= 0 and self._bars_since_trade >= self.cooldown_bars:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._bars_since_trade = 0

        self._prev_plus = plus
        self._prev_minus = minus

    def CreateClone(self):
        return roboti_adx_profit_strategy()
