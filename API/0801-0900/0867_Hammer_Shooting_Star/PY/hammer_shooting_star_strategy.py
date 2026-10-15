import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class hammer_shooting_star_strategy(Strategy):
    """
    Hammer and Shooting Star strategy.
    A candle whose body is at least MinBodyRangePct of its range is a hammer when its lower wick is at least WickFactor times the
    body and its upper wick at most MaxOppositeWickFactor times the body; a shooting star mirrors this. After a hammer closes the
    strategy buys with the stop at the hammer's low and the target at its high; after a shooting star it sells with the stop at
    its high and the target at its low. An opposite pattern reverses the position.
    """

    def __init__(self):
        super(hammer_shooting_star_strategy, self).__init__()
        self._wick_factor = self.Param("WickFactor", 0.9).SetGreaterThanZero().SetDisplay("Wick Factor", "Minimum main wick length as a multiple of the body", "Pattern")
        self._max_opposite_wick_factor = self.Param("MaxOppositeWickFactor", 0.45).SetNotNegative().SetDisplay("Max Opposite Wick", "Maximum opposite wick length as a multiple of the body", "Pattern")
        self._min_body_range_pct = self.Param("MinBodyRangePct", 0.2).SetNotNegative().SetDisplay("Min Body/Range", "Minimum body size as a fraction of the candle range", "Pattern")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._stop_price = 0.0
        self._take_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(hammer_shooting_star_strategy, self).OnReseted()
        self._stop_price = 0.0
        self._take_price = 0.0

    def OnStarted2(self, time):
        super(hammer_shooting_star_strategy, self).OnStarted2(time)

        self._stop_price = 0.0
        self._take_price = 0.0

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        open_price = float(candle.OpenPrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)

        # Exits of the open position against the signal candle levels; the stop is checked first.
        if self.Position > 0:
            if low <= self._stop_price or high >= self._take_price:
                self.SellMarket(self.Position)
                return
        elif self.Position < 0:
            if high >= self._stop_price or low <= self._take_price:
                self.BuyMarket(-self.Position)
                return

        rng = high - low
        if rng <= 0:
            return

        body = abs(close - open_price)
        if body <= 0 or body < float(self._min_body_range_pct.Value) * rng:
            return

        upper_wick = high - max(open_price, close)
        lower_wick = min(open_price, close) - low
        wick_factor = float(self._wick_factor.Value)
        opposite_factor = float(self._max_opposite_wick_factor.Value)

        is_hammer = lower_wick >= wick_factor * body and upper_wick <= opposite_factor * body
        is_shooting_star = upper_wick >= wick_factor * body and lower_wick <= opposite_factor * body

        if is_hammer and not is_shooting_star and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._stop_price = low
            self._take_price = high
        elif is_shooting_star and not is_hammer and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._stop_price = high
            self._take_price = low

    def CreateClone(self):
        return hammer_shooting_star_strategy()
