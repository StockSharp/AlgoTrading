import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import JurikMovingAverage, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class x2_ma_jfatl_strategy(Strategy):
    def __init__(self):
        super(x2_ma_jfatl_strategy, self).__init__()

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
        super(x2_ma_jfatl_strategy, self).OnReseted()
        self._prev_diff = 0.0
        self._is_initialized = False
        self._bars_since_trade = 10

    def OnStarted(self, time):
        super(x2_ma_jfatl_strategy, self).OnStarted(time)

        self._prev_diff = 0.0
        self._is_initialized = False
        self._bars_since_trade = 10

        fast_ma = SimpleMovingAverage()
        fast_ma.Length = self.fast_length
        slow_ma = JurikMovingAverage()
        slow_ma.Length = self.slow_length
        filter_ma = JurikMovingAverage()
        filter_ma.Length = self.filter_length

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_ma, slow_ma, filter_ma, self._process_candle).Start()

    def _process_candle(self, candle, fast_value, slow_value, filter_value):
        if candle.State != CandleStates.Finished:
            return

        self._bars_since_trade += 1

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        fv = float(fast_value)
        sv = float(slow_value)
        flv = float(filter_value)
        close = float(candle.ClosePrice)

        if not self._is_initialized:
            self._prev_diff = fv - sv
            self._is_initialized = True
            return

        diff = fv - sv

        # Exit if price moves against the filter
        if self.Position > 0 and close < flv:
            self.SellMarket()
            self._bars_since_trade = 0
        elif self.Position < 0 and close > flv:
            self.BuyMarket()
            self._bars_since_trade = 0

        # Crossover entries
        if self._bars_since_trade >= 5 and self._prev_diff <= 0.0 and diff > 0.0 and close > flv and self.Position <= 0:
            self.BuyMarket()
            self._bars_since_trade = 0
        elif self._bars_since_trade >= 5 and self._prev_diff >= 0.0 and diff < 0.0 and close < flv and self.Position >= 0:
            self.SellMarket()
            self._bars_since_trade = 0

        self._prev_diff = diff

    def CreateClone(self):
        return x2_ma_jfatl_strategy()
