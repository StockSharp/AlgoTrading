import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class consecutive_bars_above_ma_strategy(Strategy):
    def __init__(self):
        super(consecutive_bars_above_ma_strategy, self).__init__()
        self._threshold = self.Param("Threshold", 3) \
            .SetDisplay("Signal Threshold", "Number of bars above MA", "Parameters")
        self._ma_length = self.Param("MaLength", 10) \
            .SetDisplay("MA Length", "Moving average length", "Parameters")
        self._ema_period = self.Param("EmaPeriod", 50) \
            .SetDisplay("EMA Period", "EMA length for trend filter", "Filters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._bull_count = 0
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._is_ready = False

    @property
    def threshold(self):
        return self._threshold.Value

    @property
    def ma_length(self):
        return self._ma_length.Value

    @property
    def ema_period(self):
        return self._ema_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(consecutive_bars_above_ma_strategy, self).OnReseted()
        self._bull_count = 0
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._is_ready = False

    def OnStarted2(self, time):
        super(consecutive_bars_above_ma_strategy, self).OnStarted2(time)
        sma = SimpleMovingAverage()
        sma.Length = self.ma_length
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, ema, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def on_process(self, candle, ma_value, ema_value):
        if candle.State != CandleStates.Finished:
            return
        if not self._is_ready:
            self._prev_high = candle.HighPrice
            self._prev_low = candle.LowPrice
            self._is_ready = True
            return
        if candle.ClosePrice > ma_value:
            self._bull_count += 1
        else:
            self._bull_count = 0
        # Short: consecutive bars above MA, below EMA trend filter (mean reversion)
        short_condition = (self._bull_count >= self.threshold and
            candle.ClosePrice < ema_value)
        if short_condition and self.Position >= 0:
            self.SellMarket()
        elif self.Position < 0 and candle.ClosePrice < self._prev_low:
            self.BuyMarket()
        self._prev_high = candle.HighPrice
        self._prev_low = candle.LowPrice

    def CreateClone(self):
        return consecutive_bars_above_ma_strategy()
