import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class iu_opening_range_breakout_strategy(Strategy):
    """
    IU Opening Range Breakout: trades breakouts of the first session bars
    with risk/reward management and daily trade limit.
    """

    def __init__(self):
        super(iu_opening_range_breakout_strategy, self).__init__()
        self._risk_reward = self.Param("RiskReward", 2.0) \
            .SetDisplay("Risk/Reward", "Risk to reward ratio", "General")
        self._max_trades = self.Param("MaxTrades", 3) \
            .SetDisplay("Max Trades", "Maximum trades per day", "General")
        self._cooldown_days = self.Param("CooldownDays", 3) \
            .SetDisplay("Cooldown Days", "Minimum days between entries", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._or_high = 0.0
        self._or_low = 0.0
        self._range_set = False
        self._stop_price = 0.0
        self._target_price = 0.0
        self._trades_today = 0
        self._current_day = None
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._or_bar_count = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(iu_opening_range_breakout_strategy, self).OnReseted()
        self._or_high = 0.0
        self._or_low = 0.0
        self._range_set = False
        self._stop_price = 0.0
        self._target_price = 0.0
        self._trades_today = 0
        self._current_day = None
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._or_bar_count = 0

    def OnStarted(self, time):
        super(iu_opening_range_breakout_strategy, self).OnStarted(time)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        open_time = candle.OpenTime
        day = open_time.Date

        if self._current_day is None or self._current_day != day:
            self._current_day = day
            self._range_set = False
            self._trades_today = 0
            self._or_bar_count = 0
            self._or_high = 0.0
            self._or_low = float('inf')

        self._or_bar_count += 1
        if not self._range_set:
            self._or_high = max(self._or_high, high)
            self._or_low = min(self._or_low, low)
            if self._or_bar_count >= 2:
                self._range_set = True
            self._prev_high = high
            self._prev_low = low
            return

        if self.Position == 0 and self._trades_today < self._max_trades.Value:
            if high > self._or_high:
                self.BuyMarket()
                self._trades_today += 1
                self._stop_price = self._prev_low
                self._target_price = close + (close - self._stop_price) * self._risk_reward.Value
            elif low < self._or_low:
                self.SellMarket()
                self._trades_today += 1
                self._stop_price = self._prev_high
                self._target_price = close - (self._stop_price - close) * self._risk_reward.Value
        elif self.Position > 0:
            if low <= self._stop_price or high >= self._target_price:
                self.SellMarket()
        elif self.Position < 0:
            if high >= self._stop_price or low <= self._target_price:
                self.BuyMarket()

        self._prev_high = high
        self._prev_low = low

    def CreateClone(self):
        return iu_opening_range_breakout_strategy()
