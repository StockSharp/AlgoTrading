import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Decimal, Math as CMath

from StockSharp.Messages import DataType, CandleStates, Sides, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class mt45_strategy(Strategy):
    def __init__(self):
        super(mt45_strategy, self).__init__()

        self._stop_points = self.Param("StopPoints", Decimal(600))
        self._take_points = self.Param("TakePoints", Decimal(700))
        self._base_volume = self.Param("BaseVolume", Decimal(1))
        self._multiplier = self.Param("MartingaleMultiplier", Decimal(2))
        self._max_volume = self.Param("MaxVolume", Decimal(10))
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))

        self._next_side = Sides.Buy
        self._pending_side = None
        self._entry_pending = False
        self._entry_price = Decimal(0)
        self._last_trade_volume = Decimal(1)
        self._next_volume = Decimal(1)
        self._prev_position = Decimal(0)
        self._point_value = Decimal(0)
        self.Volume = Decimal(1)

    @property
    def StopPoints(self):
        return self._stop_points.Value

    @property
    def TakePoints(self):
        return self._take_points.Value

    @property
    def BaseVolume(self):
        return self._base_volume.Value

    @property
    def MartingaleMultiplier(self):
        return self._multiplier.Value

    @property
    def MaxVolume(self):
        return self._max_volume.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(mt45_strategy, self).OnStarted(time)

        sec = self.Security
        self._point_value = sec.PriceStep if sec is not None and sec.PriceStep is not None else Decimal(1)
        self.Volume = self.BaseVolume

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

        tp = self._create_price_unit(self.TakePoints)
        sl = self._create_price_unit(self.StopPoints)
        if tp is not None and sl is not None:
            self.StartProtection(tp, sl)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self._entry_pending or self.Position != Decimal(0):
            return

        volume = self._next_volume
        if volume <= Decimal(0):
            return

        side = self._next_side
        self._pending_side = side

        if side == Sides.Buy:
            self.BuyMarket()
        else:
            self.SellMarket()

        self._entry_pending = True

    def OnOwnTradeReceived(self, trade):
        super(mt45_strategy, self).OnOwnTradeReceived(trade)

        if trade.Order is None or trade.Trade is None:
            return

        new_position = self.Position
        previous_position = self._prev_position
        self._prev_position = new_position

        if previous_position == Decimal(0) and new_position != Decimal(0):
            self._entry_price = trade.Trade.Price
            self._last_trade_volume = CMath.Abs(new_position)
            self._entry_pending = False

            if self._pending_side is not None:
                self._next_side = Sides.Sell if self._pending_side == Sides.Buy else Sides.Buy
                self._pending_side = None

        elif previous_position != Decimal(0) and new_position == Decimal(0):
            direction = Sides.Buy if previous_position > Decimal(0) else Sides.Sell
            self._update_next_volume(direction, trade.Trade.Price, CMath.Abs(previous_position))
            self._entry_price = Decimal(0)
            self._entry_pending = False

    def _update_next_volume(self, direction, exit_price, volume):
        if volume <= Decimal(0):
            return

        if direction == Sides.Buy:
            profit = (exit_price - self._entry_price) * volume
        else:
            profit = (self._entry_price - exit_price) * volume

        if profit < Decimal(0):
            scaled = self._last_trade_volume * self.MartingaleMultiplier
            self._next_volume = self.BaseVolume if scaled > self.MaxVolume else scaled
        else:
            self._next_volume = self.BaseVolume

        self.Volume = self._next_volume

    def _create_price_unit(self, points):
        if points <= Decimal(0) or self._point_value <= Decimal(0):
            return None
        return Unit(points * self._point_value, UnitTypes.Absolute)

    def OnReseted(self):
        super(mt45_strategy, self).OnReseted()
        self._next_side = Sides.Buy
        self._pending_side = None
        self._entry_pending = False
        self._entry_price = Decimal(0)
        self._last_trade_volume = self.BaseVolume
        self._next_volume = self.BaseVolume
        self._prev_position = Decimal(0)
        self._point_value = Decimal(0)
        self.Volume = self.BaseVolume

    def CreateClone(self):
        return mt45_strategy()
