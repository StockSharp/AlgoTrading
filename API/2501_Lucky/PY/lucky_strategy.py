import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, Level1Fields
from StockSharp.Algo.Strategies import Strategy


class lucky_strategy(Strategy):
    """Breakout strategy reacting to fast bid/ask shifts, closes on profit or adverse move limits."""

    def __init__(self):
        super(lucky_strategy, self).__init__()

        self._shift_points = self.Param("ShiftPoints", 1) \
            .SetGreaterThanZero() \
            .SetDisplay("Shift points", "Minimum pip movement required to trigger a trade", "Trading")

        self._limit_points = self.Param("LimitPoints", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("Limit points", "Maximum adverse pip movement before closing", "Risk management")

        self._reverse = self.Param("Reverse", False) \
            .SetDisplay("Reverse mode", "Invert the direction of new trades", "Trading")

        self._entry_price = 0.0
        self._previous_ask = None
        self._previous_bid = None
        self._current_ask = None
        self._current_bid = None
        self._shift_threshold = 0.0
        self._limit_threshold = 0.0

    @property
    def ShiftPoints(self):
        return self._shift_points.Value

    @property
    def LimitPoints(self):
        return self._limit_points.Value

    @property
    def Reverse(self):
        return self._reverse.Value

    def _calculate_price_offset(self, points):
        if points <= 0:
            return 0.0
        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 0.0
        if step <= 0:
            return 0.0
        multiplier = self._get_pip_multiplier(step)
        return points * step * multiplier

    def _get_pip_multiplier(self, step):
        digits = 0
        temp = step
        while temp > 0 and temp < 1 and digits < 10:
            temp *= 10
            digits += 1
        return 10.0 if digits == 3 or digits == 5 else 1.0

    def OnStarted(self, time):
        super(lucky_strategy, self).OnStarted(time)

        self._shift_threshold = self._calculate_price_offset(self.ShiftPoints)
        self._limit_threshold = self._calculate_price_offset(self.LimitPoints)

        self.SubscribeLevel1() \
            .Bind(self.process_level1) \
            .Start()

    def process_level1(self, level1):
        changes = level1.Changes

        if changes.ContainsKey(Level1Fields.BestAskPrice):
            ask = float(changes[Level1Fields.BestAskPrice])

            if self._previous_ask is not None and self._shift_threshold > 0 and ask - self._previous_ask >= self._shift_threshold:
                if self.Reverse:
                    self._open_short(ask)
                else:
                    self._open_long(ask)

            self._previous_ask = ask
            self._current_ask = ask

        if changes.ContainsKey(Level1Fields.BestBidPrice):
            bid = float(changes[Level1Fields.BestBidPrice])

            if self._previous_bid is not None and self._shift_threshold > 0 and self._previous_bid - bid >= self._shift_threshold:
                if self.Reverse:
                    self._open_long(bid)
                else:
                    self._open_short(bid)

            self._previous_bid = bid
            self._current_bid = bid

        self._try_close()

    def _open_long(self, price):
        if not self.IsFormed:
            return
        vol = self.Volume
        if vol <= 0:
            return
        self.BuyMarket(vol)
        self._entry_price = price

    def _open_short(self, price):
        if not self.IsFormed:
            return
        vol = self.Volume
        if vol <= 0:
            return
        self.SellMarket(vol)
        self._entry_price = price

    def _try_close(self):
        if self.Position == 0:
            return

        avg = self._entry_price
        if avg <= 0:
            return

        if self.Position > 0:
            if self._current_bid is not None and self._current_bid > avg:
                self.SellMarket(self.Position)
            elif self._limit_threshold > 0 and self._current_ask is not None and avg - self._current_ask >= self._limit_threshold:
                self.SellMarket(self.Position)
        elif self.Position < 0:
            if self._current_ask is not None and self._current_ask < avg:
                self.BuyMarket(abs(self.Position))
            elif self._limit_threshold > 0 and self._current_bid is not None and self._current_bid - avg >= self._limit_threshold:
                self.BuyMarket(abs(self.Position))

    def OnReseted(self):
        super(lucky_strategy, self).OnReseted()
        self._previous_ask = None
        self._previous_bid = None
        self._current_ask = None
        self._current_bid = None
        self._entry_price = 0.0
        self._shift_threshold = 0.0
        self._limit_threshold = 0.0

    def CreateClone(self):
        return lucky_strategy()
