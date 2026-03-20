import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class stlm_candle_strategy(Strategy):
    def __init__(self):
        super(stlm_candle_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._stop_loss = self.Param("StopLoss", 1000.0) \
            .SetDisplay("Stop Loss", "Stop loss in price units", "Risk")
        self._take_profit = self.Param("TakeProfit", 2000.0) \
            .SetDisplay("Take Profit", "Take profit in price units", "Risk")
        self._bars_since_signal = 2
        self._prev_direction = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def stop_loss(self):
        return self._stop_loss.Value

    @property
    def take_profit(self):
        return self._take_profit.Value

    def OnReseted(self):
        super(stlm_candle_strategy, self).OnReseted()
        self._bars_since_signal = 2
        self._prev_direction = 0

    def OnStarted(self, time):
        super(stlm_candle_strategy, self).OnStarted(time)
        self._bars_since_signal = 2
        self._prev_direction = 0
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    @staticmethod
    def _get_direction(candle):
        if float(candle.ClosePrice) > float(candle.OpenPrice):
            return 1
        if float(candle.ClosePrice) < float(candle.OpenPrice):
            return -1
        return 0

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        self._bars_since_signal += 1
        direction = self._get_direction(candle)
        if self._bars_since_signal >= 2 and direction == 1 and self._prev_direction == 1:
            if self.Position <= 0:
                self.BuyMarket()
                self._bars_since_signal = 0
        elif self._bars_since_signal >= 2 and direction == -1 and self._prev_direction == -1:
            if self.Position >= 0:
                self.SellMarket()
                self._bars_since_signal = 0
        self._prev_direction = direction

    def CreateClone(self):
        return stlm_candle_strategy()
