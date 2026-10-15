import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class outside_bar_strategy(Strategy):
    """
    Outside bar strategy.
    A candle whose high is above the previous high and whose low is below the previous low is an outside bar, bullish when it closes
    above its open and bearish otherwise. An entry is placed inside the bar, EntryPercentage of its range back from the extreme in the
    signal direction. The stop sits StopLossOffset price steps beyond the opposite extreme and the target TpPercentage of the range
    from the entry. At PartialRR times the risk PartialExitPercent of the position is closed and the stop moves to breakeven.
    """

    def __init__(self):
        super(outside_bar_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._entry_percentage = self.Param("EntryPercentage", 0.5).SetRange(0.0, 1.0).SetDisplay("Entry %", "Fraction of the bar range the entry sits back from the extreme", "Trading")
        self._tp_percentage = self.Param("TpPercentage", 1.0).SetGreaterThanZero().SetDisplay("Take Profit %", "Take profit distance as a fraction of the bar range", "Risk")
        self._partial_rr = self.Param("PartialRR", 1.0).SetGreaterThanZero().SetDisplay("Partial RR", "Reward to risk ratio of the partial exit", "Risk")
        self._partial_exit_percent = self.Param("PartialExitPercent", 0.5).SetRange(0.0, 1.0).SetDisplay("Partial Exit %", "Fraction of the position closed at the partial exit", "Risk")
        self._stop_loss_offset = self.Param("StopLossOffset", 10.0).SetNotNegative().SetDisplay("Stop Offset", "Stop distance beyond the bar in price steps", "Risk")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_high = None
        self._prev_low = None
        self._pending_direction = 0
        self._pending_entry = Decimal(0)
        self._pending_stop = Decimal(0)
        self._pending_take = Decimal(0)
        self._entry_price = Decimal(0)
        self._stop_price = Decimal(0)
        self._take_price = Decimal(0)
        self._partial_price = Decimal(0)
        self._partial_done = False

    def OnReseted(self):
        super(outside_bar_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(outside_bar_strategy, self).OnStarted2(time)

        self._reset_state()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        prev_high = self._prev_high
        prev_low = self._prev_low
        self._prev_high = candle.HighPrice
        self._prev_low = candle.LowPrice

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position != 0:
            self._manage_position(candle)
            return

        if self._pending_direction != 0 and self._try_fill_pending(candle):
            return

        if prev_high is None or prev_low is None:
            return

        if candle.HighPrice <= prev_high or candle.LowPrice >= prev_low:
            return

        rng = candle.HighPrice - candle.LowPrice
        step = self.Security.PriceStep if self.Security is not None and self.Security.PriceStep is not None else Decimal(1)
        offset = Decimal(self._stop_loss_offset.Value) * step
        entry_pct = Decimal(self._entry_percentage.Value)
        tp_pct = Decimal(self._tp_percentage.Value)

        # A newer outside bar replaces any unfilled entry.
        if candle.ClosePrice > candle.OpenPrice:
            self._pending_direction = 1
            self._pending_entry = candle.HighPrice - rng * entry_pct
            self._pending_stop = candle.LowPrice - offset
            self._pending_take = self._pending_entry + rng * tp_pct
        else:
            self._pending_direction = -1
            self._pending_entry = candle.LowPrice + rng * entry_pct
            self._pending_stop = candle.HighPrice + offset
            self._pending_take = self._pending_entry - rng * tp_pct

    def _try_fill_pending(self, candle):
        if candle.LowPrice > self._pending_entry or candle.HighPrice < self._pending_entry:
            return False

        risk = abs(self._pending_entry - self._pending_stop)
        partial_rr = Decimal(self._partial_rr.Value)

        self._entry_price = self._pending_entry
        self._stop_price = self._pending_stop
        self._take_price = self._pending_take
        if self._pending_direction > 0:
            self._partial_price = self._entry_price + risk * partial_rr
        else:
            self._partial_price = self._entry_price - risk * partial_rr
        self._partial_done = False

        if self._pending_direction > 0:
            self.BuyMarket(self.Volume)
        else:
            self.SellMarket(self.Volume)

        self._pending_direction = 0
        return True

    def _manage_position(self, candle):
        exit_pct = Decimal(self._partial_exit_percent.Value)

        if self.Position > 0:
            if candle.LowPrice <= self._stop_price or candle.HighPrice >= self._take_price:
                self.SellMarket(self.Position)
                return

            if not self._partial_done and candle.HighPrice >= self._partial_price:
                self._partial_done = True
                self._stop_price = self._entry_price
                part = self.Position * exit_pct
                if part > 0:
                    self.SellMarket(min(part, self.Position))
        else:
            if candle.HighPrice >= self._stop_price or candle.LowPrice <= self._take_price:
                self.BuyMarket(-self.Position)
                return

            if not self._partial_done and candle.LowPrice <= self._partial_price:
                self._partial_done = True
                self._stop_price = self._entry_price
                part = -self.Position * exit_pct
                if part > 0:
                    self.BuyMarket(min(part, -self.Position))

    def CreateClone(self):
        return outside_bar_strategy()
