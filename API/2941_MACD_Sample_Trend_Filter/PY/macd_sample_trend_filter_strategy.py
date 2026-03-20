import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class macd_sample_trend_filter_strategy(Strategy):
    def __init__(self):
        super(macd_sample_trend_filter_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._fast_period = self.Param("FastPeriod", 12) \
            .SetDisplay("Fast Period", "Fast EMA for MACD", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 26) \
            .SetDisplay("Slow Period", "Slow EMA for MACD", "Indicators")
        self._trend_period = self.Param("TrendPeriod", 100) \
            .SetDisplay("Trend Period", "Trend EMA period", "Indicators")

        self._prev_signal = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def FastPeriod(self):
        return self._fast_period.Value

    @property
    def SlowPeriod(self):
        return self._slow_period.Value

    @property
    def TrendPeriod(self):
        return self._trend_period.Value

    def OnReseted(self):
        super(macd_sample_trend_filter_strategy, self).OnReseted()
        self._prev_signal = 0

    def OnStarted(self, time):
        super(macd_sample_trend_filter_strategy, self).OnStarted(time)
        self._prev_signal = 0

        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self.FastPeriod
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self.SlowPeriod
        trend_ema = ExponentialMovingAverage()
        trend_ema.Length = self.TrendPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast_ema, slow_ema, trend_ema, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_ema)
            self.DrawIndicator(area, trend_ema)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, fast_value, slow_value, trend_value):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = float(candle.ClosePrice)
        fv = float(fast_value)
        sv = float(slow_value)
        tv = float(trend_value)
        macd_line = fv - sv

        signal = 0
        if macd_line > 0 and close > tv:
            signal = 1
        elif macd_line < 0 and close < tv:
            signal = -1

        if signal == self._prev_signal:
            return

        old_signal = self._prev_signal
        self._prev_signal = signal

        if signal == 1 and old_signal <= 0:
            if self.Position < 0:
                self.BuyMarket()
            if self.Position <= 0:
                self.BuyMarket()
        elif signal == -1 and old_signal >= 0:
            if self.Position > 0:
                self.SellMarket()
            if self.Position >= 0:
                self.SellMarket()

    def CreateClone(self):
        return macd_sample_trend_filter_strategy()
