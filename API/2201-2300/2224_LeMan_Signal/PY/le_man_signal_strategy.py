import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class le_man_signal_strategy(Strategy):
    def __init__(self):
        super(le_man_signal_strategy, self).__init__()
        self._period = self.Param("Period", 12) \
            .SetDisplay("Period", "LeManSignal lookback period", "Indicator")
        self._signal_bar = self.Param("SignalBar", 1) \
            .SetDisplay("Signal Bar", "Offset for confirmed signal", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._highs = []
        self._lows = []

    @property
    def period(self):
        return self._period.Value

    @property
    def signal_bar(self):
        return self._signal_bar.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(le_man_signal_strategy, self).OnReseted()
        self._highs = []
        self._lows = []

    def OnStarted2(self, time):
        super(le_man_signal_strategy, self).OnStarted2(time)
        warmup = ExponentialMovingAverage()
        warmup.Length = 2 * self.period + 3
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(warmup, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, _warmup_val):
        if candle.State != CandleStates.Finished:
            return
        self._highs.append(float(candle.HighPrice))
        self._lows.append(float(candle.LowPrice))
        max_len = 2 * self.period + 3
        if len(self._highs) > max_len:
            self._highs.pop(0)
            self._lows.pop(0)
        if len(self._highs) < max_len:
            return
        signal = self._get_signal(self.signal_bar)
        if signal > 0 and self.Position <= 0:
            self.BuyMarket()
        elif signal < 0 and self.Position >= 0:
            self.SellMarket()

    def _get_signal(self, bar):
        size = len(self._highs)
        p = self.period
        bar1 = bar + 1
        bar2 = bar + 2
        bar1p = bar1 + p
        bar2p = bar2 + p
        h1 = self._highest_range(size - bar1 - p, p)
        h2 = self._highest_range(size - bar1p - p, p)
        h3 = self._highest_range(size - bar2 - p, p)
        h4 = self._highest_range(size - bar2p - p, p)
        l1 = self._lowest_range(size - bar1 - p, p)
        l2 = self._lowest_range(size - bar1p - p, p)
        l3 = self._lowest_range(size - bar2 - p, p)
        l4 = self._lowest_range(size - bar2p - p, p)
        buy = h3 <= h4 and h1 > h2
        sell = l3 >= l4 and l1 < l2
        if buy:
            return 1
        if sell:
            return -1
        return 0

    def _highest_range(self, start, length):
        mx = float('-inf')
        for i in range(start, start + length):
            if self._highs[i] > mx:
                mx = self._highs[i]
        return mx

    def _lowest_range(self, start, length):
        mn = float('inf')
        for i in range(start, start + length):
            if self._lows[i] < mn:
                mn = self._lows[i]
        return mn

    def CreateClone(self):
        return le_man_signal_strategy()
