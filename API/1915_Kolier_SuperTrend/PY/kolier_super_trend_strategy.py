import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class kolier_super_trend_strategy(Strategy):
    """
    Kolier SuperTrend: ATR-based trend following.
    Enters long when price crosses above SuperTrend, short when below.
    """

    def __init__(self):
        super(kolier_super_trend_strategy, self).__init__()
        self._period = self.Param("Period", 10) \
            .SetDisplay("ATR Period", "ATR period for SuperTrend", "Indicators")
        self._multiplier = self.Param("Multiplier", 3.0) \
            .SetDisplay("ATR Multiplier", "ATR multiplier for SuperTrend", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._atr = None
        self._prev_price_above = False
        self._prev_supertrend = 0.0
        self._is_initialized = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(kolier_super_trend_strategy, self).OnReseted()
        self._prev_price_above = False
        self._prev_supertrend = 0.0
        self._is_initialized = False

    def OnStarted(self, time):
        super(kolier_super_trend_strategy, self).OnStarted(time)

        self._atr = AverageTrueRange()
        self._atr.Length = self._period.Value
        self.Indicators.Add(self._atr)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        atr_result = self._atr.Process(candle)
        if not atr_result.IsFormed:
            return

        atr_val = float(atr_result)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        median = (high + low) / 2.0
        mult = self._multiplier.Value
        upper = median + mult * atr_val
        lower = median - mult * atr_val

        if not self._is_initialized:
            supertrend = lower if close > median else upper
            self._prev_supertrend = supertrend
            self._prev_price_above = close > supertrend
            self._is_initialized = True
            return

        if self._prev_supertrend <= high:
            supertrend = max(lower, self._prev_supertrend)
        elif self._prev_supertrend >= low:
            supertrend = min(upper, self._prev_supertrend)
        else:
            supertrend = lower if close > self._prev_supertrend else upper

        price_above = close > supertrend
        cross_up = price_above and not self._prev_price_above
        cross_down = not price_above and self._prev_price_above

        if cross_up and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif cross_down and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_supertrend = supertrend
        self._prev_price_above = price_above

    def CreateClone(self):
        return kolier_super_trend_strategy()
