import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class expert_candles_strategy(Strategy):
    def __init__(self):
        super(expert_candles_strategy, self).__init__()

        self._shadow_ratio = self.Param("ShadowRatio", 0.3) \
            .SetDisplay("Shadow Ratio", "Min shadow to body ratio for pattern", "Signals")

        self._sma = None

    @property
    def shadow_ratio(self):
        return self._shadow_ratio.Value

    def OnReseted(self):
        super(expert_candles_strategy, self).OnReseted()
        self._sma = None

    def OnStarted(self, time):
        super(expert_candles_strategy, self).OnStarted(time)

        self._sma = SimpleMovingAverage()
        self._sma.Length = 20

        subscription = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromMinutes(30)))
        subscription.Bind(self._sma, self._process_candle)
        subscription.Start()

    def _process_candle(self, candle, sma_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._sma.IsFormed:
            return

        open_p = float(candle.OpenPrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        sma_val = float(sma_value)
        range_size = high - low

        if range_size <= 0:
            return

        body = abs(close - open_p)
        upper_shadow = high - max(open_p, close)
        lower_shadow = min(open_p, close) - low

        is_hammer = lower_shadow > range_size * self.shadow_ratio and upper_shadow < body
        is_shooting_star = upper_shadow > range_size * self.shadow_ratio and lower_shadow < body

        if is_hammer and close > sma_val and self.Position <= 0:
            self.BuyMarket()
        elif is_shooting_star and close < sma_val and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return expert_candles_strategy()
