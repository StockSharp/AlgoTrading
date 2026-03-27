import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Decimal

from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import RelativeStrengthIndex, JurikMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class color_jjrsx_time_plus_strategy(Strategy):
    def __init__(self):
        super(color_jjrsx_time_plus_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))
        self._rsi_length = self.Param("RsiLength", 14)
        self._smoothing_length = self.Param("SmoothingLength", 3)
        self._signal_shift = self.Param("SignalShift", 1)
        self._enable_buy_entries = self.Param("EnableBuyEntries", True)
        self._enable_sell_entries = self.Param("EnableSellEntries", True)
        self._enable_buy_exit = self.Param("EnableBuyExit", True)
        self._enable_sell_exit = self.Param("EnableSellExit", True)
        self._enable_time_exit = self.Param("EnableTimeExit", True)
        self._holding_minutes = self.Param("HoldingMinutes", 480)
        self._stop_loss_points = self.Param("StopLossPoints", 1000)
        self._take_profit_points = self.Param("TakeProfitPoints", 2000)

        self._rsi = None
        self._smoother = None
        self._smoothed_values = []
        self._entry_time = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def RsiLength(self):
        return self._rsi_length.Value

    @property
    def SmoothingLength(self):
        return self._smoothing_length.Value

    @property
    def SignalShift(self):
        return self._signal_shift.Value

    @property
    def EnableBuyEntries(self):
        return self._enable_buy_entries.Value

    @property
    def EnableSellEntries(self):
        return self._enable_sell_entries.Value

    @property
    def EnableBuyExit(self):
        return self._enable_buy_exit.Value

    @property
    def EnableSellExit(self):
        return self._enable_sell_exit.Value

    @property
    def EnableTimeExit(self):
        return self._enable_time_exit.Value

    @property
    def HoldingMinutes(self):
        return self._holding_minutes.Value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    def OnStarted(self, time):
        super(color_jjrsx_time_plus_strategy, self).OnStarted(time)

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.RsiLength

        self._smoother = JurikMovingAverage()
        self._smoother.Length = self.SmoothingLength

        self._smoothed_values = []
        self._entry_time = None

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._rsi, self._process_candle).Start()

        sec = self.Security
        ps = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 0.01
        sl_unit = Unit()
        tp_unit = Unit()
        if self.StopLossPoints > 0:
            sl_unit = Unit(self.StopLossPoints * ps, UnitTypes.Absolute)
        if self.TakeProfitPoints > 0:
            tp_unit = Unit(self.TakeProfitPoints * ps, UnitTypes.Absolute)
        self.StartProtection(tp_unit, sl_unit)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._rsi)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        if self._smoother is None:
            return

        self._handle_time_exit(candle.CloseTime)

        if not self._rsi.IsFormed:
            return

        rsi_v = float(rsi_value)
        iv = DecimalIndicatorValue(self._smoother, Decimal(rsi_v), candle.ServerTime)
        iv.IsFinal = True
        smooth_val = self._smoother.Process(iv)

        if not self._smoother.IsFormed:
            return

        sv = float(smooth_val.Value)
        self._smoothed_values.append(sv)

        required = self.SignalShift + 3
        if len(self._smoothed_values) < required:
            return

        while len(self._smoothed_values) > required:
            self._smoothed_values.pop(0)

        values = self._smoothed_values
        current_idx = len(values) - self.SignalShift - 1
        previous_idx = len(values) - self.SignalShift - 2
        older_idx = len(values) - self.SignalShift - 3

        if current_idx < 0 or previous_idx < 0 or older_idx < 0:
            return

        current = values[current_idx]
        previous = values[previous_idx]
        older = values[older_idx]

        slope_up = previous < older
        slope_down = previous > older

        if self.EnableSellExit and slope_up and self.Position < 0:
            self.BuyMarket()
            self._entry_time = None

        if self.EnableBuyExit and slope_down and self.Position > 0:
            self.SellMarket()
            self._entry_time = None

        if self.EnableBuyEntries and slope_up and current > previous and self.Position <= 0:
            self.BuyMarket()
            self._entry_time = candle.CloseTime
        elif self.EnableSellEntries and slope_down and current < previous and self.Position >= 0:
            self.SellMarket()
            self._entry_time = candle.CloseTime

    def _handle_time_exit(self, candle_time):
        if not self.EnableTimeExit or self.Position == 0 or self._entry_time is None:
            return

        elapsed = candle_time - self._entry_time
        if elapsed.TotalMinutes < self.HoldingMinutes:
            return

        if self.Position > 0:
            self.SellMarket()
        elif self.Position < 0:
            self.BuyMarket()

        self._entry_time = None

    def OnReseted(self):
        super(color_jjrsx_time_plus_strategy, self).OnReseted()
        self._smoothed_values = []
        self._entry_time = None

    def CreateClone(self):
        return color_jjrsx_time_plus_strategy()
