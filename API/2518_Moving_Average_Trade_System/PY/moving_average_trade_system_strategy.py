import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class moving_average_trade_system_strategy(Strategy):
    """
    Moving Average Trade System: 4 SMAs with signal/slow crossover and trend structure.
    """

    def __init__(self):
        super(moving_average_trade_system_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 5).SetDisplay("Fast SMA", "Fast SMA length", "Signals")
        self._medium_period = self.Param("MediumPeriod", 20).SetDisplay("Medium SMA", "Medium SMA length", "Signals")
        self._signal_period = self.Param("SignalPeriod", 40).SetDisplay("Signal SMA", "Signal SMA length", "Signals")
        self._slow_period = self.Param("SlowPeriod", 60).SetDisplay("Slow SMA", "Slow SMA length", "Signals")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))).SetDisplay("Candle Type", "Candles", "General")

        self._prev_signal = None
        self._prev_slow = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(moving_average_trade_system_strategy, self).OnReseted()
        self._prev_signal = None
        self._prev_slow = None

    def OnStarted(self, time):
        super(moving_average_trade_system_strategy, self).OnStarted(time)
        fast = SimpleMovingAverage()
        fast.Length = self._fast_period.Value
        medium = SimpleMovingAverage()
        medium.Length = self._medium_period.Value
        signal = SimpleMovingAverage()
        signal.Length = self._signal_period.Value
        slow = SimpleMovingAverage()
        slow.Length = self._slow_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, medium, signal, slow, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, medium)
            self.DrawIndicator(area, signal)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_val, medium_val, signal_val, slow_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        fast = float(fast_val)
        medium = float(medium_val)
        sig = float(signal_val)
        slow = float(slow_val)
        if self._prev_signal is None:
            self._prev_signal = sig
            self._prev_slow = slow
            return
        bullish_structure = fast > medium and medium > slow
        bearish_structure = fast < medium and medium < slow
        bullish_cross = self._prev_signal <= self._prev_slow and sig > slow
        bearish_cross = self._prev_signal >= self._prev_slow and sig < slow
        buy_signal = bullish_structure and bullish_cross
        sell_signal = bearish_structure and bearish_cross
        if buy_signal and self.Position <= 0:
            self.BuyMarket()
        elif sell_signal and self.Position >= 0:
            self.SellMarket()
        elif self.Position > 0 and sig <= slow:
            self.SellMarket()
        elif self.Position < 0 and sig >= slow:
            self.BuyMarket()
        self._prev_signal = sig
        self._prev_slow = slow

    def CreateClone(self):
        return moving_average_trade_system_strategy()
