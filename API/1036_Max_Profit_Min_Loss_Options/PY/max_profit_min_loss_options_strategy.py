import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class max_profit_min_loss_options_strategy(Strategy):
    def __init__(self):
        super(max_profit_min_loss_options_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 12) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast Length", "Fast EMA period", "General")
        self._slow_length = self.Param("SlowLength", 48) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow Length", "Slow EMA period", "General")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Length", "RSI period", "General")
        self._stop_loss_perc = self.Param("StopLossPerc", 1.5) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Hard stop loss percent", "General")
        self._trail_profit_perc = self.Param("TrailProfitPerc", 5.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Trail Profit %", "Trailing exit percent", "General")
        self._min_trend_percent = self.Param("MinTrendPercent", 0.20) \
            .SetGreaterThanZero() \
            .SetDisplay("Min Trend %", "Minimum EMA spread in percent", "General")
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Signal Cooldown Bars", "Minimum bars between entries", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Candles timeframe", "General")
        self._entry_price = 0.0
        self._highest_price = 0.0
        self._lowest_price = 999999999.0
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False
        self._bars_from_signal = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(max_profit_min_loss_options_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._highest_price = 0.0
        self._lowest_price = 999999999.0
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False
        self._bars_from_signal = 0

    def OnStarted(self, time):
        super(max_profit_min_loss_options_strategy, self).OnStarted(time)
        self._entry_price = 0.0
        self._highest_price = 0.0
        self._lowest_price = 999999999.0
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False
        self._bars_from_signal = self._signal_cooldown_bars.Value
        self._ma_fast = ExponentialMovingAverage()
        self._ma_fast.Length = self._fast_length.Value
        self._ma_slow = ExponentialMovingAverage()
        self._ma_slow.Length = self._slow_length.Value
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self._rsi_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._ma_fast, self._ma_slow, self._rsi, self.OnProcess).Start()

    def OnProcess(self, candle, ma_fast, ma_slow, rsi):
        if candle.State != CandleStates.Finished:
            return
        if not self._ma_fast.IsFormed or not self._ma_slow.IsFormed or not self._rsi.IsFormed:
            return
        fv = float(ma_fast)
        sv = float(ma_slow)
        rv = float(rsi)
        crossed_up = self._has_prev and self._prev_fast <= self._prev_slow and fv > sv
        crossed_down = self._has_prev and self._prev_fast >= self._prev_slow and fv < sv
        self._has_prev = True
        self._prev_fast = fv
        self._prev_slow = sv
        close = float(candle.ClosePrice)
        if close <= 0.0:
            return
        trend_percent = abs(fv - sv) / close * 100.0
        min_trend = float(self._min_trend_percent.Value)
        self._bars_from_signal += 1
        cd = self._signal_cooldown_bars.Value
        if self._bars_from_signal >= cd and crossed_up and rv >= 55.0 and trend_percent >= min_trend and self.Position <= 0:
            self.BuyMarket()
            self._entry_price = close
            self._highest_price = close
            self._bars_from_signal = 0
        elif self._bars_from_signal >= cd and crossed_down and rv <= 45.0 and trend_percent >= min_trend and self.Position >= 0:
            self.SellMarket()
            self._entry_price = close
            self._lowest_price = close
            self._bars_from_signal = 0
        sl_pct = float(self._stop_loss_perc.Value) / 100.0
        tp_pct = float(self._trail_profit_perc.Value) / 100.0
        if self.Position > 0:
            if close > self._highest_price:
                self._highest_price = close
            stop = self._entry_price * (1.0 - sl_pct)
            trail = self._highest_price * (1.0 - tp_pct)
            exit_p = max(stop, trail)
            if close <= exit_p:
                self.SellMarket()
        elif self.Position < 0:
            if close < self._lowest_price:
                self._lowest_price = close
            stop = self._entry_price * (1.0 + sl_pct)
            trail = self._lowest_price * (1.0 + tp_pct)
            exit_p = min(stop, trail)
            if close >= exit_p:
                self.BuyMarket()

    def CreateClone(self):
        return max_profit_min_loss_options_strategy()
