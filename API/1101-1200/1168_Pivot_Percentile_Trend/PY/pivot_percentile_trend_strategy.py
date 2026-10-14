import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import Array, Decimal, TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SuperTrend
from StockSharp.Algo.Strategies import Strategy


class pivot_percentile_trend_strategy(Strategy):
    def __init__(self):
        super(pivot_percentile_trend_strategy, self).__init__()
        self._percentile_length = self.Param("PercentileLength", 40).SetGreaterThanZero() \
            .SetDisplay("Percentile Length", "Base window; the longest contains seven times as many candles", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 10).SetGreaterThanZero() \
            .SetDisplay("ATR Period", "Wilder ATR period of the price Supertrend", "Indicators")
        self._atr_multiplier = self.Param("AtrMultiplier", Decimal.Parse("3")).SetGreaterThanZero() \
            .SetDisplay("ATR Multiplier", "ATR width of the price Supertrend", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles used for calculation", "General")
        self._history = []

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(pivot_percentile_trend_strategy, self).OnReseted()
        self._history = []

    def OnStarted2(self, time):
        super(pivot_percentile_trend_strategy, self).OnStarted2(time)
        self._history = []
        self._supertrend = SuperTrend()
        self._supertrend.Length = self._atr_period.Value
        self._supertrend.Multiplier = self._atr_multiplier.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindWithEmpty(self._supertrend, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._supertrend)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, supertrend):
        if candle.State != CandleStates.Finished:
            return
        self._history.append((candle.HighPrice, candle.LowPrice))
        maximum_history = self._percentile_length.Value * 7
        if len(self._history) > maximum_history:
            self._history.pop(0)
        if supertrend is None or len(self._history) < maximum_history:
            return
        strength = 0
        for window in range(1, 8):
            period = self._percentile_length.Value * window
            recent = self._history[-period:]
            highs = Array[Decimal]([bar[0] for bar in recent])
            lows = Array[Decimal]([bar[1] for bar in recent])
            Array.Sort[Decimal](highs)
            Array.Sort[Decimal](lows)
            upper = highs[(3 * period + 3) // 4 - 1]
            lower = lows[(period + 3) // 4 - 1]
            if candle.HighPrice > upper:
                strength += 1
            if candle.LowPrice < lower:
                strength -= 1
        if strength > 0 and candle.ClosePrice > supertrend and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif strength < 0 and candle.ClosePrice < supertrend and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return pivot_percentile_trend_strategy()
