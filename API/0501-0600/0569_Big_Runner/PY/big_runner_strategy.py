import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class big_runner_strategy(Strategy):
    def __init__(self):
        super(big_runner_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 120) \
            .SetDisplay("Fast Length", "Fast SMA period", "SMA")
        self._slow_length = self.Param("SlowLength", 450) \
            .SetDisplay("Slow Length", "Slow SMA period", "SMA")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percent from entry", "Risk")
        self._take_profit_percent = self.Param("TakeProfitPercent", 4.0) \
            .SetDisplay("Take Profit %", "Take profit percent from entry", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0

    @property
    def fast_length(self):
        return self._fast_length.Value
    @property
    def slow_length(self):
        return self._slow_length.Value
    @property
    def stop_loss_percent(self):
        return self._stop_loss_percent.Value
    @property
    def take_profit_percent(self):
        return self._take_profit_percent.Value
    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(big_runner_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0

    def OnStarted2(self, time):
        super(big_runner_strategy, self).OnStarted2(time)
        fast_ma = SimpleMovingAverage()
        fast_ma.Length = self.fast_length
        slow_ma = SimpleMovingAverage()
        slow_ma.Length = self.slow_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_ma, slow_ma, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ma)
            self.DrawIndicator(area, slow_ma)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return

        if self._prev_fast == 0 or self._prev_slow == 0:
            self._prev_fast = float(fast_value)
            self._prev_slow = float(slow_value)
            return

        if self._prev_fast <= self._prev_slow and fast_value > slow_value and self.Position <= 0:
            self.BuyMarket()
            self._entry_price = float(candle.ClosePrice)
        elif self._prev_fast >= self._prev_slow and fast_value < slow_value and self.Position >= 0:
            self.SellMarket()
            self._entry_price = float(candle.ClosePrice)

        if self.Position > 0 and self._entry_price > 0:
            pnl_percent = (float(candle.ClosePrice) - self._entry_price) / self._entry_price * 100.0
            if pnl_percent <= -self.stop_loss_percent or pnl_percent >= self.take_profit_percent:
                self.SellMarket()
                self._entry_price = 0.0
        elif self.Position < 0 and self._entry_price > 0:
            pnl_percent = (self._entry_price - float(candle.ClosePrice)) / self._entry_price * 100.0
            if pnl_percent <= -self.stop_loss_percent or pnl_percent >= self.take_profit_percent:
                self.BuyMarket()
                self._entry_price = 0.0

        self._prev_fast = float(fast_value)
        self._prev_slow = float(slow_value)

    def CreateClone(self):
        return big_runner_strategy()
