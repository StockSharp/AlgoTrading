import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, CommodityChannelIndex, WilliamsR
from StockSharp.Algo.Strategies import Strategy

class knux_strategy(Strategy):
    """
    Knux multi-indicator: SMA crossover with CCI and Williams %R confirmation.
    """

    def __init__(self):
        super(knux_strategy, self).__init__()
        self._fast_ma_length = self.Param("FastMaLength", 5) \
            .SetDisplay("Fast MA Length", "Period of fast MA", "General")
        self._slow_ma_length = self.Param("SlowMaLength", 20) \
            .SetDisplay("Slow MA Length", "Period of slow MA", "General")
        self._cci_period = self.Param("CciPeriod", 20) \
            .SetDisplay("CCI Period", "CCI calculation period", "General")
        self._wpr_period = self.Param("WprPeriod", 14) \
            .SetDisplay("WPR Period", "Williams %R period", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._slow_ma = None
        self._cci = None
        self._wpr = None
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._is_initialized = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(knux_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._is_initialized = False

    def OnStarted(self, time):
        super(knux_strategy, self).OnStarted(time)

        fast_ma = SimpleMovingAverage()
        fast_ma.Length = self._fast_ma_length.Value
        self._slow_ma = SimpleMovingAverage()
        self._slow_ma.Length = self._slow_ma_length.Value
        self._cci = CommodityChannelIndex()
        self._cci.Length = self._cci_period.Value
        self._wpr = WilliamsR()
        self._wpr.Length = self._wpr_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_ma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ma)
            self.DrawIndicator(area, self._slow_ma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_val):
        if candle.State != CandleStates.Finished:
            return

        fast = float(fast_val)

        slow_result = self._slow_ma.Process(candle.ClosePrice, candle.OpenTime, True)
        self._cci.Process(candle)
        self._wpr.Process(candle)

        if not slow_result.IsFormed:
            self._prev_fast = fast
            return

        slow = float(slow_result.ToDecimal())

        if not self._is_initialized:
            self._prev_fast = fast
            self._prev_slow = slow
            self._is_initialized = True
            return

        cross_up = self._prev_fast <= self._prev_slow and fast > slow
        cross_down = self._prev_fast >= self._prev_slow and fast < slow

        if cross_up and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif cross_down and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return knux_strategy()
