import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class forex_hammer_and_hanging_man_strategy(Strategy):
    """
    Forex Hammer and Hanging Man strategy.
    A candle qualifies when its range exceeds BodyLengthMultiplier times its body and its lower shadow is longer than ShadowRatio
    times its upper shadow. A bullish one is a hammer and goes long, a bearish one is a hanging man and goes short, reversing an
    opposite position. A position is closed after HoldPeriods candles.
    """

    def __init__(self):
        super(forex_hammer_and_hanging_man_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._body_length_multiplier = self.Param("BodyLengthMultiplier", 5.0).SetNotNegative().SetDisplay("Body Multiplier", "How many bodies the candle range must exceed", "Pattern")
        self._shadow_ratio = self.Param("ShadowRatio", 1.0).SetNotNegative().SetDisplay("Shadow Ratio", "How many upper shadows the lower shadow must exceed", "Pattern")
        self._hold_periods = self.Param("HoldPeriods", 26).SetGreaterThanZero().SetDisplay("Hold Periods", "Candles a position is held", "Trading")
        self._bars_in_position = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(forex_hammer_and_hanging_man_strategy, self).OnReseted()
        self._bars_in_position = 0

    def OnStarted2(self, time):
        super(forex_hammer_and_hanging_man_strategy, self).OnStarted2(time)

        self._bars_in_position = 0

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
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        body = abs(close - open_price)
        rng = high - low
        lower_shadow = min(open_price, close) - low
        upper_shadow = high - max(open_price, close)

        shape = rng > 0 and rng > float(self._body_length_multiplier.Value) * body and lower_shadow > float(self._shadow_ratio.Value) * upper_shadow
        hammer = shape and close > open_price
        hanging_man = shape and close < open_price

        if hammer and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._bars_in_position = 0
            return

        if hanging_man and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._bars_in_position = 0
            return

        if self.Position == 0:
            return

        self._bars_in_position += 1

        if self._bars_in_position < self._hold_periods.Value:
            return

        if self.Position > 0:
            self.SellMarket(self.Position)
        else:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return forex_hammer_and_hanging_man_strategy()
