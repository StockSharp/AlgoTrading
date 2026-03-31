import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class over_hedge_v2_strategy(Strategy):
    def __init__(self):
        super(over_hedge_v2_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._short_ema_period = self.Param("ShortEmaPeriod", 8) \
            .SetDisplay("Short EMA", "Fast EMA length", "Indicators")
        self._long_ema_period = self.Param("LongEmaPeriod", 21) \
            .SetDisplay("Long EMA", "Slow EMA length", "Indicators")

        self._prev_signal = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def ShortEmaPeriod(self):
        return self._short_ema_period.Value

    @property
    def LongEmaPeriod(self):
        return self._long_ema_period.Value

    def OnReseted(self):
        super(over_hedge_v2_strategy, self).OnReseted()
        self._prev_signal = 0

    def OnStarted2(self, time):
        super(over_hedge_v2_strategy, self).OnStarted2(time)
        self._prev_signal = 0

        short_ema = ExponentialMovingAverage()
        short_ema.Length = self.ShortEmaPeriod
        long_ema = ExponentialMovingAverage()
        long_ema.Length = self.LongEmaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(short_ema, long_ema, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, short_ema)
            self.DrawIndicator(area, long_ema)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, short_value, long_value):
        if candle.State != CandleStates.Finished:
            return
        sv = float(short_value)
        lv = float(long_value)
        if sv > lv:
            signal = 1
        elif sv < lv:
            signal = -1
        else:
            signal = self._prev_signal
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
        return over_hedge_v2_strategy()
