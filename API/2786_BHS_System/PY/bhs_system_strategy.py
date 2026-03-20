import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import KaufmanAdaptiveMovingAverage
from StockSharp.Algo.Strategies import Strategy


class bhs_system_strategy(Strategy):

    def __init__(self):
        super(bhs_system_strategy, self).__init__()
        self._order_volume = self.Param("OrderVolume", 0.1)
        self._stop_loss_buy_points = self.Param("StopLossBuyPoints", 300)
        self._stop_loss_sell_points = self.Param("StopLossSellPoints", 300)
        self._trailing_stop_buy_points = self.Param("TrailingStopBuyPoints", 100)
        self._trailing_stop_sell_points = self.Param("TrailingStopSellPoints", 100)
        self._trailing_step_points = self.Param("TrailingStepPoints", 10)
        self._round_step_points = self.Param("RoundStepPoints", 2000)
        self._expiration_hours = self.Param("ExpirationHours", 1.0)
        self._ama_length = self.Param("AmaLength", 15)
        self._ama_fast_period = self.Param("AmaFastPeriod", 2)
        self._ama_slow_period = self.Param("AmaSlowPeriod", 30)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))

        self._previous_ama = 0.0
        self._has_previous_ama = False
        self._buy_stop_level = None
        self._sell_stop_level = None
        self._buy_order_time = None
        self._sell_order_time = None
        self._entry_price = 0.0
        self._highest_since_entry = 0.0
        self._lowest_since_entry = 0.0

    @property
    def OrderVolume(self):
        return self._order_volume.Value

    @property
    def StopLossBuyPoints(self):
        return self._stop_loss_buy_points.Value

    @property
    def StopLossSellPoints(self):
        return self._stop_loss_sell_points.Value

    @property
    def TrailingStopBuyPoints(self):
        return self._trailing_stop_buy_points.Value

    @property
    def TrailingStopSellPoints(self):
        return self._trailing_stop_sell_points.Value

    @property
    def TrailingStepPoints(self):
        return self._trailing_step_points.Value

    @property
    def RoundStepPoints(self):
        return self._round_step_points.Value

    @property
    def ExpirationHours(self):
        return self._expiration_hours.Value

    @property
    def AmaLength(self):
        return self._ama_length.Value

    @property
    def AmaFastPeriod(self):
        return self._ama_fast_period.Value

    @property
    def AmaSlowPeriod(self):
        return self._ama_slow_period.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(bhs_system_strategy, self).OnStarted(time)

        ama = KaufmanAdaptiveMovingAverage()
        ama.Length = self.AmaLength
        ama.FastSCPeriod = self.AmaFastPeriod
        ama.SlowSCPeriod = self.AmaSlowPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ama, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ama)
            self.DrawOwnTrades(area)

    def OnOwnTradeReceived(self, trade):
        super(bhs_system_strategy, self).OnOwnTradeReceived(trade)
        if trade is None or trade.Trade is None:
            return
        pos = float(self.Position)
        if pos != 0 and self._entry_price == 0.0:
            self._entry_price = float(trade.Trade.Price)
            self._highest_since_entry = float(trade.Trade.Price)
            self._lowest_since_entry = float(trade.Trade.Price)
        if pos == 0:
            self._entry_price = 0.0
            self._highest_since_entry = 0.0
            self._lowest_since_entry = 0.0

    def _process_candle(self, candle, ama_value):
        if candle.State != CandleStates.Finished:
            return

        ama_val = float(ama_value)

        if not self._has_previous_ama:
            self._previous_ama = ama_val
            self._has_previous_ama = True
            return

        self._check_stop_loss(candle)
        self._check_trailing_stop(candle)
        self._cancel_expired_levels()
        self._check_pending_triggers(candle)

        price = float(candle.ClosePrice)
        price_ceil, price_floor = self._calculate_round_levels(price)

        pos = float(self.Position)
        if pos > 0 and price > self._highest_since_entry:
            self._highest_since_entry = price
        if pos < 0 and (self._lowest_since_entry == 0 or price < self._lowest_since_entry):
            self._lowest_since_entry = price

        has_pending = self._buy_stop_level is not None or self._sell_stop_level is not None

        if float(self.Position) == 0 and not has_pending:
            if price > self._previous_ama:
                self._buy_stop_level = price_ceil
                self._buy_order_time = candle.OpenTime
            elif price < self._previous_ama:
                self._sell_stop_level = price_floor
                self._sell_order_time = candle.OpenTime

        self._previous_ama = ama_val

    def _check_stop_loss(self, candle):
        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 1.0
        pos = float(self.Position)

        if pos > 0 and self.StopLossBuyPoints > 0:
            stop_price = self._entry_price - self.StopLossBuyPoints * step
            if float(candle.LowPrice) <= stop_price:
                self.SellMarket(abs(pos))
                return

        if pos < 0 and self.StopLossSellPoints > 0:
            stop_price = self._entry_price + self.StopLossSellPoints * step
            if float(candle.HighPrice) >= stop_price:
                self.BuyMarket(abs(pos))

    def _check_trailing_stop(self, candle):
        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 1.0
        pos = float(self.Position)

        if pos > 0 and self.TrailingStopBuyPoints > 0:
            trailing_dist = self.TrailingStopBuyPoints * step
            trailing_step = self.TrailingStepPoints * step
            profit = self._highest_since_entry - self._entry_price
            if profit > trailing_dist + trailing_step:
                trail_stop = self._highest_since_entry - trailing_dist
                if float(candle.LowPrice) <= trail_stop:
                    self.SellMarket(abs(pos))
                    return

        if pos < 0 and self.TrailingStopSellPoints > 0 and self._lowest_since_entry > 0:
            trailing_dist = self.TrailingStopSellPoints * step
            trailing_step = self.TrailingStepPoints * step
            profit = self._entry_price - self._lowest_since_entry
            if profit > trailing_dist + trailing_step:
                trail_stop = self._lowest_since_entry + trailing_dist
                if float(candle.HighPrice) >= trail_stop:
                    self.BuyMarket(abs(pos))

    def _check_pending_triggers(self, candle):
        pos = float(self.Position)
        if self._buy_stop_level is not None and pos <= 0 and float(candle.HighPrice) >= self._buy_stop_level:
            if pos < 0:
                self.BuyMarket(abs(pos))
            self.BuyMarket(float(self.OrderVolume))
            self._buy_stop_level = None
            self._buy_order_time = None
            self._sell_stop_level = None
            self._sell_order_time = None

        pos = float(self.Position)
        if self._sell_stop_level is not None and pos >= 0 and float(candle.LowPrice) <= self._sell_stop_level:
            if pos > 0:
                self.SellMarket(abs(pos))
            self.SellMarket(float(self.OrderVolume))
            self._sell_stop_level = None
            self._sell_order_time = None
            self._buy_stop_level = None
            self._buy_order_time = None

    def _cancel_expired_levels(self):
        if float(self.ExpirationHours) <= 0:
            return

        expiration = TimeSpan.FromHours(float(self.ExpirationHours))
        now = self.CurrentTime

        if self._buy_order_time is not None and now - self._buy_order_time >= expiration:
            self._buy_stop_level = None
            self._buy_order_time = None

        if self._sell_order_time is not None and now - self._sell_order_time >= expiration:
            self._sell_stop_level = None
            self._sell_order_time = None

    def _calculate_round_levels(self, price):
        sec = self.Security
        point = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 1.0
        step_points = self.RoundStepPoints

        if point <= 0 or step_points <= 0:
            return (price, price)

        step = step_points * point
        if step <= 0:
            return (price, price)

        ratio = price / step
        rounded_index = round(ratio)
        price_round = rounded_index * step

        import math
        ceil_index = math.ceil((price_round + step / 2.0) / step)
        floor_index = math.floor((price_round - step / 2.0) / step)

        price_ceil = ceil_index * step
        price_floor = floor_index * step

        return (price_ceil, price_floor)

    def OnReseted(self):
        super(bhs_system_strategy, self).OnReseted()
        self._previous_ama = 0.0
        self._has_previous_ama = False
        self._buy_stop_level = None
        self._sell_stop_level = None
        self._buy_order_time = None
        self._sell_order_time = None
        self._entry_price = 0.0
        self._highest_since_entry = 0.0
        self._lowest_since_entry = 0.0

    def CreateClone(self):
        return bhs_system_strategy()
