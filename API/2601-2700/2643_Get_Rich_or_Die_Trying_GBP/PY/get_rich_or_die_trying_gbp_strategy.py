import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class get_rich_or_die_trying_gbp_strategy(Strategy):
    """Bar imbalance strategy with SL/TP, secondary TP and trailing stop."""

    def __init__(self):
        super(get_rich_or_die_trying_gbp_strategy, self).__init__()

        self._stop_loss_pips = self.Param("StopLossPips", 100) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss (pips)", "SL distance in pips", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", 100) \
            .SetGreaterThanZero() \
            .SetDisplay("Take Profit (pips)", "Primary TP distance in pips", "Risk")
        self._secondary_tp_pips = self.Param("SecondaryTakeProfitPips", 40) \
            .SetDisplay("Secondary TP (pips)", "Early exit distance in pips", "Risk")
        self._trailing_stop_pips = self.Param("TrailingStopPips", 30) \
            .SetDisplay("Trailing Stop (pips)", "Trailing stop distance in pips", "Risk")
        self._trailing_step_pips = self.Param("TrailingStepPips", 5) \
            .SetDisplay("Trailing Step (pips)", "Minimal improvement before trailing", "Risk")
        self._count_bars = self.Param("CountBars", 18) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback Bars", "Candles for imbalance detection", "Logic")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for processing", "General")

        self._dir_queue = []
        self._up_count = 0
        self._down_count = 0
        self._pip_value = 1.0
        self._entry_price = None
        self._long_trail = None
        self._short_trail = None
        self._sl_price = None
        self._tp_price = None
        self._exit_requested = False

    @property
    def StopLossPips(self):
        return self._stop_loss_pips.Value
    @property
    def TakeProfitPips(self):
        return self._take_profit_pips.Value
    @property
    def SecondaryTakeProfitPips(self):
        return self._secondary_tp_pips.Value
    @property
    def TrailingStopPips(self):
        return self._trailing_stop_pips.Value
    @property
    def TrailingStepPips(self):
        return self._trailing_step_pips.Value
    @property
    def CountBars(self):
        return self._count_bars.Value
    @property
    def CandleType(self):
        return self._candle_type.Value

    def _calc_pip(self):
        sec = self.Security
        if sec is None or sec.PriceStep is None or float(sec.PriceStep) <= 0:
            return 1.0
        step = float(sec.PriceStep)
        decimals = sec.Decimals if sec.Decimals is not None else 2
        if decimals == 3 or decimals == 5:
            return step * 10.0
        return step

    def OnStarted2(self, time):
        super(get_rich_or_die_trying_gbp_strategy, self).OnStarted2(time)

        self._pip_value = self._calc_pip()
        self._dir_queue = []
        self._up_count = 0
        self._down_count = 0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.process_candle).Start()

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self.Position == 0 and self._exit_requested:
            self._exit_requested = False
            self._reset_state()

        self._update_dirs(candle)

        if self.Position != 0:
            if self._manage_position(candle):
                return

        if self._exit_requested:
            return

        if len(self._dir_queue) < self.CountBars:
            return

        if self.Position != 0:
            return

        close = float(candle.ClosePrice)
        pip = self._pip_value

        if self._up_count > self._down_count:
            self._entry_price = close
            self._sl_price = close - self.StopLossPips * pip if self.StopLossPips > 0 else None
            self._tp_price = close + self.TakeProfitPips * pip if self.TakeProfitPips > 0 else None
            self._long_trail = None
            self._short_trail = None
            self._exit_requested = False
            self.BuyMarket()
        elif self._down_count > self._up_count:
            self._entry_price = close
            self._sl_price = close + self.StopLossPips * pip if self.StopLossPips > 0 else None
            self._tp_price = close - self.TakeProfitPips * pip if self.TakeProfitPips > 0 else None
            self._short_trail = None
            self._long_trail = None
            self._exit_requested = False
            self.SellMarket()

    def _manage_position(self, candle):
        if self._exit_requested:
            return True

        entry = self._entry_price if self._entry_price is not None else float(candle.ClosePrice)
        current = float(candle.ClosePrice)
        pip = self._pip_value
        sec_target = self.SecondaryTakeProfitPips * pip
        trail_dist = self.TrailingStopPips * pip
        trail_step = self.TrailingStepPips * pip

        if self.Position > 0:
            if self._tp_price is not None and float(candle.HighPrice) >= self._tp_price:
                self._exit_requested = True
                self.SellMarket()
                return True
            if self._sl_price is not None and float(candle.LowPrice) <= self._sl_price:
                self._exit_requested = True
                self.SellMarket()
                return True
            if sec_target > 0 and current - entry >= sec_target:
                self._exit_requested = True
                self.SellMarket()
                return True
            if self.TrailingStopPips > 0:
                if current - entry > trail_dist + trail_step:
                    new_stop = current - trail_dist
                    if self._long_trail is None or new_stop > self._long_trail + trail_step:
                        self._long_trail = new_stop
                if self._long_trail is not None and float(candle.LowPrice) <= self._long_trail:
                    self._exit_requested = True
                    self.SellMarket()
                    return True

        elif self.Position < 0:
            if self._tp_price is not None and float(candle.LowPrice) <= self._tp_price:
                self._exit_requested = True
                self.BuyMarket()
                return True
            if self._sl_price is not None and float(candle.HighPrice) >= self._sl_price:
                self._exit_requested = True
                self.BuyMarket()
                return True
            if sec_target > 0 and entry - current >= sec_target:
                self._exit_requested = True
                self.BuyMarket()
                return True
            if self.TrailingStopPips > 0:
                if entry - current > trail_dist + trail_step:
                    new_stop = current + trail_dist
                    if self._short_trail is None or new_stop < self._short_trail - trail_step:
                        self._short_trail = new_stop
                if self._short_trail is not None and float(candle.HighPrice) >= self._short_trail:
                    self._exit_requested = True
                    self.BuyMarket()
                    return True

        return False

    def _update_dirs(self, candle):
        d = 0
        o = float(candle.OpenPrice)
        c = float(candle.ClosePrice)
        if o > c:
            d = 1
            self._up_count += 1
        elif o < c:
            d = -1
            self._down_count += 1

        self._dir_queue.append(d)
        while len(self._dir_queue) > self.CountBars:
            removed = self._dir_queue.pop(0)
            if removed > 0:
                self._up_count -= 1
            elif removed < 0:
                self._down_count -= 1

    def _reset_state(self):
        self._entry_price = None
        self._sl_price = None
        self._tp_price = None
        self._long_trail = None
        self._short_trail = None

    def OnReseted(self):
        super(get_rich_or_die_trying_gbp_strategy, self).OnReseted()
        self._dir_queue = []
        self._up_count = 0
        self._down_count = 0
        self._pip_value = 1.0
        self._exit_requested = False
        self._reset_state()

    def CreateClone(self):
        return get_rich_or_die_trying_gbp_strategy()
