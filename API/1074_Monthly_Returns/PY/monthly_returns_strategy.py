import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class monthly_returns_strategy(Strategy):
    """
    Monthly returns: pivot breakout strategy with high/low pivot detection.
    """

    def __init__(self):
        super(monthly_returns_strategy, self).__init__()
        self._left_bars = self.Param("LeftBars", 6).SetDisplay("Left Bars", "Left bars for pivots", "General")
        self._right_bars = self.Param("RightBars", 3).SetDisplay("Right Bars", "Right bars for pivots", "General")
        self._cooldown_bars = self.Param("CooldownBars", 14).SetDisplay("Cooldown", "Min bars between entries", "General")
        self._breakout_pct = self.Param("BreakoutOffsetPercent", 0.10).SetDisplay("Breakout %", "Min breakout offset", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(10))).SetDisplay("Candle Type", "Candles", "General")

        self._high_buf = []
        self._low_buf = []
        self._h_price = 0.0
        self._l_price = 0.0
        self._await_long = False
        self._await_short = False
        self._bar_index = 0
        self._last_signal_bar = -1000000

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(monthly_returns_strategy, self).OnReseted()
        self._high_buf = []
        self._low_buf = []
        self._h_price = 0.0
        self._l_price = 0.0
        self._await_long = False
        self._await_short = False
        self._bar_index = 0
        self._last_signal_bar = -1000000

    def OnStarted2(self, time):
        super(monthly_returns_strategy, self).OnStarted2(time)
        ema1 = ExponentialMovingAverage()
        ema1.Length = 10
        ema2 = ExponentialMovingAverage()
        ema2.Length = 20
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema1, ema2, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, d1, d2):
        if candle.State != CandleStates.Finished:
            return
        self._bar_index += 1
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        self._high_buf.append(high)
        self._low_buf.append(low)
        left = self._left_bars.Value
        right = self._right_bars.Value
        size = left + right + 1
        if len(self._high_buf) > size:
            self._high_buf = self._high_buf[-size:]
        if len(self._low_buf) > size:
            self._low_buf = self._low_buf[-size:]
        if len(self._high_buf) == size:
            idx = len(self._high_buf) - right - 1
            candidate = self._high_buf[idx]
            is_pivot = True
            for i in range(len(self._high_buf)):
                if i == idx:
                    continue
                if self._high_buf[i] >= candidate:
                    is_pivot = False
                    break
            if is_pivot:
                self._h_price = candidate
                self._await_long = True
        if len(self._low_buf) == size:
            idx = len(self._low_buf) - right - 1
            candidate = self._low_buf[idx]
            is_pivot = True
            for i in range(len(self._low_buf)):
                if i == idx:
                    continue
                if self._low_buf[i] <= candidate:
                    is_pivot = False
                    break
            if is_pivot:
                self._l_price = candidate
                self._await_short = True
        can_signal = self._bar_index - self._last_signal_bar >= self._cooldown_bars.Value
        bo_pct = float(self._breakout_pct.Value)
        long_trigger = self._h_price * (1.0 + bo_pct / 100.0)
        short_trigger = self._l_price * (1.0 - bo_pct / 100.0)
        if self._await_long and can_signal and high > long_trigger and self.Position <= 0:
            self.BuyMarket()
            self._await_long = False
            self._await_short = False
            self._last_signal_bar = self._bar_index
        if self._await_short and can_signal and low < short_trigger and self.Position >= 0:
            self.SellMarket()
            self._await_short = False
            self._await_long = False
            self._last_signal_bar = self._bar_index

    def CreateClone(self):
        return monthly_returns_strategy()
