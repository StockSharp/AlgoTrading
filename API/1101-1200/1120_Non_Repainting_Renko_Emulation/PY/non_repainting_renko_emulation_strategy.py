import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

import math

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class non_repainting_renko_emulation_strategy(Strategy):
    """
    Non-repainting Renko emulation strategy.
    Renko bricks of BrickSize price units are built from finished candle closes only, so a brick never changes once formed.
    When a new brick continues the direction of the previous brick the strategy enters in that direction (reversing an
    opposite position); when a new brick reverses the direction, the open position is closed.
    """

    def __init__(self):
        super(non_repainting_renko_emulation_strategy, self).__init__()
        self._brick_size = self.Param("BrickSize", 3.0).SetGreaterThanZero().SetDisplay("Brick Size", "Brick size in price units", "Renko")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._brick_level = None
        self._prev_direction = 0

    def OnReseted(self):
        super(non_repainting_renko_emulation_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(non_repainting_renko_emulation_strategy, self).OnStarted2(time)

        self._reset_state()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = candle.ClosePrice

        if self._brick_level is None:
            self._brick_level = close
            return

        level = self._brick_level
        brick = Decimal(self._brick_size.Value)
        bricks = int(math.floor(float(abs(close - level) / brick)))
        if bricks == 0:
            return

        direction = 1 if close > level else -1
        self._brick_level = level + Decimal(direction * bricks) * brick

        # With several bricks in one candle the previous brick has the same direction.
        prev_direction = direction if bricks > 1 else self._prev_direction
        self._prev_direction = direction

        if prev_direction == 0 or not self.IsFormedAndOnlineAndAllowTrading():
            return

        if direction == prev_direction:
            if direction > 0 and self.Position <= 0:
                self.BuyMarket(self.Volume + abs(self.Position))
            elif direction < 0 and self.Position >= 0:
                self.SellMarket(self.Volume + abs(self.Position))
        elif direction < 0 and self.Position > 0:
            self.SellMarket(self.Position)
        elif direction > 0 and self.Position < 0:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return non_repainting_renko_emulation_strategy()
