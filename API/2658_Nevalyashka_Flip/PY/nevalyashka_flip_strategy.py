import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class nevalyashka_flip_strategy(Strategy):
    """Alternating long-short strategy that flips direction each time position is closed."""

    def __init__(self):
        super(nevalyashka_flip_strategy, self).__init__()

        self._stop_loss_points = self.Param("StopLossPoints", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss (pts)", "Stop loss distance in price steps", "Risk")
        self._take_profit_points = self.Param("TakeProfitPoints", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Take Profit (pts)", "Take profit distance in price steps", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._entry_price = 0.0
        self._current_side = 0  # 0=none, 1=buy, -1=sell
        self._last_completed_side = 0

    @property
    def StopLossPoints(self):
        return int(self._stop_loss_points.Value)
    @property
    def TakeProfitPoints(self):
        return int(self._take_profit_points.Value)
    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(nevalyashka_flip_strategy, self).OnStarted(time)

        self._entry_price = 0.0
        self._current_side = 0
        self._last_completed_side = 0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None and float(sec.PriceStep) > 0 else 1.0
        stop_dist = self.StopLossPoints * step
        take_dist = self.TakeProfitPoints * step
        price = float(candle.ClosePrice)

        # Check SL/TP for current position
        if self.Position != 0 and self._entry_price > 0:
            hit = False

            if self._current_side == 1:
                if stop_dist > 0 and float(candle.LowPrice) <= self._entry_price - stop_dist:
                    hit = True
                if take_dist > 0 and float(candle.HighPrice) >= self._entry_price + take_dist:
                    hit = True
            elif self._current_side == -1:
                if stop_dist > 0 and float(candle.HighPrice) >= self._entry_price + stop_dist:
                    hit = True
                if take_dist > 0 and float(candle.LowPrice) <= self._entry_price - take_dist:
                    hit = True

            if hit:
                if self.Position > 0:
                    self.SellMarket()
                elif self.Position < 0:
                    self.BuyMarket()
                self._last_completed_side = self._current_side
                self._current_side = 0
                self._entry_price = 0.0

        # If flat, open next position
        if self.Position == 0 and self._current_side == 0:
            # Alternate: start with sell, then flip
            if self._last_completed_side == 1:
                side_to_open = -1
            elif self._last_completed_side == -1:
                side_to_open = 1
            else:
                side_to_open = -1

            if side_to_open == 1:
                self.BuyMarket()
            else:
                self.SellMarket()

            self._current_side = side_to_open
            self._entry_price = price

    def OnReseted(self):
        super(nevalyashka_flip_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._current_side = 0
        self._last_completed_side = 0

    def CreateClone(self):
        return nevalyashka_flip_strategy()
