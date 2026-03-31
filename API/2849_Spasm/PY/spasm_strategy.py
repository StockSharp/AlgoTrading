import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class spasm_strategy(Strategy):
    def __init__(self):
        super(spasm_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for analysis", "General")
        self._volatility_multiplier = self.Param("VolatilityMultiplier", 2.0) \
            .SetDisplay("Volatility Multiplier", "Multiplier applied to ATR for breakout bands", "Trading")

        self._highest_price = 0.0
        self._lowest_price = float('inf')
        self._prev_range = 0.0
        self._initialized = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def VolatilityMultiplier(self):
        return self._volatility_multiplier.Value

    def OnReseted(self):
        super(spasm_strategy, self).OnReseted()
        self._highest_price = 0.0
        self._lowest_price = float('inf')
        self._prev_range = 0.0
        self._initialized = False

    def OnStarted2(self, time):
        super(spasm_strategy, self).OnStarted2(time)

        self._highest_price = 0.0
        self._lowest_price = float('inf')
        self._prev_range = 0.0
        self._initialized = False

        subscription = self.SubscribeCandles(self.CandleType)
        subscription \
            .Bind(self._on_process) \
            .Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _on_process(self, candle):
        if candle.State != CandleStates.Finished:
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)

        if not self._initialized:
            self._highest_price = high
            self._lowest_price = low
            self._prev_range = high - low
            self._initialized = True
            return

        if high > self._highest_price:
            self._highest_price = high
        if low < self._lowest_price:
            self._lowest_price = low

        threshold = self._prev_range * float(self.VolatilityMultiplier)

        if threshold <= 0:
            self._prev_range = high - low
            return

        if close > self._lowest_price + threshold and self.Position <= 0:
            self.BuyMarket()
            self._highest_price = high
            self._lowest_price = low
        elif close < self._highest_price - threshold and self.Position >= 0:
            self.SellMarket()
            self._highest_price = high
            self._lowest_price = low

        self._prev_range = high - low

    def CreateClone(self):
        return spasm_strategy()
