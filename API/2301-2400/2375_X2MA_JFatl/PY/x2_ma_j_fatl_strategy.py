import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, JurikMovingAverage
from StockSharp.Algo.Strategies import Strategy


class x2_ma_j_fatl_strategy(Strategy):
    def __init__(self):
        super(x2_ma_j_fatl_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 5) \
            .SetDisplay("Fast MA Length", "Length of the fast moving average", "Parameters")
        self._slow_length = self.Param("SlowLength", 13) \
            .SetDisplay("Slow MA Length", "Length of the slow Jurik MA", "Parameters")
        self._filter_length = self.Param("FilterLength", 21) \
            .SetDisplay("Filter Length", "Length of the Jurik filter", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Type of candles for calculation", "General")
        self._prev_diff = 0.0
        self._is_initialized = False
        self._bars_since_trade = 10

    @property
    def fast_length(self):
        return self._fast_length.Value

    @property
    def slow_length(self):
        return self._slow_length.Value

    @property
    def filter_length(self):
        return self._filter_length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(x2_ma_j_fatl_strategy, self).OnReseted()
        self._prev_diff = 0.0
        self._is_initialized = False
        self._bars_since_trade = 10

    def OnStarted2(self, time):
        super(x2_ma_j_fatl_strategy, self).OnStarted2(time)
        self._prev_diff = 0.0
        self._is_initialized = False
        self._bars_since_trade = 10
        fast_ma = SimpleMovingAverage()
        fast_ma.Length = int(self.fast_length)
        slow_ma = JurikMovingAverage()
        slow_ma.Length = int(self.slow_length)
        filter_ma = JurikMovingAverage()
        filter_ma.Length = int(self.filter_length)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_ma, slow_ma, filter_ma, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ma)
            self.DrawIndicator(area, slow_ma)
            self.DrawIndicator(area, filter_ma)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, fast_value, slow_value, filter_value):
        if candle.State != CandleStates.Finished:
            return
        fast_value = float(fast_value)
        slow_value = float(slow_value)
        filter_value = float(filter_value)
        self._bars_since_trade += 1
        if not self._is_initialized:
            self._prev_diff = fast_value - slow_value
            self._is_initialized = True
            return
        diff = fast_value - slow_value
        close = float(candle.ClosePrice)
        if self.Position > 0 and close < filter_value:
            self.SellMarket()
            self._bars_since_trade = 0
        elif self.Position < 0 and close > filter_value:
            self.BuyMarket()
            self._bars_since_trade = 0
        if self._bars_since_trade >= 5 and self._prev_diff <= 0 and diff > 0 and close > filter_value and self.Position <= 0:
            self.BuyMarket()
            self._bars_since_trade = 0
        elif self._bars_since_trade >= 5 and self._prev_diff >= 0 and diff < 0 and close < filter_value and self.Position >= 0:
            self.SellMarket()
            self._bars_since_trade = 0
        self._prev_diff = diff

    def CreateClone(self):
        return x2_ma_j_fatl_strategy()
