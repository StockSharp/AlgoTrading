import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, MovingAverageConvergenceDivergence, ParabolicSar
from StockSharp.Algo.Strategies import Strategy


class macd_ema_sar_bollinger_bull_bear_strategy(Strategy):
    def __init__(self):
        super(macd_ema_sar_bollinger_bull_bear_strategy, self).__init__()
        self._fast_ma_period = self.Param("FastMaPeriod", 8) \
            .SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_ma_period = self.Param("SlowMaPeriod", 21) \
            .SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    @property
    def fast_ma_period(self):
        return self._fast_ma_period.Value

    @property
    def slow_ma_period(self):
        return self._slow_ma_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(macd_ema_sar_bollinger_bull_bear_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(macd_ema_sar_bollinger_bull_bear_strategy, self).OnStarted2(time)
        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self.fast_ma_period
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self.slow_ma_period
        sar = ParabolicSar()
        macd = MovingAverageConvergenceDivergence()
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_ema, slow_ema, sar, macd, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, fast, slow, sar_val, macd_val):
        if candle.State != CandleStates.Finished:
            return
        if not self._has_prev:
            self._prev_fast = fast
            self._prev_slow = slow
            self._has_prev = True
            return
        close = candle.ClosePrice
        # Buy: EMA crossover up + SAR below
        buy_signal = (self._prev_fast <= self._prev_slow and fast > slow and
            sar_val < close)
        # Sell: EMA crossover down + SAR above
        sell_signal = (self._prev_fast >= self._prev_slow and fast < slow and
            sar_val > close)
        if buy_signal:
            if self.Position < 0:
                self.BuyMarket()
            if self.Position <= 0:
                self.BuyMarket()
        elif sell_signal:
            if self.Position > 0:
                self.SellMarket()
            if self.Position >= 0:
                self.SellMarket()
        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return macd_ema_sar_bollinger_bull_bear_strategy()
