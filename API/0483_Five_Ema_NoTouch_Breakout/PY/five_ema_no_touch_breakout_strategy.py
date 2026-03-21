import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class five_ema_no_touch_breakout_strategy(Strategy):
    """
    5 EMA No-Touch Breakout: waits for candle completely above/below EMA,
    enters on breakout with R:R based stop/target.
    """

    def __init__(self):
        super(five_ema_no_touch_breakout_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._ema_period = self.Param("EmaPeriod", 5) \
            .SetDisplay("EMA Period", "Length of EMA", "EMA")
        self._reward_risk = self.Param("RewardRisk", 3.0) \
            .SetDisplay("Reward:Risk", "Reward to risk ratio", "Risk Management")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")

        self._pending_long_high = None
        self._pending_long_low = None
        self._pending_short_low = None
        self._pending_short_high = None
        self._long_ready = False
        self._short_ready = False
        self._long_stop = None
        self._long_target = None
        self._short_stop = None
        self._short_target = None
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(five_ema_no_touch_breakout_strategy, self).OnReseted()
        self._pending_long_high = None
        self._pending_long_low = None
        self._pending_short_low = None
        self._pending_short_high = None
        self._long_ready = False
        self._short_ready = False
        self._long_stop = None
        self._long_target = None
        self._short_stop = None
        self._short_target = None
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(five_ema_no_touch_breakout_strategy, self).OnStarted(time)

        ema = ExponentialMovingAverage()
        ema.Length = self._ema_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return

        ema_val = float(ema_value)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)

        if high < ema_val:
            self._pending_long_high = high
            self._pending_long_low = low
            self._long_ready = True
            self._short_ready = False
        elif low > ema_val:
            self._pending_short_low = low
            self._pending_short_high = high
            self._short_ready = True
            self._long_ready = False

        # Check stop/target exits
        if self.Position > 0 and self._long_stop is not None and self._long_target is not None:
            if low <= self._long_stop or high >= self._long_target:
                self.SellMarket()
                self._long_stop = None
                self._long_target = None
                self._cooldown_remaining = self._cooldown_bars.Value
                return
        elif self.Position < 0 and self._short_stop is not None and self._short_target is not None:
            if high >= self._short_stop or low <= self._short_target:
                self.BuyMarket()
                self._short_stop = None
                self._short_target = None
                self._cooldown_remaining = self._cooldown_bars.Value
                return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        if self._long_ready and self._pending_long_high is not None and high > self._pending_long_high:
            if self._pending_long_low is not None:
                self._long_stop = self._pending_long_low
                self._long_target = close + (close - self._pending_long_low) * self._reward_risk.Value
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
                self._long_ready = False
                self._cooldown_remaining = self._cooldown_bars.Value
        elif self._short_ready and self._pending_short_low is not None and low < self._pending_short_low:
            if self._pending_short_high is not None:
                self._short_stop = self._pending_short_high
                self._short_target = close - (self._pending_short_high - close) * self._reward_risk.Value
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()
                self._short_ready = False
                self._cooldown_remaining = self._cooldown_bars.Value

    def CreateClone(self):
        return five_ema_no_touch_breakout_strategy()
