import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, Lowest
from StockSharp.Algo.Strategies import Strategy


class short_only10_bar_low_pullback_strategy(Strategy):
    def __init__(self):
        super(short_only10_bar_low_pullback_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles for processing", "General")
        self._lowest_period = self.Param("LowestPeriod", 10) \
            .SetDisplay("Lowest Low Period", "Lookback for lowest low", "Indicators")
        self._ibs_threshold = self.Param("IbsThreshold", 0.85) \
            .SetDisplay("IBS Threshold", "Internal bar strength threshold", "Signals")
        self._ema_period = self.Param("EmaPeriod", 50) \
            .SetDisplay("EMA Period", "EMA period for filter", "Trend Filter")
        self._prev_lowest = 0.0
        self._prev_low = 0.0
        self._is_ready = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def lowest_period(self):
        return self._lowest_period.Value

    @property
    def ibs_threshold(self):
        return self._ibs_threshold.Value

    @property
    def ema_period(self):
        return self._ema_period.Value

    def OnReseted(self):
        super(short_only10_bar_low_pullback_strategy, self).OnReseted()
        self._prev_lowest = 0.0
        self._prev_low = 0.0
        self._is_ready = False

    def OnStarted2(self, time):
        super(short_only10_bar_low_pullback_strategy, self).OnStarted2(time)
        lowest = Lowest()
        lowest.Length = self.lowest_period
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(lowest, ema, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def on_process(self, candle, lowest_val, ema_val):
        if candle.State != CandleStates.Finished:
            return
        if not self._is_ready:
            self._prev_lowest = lowest_val
            self._prev_low = candle.LowPrice
            self._is_ready = True
            return
        rng = candle.HighPrice - candle.LowPrice
        if rng == 0:
            self._prev_lowest = lowest_val
            self._prev_low = candle.LowPrice
            return
        ibs = (candle.ClosePrice - candle.LowPrice) / rng
        # Short: new low breakout with high IBS and below EMA
        short_condition = candle.LowPrice < self._prev_lowest and ibs > self.ibs_threshold and candle.ClosePrice < ema_val
        if short_condition and self.Position >= 0:
            self.SellMarket()
        # Cover: close below previous low
        if self.Position < 0 and candle.ClosePrice < self._prev_low:
            self.BuyMarket()
        self._prev_lowest = lowest_val
        self._prev_low = candle.LowPrice

    def CreateClone(self):
        return short_only10_bar_low_pullback_strategy()
