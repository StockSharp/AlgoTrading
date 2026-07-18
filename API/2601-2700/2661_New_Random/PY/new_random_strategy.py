import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class new_random_strategy(Strategy):
    """Alternating entry strategy with SL/TP that mimics the MetaTrader New Random expert."""

    def __init__(self):
        super(new_random_strategy, self).__init__()

        self._stop_loss_points = self.Param("StopLossPoints", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss (pts)", "Stop loss in price steps", "Risk")
        self._take_profit_points = self.Param("TakeProfitPoints", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Take Profit (pts)", "Take profit in price steps", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._seq_last_side = 0  # 0=none, 1=buy, -1=sell
        self._pos_side = 0
        self._entry_price = 0.0
        self._candle_count = 0

    @property
    def StopLossPoints(self):
        return int(self._stop_loss_points.Value)
    @property
    def TakeProfitPoints(self):
        return int(self._take_profit_points.Value)
    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(new_random_strategy, self).OnStarted2(time)

        self._seq_last_side = -1  # start with sell, so first entry is buy
        self._pos_side = 0
        self._entry_price = 0.0
        self._candle_count = 0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._candle_count += 1
        if self._candle_count < 3:
            return

        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None and float(sec.PriceStep) > 0 else 1.0
        stop_dist = self.StopLossPoints * step
        take_dist = self.TakeProfitPoints * step
        price = float(candle.ClosePrice)

        # Check SL/TP
        if self.Position != 0 and self._entry_price > 0:
            hit = False

            if self._pos_side == 1:
                if stop_dist > 0 and float(candle.LowPrice) <= self._entry_price - stop_dist:
                    hit = True
                if take_dist > 0 and float(candle.HighPrice) >= self._entry_price + take_dist:
                    hit = True
            elif self._pos_side == -1:
                if stop_dist > 0 and float(candle.HighPrice) >= self._entry_price + stop_dist:
                    hit = True
                if take_dist > 0 and float(candle.LowPrice) <= self._entry_price - take_dist:
                    hit = True

            if hit:
                if self.Position > 0:
                    self.SellMarket()
                elif self.Position < 0:
                    self.BuyMarket()
                self._pos_side = 0
                self._entry_price = 0.0

        # If flat, open new position (alternating)
        if self.Position == 0 and self._pos_side == 0:
            side = -1 if self._seq_last_side == 1 else 1

            if side == 1:
                self.BuyMarket()
            else:
                self.SellMarket()

            self._pos_side = side
            self._entry_price = price
            self._seq_last_side = side

    def OnReseted(self):
        super(new_random_strategy, self).OnReseted()
        self._seq_last_side = 0
        self._pos_side = 0
        self._entry_price = 0.0
        self._candle_count = 0

    def CreateClone(self):
        return new_random_strategy()
