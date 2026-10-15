import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex, ExponentialMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class ma2_cci_strategy(Strategy):
    def __init__(self):
        super(ma2_cci_strategy, self).__init__()
        self._fast_ma_period = self.Param("FastMaPeriod", 10) \
            .SetDisplay("Fast MA Period", "Period of the fast moving average", "Indicators")
        self._slow_ma_period = self.Param("SlowMaPeriod", 15) \
            .SetDisplay("Slow MA Period", "Period of the slow moving average", "Indicators")
        self._cci_period = self.Param("CciPeriod", 14) \
            .SetDisplay("CCI Period", "Period for CCI filter", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "Period for ATR stop-loss", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._is_initialized = False
        self._stop_loss = 0.0

    @property
    def fast_ma_period(self):
        return self._fast_ma_period.Value

    @property
    def slow_ma_period(self):
        return self._slow_ma_period.Value

    @property
    def cci_period(self):
        return self._cci_period.Value

    @property
    def atr_period(self):
        return self._atr_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ma2_cci_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._is_initialized = False
        self._stop_loss = 0.0

    def OnStarted2(self, time):
        super(ma2_cci_strategy, self).OnStarted2(time)
        fast_ma = ExponentialMovingAverage()
        fast_ma.Length = self.fast_ma_period
        slow_ma = ExponentialMovingAverage()
        slow_ma.Length = self.slow_ma_period
        cci = CommodityChannelIndex()
        cci.Length = self.cci_period
        atr = StandardDeviation()
        atr.Length = self.atr_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_ma, slow_ma, cci, atr, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, fast, slow, cci_value, atr_value):
        if candle.State != CandleStates.Finished:
            return
        if not self._is_initialized:
            self._prev_fast = fast
            self._prev_slow = slow
            self._is_initialized = True
            return
        # Stop-loss check
        if self.Position > 0 and self._stop_loss > 0 and candle.ClosePrice <= self._stop_loss:
            self.SellMarket()
            self._stop_loss = 0
            self._prev_fast = fast
            self._prev_slow = slow
            return
        elif self.Position < 0 and self._stop_loss > 0 and candle.ClosePrice >= self._stop_loss:
            self.BuyMarket()
            self._stop_loss = 0
            self._prev_fast = fast
            self._prev_slow = slow
            return
        is_fast_above = fast > slow
        was_fast_above = self._prev_fast > self._prev_slow
        # MA crossover up => long (CCI as optional filter)
        if is_fast_above and not was_fast_above:
            if self.Position < 0:
                self.BuyMarket()
            if self.Position <= 0:
                self.BuyMarket()
                if atr_value > 0:
                    self._stop_loss = candle.ClosePrice - atr_value * 2
        # MA crossover down => short
        elif not is_fast_above and was_fast_above:
            if self.Position > 0:
                self.SellMarket()
            if self.Position >= 0:
                self.SellMarket()
                if atr_value > 0:
                    self._stop_loss = candle.ClosePrice + atr_value * 2
        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return ma2_cci_strategy()
