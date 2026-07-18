import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class mean_reversion_pro_strategy(Strategy):
    """
    Mean Reversion Pro: fast/slow SMA with candle range threshold.
    """

    def __init__(self):
        super(mean_reversion_pro_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 5).SetDisplay("Fast", "Fast SMA", "Indicators")
        self._slow_length = self.Param("SlowLength", 50).SetDisplay("Slow", "Slow SMA", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candles", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(mean_reversion_pro_strategy, self).OnReseted()

    def OnStarted2(self, time):
        super(mean_reversion_pro_strategy, self).OnStarted2(time)
        self._fast_sma = SimpleMovingAverage()
        self._fast_sma.Length = self._fast_length.Value
        self._slow_sma = SimpleMovingAverage()
        self._slow_sma.Length = self._slow_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._fast_sma, self._slow_sma, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._fast_sma)
            self.DrawIndicator(area, self._slow_sma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return
        if not self._fast_sma.IsFormed or not self._slow_sma.IsFormed:
            return
        fast = float(fast_val)
        slow = float(slow_val)
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        rng = high - low
        long_threshold = low + 0.2 * rng
        short_threshold = high - 0.2 * rng
        long_signal = close < fast and close < long_threshold and close > slow
        short_signal = close > fast and close > short_threshold and close < slow
        exit_long = close > fast and self.Position > 0
        exit_short = close < fast and self.Position < 0
        if long_signal and self.Position <= 0:
            self.BuyMarket()
        elif short_signal and self.Position >= 0:
            self.SellMarket()
        elif exit_long:
            self.SellMarket()
        elif exit_short:
            self.BuyMarket()

    def CreateClone(self):
        return mean_reversion_pro_strategy()
