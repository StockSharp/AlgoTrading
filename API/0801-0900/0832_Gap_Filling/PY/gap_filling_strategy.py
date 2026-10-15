import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class CloseModes:
    """When a position is closed besides the gap level."""
    NewSession = 0
    GapLevel = 1


class gap_filling_strategy(Strategy):
    """
    Gap filling strategy.
    A session is a UTC calendar day. When its first candle opens away from the previous session's last close, the strategy fades the gap
    (short an up gap, long a down gap) with the previous close as the profit target. With Invert it trades in the gap direction instead
    and the previous close becomes the stop. With CloseWhen set to NewSession an open position is also closed when the next session
    starts; with GapLevel it stays open until the target or stop is reached.
    """

    def __init__(self):
        super(gap_filling_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._invert = self.Param("Invert", False).SetDisplay("Invert", "Trade in the gap direction with a stop at the gap level", "Trading")
        self._close_when = self.Param("CloseWhen", CloseModes.NewSession).SetDisplay("Close When", "When the position is closed besides the gap level", "Trading")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._session_day = None
        self._last_close = None
        self._gap_level = None

    def OnReseted(self):
        super(gap_filling_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(gap_filling_strategy, self).OnStarted2(time)

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

        day = candle.OpenTime.Date
        new_session = self._session_day is not None and self._session_day != day
        previous_close = self._last_close

        self._session_day = day
        self._last_close = candle.ClosePrice

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        invert = self._invert.Value

        if new_session:
            if self.Position != 0 and self._close_when.Value != CloseModes.NewSession:
                return

            target = 0
            if previous_close is not None and candle.OpenPrice != previous_close:
                self._gap_level = previous_close
                gap_up = candle.OpenPrice > previous_close
                target = -self.Volume if gap_up != invert else self.Volume

            # One order both closes the previous session's position and opens the new one.
            diff = target - self.Position
            if diff > 0:
                self.BuyMarket(diff)
            elif diff < 0:
                self.SellMarket(-diff)
            return

        if self.Position == 0 or self._gap_level is None:
            return

        # A faded gap targets the gap level; an inverted trade is stopped there.
        if self.Position > 0:
            hit = candle.LowPrice <= self._gap_level if invert else candle.HighPrice >= self._gap_level
            if hit:
                self.SellMarket(self.Position)
        else:
            hit = candle.HighPrice >= self._gap_level if invert else candle.LowPrice <= self._gap_level
            if hit:
                self.BuyMarket(abs(self.Position))

    def CreateClone(self):
        return gap_filling_strategy()
