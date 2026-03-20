import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan

from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, ParabolicSar
from StockSharp.Algo.Strategies import Strategy


class sar_trading_v20_strategy(Strategy):
    def __init__(self):
        super(sar_trading_v20_strategy, self).__init__()

        self._ma_period = self.Param("MaPeriod", 18)
        self._ma_shift = self.Param("MaShift", 2)
        self._sar_step = self.Param("SarStep", 0.02)
        self._sar_max_step = self.Param("SarMaxStep", 0.2)
        self._stop_loss_pips = self.Param("StopLossPips", 50)
        self._take_profit_pips = self.Param("TakeProfitPips", 50)
        self._trailing_stop_pips = self.Param("TrailingStopPips", 15)
        self._trailing_step_pips = self.Param("TrailingStepPips", 5)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15)))

        self._ma = None
        self._sar = None
        self._close_history = []
        self._entry_price = None
        self._stop_price = None
        self._take_profit_price = None
        self._pip_size = 0.0
        self._exit_pending = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def MaPeriod(self):
        return self._ma_period.Value

    @property
    def MaShift(self):
        return self._ma_shift.Value

    @property
    def SarStep(self):
        return self._sar_step.Value

    @property
    def SarMaxStep(self):
        return self._sar_max_step.Value

    @property
    def StopLossPips(self):
        return self._stop_loss_pips.Value

    @property
    def TakeProfitPips(self):
        return self._take_profit_pips.Value

    @property
    def TrailingStopPips(self):
        return self._trailing_stop_pips.Value

    @property
    def TrailingStepPips(self):
        return self._trailing_step_pips.Value

    def OnStarted(self, time):
        super(sar_trading_v20_strategy, self).OnStarted(time)

        sec = self.Security
        ps = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 0.0
        self._pip_size = ps if ps > 0 else 0.0001

        self._ma = SimpleMovingAverage()
        self._ma.Length = self.MaPeriod
        self._sar = ParabolicSar()
        self._sar.Acceleration = self.SarStep
        self._sar.AccelerationMax = self.SarMaxStep

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._ma, self._sar, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ma)
            self.DrawIndicator(area, self._sar)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ma_value, sar_value):
        if candle.State != CandleStates.Finished:
            return

        self._update_close_history(float(candle.ClosePrice))

        if self._exit_pending and self.Position == 0:
            self._reset_position_state()

        if self.Position != 0:
            self._manage_existing_position(candle)
            return

        if not self._ma.IsFormed or not self._sar.IsFormed:
            return

        if len(self._close_history) <= self.MaShift:
            return

        shifted_close = self._close_history[len(self._close_history) - 1 - self.MaShift]
        ma_v = float(ma_value)
        sar_v = float(sar_value)

        sar_below_ma = sar_v < ma_v
        sar_above_ma = sar_v > ma_v
        close_below_ma = shifted_close < ma_v
        close_above_ma = shifted_close > ma_v

        if sar_below_ma or close_below_ma:
            self._open_long(float(candle.ClosePrice))
        elif sar_above_ma or close_above_ma:
            self._open_short(float(candle.ClosePrice))

    def _manage_existing_position(self, candle):
        if self._exit_pending:
            return
        if self._entry_price is None:
            return

        if self.Position > 0:
            self._update_trailing_for_long(candle)
            self._try_exit_long(candle)
        elif self.Position < 0:
            self._update_trailing_for_short(candle)
            self._try_exit_short(candle)

    def _update_trailing_for_long(self, candle):
        if self.TrailingStopPips <= 0 or self._entry_price is None:
            return
        trail_dist = self.TrailingStopPips * self._pip_size
        trail_step = self.TrailingStepPips * self._pip_size
        profit = float(candle.ClosePrice) - self._entry_price
        if profit <= trail_dist + trail_step:
            return
        candidate = float(candle.ClosePrice) - trail_dist
        min_inc = trail_step if self.TrailingStepPips > 0 else 0
        if self._stop_price is None or candidate > self._stop_price + min_inc:
            self._stop_price = candidate

    def _update_trailing_for_short(self, candle):
        if self.TrailingStopPips <= 0 or self._entry_price is None:
            return
        trail_dist = self.TrailingStopPips * self._pip_size
        trail_step = self.TrailingStepPips * self._pip_size
        profit = self._entry_price - float(candle.ClosePrice)
        if profit <= trail_dist + trail_step:
            return
        candidate = float(candle.ClosePrice) + trail_dist
        min_dec = trail_step if self.TrailingStepPips > 0 else 0
        if self._stop_price is None or candidate < self._stop_price - min_dec:
            self._stop_price = candidate

    def _try_exit_long(self, candle):
        if self._stop_price is not None and float(candle.LowPrice) <= self._stop_price:
            self.SellMarket()
            self._exit_pending = True
            return
        if self._take_profit_price is not None and float(candle.HighPrice) >= self._take_profit_price:
            self.SellMarket()
            self._exit_pending = True

    def _try_exit_short(self, candle):
        if self._stop_price is not None and float(candle.HighPrice) >= self._stop_price:
            self.BuyMarket()
            self._exit_pending = True
            return
        if self._take_profit_price is not None and float(candle.LowPrice) <= self._take_profit_price:
            self.BuyMarket()
            self._exit_pending = True

    def _open_long(self, price):
        self.BuyMarket()
        self._init_position_state(price, True)

    def _open_short(self, price):
        self.SellMarket()
        self._init_position_state(price, False)

    def _init_position_state(self, entry_price, is_long):
        self._entry_price = entry_price
        self._exit_pending = False
        pip = self._pip_size if self._pip_size > 0 else 0.0001

        if self.StopLossPips > 0:
            self._stop_price = entry_price - self.StopLossPips * pip if is_long else entry_price + self.StopLossPips * pip
        else:
            self._stop_price = None

        if self.TakeProfitPips > 0:
            self._take_profit_price = entry_price + self.TakeProfitPips * pip if is_long else entry_price - self.TakeProfitPips * pip
        else:
            self._take_profit_price = None

    def _reset_position_state(self):
        self._entry_price = None
        self._stop_price = None
        self._take_profit_price = None
        self._exit_pending = False

    def _update_close_history(self, close_price):
        self._close_history.append(close_price)
        max_count = max(self.MaShift + 1, 1)
        if len(self._close_history) > max_count:
            self._close_history = self._close_history[-max_count:]

    def OnReseted(self):
        super(sar_trading_v20_strategy, self).OnReseted()
        self._ma = None
        self._sar = None
        self._close_history = []
        self._reset_position_state()
        self._pip_size = 0.0

    def CreateClone(self):
        return sar_trading_v20_strategy()
