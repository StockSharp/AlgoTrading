import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan

from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import AverageDirectionalIndex
from StockSharp.Algo.Strategies import Strategy


class ytg_adx_level_cross_strategy(Strategy):
    def __init__(self):
        super(ytg_adx_level_cross_strategy, self).__init__()

        self._adx_period = self.Param("AdxPeriod", 14)
        self._level_plus = self.Param("LevelPlus", 15)
        self._level_minus = self.Param("LevelMinus", 15)
        self._shift = self.Param("Shift", 1)
        self._take_profit_points = self.Param("TakeProfitPoints", 500.0)
        self._stop_loss_points = self.Param("StopLossPoints", 500.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))

        self._adx = None
        self._plus_di_history = []
        self._minus_di_history = []

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def AdxPeriod(self):
        return self._adx_period.Value

    @property
    def LevelPlus(self):
        return self._level_plus.Value

    @property
    def LevelMinus(self):
        return self._level_minus.Value

    @property
    def Shift(self):
        return self._shift.Value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    def OnStarted(self, time):
        super(ytg_adx_level_cross_strategy, self).OnStarted(time)

        self._adx = AverageDirectionalIndex()
        self._adx.Length = self.AdxPeriod

        self._plus_di_history = []
        self._minus_di_history = []

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 1.0
        tp_unit = None
        sl_unit = None
        if self.TakeProfitPoints > 0:
            tp_unit = Unit(self.TakeProfitPoints * step, UnitTypes.Absolute)
        if self.StopLossPoints > 0:
            sl_unit = Unit(self.StopLossPoints * step, UnitTypes.Absolute)

        if tp_unit is not None or sl_unit is not None:
            self.StartProtection(tp_unit, sl_unit)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        adx_val = self._adx.Process(candle)
        if not self._adx.IsFormed:
            return
        if not adx_val.IsFinal:
            return

        plus_di = None
        minus_di = None
        try:
            dx = adx_val.Dx
            if dx is not None:
                plus_di = dx.Plus
                minus_di = dx.Minus
        except Exception:
            return

        if plus_di is None or minus_di is None:
            return

        plus_di = float(plus_di)
        minus_di = float(minus_di)

        self._update_history(self._plus_di_history, plus_di)
        self._update_history(self._minus_di_history, minus_di)

        current_shift = self.Shift
        min_count = current_shift + 2

        if len(self._plus_di_history) < min_count or len(self._minus_di_history) < min_count:
            return

        current_idx = len(self._plus_di_history) - 1 - current_shift
        prev_idx = current_idx - 1

        if prev_idx < 0:
            return

        shifted_plus = self._plus_di_history[current_idx]
        shifted_plus_prev = self._plus_di_history[prev_idx]
        shifted_minus = self._minus_di_history[current_idx]
        shifted_minus_prev = self._minus_di_history[prev_idx]

        long_signal = shifted_plus > self.LevelPlus and shifted_plus_prev < self.LevelPlus
        short_signal = shifted_minus > self.LevelMinus and shifted_minus_prev < self.LevelMinus

        if self.Position == 0:
            if long_signal:
                self.BuyMarket()
            elif short_signal:
                self.SellMarket()

    def _update_history(self, history, value):
        history.append(value)
        max_length = self.Shift + 2
        while len(history) > max_length:
            history.pop(0)

    def OnReseted(self):
        super(ytg_adx_level_cross_strategy, self).OnReseted()
        self._plus_di_history = []
        self._minus_di_history = []

    def CreateClone(self):
        return ytg_adx_level_cross_strategy()
