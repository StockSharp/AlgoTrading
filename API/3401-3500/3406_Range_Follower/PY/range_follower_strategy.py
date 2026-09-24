import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Level1Fields, Sides
from StockSharp.Algo.Strategies import Strategy


class range_follower_strategy(Strategy):
    def __init__(self):
        super(range_follower_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15)))
        self._trigger_percent = self.Param("TriggerPercent", 60.0).SetRange(10.0, 90.0)

        self._daily_ranges = []
        self._previous_daily_close = None
        self._daily_atr = None
        self._best_bid = None
        self._best_ask = None
        self._session_date = None
        self._session_high = 0.0
        self._session_low = 0.0
        self._traded_today = False
        self._skip_today = False
        self._stop_price = None
        self._take_price = None

    def GetWorkingSecurities(self):
        return [
            (self.Security, self._candle_type.Value),
            (self.Security, DataType.TimeFrame(TimeSpan.FromDays(1))),
            (self.Security, DataType.Level1),
        ]

    def OnReseted(self):
        super(range_follower_strategy, self).OnReseted()
        self._daily_ranges = []
        self._previous_daily_close = None
        self._daily_atr = None
        self._best_bid = None
        self._best_ask = None
        self._reset_session(None)

    def OnStarted2(self, time):
        super(range_follower_strategy, self).OnStarted2(time)
        self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromDays(1))).Bind(self._process_daily).Start()
        self.SubscribeLevel1().Bind(self._process_level1).Start()
        self.SubscribeCandles(self._candle_type.Value).Bind(self._process_working).Start()

    def _process_daily(self, candle):
        if candle.State != CandleStates.Finished:
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        if self._previous_daily_close is None:
            tr = high - low
        else:
            tr = max(high - low, abs(high - self._previous_daily_close), abs(low - self._previous_daily_close))

        self._daily_ranges.append(tr)
        if len(self._daily_ranges) > 20:
            del self._daily_ranges[0]

        if len(self._daily_ranges) == 20:
            self._daily_atr = sum(self._daily_ranges) / 20.0

        self._previous_daily_close = float(candle.ClosePrice)

    def _process_level1(self, message):
        bid = message.TryGetDecimal(Level1Fields.BestBidPrice)
        ask = message.TryGetDecimal(Level1Fields.BestAskPrice)
        if bid is not None and float(bid) > 0:
            self._best_bid = float(bid)
        if ask is not None and float(ask) > 0:
            self._best_ask = float(ask)
        self._evaluate_quote()

    def _process_working(self, candle):
        if candle.State != CandleStates.Finished:
            return

        date = candle.OpenTime.Date
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        if self._session_date != date:
            if self._session_date is not None and self.Position != 0:
                self._flatten()
            self._reset_session(date)
            self._session_high = high
            self._session_low = low
        else:
            self._session_high = max(self._session_high, high)
            self._session_low = min(self._session_low, low)

        if self._daily_atr is not None:
            trigger = self._daily_atr * float(self._trigger_percent.Value) / 100.0
            if not self._traded_today and self._session_high - self._session_low >= trigger:
                self._skip_today = True

        if self.Position != 0:
            self._apply_protection(high, low)

        self._evaluate_quote()

    def _evaluate_quote(self):
        if (self._daily_atr is None or self._session_date is None or self._traded_today or
                self._skip_today or self.Position != 0 or self._best_bid is None or self._best_ask is None or
                self._session_low == 0 or self._session_high == 0):
            return

        trigger = self._daily_atr * float(self._trigger_percent.Value) / 100.0
        residual = self._daily_atr - trigger
        long_distance = self._best_bid - self._session_low
        short_distance = self._session_high - self._best_ask

        if long_distance < trigger and short_distance < trigger:
            return

        if long_distance >= short_distance:
            self._enter(Sides.Buy, self._best_ask, trigger, residual)
        else:
            self._enter(Sides.Sell, self._best_bid, trigger, residual)

    def _enter(self, side, price, trigger, residual):
        if side == Sides.Buy:
            self.BuyMarket()
            self._stop_price = price - trigger
            self._take_price = price + residual
        else:
            self.SellMarket()
            self._stop_price = price + trigger
            self._take_price = price - residual

        self._traded_today = True

    def _apply_protection(self, high, low):
        if self.Position > 0 and ((self._stop_price is not None and low <= self._stop_price) or
                                  (self._take_price is not None and high >= self._take_price)):
            self._flatten()
        elif self.Position < 0 and ((self._stop_price is not None and high >= self._stop_price) or
                                    (self._take_price is not None and low <= self._take_price)):
            self._flatten()

    def _flatten(self):
        if self.Position > 0:
            self.SellMarket(Math.Abs(self.Position))
        elif self.Position < 0:
            self.BuyMarket(Math.Abs(self.Position))
        self._stop_price = None
        self._take_price = None

    def _reset_session(self, date):
        self._session_date = date
        self._session_high = 0.0
        self._session_low = 0.0
        self._traded_today = False
        self._skip_today = False
        self._stop_price = None
        self._take_price = None

    def CreateClone(self):
        return range_follower_strategy()
