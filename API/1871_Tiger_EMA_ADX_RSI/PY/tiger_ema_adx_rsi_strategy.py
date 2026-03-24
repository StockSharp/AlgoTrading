import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class tiger_ema_adx_rsi_strategy(Strategy):
    def __init__(self):
        super(tiger_ema_adx_rsi_strategy, self).__init__()
        self._fast_ma = self.Param("FastMaPeriod", 21) \
            .SetDisplay("Fast EMA", "Fast EMA period", "Parameters")
        self._slow_ma = self.Param("SlowMaPeriod", 89) \
            .SetDisplay("Slow EMA", "Slow EMA period", "Parameters")
        self._adx_period = self.Param("AdxPeriod", 14) \
            .SetDisplay("Momentum Period", "Momentum RSI period", "Parameters")
        self._adx_threshold = self.Param("AdxThreshold", 52.0) \
            .SetDisplay("Momentum Threshold", "Min RSI momentum value", "Parameters")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI calculation period", "Parameters")
        self._rsi_upper = self.Param("RsiUpper", 65.0) \
            .SetDisplay("RSI Upper", "Upper RSI bound", "Parameters")
        self._rsi_lower = self.Param("RsiLower", 35.0) \
            .SetDisplay("RSI Lower", "Lower RSI bound", "Parameters")
        self._take_profit = self.Param("TakeProfit", 500.0) \
            .SetDisplay("Take Profit", "Take profit distance", "Risk")
        self._stop_loss = self.Param("StopLoss", 200.0) \
            .SetDisplay("Stop Loss", "Stop loss distance", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles", "Parameters")
        self._cooldown_bars = self.Param("CooldownBars", 3) \
            .SetDisplay("Cooldown Bars", "Candles to wait after position change", "Trading")
        self._take_price = 0.0
        self._stop_price = 0.0
        self._prev_fast = None
        self._prev_slow = None
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(tiger_ema_adx_rsi_strategy, self).OnReseted()
        self._take_price = 0.0
        self._stop_price = 0.0
        self._prev_fast = None
        self._prev_slow = None
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(tiger_ema_adx_rsi_strategy, self).OnStarted(time)

        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self._fast_ma.Value
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self._slow_ma.Value
        momentum = RelativeStrengthIndex()
        momentum.Length = self._adx_period.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value

        sub = self.SubscribeCandles(self.candle_type)
        sub.Bind(fast_ema, slow_ema, momentum, rsi, self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_ema)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, fast, slow, momentum_val, rsi_val):
        if candle.State != CandleStates.Finished:
            return

        fast = float(fast)
        slow = float(slow)
        momentum_val = float(momentum_val)
        rsi_val = float(rsi_val)

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        if self._prev_fast is None or self._prev_slow is None:
            self._prev_fast = fast
            self._prev_slow = slow
            return

        close = float(candle.ClosePrice)
        cross_up = self._prev_fast <= self._prev_slow and fast > slow
        cross_down = self._prev_fast >= self._prev_slow and fast < slow
        adx_thresh = float(self._adx_threshold.Value)
        rsi_upper = float(self._rsi_upper.Value)
        rsi_lower = float(self._rsi_lower.Value)
        can_long = momentum_val >= adx_thresh and rsi_val > rsi_lower and rsi_val < rsi_upper
        can_short = momentum_val <= 100.0 - adx_thresh and rsi_val > rsi_lower and rsi_val < rsi_upper

        if self.Position == 0 and self._cooldown_remaining == 0:
            if cross_up and can_long:
                self.BuyMarket()
                self._take_price = close + float(self._take_profit.Value)
                self._stop_price = close - float(self._stop_loss.Value)
                self._cooldown_remaining = self._cooldown_bars.Value
            elif cross_down and can_short:
                self.SellMarket()
                self._take_price = close - float(self._take_profit.Value)
                self._stop_price = close + float(self._stop_loss.Value)
                self._cooldown_remaining = self._cooldown_bars.Value
        elif self.Position > 0:
            if close >= self._take_price or close <= self._stop_price or cross_down:
                self.SellMarket()
                self._cooldown_remaining = self._cooldown_bars.Value
        elif self.Position < 0:
            if close <= self._take_price or close >= self._stop_price or cross_up:
                self.BuyMarket()
                self._cooldown_remaining = self._cooldown_bars.Value

        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return tiger_ema_adx_rsi_strategy()
