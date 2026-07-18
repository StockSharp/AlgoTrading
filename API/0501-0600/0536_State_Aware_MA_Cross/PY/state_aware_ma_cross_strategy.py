import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class state_aware_ma_cross_strategy(Strategy):
    def __init__(self):
        super(state_aware_ma_cross_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 15).SetGreaterThanZero().SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_length = self.Param("SlowLength", 45).SetGreaterThanZero().SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 350).SetDisplay("Cooldown Bars", "Bars between trades", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(state_aware_ma_cross_strategy, self).OnReseted()
        self._prev_fast = 0
        self._prev_slow = 0
        self._bar_index = 0
        self._last_trade_bar = 0

    def OnStarted2(self, time):
        super(state_aware_ma_cross_strategy, self).OnStarted2(time)
        self._prev_fast = 0
        self._prev_slow = 0
        self._bar_index = 0
        self._last_trade_bar = 0

        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self._fast_length.Value
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self._slow_length.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(fast_ema, slow_ema, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_ema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return

        self._bar_index += 1
        cooldown_ok = self._bar_index - self._last_trade_bar > self._cooldown_bars.Value

        cross_up = self._prev_fast > 0 and self._prev_fast <= self._prev_slow and fast_val > slow_val
        cross_down = self._prev_fast > 0 and self._prev_fast >= self._prev_slow and fast_val < slow_val

        if cross_up and self.Position <= 0 and cooldown_ok:
            self.BuyMarket()
            self._last_trade_bar = self._bar_index
        elif cross_down and self.Position >= 0 and cooldown_ok:
            self.SellMarket()
            self._last_trade_bar = self._bar_index

        self._prev_fast = fast_val
        self._prev_slow = slow_val

    def CreateClone(self):
        return state_aware_ma_cross_strategy()
