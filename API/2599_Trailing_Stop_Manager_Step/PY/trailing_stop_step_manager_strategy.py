import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, Level1Fields
from StockSharp.Algo.Strategies import Strategy


class trailing_stop_step_manager_strategy(Strategy):
    """Manages trailing stops for existing positions using Level1 bid/ask data."""

    def __init__(self):
        super(trailing_stop_step_manager_strategy, self).__init__()

        self._stop_loss_points = self.Param("StopLossPoints", 1000.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop-loss distance", "Distance from market price to the stop in price steps", "Risk Management")

        self._trailing_start_points = self.Param("TrailingStartPoints", 1000.0) \
            .SetNotNegative() \
            .SetDisplay("Trailing activation", "Profit distance in price steps required to enable trailing", "Risk Management")

        self._trailing_step_points = self.Param("TrailingStepPoints", 200.0) \
            .SetNotNegative() \
            .SetDisplay("Trailing step", "Minimum improvement in price steps before moving the stop again", "Risk Management")

        self._price_deviation_points = self.Param("PriceDeviationPoints", 10.0) \
            .SetNotNegative() \
            .SetDisplay("Price deviation", "Reserved parameter for compatibility", "Execution")

        self._long_stop = None
        self._short_stop = None
        self._last_ask = None
        self._last_bid = None
        self._previous_position = 0.0
        self._entry_price = 0.0

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @property
    def TrailingStartPoints(self):
        return self._trailing_start_points.Value

    @property
    def TrailingStepPoints(self):
        return self._trailing_step_points.Value

    @property
    def PriceDeviationPoints(self):
        return self._price_deviation_points.Value

    def OnStarted(self, time):
        super(trailing_stop_step_manager_strategy, self).OnStarted(time)

        self.SubscribeLevel1() \
            .Bind(self.process_level1) \
            .Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawOwnTrades(area)

    def process_level1(self, msg):
        changes = msg.Changes

        if changes.ContainsKey(Level1Fields.BestAskPrice):
            self._last_ask = float(changes[Level1Fields.BestAskPrice])

        if changes.ContainsKey(Level1Fields.BestBidPrice):
            self._last_bid = float(changes[Level1Fields.BestBidPrice])

        self._update_trailing()

    def _update_trailing(self):
        sec = self.Security
        if sec is None or sec.PriceStep is None or float(sec.PriceStep) <= 0:
            return

        step = float(sec.PriceStep)
        start_dist = float(self.TrailingStartPoints) * step
        step_dist = float(self.TrailingStepPoints) * step

        if self.Position > 0:
            if self._last_ask is None:
                return

            ask = self._last_ask
            entry = self._entry_price
            if entry <= 0:
                return

            profit = ask - entry
            if profit > start_dist:
                new_stop = ask - float(self.StopLossPoints) * step

                if self._long_stop is None or new_stop - self._long_stop > step_dist:
                    self._long_stop = new_stop

            if self._long_stop is not None and self._last_bid is not None and self._last_bid <= self._long_stop:
                if self.Position > 0:
                    self.SellMarket()
                self._reset_trailing()

        elif self.Position < 0:
            if self._last_bid is None:
                return

            bid = self._last_bid
            entry = self._entry_price
            if entry <= 0:
                return

            profit = entry - bid
            if profit > start_dist:
                new_stop = bid + float(self.StopLossPoints) * step

                if self._short_stop is None or self._short_stop - new_stop > step_dist:
                    self._short_stop = new_stop

            if self._short_stop is not None and self._last_ask is not None and self._last_ask >= self._short_stop:
                if self.Position < 0:
                    self.BuyMarket()
                self._reset_trailing()
        else:
            self._reset_trailing()

    def _reset_trailing(self):
        self._long_stop = None
        self._short_stop = None

    def OnReseted(self):
        super(trailing_stop_step_manager_strategy, self).OnReseted()
        self._reset_trailing()
        self._last_ask = None
        self._last_bid = None
        self._previous_position = 0.0
        self._entry_price = 0.0

    def CreateClone(self):
        return trailing_stop_step_manager_strategy()
