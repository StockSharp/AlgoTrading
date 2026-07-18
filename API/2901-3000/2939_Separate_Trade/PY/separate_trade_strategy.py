import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class separate_trade_strategy(Strategy):
    def __init__(self):
        super(separate_trade_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._fast_period = self.Param("FastPeriod", 14) \
            .SetDisplay("Fast Period", "Fast EMA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 65) \
            .SetDisplay("Slow Period", "Slow EMA period", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "ATR period for volatility filter", "Indicators")

        self._prev_fast = None
        self._prev_slow = None

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
    def AtrPeriod(self):
        return self._atr_period.Value

    def OnReseted(self):
        super(separate_trade_strategy, self).OnReseted()
        self._prev_fast = None
        self._prev_slow = None

    def OnStarted2(self, time):
        super(separate_trade_strategy, self).OnStarted2(time)
        self._prev_fast = None
        self._prev_slow = None

        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self.FastPeriod
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self.SlowPeriod
        atr = AverageTrueRange()
        atr.Length = self.AtrPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast_ema, slow_ema, atr, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_ema)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, fast_value, slow_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        fv = float(fast_value)
        sv = float(slow_value)

        if self._prev_fast is None or self._prev_slow is None:
            self._prev_fast = fv
            self._prev_slow = sv
            return

        prev_above = self._prev_fast > self._prev_slow
        curr_above = fv > sv

        self._prev_fast = fv
        self._prev_slow = sv

        # Require minimum ATR relative to price
        av = float(atr_value)
        close = float(candle.ClosePrice)
        if av < close * 0.0005:
            return

        # Golden cross
        if not prev_above and curr_above:
            if self.Position < 0:
                self.BuyMarket()
            if self.Position <= 0:
                self.BuyMarket()
        # Death cross
        elif prev_above and not curr_above:
            if self.Position > 0:
                self.SellMarket()
            if self.Position >= 0:
                self.SellMarket()

    def CreateClone(self):
        return separate_trade_strategy()
