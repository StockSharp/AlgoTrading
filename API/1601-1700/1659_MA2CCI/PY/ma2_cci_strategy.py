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
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._fast_ma_period = self.Param("FastMaPeriod", 10) \
            .SetDisplay("Fast MA", "Fast moving average period", "Parameters")
        self._slow_ma_period = self.Param("SlowMaPeriod", 20) \
            .SetDisplay("Slow MA", "Slow moving average period", "Parameters")
        self._cci_period = self.Param("CciPeriod", 14) \
            .SetDisplay("CCI Period", "Commodity Channel Index period", "Parameters")
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._prev_cci = 0.0
        self._has_prev = False
        self._stop_price = 0.0
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def fast_ma_period(self):
        return self._fast_ma_period.Value

    @property
    def slow_ma_period(self):
        return self._slow_ma_period.Value

    @property
    def cci_period(self):
        return self._cci_period.Value

    def OnReseted(self):
        super(ma2_cci_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._prev_cci = 0.0
        self._has_prev = False
        self._stop_price = 0.0
        self._entry_price = 0.0

    def OnStarted2(self, time):
        super(ma2_cci_strategy, self).OnStarted2(time)
        fast_ma = ExponentialMovingAverage()
        fast_ma.Length = self.fast_ma_period
        slow_ma = ExponentialMovingAverage()
        slow_ma.Length = self.slow_ma_period
        cci = CommodityChannelIndex()
        cci.Length = self.cci_period
        std_dev = StandardDeviation()
        std_dev.Length = 14
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_ma, slow_ma, cci, std_dev, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ma)
            self.DrawIndicator(area, slow_ma)
            self.DrawOwnTrades(area)

    def on_process(self, candle, fast, slow, cci, std_val):
        if candle.State != CandleStates.Finished:
            return
        # Check stop loss
        if self._stop_price > 0:
            if self.Position > 0 and candle.LowPrice <= self._stop_price:
                self.SellMarket()
                self._stop_price = 0
                self._entry_price = 0
            elif self.Position < 0 and candle.HighPrice >= self._stop_price:
                self.BuyMarket()
                self._stop_price = 0
                self._entry_price = 0
        if not self._has_prev:
            self._prev_fast = fast
            self._prev_slow = slow
            self._prev_cci = cci
            self._has_prev = True
            return
        # Entry signals: MA crossover
        if fast > slow and self._prev_fast <= self._prev_slow and self.Position <= 0:
            self.BuyMarket()
            self._entry_price = candle.ClosePrice
            self._stop_price = candle.ClosePrice - std_val * 2
        elif fast < slow and self._prev_fast >= self._prev_slow and self.Position >= 0:
            self.SellMarket()
            self._entry_price = candle.ClosePrice
            self._stop_price = candle.ClosePrice + std_val * 2
        self._prev_fast = fast
        self._prev_slow = slow
        self._prev_cci = cci

    def CreateClone(self):
        return ma2_cci_strategy()
