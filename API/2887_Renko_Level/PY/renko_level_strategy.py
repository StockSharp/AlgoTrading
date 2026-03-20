import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class renko_level_strategy(Strategy):
    def __init__(self):
        super(renko_level_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candles", "General")
        self._block_size = self.Param("BlockSize", 5000) \
            .SetDisplay("Block Size", "Renko block size in price steps", "Renko")

        self._upper_level = 0.0
        self._lower_level = 0.0
        self._has_levels = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def BlockSize(self):
        return self._block_size.Value

    def OnReseted(self):
        super(renko_level_strategy, self).OnReseted()
        self._upper_level = 0.0
        self._lower_level = 0.0
        self._has_levels = False

    def OnStarted(self, time):
        super(renko_level_strategy, self).OnStarted(time)
        self._upper_level = 0.0
        self._lower_level = 0.0
        self._has_levels = False

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _on_process(self, candle):
        if candle.State != CandleStates.Finished:
            return

        brick_size = self._get_brick_size()
        if brick_size <= 0:
            return

        close = float(candle.ClosePrice)

        if not self._has_levels:
            self._initialize_levels(close, brick_size)
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        previous_upper = self._upper_level
        moved = False

        if close < self._lower_level:
            rnd, _, ceil = self._calculate_levels(close, brick_size)
            if abs(rnd - self._lower_level) > brick_size * 0.01:
                self._lower_level = rnd
                self._upper_level = ceil
                moved = True
        elif close > self._upper_level:
            rnd, floor, _ = self._calculate_levels(close, brick_size)
            if abs(rnd - self._upper_level) > brick_size * 0.01:
                self._lower_level = floor
                self._upper_level = rnd
                moved = True

        if not moved:
            return

        if self._upper_level > previous_upper and self.Position <= 0:
            self.BuyMarket()
        elif self._upper_level < previous_upper and self.Position >= 0:
            self.SellMarket()

    def _initialize_levels(self, price, brick_size):
        rnd, floor, _ = self._calculate_levels(price, brick_size)
        self._upper_level = rnd
        self._lower_level = floor
        self._has_levels = True

    def _calculate_levels(self, price, brick_size):
        ratio = price / brick_size
        rounded = round(ratio)
        price_round = rounded * brick_size
        price_floor = price_round - brick_size
        price_ceil = price_round + brick_size
        return (price_round, price_floor, price_ceil)

    def _get_brick_size(self):
        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 0.01
        return step * self.BlockSize

    def CreateClone(self):
        return renko_level_strategy()
