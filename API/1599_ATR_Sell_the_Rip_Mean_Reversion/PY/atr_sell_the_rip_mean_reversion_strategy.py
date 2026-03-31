import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class atr_sell_the_rip_mean_reversion_strategy(Strategy):
    def __init__(self):
        super(atr_sell_the_rip_mean_reversion_strategy, self).__init__()
        self._std_period = self.Param("StdPeriod", 14) \
            .SetDisplay("StdDev Period", "Standard deviation period", "Parameters")
        self._multiplier = self.Param("Multiplier", 1.0) \
            .SetDisplay("Multiplier", "Multiplier for threshold", "Parameters")
        self._ema_period = self.Param("EmaPeriod", 20) \
            .SetDisplay("EMA Period", "EMA length for trend filter", "Filters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_low = 0.0
        self._prev_std = 0.0
        self._prev_ema = 0.0
        self._is_ready = False

    @property
    def std_period(self):
        return self._std_period.Value

    @property
    def multiplier(self):
        return self._multiplier.Value

    @property
    def ema_period(self):
        return self._ema_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(atr_sell_the_rip_mean_reversion_strategy, self).OnReseted()
        self._prev_low = 0.0
        self._prev_std = 0.0
        self._prev_ema = 0.0
        self._is_ready = False

    def OnStarted2(self, time):
        super(atr_sell_the_rip_mean_reversion_strategy, self).OnStarted2(time)
        std_dev = StandardDeviation()
        std_dev.Length = self.std_period
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(std_dev, ema, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def on_process(self, candle, std_value, ema_value):
        if candle.State != CandleStates.Finished:
            return
        if not self._is_ready:
            self._prev_low = candle.LowPrice
            self._prev_std = std_value
            self._prev_ema = ema_value
            self._is_ready = True
            return
        # Short condition: price exceeds previous bar's threshold above EMA (overextended)
        if self._prev_std > 0 and self._prev_ema > 0:
            upper_threshold = self._prev_ema + self._prev_std * self.multiplier
            short_condition = candle.ClosePrice > upper_threshold
            if short_condition and self.Position >= 0:
                self.SellMarket()
        # Cover condition: close below previous low (mean reversion complete)
        if self.Position < 0 and candle.ClosePrice < self._prev_low:
            self.BuyMarket()
        self._prev_low = candle.LowPrice
        self._prev_std = std_value
        self._prev_ema = ema_value

    def CreateClone(self):
        return atr_sell_the_rip_mean_reversion_strategy()
