import clr

clr.AddReference("Ecng.Common")
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import ArgumentException, Decimal, TimeSpan, TimeZoneInfo
from Ecng.Common import MathHelper, TimeHelper
from StockSharp.Messages import CandleStates, DataType, OrderStates, OrderTypes, Sides
from StockSharp.BusinessEntities import Order
from StockSharp.Algo.Strategies import Strategy


class premarket_gap_momo_trader_strategy(Strategy):
    def __init__(self):
        super(premarket_gap_momo_trader_strategy, self).__init__()
        self._min_gain_pct = self.Param("MinGainPct", Decimal(5)).SetRange(Decimal.Zero, Decimal.MaxValue) \
            .SetDisplay("Minimum Gain Percent", "Inclusive close gain over the preceding finished candle", "Entry")
        self._min_volume = self.Param("MinVolume", Decimal(15000)).SetRange(Decimal.Zero, Decimal.MaxValue) \
            .SetDisplay("Minimum Candle Volume", "Strict lower bound for candle volume", "Entry")
        self._use_session = self.Param("UseSession", True) \
            .SetDisplay("Use Session", "Restrict entries to the New York premarket session", "Session")
        self._session_start = self.Param("SessionStart", TimeSpan.FromHours(4)) \
            .SetDisplay("Session Start", "Inclusive New York entry time", "Session")
        self._session_end = self.Param("SessionEnd", TimeSpan.FromMinutes(570)) \
            .SetDisplay("Session End", "Exclusive New York entry time", "Session")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Finished candles supplying price, body and volume", "General")
        self._reset_state()
        self.OrderRegistering += self._observe_order

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def _reset_state(self):
        self._previous_close = self._previous_volume = Decimal.Zero
        self._is_initialized = False
        self._last_entry_date = None
        self._pending_order = None

    def OnReseted(self):
        super(premarket_gap_momo_trader_strategy, self).OnReseted()
        self._reset_state()

    def _observe_order(self, order):
        self._pending_order = order

    def OnStarted2(self, time):
        super(premarket_gap_momo_trader_strategy, self).OnStarted2(time)
        if self._min_gain_pct.Value < 0 or self._min_volume.Value < 0 \
                or self._session_start.Value < TimeSpan.Zero or self._session_end.Value > TimeSpan.FromDays(1) \
                or self._session_start.Value >= self._session_end.Value:
            raise ArgumentException("Gain and volume thresholds cannot be negative; session times must be ordered within one New York day.")
        self._reset_state()
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _submit(self, side, volume):
        order = Order()
        order.Security = self.Security
        order.Portfolio = self.Portfolio
        order.Type = OrderTypes.Market
        order.Side = side
        order.Volume = volume
        order.Comment = "Premarket body momentum" if side == Sides.Buy else "Premarket momentum exit"
        self.RegisterOrder(order)

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return
        previous_close = self._previous_close
        previous_volume = self._previous_volume
        self._previous_close = candle.ClosePrice
        self._previous_volume = candle.TotalVolume
        if not self._is_initialized:
            self._is_initialized = True
            return
        pending = self._pending_order
        if not self.IsFormedAndOnlineAndAllowTrading() \
                or pending is not None and pending.State != OrderStates.Done and pending.State != OrderStates.Failed:
            return
        if self.Position > 0:
            if candle.ClosePrice < candle.OpenPrice or candle.TotalVolume <= previous_volume:
                self._submit(Sides.Sell, self.Position)
            return
        if self.Position != 0 or previous_close <= 0 or candle.ClosePrice <= candle.OpenPrice \
                or candle.TotalVolume <= self._min_volume.Value or candle.HighPrice <= candle.LowPrice \
                or Decimal(100) * (candle.ClosePrice - previous_close) < self._min_gain_pct.Value * previous_close:
            return
        body_percent = Decimal(100) * (candle.ClosePrice - candle.OpenPrice) / (candle.HighPrice - candle.LowPrice)
        fraction = Decimal.One if body_percent >= 90 else Decimal.One / Decimal(2) if body_percent >= 85 \
            else Decimal.One / Decimal(4) if body_percent >= 75 else Decimal.Zero
        if fraction == 0:
            return
        local = TimeZoneInfo.ConvertTimeFromUtc(self.CurrentTime, TimeHelper.Est)
        if self._last_entry_date == local.Date or self._use_session.Value \
                and (local.TimeOfDay < self._session_start.Value or local.TimeOfDay >= self._session_end.Value):
            return
        volume = self.Volume * fraction
        step = self.Security.VolumeStep
        if step is not None and step > 0:
            volume = MathHelper.Floor(volume, step)
        minimum = self.Security.MinVolume
        maximum = self.Security.MaxVolume
        if volume <= 0 or minimum is not None and volume < minimum \
                or maximum is not None and maximum > 0 and volume > maximum:
            return
        self._last_entry_date = local.Date
        self._submit(Sides.Buy, volume)

    def CreateClone(self):
        return premarket_gap_momo_trader_strategy()
