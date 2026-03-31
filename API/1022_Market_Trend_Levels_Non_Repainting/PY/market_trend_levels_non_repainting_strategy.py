import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class market_trend_levels_non_repainting_strategy(Strategy):
    """
    Market Trend Levels: EMA crossover with RSI filter.
    """

    def __init__(self):
        super(market_trend_levels_non_repainting_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 12).SetDisplay("Fast", "Fast EMA", "Indicators")
        self._slow_length = self.Param("SlowLength", 25).SetDisplay("Slow", "Slow EMA", "Indicators")
        self._rsi_length = self.Param("RsiLength", 14).SetDisplay("RSI", "RSI period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(10))).SetDisplay("Candle Type", "Candles", "General")

        self._prev_diff = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(market_trend_levels_non_repainting_strategy, self).OnReseted()
        self._prev_diff = None

    def OnStarted2(self, time):
        super(market_trend_levels_non_repainting_strategy, self).OnStarted2(time)
        self._prev_diff = None
        self._ema_fast = ExponentialMovingAverage()
        self._ema_fast.Length = self._fast_length.Value
        self._ema_slow = ExponentialMovingAverage()
        self._ema_slow.Length = self._slow_length.Value
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self._rsi_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._ema_fast, self._ema_slow, self._rsi, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ema_fast)
            self.DrawIndicator(area, self._ema_slow)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_val, slow_val, rsi_val):
        if candle.State != CandleStates.Finished:
            return
        fast = float(fast_val)
        slow = float(slow_val)
        rsi = float(rsi_val)
        diff = fast - slow
        if not self._ema_fast.IsFormed or not self._ema_slow.IsFormed or not self._rsi.IsFormed:
            self._prev_diff = diff
            return
        if self._prev_diff is None:
            self._prev_diff = diff
            return
        cross_up = self._prev_diff <= 0 and diff > 0
        cross_down = self._prev_diff >= 0 and diff < 0
        self._prev_diff = diff
        if cross_up and self.Position <= 0 and rsi > 52:
            self.BuyMarket()
        if cross_down and self.Position >= 0 and rsi < 48:
            self.SellMarket()

    def CreateClone(self):
        return market_trend_levels_non_repainting_strategy()
