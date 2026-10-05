import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

# Minutes after midnight UTC.
SESSION_OPEN = 90.0
RANGE_END = 120.0
SESSION_CLOSE = 480.0

class hsi_first_30m_candle_strategy(Strategy):
    """
    HSI First 30m Candle strategy.
    Records the high and low of the first 30 minutes after the Hong Kong session opens (09:30 HKT, 01:30 UTC). During the rest of
    the session a close above that high goes long and a close below that low goes short, at most one trade per day. The stop sits
    at the opposite side of the range and the target at the range size multiplied by RiskReward from the entry.
    """

    def __init__(self):
        super(hsi_first_30m_candle_strategy, self).__init__()
        self._risk_reward = self.Param("RiskReward", 1.0).SetGreaterThanZero().SetDisplay("Risk Reward", "Target distance as a multiple of the range size", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._current_day = None
        self._range_high = None
        self._range_low = None
        self._traded_today = False
        self._stop_price = 0.0
        self._take_price = 0.0

    def OnReseted(self):
        super(hsi_first_30m_candle_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(hsi_first_30m_candle_strategy, self).OnStarted2(time)

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

        open_time = candle.OpenTime
        day = open_time.Date
        time_of_day = open_time.TimeOfDay.TotalMinutes

        if self._current_day is None or day != self._current_day:
            self._current_day = day
            self._range_high = None
            self._range_low = None
            self._traded_today = False

        high_price = float(candle.HighPrice)
        low_price = float(candle.LowPrice)

        if time_of_day >= SESSION_OPEN and time_of_day < RANGE_END:
            self._range_high = high_price if self._range_high is None else max(self._range_high, high_price)
            self._range_low = low_price if self._range_low is None else min(self._range_low, low_price)
            return

        if self.Position > 0:
            if low_price <= self._stop_price or high_price >= self._take_price:
                self.SellMarket(self.Position)
            return

        if self.Position < 0:
            if high_price >= self._stop_price or low_price <= self._take_price:
                self.BuyMarket(-self.Position)
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._traded_today or time_of_day < RANGE_END or time_of_day >= SESSION_CLOSE:
            return

        if self._range_high is None or self._range_low is None:
            return

        high = self._range_high
        low = self._range_low
        rng = high - low
        if rng <= 0:
            return

        close = float(candle.ClosePrice)
        rr = float(self._risk_reward.Value)

        if close > high:
            self.BuyMarket(self.Volume)
            self._stop_price = low
            self._take_price = close + rng * rr
            self._traded_today = True
        elif close < low:
            self.SellMarket(self.Volume)
            self._stop_price = high
            self._take_price = close - rng * rr
            self._traded_today = True

    def CreateClone(self):
        return hsi_first_30m_candle_strategy()
