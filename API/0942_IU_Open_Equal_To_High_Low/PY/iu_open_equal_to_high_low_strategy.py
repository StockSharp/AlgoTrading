import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class iu_open_equal_to_high_low_strategy(Strategy):
    """
    Enters when candle open equals its high or low at day boundary.
    Uses risk/reward management with SL/TP based on previous candle.
    """

    def __init__(self):
        super(iu_open_equal_to_high_low_strategy, self).__init__()
        self._risk_reward = self.Param("RiskReward", 2.0) \
            .SetDisplay("Risk/Reward", "Take profit to stop ratio", "Risk")
        self._cooldown_days = self.Param("CooldownDays", 1) \
            .SetDisplay("Cooldown Days", "Min days between entries", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._current_day = None
        self._stop_price = 0.0
        self._take_price = 0.0
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._has_prev = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(iu_open_equal_to_high_low_strategy, self).OnReseted()
        self._current_day = None
        self._stop_price = 0.0
        self._take_price = 0.0
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(iu_open_equal_to_high_low_strategy, self).OnStarted(time)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        open_price = float(candle.OpenPrice)
        close = float(candle.ClosePrice)
        day = candle.OpenTime.Date

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_high = high
            self._prev_low = low
            self._has_prev = True
            self._current_day = day
            return

        if self._current_day is None or self._current_day != day:
            self._current_day = day

            if self.Position == 0 and self._has_prev:
                tolerance = open_price * 0.005
                is_open_near_low = open_price - low <= tolerance
                is_open_near_high = high - open_price <= tolerance

                if is_open_near_low:
                    self._stop_price = self._prev_low
                    self._take_price = open_price + (open_price - self._stop_price) * self._risk_reward.Value
                    self.BuyMarket()
                elif is_open_near_high:
                    self._stop_price = self._prev_high
                    self._take_price = open_price - (self._stop_price - open_price) * self._risk_reward.Value
                    self.SellMarket()

        if self.Position > 0:
            if low <= self._stop_price or high >= self._take_price:
                self.SellMarket()
                self._stop_price = 0.0
                self._take_price = 0.0
        elif self.Position < 0:
            if high >= self._stop_price or low <= self._take_price:
                self.BuyMarket()
                self._stop_price = 0.0
                self._take_price = 0.0

        self._prev_high = high
        self._prev_low = low
        self._has_prev = True

    def CreateClone(self):
        return iu_open_equal_to_high_low_strategy()
