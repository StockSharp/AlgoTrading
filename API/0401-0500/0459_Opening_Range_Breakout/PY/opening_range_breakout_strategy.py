import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class opening_range_breakout_strategy(Strategy):
    """
    Opening Range Breakout Strategy.
    Candles opening during the first RangeMinutes after SessionStart (UTC) form the opening range. Afterwards, a close above the
    range high plus EntryBuffer buys and a close below the range low minus EntryBuffer sells, once per session. The stop sits
    on the opposite side of the range and the target is RewardRisk times the risk away from the entry.
    """

    def __init__(self):
        super(opening_range_breakout_strategy, self).__init__()
        self._range_minutes = self.Param("RangeMinutes", 15).SetGreaterThanZero().SetDisplay("Range Minutes", "Length of the opening range in minutes", "Session")
        self._reward_risk = self.Param("RewardRisk", 2.0).SetGreaterThanZero().SetDisplay("Reward/Risk", "Target distance as a multiple of the risk", "Risk")
        self._entry_buffer = self.Param("EntryBuffer", 0.0001).SetNotNegative().SetDisplay("Entry Buffer", "Price buffer beyond the range required for a breakout", "Trading")
        self._session_start = self.Param("SessionStart", TimeSpan(8, 0, 0)).SetDisplay("Session Start", "Session start time (UTC)", "Session")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._session_date = None
        self._range_high = None
        self._range_low = None
        self._traded_today = False
        self._stop_price = None
        self._target_price = None

    def OnReseted(self):
        super(opening_range_breakout_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(opening_range_breakout_strategy, self).OnStarted2(time)

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
        date = open_time.Date

        if self._session_date is None or self._session_date != date:
            self._session_date = date
            self._range_high = None
            self._range_low = None
            self._traded_today = False

        range_start = date.Add(self._session_start.Value)
        range_end = range_start.Add(TimeSpan.FromMinutes(self._range_minutes.Value))

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        if open_time >= range_start and open_time < range_end:
            self._range_high = high if self._range_high is None else max(self._range_high, high)
            self._range_low = low if self._range_low is None else min(self._range_low, low)

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position > 0:
            if (self._stop_price is not None and low <= self._stop_price) or (self._target_price is not None and high >= self._target_price):
                self.SellMarket(self.Position)
                self._stop_price = None
                self._target_price = None
            return

        if self.Position < 0:
            if (self._stop_price is not None and high >= self._stop_price) or (self._target_price is not None and low <= self._target_price):
                self.BuyMarket(-self.Position)
                self._stop_price = None
                self._target_price = None
            return

        if self._traded_today or open_time < range_end or self._range_high is None or self._range_low is None:
            return

        close = float(candle.ClosePrice)
        buffer = float(self._entry_buffer.Value)
        reward_risk = float(self._reward_risk.Value)
        range_high = self._range_high
        range_low = self._range_low

        if close > range_high + buffer:
            self.BuyMarket(self.Volume)
            self._stop_price = range_low
            self._target_price = close + reward_risk * (close - range_low)
            self._traded_today = True
        elif close < range_low - buffer:
            self.SellMarket(self.Volume)
            self._stop_price = range_high
            self._target_price = close - reward_risk * (range_high - close)
            self._traded_today = True

    def CreateClone(self):
        return opening_range_breakout_strategy()
