import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class freeman_strategy(Strategy):
    def __init__(self):
        super(freeman_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._fast_period = self.Param("FastPeriod", 10) \
            .SetDisplay("Fast Period", "Fast SMA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 30) \
            .SetDisplay("Slow Period", "Slow SMA period", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI period for filter", "Indicators")
        self._rsi_buy_level = self.Param("RsiBuyLevel", 55.0) \
            .SetDisplay("RSI Buy Level", "RSI below which buys are allowed", "Levels")
        self._rsi_sell_level = self.Param("RsiSellLevel", 45.0) \
            .SetDisplay("RSI Sell Level", "RSI above which sells are allowed", "Levels")

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
    def RsiPeriod(self):
        return self._rsi_period.Value

    @property
    def RsiBuyLevel(self):
        return self._rsi_buy_level.Value

    @property
    def RsiSellLevel(self):
        return self._rsi_sell_level.Value

    def OnReseted(self):
        super(freeman_strategy, self).OnReseted()
        self._prev_fast = None
        self._prev_slow = None

    def OnStarted2(self, time):
        super(freeman_strategy, self).OnStarted2(time)
        self._prev_fast = None
        self._prev_slow = None

        fast_sma = SimpleMovingAverage()
        fast_sma.Length = self.FastPeriod
        slow_sma = SimpleMovingAverage()
        slow_sma.Length = self.SlowPeriod
        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast_sma, slow_sma, rsi, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_sma)
            self.DrawIndicator(area, slow_sma)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, fast_value, slow_value, rsi_value):
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

        # MA crossover
        if not prev_above and curr_above:
            if self.Position < 0:
                self.BuyMarket()
            if self.Position <= 0:
                self.BuyMarket()
        elif prev_above and not curr_above:
            if self.Position > 0:
                self.SellMarket()
            if self.Position >= 0:
                self.SellMarket()

    def CreateClone(self):
        return freeman_strategy()
