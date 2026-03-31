import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class mfs_3_bars_pattern_strategy(Strategy):
    def __init__(self):
        super(mfs_3_bars_pattern_strategy, self).__init__()
        self._sma_length = self.Param("SmaLength", 30) \
            .SetGreaterThanZero() \
            .SetDisplay("SMA Length", "SMA period", "General")
        self._risk_reward = self.Param("RiskReward", 2.5) \
            .SetGreaterThanZero() \
            .SetDisplay("Risk Reward", "Target reward to risk ratio", "General")
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("Signal Cooldown Bars", "Minimum bars between entries", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candles timeframe", "General")
        self._prev1_close = 0.0
        self._prev1_open = 0.0
        self._prev1_high = 0.0
        self._prev1_low = 0.0
        self._prev2_close = 0.0
        self._prev2_open = 0.0
        self._prev2_high = 0.0
        self._prev2_low = 0.0
        self._bar_count = 0
        self._stop_price = 0.0
        self._take_price = 0.0
        self._bars_from_signal = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(mfs_3_bars_pattern_strategy, self).OnReseted()
        self._prev1_close = 0.0
        self._prev1_open = 0.0
        self._prev1_high = 0.0
        self._prev1_low = 0.0
        self._prev2_close = 0.0
        self._prev2_open = 0.0
        self._prev2_high = 0.0
        self._prev2_low = 0.0
        self._bar_count = 0
        self._stop_price = 0.0
        self._take_price = 0.0
        self._bars_from_signal = 0

    def OnStarted2(self, time):
        super(mfs_3_bars_pattern_strategy, self).OnStarted2(time)
        self._bar_count = 0
        self._stop_price = 0.0
        self._take_price = 0.0
        self._bars_from_signal = self._signal_cooldown_bars.Value
        self._sma = SimpleMovingAverage()
        self._sma.Length = self._sma_length.Value
        dummy = ExponentialMovingAverage()
        dummy.Length = 10
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._sma, dummy, self.OnProcess).Start()

    def OnProcess(self, candle, sma_value, dummy_value):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        opn = float(candle.OpenPrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        if not self._sma.IsFormed or self._bar_count < 2:
            self._prev2_close = self._prev1_close
            self._prev2_open = self._prev1_open
            self._prev2_high = self._prev1_high
            self._prev2_low = self._prev1_low
            self._prev1_close = close
            self._prev1_open = opn
            self._prev1_high = high
            self._prev1_low = low
            self._bar_count += 1
            return
        bar1_bullish = self._prev2_close > self._prev2_open
        bar1_body = abs(self._prev2_close - self._prev2_open)
        bar2_bearish = self._prev1_close < self._prev1_open
        bar2_body = abs(self._prev1_close - self._prev1_open)
        bar3_bullish = close > opn
        body_pct = bar1_body / close * 100.0 if close > 0.0 else 0.0
        strong_bar1 = body_pct >= 0.05
        confirmation = close > self._prev1_high
        pattern = bar1_bullish and strong_bar1 and bar2_bearish and bar2_body < bar1_body * 0.5 and bar3_bullish and confirmation
        self._bars_from_signal += 1
        cd = self._signal_cooldown_bars.Value
        rr = float(self._risk_reward.Value)
        if self._bars_from_signal >= cd and pattern and self.Position == 0:
            self.BuyMarket()
            self._stop_price = self._prev2_low
            self._take_price = close + (close - self._stop_price) * rr
            self._bars_from_signal = 0
        if self.Position > 0:
            if low <= self._stop_price or high >= self._take_price:
                self.SellMarket()
        self._prev2_close = self._prev1_close
        self._prev2_open = self._prev1_open
        self._prev2_high = self._prev1_high
        self._prev2_low = self._prev1_low
        self._prev1_close = close
        self._prev1_open = opn
        self._prev1_high = high
        self._prev1_low = low

    def CreateClone(self):
        return mfs_3_bars_pattern_strategy()
