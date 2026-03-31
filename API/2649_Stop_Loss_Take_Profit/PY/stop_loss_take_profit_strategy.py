import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class stop_loss_take_profit_strategy(Strategy):
    """Candle-direction entries with pip-based SL/TP and martingale volume doubling on losses."""

    def __init__(self):
        super(stop_loss_take_profit_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for evaluating entries", "General")
        self._stop_loss_distance = self.Param("StopLossDistance", 5.0) \
            .SetDisplay("Stop Loss Distance", "Stop loss distance in price units", "Risk")
        self._take_profit_distance = self.Param("TakeProfitDistance", 5.0) \
            .SetDisplay("Take Profit Distance", "Take profit distance in price units", "Risk")
        self._initial_volume = self.Param("InitialVolume", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Initial Volume", "Starting order volume", "Risk")

        self._current_volume = 0.0
        self._entry_price = 0.0
        self._trade_count = 0

    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def StopLossDistance(self):
        return self._stop_loss_distance.Value
    @property
    def TakeProfitDistance(self):
        return self._take_profit_distance.Value
    @property
    def InitialVolume(self):
        return self._initial_volume.Value

    def OnStarted2(self, time):
        super(stop_loss_take_profit_strategy, self).OnStarted2(time)

        self._current_volume = float(self.InitialVolume)
        self._entry_price = 0.0
        self._trade_count = 0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        sl = float(self.StopLossDistance)
        tp = float(self.TakeProfitDistance)

        # Check SL/TP for open position
        if self.Position != 0 and self._entry_price > 0:
            if self.Position > 0:
                hit_stop = sl > 0 and float(candle.LowPrice) <= self._entry_price - sl
                hit_take = tp > 0 and float(candle.HighPrice) >= self._entry_price + tp

                if hit_stop:
                    self.SellMarket()
                    self._handle_stop_loss()
                    return
                if hit_take:
                    self.SellMarket()
                    self._handle_take_profit()
                    return

            elif self.Position < 0:
                hit_stop = sl > 0 and float(candle.HighPrice) >= self._entry_price + sl
                hit_take = tp > 0 and float(candle.LowPrice) <= self._entry_price - tp

                if hit_stop:
                    self.BuyMarket()
                    self._handle_stop_loss()
                    return
                if hit_take:
                    self.BuyMarket()
                    self._handle_take_profit()
                    return

        # Enter new position when flat
        if self.Position == 0:
            self._trade_count += 1
            close = float(candle.ClosePrice)
            o = float(candle.OpenPrice)

            if close < o:
                self.SellMarket()
            else:
                self.BuyMarket()

            self._entry_price = close

    def _handle_stop_loss(self):
        self._current_volume *= 2.0
        self._entry_price = 0.0

    def _handle_take_profit(self):
        self._current_volume = float(self.InitialVolume)
        self._entry_price = 0.0

    def OnReseted(self):
        super(stop_loss_take_profit_strategy, self).OnReseted()
        self._current_volume = 0.0
        self._entry_price = 0.0
        self._trade_count = 0

    def CreateClone(self):
        return stop_loss_take_profit_strategy()
