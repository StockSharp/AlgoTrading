import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ChandeMomentumOscillator
from StockSharp.Algo.Strategies import Strategy


class cmo_zero_cross_strategy(Strategy):
    def __init__(self):
        super(cmo_zero_cross_strategy, self).__init__()
        self._cmo_period = self.Param("CmoPeriod", 14) \
            .SetDisplay("CMO Period", "Period for Chande Momentum Oscillator", "Indicators")
        self._stop_loss = self.Param("StopLoss", 1000.0) \
            .SetDisplay("Stop Loss (pt)", "Stop loss in points", "Risk Management")
        self._take_profit = self.Param("TakeProfit", 2000.0) \
            .SetDisplay("Take Profit (pt)", "Take profit in points", "Risk Management")
        self._allow_long_entry = self.Param("AllowLongEntry", True) \
            .SetDisplay("Allow Long Entry", "Permission to open long positions", "Strategy")
        self._allow_short_entry = self.Param("AllowShortEntry", True) \
            .SetDisplay("Allow Short Entry", "Permission to open short positions", "Strategy")
        self._allow_long_exit = self.Param("AllowLongExit", True) \
            .SetDisplay("Allow Long Exit", "Permission to close long positions", "Strategy")
        self._allow_short_exit = self.Param("AllowShortExit", True) \
            .SetDisplay("Allow Short Exit", "Permission to close short positions", "Strategy")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe for calculations", "General")
        self._min_abs_cmo = self.Param("MinAbsCmo", 5.0) \
            .SetDisplay("Minimum CMO", "Minimum absolute CMO value required after a zero cross", "Filters")
        self._cooldown_bars = self.Param("CooldownBars", 4) \
            .SetDisplay("Cooldown Bars", "Completed candles to wait after a position change", "Trading")
        self._prev_cmo = None
        self._cooldown_remaining = 0

    @property
    def cmo_period(self):
        return self._cmo_period.Value
    @property
    def stop_loss(self):
        return self._stop_loss.Value
    @property
    def take_profit(self):
        return self._take_profit.Value
    @property
    def allow_long_entry(self):
        return self._allow_long_entry.Value
    @property
    def allow_short_entry(self):
        return self._allow_short_entry.Value
    @property
    def allow_long_exit(self):
        return self._allow_long_exit.Value
    @property
    def allow_short_exit(self):
        return self._allow_short_exit.Value
    @property
    def candle_type(self):
        return self._candle_type.Value
    @property
    def min_abs_cmo(self):
        return self._min_abs_cmo.Value
    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    def OnReseted(self):
        super(cmo_zero_cross_strategy, self).OnReseted()
        self._prev_cmo = None
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(cmo_zero_cross_strategy, self).OnStarted2(time)
        cmo = ChandeMomentumOscillator()
        cmo.Length = self.cmo_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(cmo, self.process_candle).Start()
        self.StartProtection(
            Unit(float(self.take_profit), UnitTypes.Absolute),
            Unit(float(self.stop_loss), UnitTypes.Absolute))
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, cmo)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, cmo_value):
        if candle.State != CandleStates.Finished:
            return
        cmo_value = float(cmo_value)
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
        prev = self._prev_cmo
        self._prev_cmo = cmo_value
        if prev is None or self._cooldown_remaining > 0:
            return
        min_cmo = float(self.min_abs_cmo)
        cross_up = prev < 0 and cmo_value > 0 and abs(cmo_value) >= min_cmo
        cross_down = prev > 0 and cmo_value < 0 and abs(cmo_value) >= min_cmo
        if cross_up:
            if self.allow_short_exit and self.Position < 0:
                self.BuyMarket()
            if self.allow_long_entry and self.Position <= 0:
                self.BuyMarket()
                self._cooldown_remaining = self.cooldown_bars
        elif cross_down:
            if self.allow_long_exit and self.Position > 0:
                self.SellMarket()
            if self.allow_short_entry and self.Position >= 0:
                self.SellMarket()
                self._cooldown_remaining = self.cooldown_bars

    def CreateClone(self):
        return cmo_zero_cross_strategy()
