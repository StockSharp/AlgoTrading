import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class martingale_trade_simulator_strategy(Strategy):
    """
    Martingale Trade Simulator: simplified to SMA crossover entry.
    The full C# version has manual buy/sell buttons, trailing stop, and martingale averaging.
    """

    def __init__(self):
        super(martingale_trade_simulator_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 10).SetDisplay("Fast SMA", "Fast SMA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 30).SetDisplay("Slow SMA", "Slow SMA period", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 10).SetDisplay("Cooldown", "Bars between signals", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candles", "General")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(martingale_trade_simulator_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._cooldown = 0

    def OnStarted(self, time):
        super(martingale_trade_simulator_strategy, self).OnStarted(time)
        fast = SimpleMovingAverage()
        fast.Length = self._fast_period.Value
        slow = SimpleMovingAverage()
        slow.Length = self._slow_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        fast = float(fast_val)
        slow = float(slow_val)
        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_fast = fast
            self._prev_slow = slow
            return
        if self._prev_fast > 0 and self._prev_slow > 0:
            if self._prev_fast <= self._prev_slow and fast > slow and self.Position <= 0:
                self.BuyMarket()
                self._cooldown = self._cooldown_bars.Value
            elif self._prev_fast >= self._prev_slow and fast < slow and self.Position >= 0:
                self.SellMarket()
                self._cooldown = self._cooldown_bars.Value
        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return martingale_trade_simulator_strategy()
