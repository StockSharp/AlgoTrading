import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Level1Fields, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class five_mins_envelopes_strategy(Strategy):
    def __init__(self):
        super(five_mins_envelopes_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._trade_volume = self.Param("TradeVolume", 1.0)
        self._distance_points = self.Param("DistancePoints", 140.0)
        self._envelope_period = self.Param("EnvelopePeriod", 3)
        self._envelope_deviation_percent = self.Param("EnvelopeDeviationPercent", 0.05)
        self._stop_loss_points = self.Param("StopLossPoints", 250.0)
        self._take_profit_points = self.Param("TakeProfitPoints", 0.0)
        self._trailing_stop_points = self.Param("TrailingStopPoints", 120.0)
        self._max_spread_points = self.Param("MaxSpreadPoints", 25.0)
        self._reset_runtime()

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def TradeVolume(self):
        return self._trade_volume.Value

    @TradeVolume.setter
    def TradeVolume(self, value):
        self._trade_volume.Value = value

    @property
    def DistancePoints(self):
        return self._distance_points.Value

    @DistancePoints.setter
    def DistancePoints(self, value):
        self._distance_points.Value = value

    @property
    def EnvelopePeriod(self):
        return self._envelope_period.Value

    @EnvelopePeriod.setter
    def EnvelopePeriod(self, value):
        self._envelope_period.Value = value

    @property
    def EnvelopeDeviationPercent(self):
        return self._envelope_deviation_percent.Value

    @EnvelopeDeviationPercent.setter
    def EnvelopeDeviationPercent(self, value):
        self._envelope_deviation_percent.Value = value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @StopLossPoints.setter
    def StopLossPoints(self, value):
        self._stop_loss_points.Value = value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @TakeProfitPoints.setter
    def TakeProfitPoints(self, value):
        self._take_profit_points.Value = value

    @property
    def TrailingStopPoints(self):
        return self._trailing_stop_points.Value

    @TrailingStopPoints.setter
    def TrailingStopPoints(self, value):
        self._trailing_stop_points.Value = value

    @property
    def MaxSpreadPoints(self):
        return self._max_spread_points.Value

    @MaxSpreadPoints.setter
    def MaxSpreadPoints(self, value):
        self._max_spread_points.Value = value

    def _reset_runtime(self):
        self._median_prices = []
        self._previous_low = None
        self._previous_high = None
        self._previous_lower_band = None
        self._previous_upper_band = None
        self._best_bid = None
        self._best_ask = None
        self._entry_price = None
        self._trailing_stop = None
        self._entry_requested = False
        self._exit_requested = False

    def OnReseted(self):
        super(five_mins_envelopes_strategy, self).OnReseted()
        self._reset_runtime()

    def _point(self):
        if self.Security is None or self.Security.PriceStep is None:
            raise ValueError("Positive Security.PriceStep is required.")
        point = float(self.Security.PriceStep)
        if point <= 0:
            raise ValueError("Positive Security.PriceStep is required.")
        return point

    def OnStarted2(self, time):
        super(five_mins_envelopes_strategy, self).OnStarted2(time)
        self._reset_runtime()

        point = self._point()
        stop = Unit(Decimal(self.StopLossPoints * point), UnitTypes.Absolute) if self.StopLossPoints > 0 else Unit()
        take = Unit(Decimal(self.TakeProfitPoints * point), UnitTypes.Absolute) if self.TakeProfitPoints > 0 else Unit()
        self.StartProtection(take, stop, useMarketOrders=True, isLocalStop=True)

        self.SubscribeCandles(self.CandleType).Bind(self._process_candle).Start()
        self.SubscribeLevel1().Bind(self._process_level1).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._median_prices.append((float(candle.HighPrice) + float(candle.LowPrice)) / 2.0)
        while len(self._median_prices) > self.EnvelopePeriod:
            self._median_prices.pop(0)

        if len(self._median_prices) < self.EnvelopePeriod:
            return

        weights = list(range(1, self.EnvelopePeriod + 1))
        middle = sum(value * weight for value, weight in zip(self._median_prices, weights)) / sum(weights)
        deviation = float(self.EnvelopeDeviationPercent) / 100.0
        self._previous_low = float(candle.LowPrice)
        self._previous_high = float(candle.HighPrice)
        self._previous_lower_band = middle * (1.0 - deviation)
        self._previous_upper_band = middle * (1.0 + deviation)

    def _process_level1(self, message):
        bid = message.TryGetDecimal(Level1Fields.BestBidPrice)
        ask = message.TryGetDecimal(Level1Fields.BestAskPrice)
        if bid is not None and bid > 0:
            self._best_bid = float(bid)
        if ask is not None and ask > 0:
            self._best_ask = float(ask)

        current_bid = self._best_bid
        current_ask = self._best_ask
        if current_bid is None or current_ask is None or current_ask < current_bid:
            return

        if self.Position != 0:
            self._apply_trailing()
            return

        if (self._entry_requested or self._exit_requested or self._previous_low is None
                or self._previous_high is None or self._previous_lower_band is None
                or self._previous_upper_band is None or self.TradeVolume <= 0):
            return

        point = self._point()
        if self.MaxSpreadPoints > 0 and current_ask - current_bid >= self.MaxSpreadPoints * point:
            return

        distance = float(self.DistancePoints) * point
        volume = Decimal(float(self.TradeVolume))
        if (self._previous_low < self._previous_lower_band - distance
                and current_bid < self._previous_lower_band - distance):
            self._entry_requested = True
            self.BuyMarket(volume)
        elif (self._previous_high > self._previous_upper_band + distance
              and current_bid > self._previous_upper_band + distance):
            self._entry_requested = True
            self.SellMarket(volume)

    def _apply_trailing(self):
        if self._exit_requested or self._entry_price is None or self.TrailingStopPoints <= 0:
            return

        price = self._best_bid if self.Position > 0 else self._best_ask
        if price is None:
            return

        gap = float(self.TrailingStopPoints) * self._point()
        if self.Position > 0:
            if price >= self._entry_price + gap:
                candidate = price - gap
                if self._trailing_stop is None or candidate > self._trailing_stop:
                    self._trailing_stop = candidate
            if self._trailing_stop is not None and price <= self._trailing_stop:
                self._request_exit()
        else:
            if price <= self._entry_price - gap:
                candidate = price + gap
                if self._trailing_stop is None or candidate < self._trailing_stop:
                    self._trailing_stop = candidate
            if self._trailing_stop is not None and price >= self._trailing_stop:
                self._request_exit()

    def _request_exit(self):
        self._exit_requested = True
        self.ClosePosition()

    def OnOwnTradeReceived(self, trade):
        super(five_mins_envelopes_strategy, self).OnOwnTradeReceived(trade)
        if self.Position == 0:
            self._entry_price = None
            self._trailing_stop = None
            self._entry_requested = False
            self._exit_requested = False
        elif self._entry_price is None:
            self._entry_price = float(trade.Trade.Price)

    def CreateClone(self):
        return five_mins_envelopes_strategy()
