import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Sides, OrderStates, OrderTypes
from StockSharp.BusinessEntities import Order
from StockSharp.Algo.Strategies import Strategy


class farhad_crab_strategy(Strategy):
    """M1 EMA-typical and SMA-open scalping with daily SMMA safety exits."""

    def __init__(self):
        super(farhad_crab_strategy, self).__init__()
        self._daily_ma_period = self.Param("DailyMaPeriod", 55).SetGreaterThanZero()
        self._order_volume = self.Param("OrderVolume", 0.1).SetGreaterThanZero()
        self._long_take_profit_pips = self.Param("LongTakeProfitPips", 10.0).SetNotNegative()
        self._short_take_profit_pips = self.Param("ShortTakeProfitPips", 10.0).SetNotNegative()
        self._long_trailing_stop_pips = self.Param("LongTrailingStopPips", 8.0).SetNotNegative()
        self._short_trailing_stop_pips = self.Param("ShortTrailingStopPips", 8.0).SetNotNegative()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1)))
        self._clear_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _clear_state(self):
        self._typical_seed = []
        self._opens = []
        self._daily_seed = []
        self._ema_typical = None
        self._daily_smma = None
        self._prior_daily_close = None
        self._entry_price = None
        self._trailing_level = None
        self._best_price = 0.0
        self._pending_order = None
        self._entry_order = None

    def OnReseted(self):
        super(farhad_crab_strategy, self).OnReseted()
        self._clear_state()

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type),
                (self.Security, DataType.TimeFrame(TimeSpan.FromDays(1)))]

    def OnStarted2(self, time):
        super(farhad_crab_strategy, self).OnStarted2(time)
        self._clear_state()
        if float(self.Security.PriceStep or 0) <= 0:
            raise ValueError("Farhad Crab requires a positive instrument PriceStep.")
        self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromDays(1))).Bind(self._process_daily).Start()
        self.SubscribeCandles(self.candle_type).Bind(self._process_candle).Start()

    def _pip(self):
        step = float(self.Security.PriceStep)
        return step * (10 if self.Security.Decimals in (3, 5) else 1)

    def _pending(self):
        return (self._pending_order is not None and
                self._pending_order.State not in (OrderStates.Done, OrderStates.Failed))

    def _process_daily(self, candle):
        if candle.State != CandleStates.Finished:
            return

        prev_smma = self._daily_smma
        prev_close = self._prior_daily_close
        close = float(candle.ClosePrice)
        if self._daily_smma is None:
            self._daily_seed.append(close)
            if len(self._daily_seed) == self._daily_ma_period.Value:
                self._daily_smma = sum(self._daily_seed) / len(self._daily_seed)
        else:
            n = self._daily_ma_period.Value
            self._daily_smma = (self._daily_smma * (n - 1) + close) / n
        self._prior_daily_close = close

        if self._pending() or self.Position == 0 or prev_smma is None or prev_close is None:
            return
        if (self.Position > 0 and prev_smma < prev_close and self._daily_smma > close or
                self.Position < 0 and prev_smma > prev_close and self._daily_smma < close):
            self._exit("Farhad Crab daily safety exit")

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        typical = (float(candle.HighPrice) + float(candle.LowPrice) +
                   float(candle.ClosePrice)) / 3.0
        if self._ema_typical is None:
            self._typical_seed.append(typical)
            if len(self._typical_seed) == 9:
                self._ema_typical = sum(self._typical_seed) / 9
        else:
            self._ema_typical = (self._ema_typical * 8 + typical * 2) / 10.0

        self._opens.append(float(candle.OpenPrice))
        if len(self._opens) > 9:
            self._opens.pop(0)

        if self._pending():
            return
        if self.Position != 0:
            self._handle_risk(candle)
            return
        if self._ema_typical is None or len(self._opens) < 9:
            return

        if float(candle.LowPrice) > self._ema_typical:
            self._enter(Sides.Buy)
        elif float(candle.HighPrice) < sum(self._opens) / 9.0:
            self._enter(Sides.Sell)

    def _enter(self, side):
        volume = float(self._order_volume.Value)
        step = float(self.Security.VolumeStep or 0)
        if step > 0:
            volume = math.floor(volume / step) * step
        minimum = float(self.Security.MinVolume or 0)
        maximum = float(self.Security.MaxVolume or 0)
        if minimum > 0:
            volume = max(volume, minimum)
        if maximum > 0:
            volume = min(volume, maximum)
        if volume > 0:
            self._submit(side, volume, "Farhad Crab entry")

    def _handle_risk(self, candle):
        if self._entry_price is None:
            return

        entry = self._entry_price
        long_position = self.Position > 0
        pip = self._pip()
        target = float(self._long_take_profit_pips.Value if long_position
                       else self._short_take_profit_pips.Value) * pip
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        if target > 0 and (high >= entry + target if long_position
                           else low <= entry - target):
            self._exit("Farhad Crab take profit")
            return

        level = self._trailing_level
        if level is not None and (low <= level if long_position else high >= level):
            self._exit("Farhad Crab trailing stop")
            return

        distance = float(self._long_trailing_stop_pips.Value if long_position
                         else self._short_trailing_stop_pips.Value) * pip
        if distance <= 0:
            return
        self._best_price = (max(self._best_price, high) if long_position
                            else min(self._best_price, low))
        move = self._best_price - entry if long_position else entry - self._best_price
        if move < distance:
            return

        next_stop = self._best_price - distance if long_position else self._best_price + distance
        if level is None or (next_stop > level if long_position else next_stop < level):
            self._trailing_level = next_stop

    def _exit(self, reason):
        if not self._pending() and self.Position != 0:
            side = Sides.Sell if self.Position > 0 else Sides.Buy
            self._submit(side, abs(float(self.Position)), reason)

    def _submit(self, side, volume, comment):
        order = Order()
        order.Security = self.Security
        order.Portfolio = self.Portfolio
        order.Type = OrderTypes.Market
        order.Side = side
        order.Volume = Decimal(volume)
        order.Comment = comment
        self._pending_order = order
        if comment == "Farhad Crab entry":
            self._entry_order = order
        self.RegisterOrder(order)

    def OnOwnTradeReceived(self, trade):
        super(farhad_crab_strategy, self).OnOwnTradeReceived(trade)
        if trade is not None and trade.Order == self._entry_order and trade.Trade is not None:
            self._entry_price = float(trade.Trade.Price)
            self._best_price = self._entry_price
            self._trailing_level = None
        elif self.Position == 0:
            self._entry_price = None
            self._best_price = 0.0
            self._trailing_level = None

    def CreateClone(self):
        return farhad_crab_strategy()
