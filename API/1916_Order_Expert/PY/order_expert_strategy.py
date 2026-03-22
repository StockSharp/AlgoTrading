import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class order_expert_strategy(Strategy):
    def __init__(self):
        super(order_expert_strategy, self).__init__()
        self._take_profit_pct = self.Param("TakeProfitPct", 3.0) \
            .SetDisplay("Take Profit %", "Take profit percentage", "Risk")
        self._stop_loss_pct = self.Param("StopLossPct", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
        self._fast_ema_period = self.Param("FastEmaPeriod", 12) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_ema_period = self.Param("SlowEmaPeriod", 26) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 6) \
            .SetGreaterThanZero() \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._slow_ema = None
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._bars_since_trade = 0
        self._is_first = True

    @property
    def take_profit_pct(self):
        return self._take_profit_pct.Value
    @property
    def stop_loss_pct(self):
        return self._stop_loss_pct.Value
    @property
    def fast_ema_period(self):
        return self._fast_ema_period.Value
    @property
    def slow_ema_period(self):
        return self._slow_ema_period.Value
    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value
    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(order_expert_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._bars_since_trade = self.cooldown_bars
        self._is_first = True

    def OnStarted(self, time):
        super(order_expert_strategy, self).OnStarted(time)
        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self.fast_ema_period
        self._slow_ema = ExponentialMovingAverage()
        self._slow_ema.Length = self.slow_ema_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_ema, self.process_candle).Start()
        self.StartProtection(
            Unit(float(self.stop_loss_pct), UnitTypes.Percent),
            Unit(float(self.take_profit_pct), UnitTypes.Percent))
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, self._slow_ema)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, fast):
        if candle.State != CandleStates.Finished:
            return
        self._bars_since_trade += 1
        slow_result = self._slow_ema.Process(candle.ClosePrice, candle.OpenTime, True)
        if not slow_result.IsFormed:
            return
        slow = float(slow_result)
        f = float(fast)
        if self._is_first:
            self._prev_fast = f
            self._prev_slow = slow
            self._is_first = False
            return
        if self._prev_fast <= self._prev_slow and f > slow and self.Position == 0 and self._bars_since_trade >= self.cooldown_bars:
            self.BuyMarket()
            self._bars_since_trade = 0
        elif self._prev_fast >= self._prev_slow and f < slow and self.Position == 0 and self._bars_since_trade >= self.cooldown_bars:
            self.SellMarket()
            self._bars_since_trade = 0
        self._prev_fast = f
        self._prev_slow = slow

    def CreateClone(self):
        return order_expert_strategy()
