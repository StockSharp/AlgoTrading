import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

RANGE_START = TimeSpan(13, 0, 0)
RANGE_END = TimeSpan(13, 30, 0)


class ny_breakout_strategy(Strategy):
    """
    NY breakout strategy.
    The high and low of the candles opening between 13:00 and 13:30 UTC form the session range. The first candle after 13:30 goes
    long if it closes above the range high and short if it closes below the range low. The stop is the opposite range boundary
    and the target lies RewardRisk times the range height from the entry.
    """

    def __init__(self):
        super(ny_breakout_strategy, self).__init__()
        self._reward_risk = self.Param("RewardRisk", 2.0).SetGreaterThanZero().SetDisplay("Reward/Risk", "Target distance as a multiple of the range height", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._current_day = None
        self._range_high = None
        self._range_low = None
        self._checked = False
        self._stop_price = None
        self._take_price = None

    def OnReseted(self):
        super(ny_breakout_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(ny_breakout_strategy, self).OnStarted2(time)

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
        tod = candle.OpenTime.TimeOfDay

        if self._current_day is None or day != self._current_day:
            self._current_day = day
            self._range_high = None
            self._range_low = None
            self._checked = False

        if tod >= RANGE_START and tod < RANGE_END:
            self._range_high = candle.HighPrice if self._range_high is None else max(self._range_high, candle.HighPrice)
            self._range_low = candle.LowPrice if self._range_low is None else min(self._range_low, candle.LowPrice)
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position > 0 and self._stop_price is not None and self._take_price is not None:
            if candle.LowPrice <= self._stop_price or candle.HighPrice >= self._take_price:
                self.SellMarket(self.Position)
                self._stop_price = None
                self._take_price = None
                return
        elif self.Position < 0 and self._stop_price is not None and self._take_price is not None:
            if candle.HighPrice >= self._stop_price or candle.LowPrice <= self._take_price:
                self.BuyMarket(-self.Position)
                self._stop_price = None
                self._take_price = None
                return

        if self._checked or tod < RANGE_END or self._range_high is None or self._range_low is None:
            return

        # Only the first candle after the window is checked for a breakout.
        self._checked = True

        high = self._range_high
        low = self._range_low
        close = candle.ClosePrice
        rng = high - low
        reward_risk = Decimal(self._reward_risk.Value)

        if close > high and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._stop_price = low
            self._take_price = close + rng * reward_risk
        elif close < low and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._stop_price = high
            self._take_price = close - rng * reward_risk

    def CreateClone(self):
        return ny_breakout_strategy()
