import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageDirectionalIndex, ParabolicSar, CandleIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class trend_me_leave_me_strategy(Strategy):
    DIR_NONE = 0
    DIR_BUY = 1
    DIR_SELL = 2

    def __init__(self):
        super(trend_me_leave_me_strategy, self).__init__()
        self._stop_loss_pips = self.Param("StopLossPips", 50)
        self._take_profit_pips = self.Param("TakeProfitPips", 180)
        self._breakeven_pips = self.Param("BreakevenPips", 5)
        self._adx_period = self.Param("AdxPeriod", 14)
        self._adx_quiet_level = self.Param("AdxQuietLevel", 20.0)
        self._sar_step = self.Param("SarStep", 0.02)
        self._sar_max = self.Param("SarMax", 0.2)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))
        self._adx = None
        self._sar = None
        self._next_direction = self.DIR_BUY
        self._breakeven_activated = False
        self._pip_size = 0.0
        self._position_direction = 0
        self._exit_order_pending = False
        self._entry_price = 0.0

    @property
    def StopLossPips(self):
        return self._stop_loss_pips.Value

    @property
    def TakeProfitPips(self):
        return self._take_profit_pips.Value

    @property
    def BreakevenPips(self):
        return self._breakeven_pips.Value

    @property
    def AdxPeriod(self):
        return self._adx_period.Value

    @property
    def AdxQuietLevel(self):
        return self._adx_quiet_level.Value

    @property
    def SarStep(self):
        return self._sar_step.Value

    @property
    def SarMax(self):
        return self._sar_max.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(trend_me_leave_me_strategy, self).OnStarted2(time)
        self._pip_size = self._calculate_pip_size()
        self._adx = AverageDirectionalIndex()
        self._adx.Length = self.AdxPeriod
        self._sar = ParabolicSar()
        self._sar.AccelerationStep = self.SarStep
        self._sar.AccelerationMax = self.SarMax
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._sar)
            self.DrawIndicator(area, self._adx)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        civ_adx = CandleIndicatorValue(self._adx, candle)
        civ_adx.IsFinal = True
        adx_value = self._adx.Process(civ_adx)
        civ_sar = CandleIndicatorValue(self._sar, candle)
        civ_sar.IsFinal = True
        sar_value = self._sar.Process(civ_sar)
        if not self._adx.IsFormed or not self._sar.IsFormed:
            return
        if not adx_value.IsFinal or not sar_value.IsFinal:
            return
        if self._pip_size <= 0:
            self._pip_size = self._calculate_pip_size()
        pos = float(self.Position)
        if self._exit_order_pending:
            if pos == 0:
                self._exit_order_pending = False
                self._position_direction = 0
                self._breakeven_activated = False
            else:
                return
        if pos != 0:
            current_dir = 1 if pos > 0 else -1
            if self._position_direction != current_dir:
                self._position_direction = current_dir
                self._breakeven_activated = False
            self._manage_open_position(candle)
            if self._exit_order_pending or float(self.Position) != 0:
                return
        else:
            self._position_direction = 0
            self._breakeven_activated = False

        try:
            ma = adx_value.MovingAverage
            if ma is None:
                return
            adx_ma = float(ma)
        except:
            try:
                adx_ma = float(adx_value.Value)
            except:
                return

        sar = float(sar_value.Value)
        close = float(candle.ClosePrice)
        quiet_market = adx_ma < float(self.AdxQuietLevel)

        if (self._next_direction == self.DIR_BUY or self._next_direction == self.DIR_NONE) and quiet_market and close > sar:
            self._breakeven_activated = False
            self.BuyMarket(float(self.Volume) + abs(float(self.Position)))
            self._position_direction = 1
            self._entry_price = close
        elif self._next_direction == self.DIR_SELL and quiet_market and close < sar:
            self._breakeven_activated = False
            self.SellMarket(float(self.Volume) + abs(float(self.Position)))
            self._position_direction = -1
            self._entry_price = close

    def _manage_open_position(self, candle):
        entry = self._entry_price
        if entry <= 0:
            return
        direction = self._position_direction
        pip = self._pip_size if self._pip_size > 0 else 1.0
        if direction > 0:
            stop_price = entry - self.StopLossPips * pip if self.StopLossPips > 0 else float('-inf')
            take_price = entry + self.TakeProfitPips * pip if self.TakeProfitPips > 0 else float('inf')
            if not self._breakeven_activated and self.BreakevenPips > 0:
                trigger = entry + self.BreakevenPips * pip
                if float(candle.HighPrice) >= trigger:
                    self._breakeven_activated = True
            stop_triggered = (self.StopLossPips > 0 and float(candle.LowPrice) <= stop_price) or (self._breakeven_activated and float(candle.LowPrice) <= entry)
            take_triggered = self.TakeProfitPips > 0 and float(candle.HighPrice) >= take_price
            if stop_triggered or take_triggered:
                self.SellMarket(float(self.Position))
                self._exit_order_pending = True
                self._update_next_direction(take_triggered and not stop_triggered, direction)
        elif direction < 0:
            stop_price = entry + self.StopLossPips * pip if self.StopLossPips > 0 else float('inf')
            take_price = entry - self.TakeProfitPips * pip if self.TakeProfitPips > 0 else float('-inf')
            if not self._breakeven_activated and self.BreakevenPips > 0:
                trigger = entry - self.BreakevenPips * pip
                if float(candle.LowPrice) <= trigger:
                    self._breakeven_activated = True
            stop_triggered = (self.StopLossPips > 0 and float(candle.HighPrice) >= stop_price) or (self._breakeven_activated and float(candle.HighPrice) >= entry)
            take_triggered = self.TakeProfitPips > 0 and float(candle.LowPrice) <= take_price
            if stop_triggered or take_triggered:
                self.BuyMarket(abs(float(self.Position)))
                self._exit_order_pending = True
                self._update_next_direction(take_triggered and not stop_triggered, direction)

    def _update_next_direction(self, was_profit, direction):
        if direction > 0:
            self._next_direction = self.DIR_SELL if was_profit else self.DIR_BUY
        elif direction < 0:
            self._next_direction = self.DIR_BUY if was_profit else self.DIR_SELL

    def _calculate_pip_size(self):
        sec = self.Security
        if sec is None:
            return 1.0
        step = float(sec.PriceStep) if sec.PriceStep is not None else 1.0
        if step <= 0:
            return 1.0
        decimals = self._get_decimal_places(step)
        if decimals == 3 or decimals == 5:
            return step * 10.0
        return step

    def _get_decimal_places(self, value):
        s = str(value)
        if '.' in s:
            return len(s.split('.')[1].rstrip('0'))
        return 0

    def OnOwnTradeReceived(self, trade):
        super(trend_me_leave_me_strategy, self).OnOwnTradeReceived(trade)
        if trade is None or trade.Trade is None:
            return
        pos = float(self.Position)
        if pos != 0 and self._entry_price == 0:
            self._entry_price = float(trade.Trade.Price)
        if pos == 0:
            self._entry_price = 0.0

    def OnReseted(self):
        super(trend_me_leave_me_strategy, self).OnReseted()
        self._next_direction = self.DIR_BUY
        self._breakeven_activated = False
        self._pip_size = 0.0
        self._position_direction = 0
        self._exit_order_pending = False
        self._entry_price = 0.0

    def CreateClone(self):
        return trend_me_leave_me_strategy()
