import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class after_effects_strategy(Strategy):
    def __init__(self):
        super(after_effects_strategy, self).__init__()
        self._stop_loss = self.Param("StopLoss", 500.0) \
            .SetDisplay("Stop Loss", "Stop Loss distance", "General")
        self._period = self.Param("Period", 8) \
            .SetDisplay("Bar Period", "Period of bars for signal", "General")
        self._random = self.Param("Random", False) \
            .SetDisplay("Random Range", "Invert signal", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle Type", "General")
        self._p_queue = []
        self._two_p_queue = []
        self._open_p = 0.0
        self._open_2p = 0.0
        self._stop_price = 0.0

    @property
    def stop_loss(self):
        return self._stop_loss.Value

    @property
    def period(self):
        return self._period.Value

    @property
    def random(self):
        return self._random.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(after_effects_strategy, self).OnReseted()
        self._p_queue = []
        self._two_p_queue = []
        self._open_p = 0.0
        self._open_2p = 0.0
        self._stop_price = 0.0

    def OnStarted2(self, time):
        super(after_effects_strategy, self).OnStarted2(time)
        self._p_queue = []
        self._two_p_queue = []
        self._open_p = 0.0
        self._open_2p = 0.0
        self._stop_price = 0.0

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        per = int(self.period)
        sl = float(self.stop_loss)
        close = float(candle.ClosePrice)

        self._p_queue.append(float(candle.OpenPrice))

        if len(self._p_queue) > per:
            self._open_p = self._p_queue.pop(0)
            self._two_p_queue.append(self._open_p)

            if len(self._two_p_queue) > per:
                self._open_2p = self._two_p_queue.pop(0)

        if len(self._two_p_queue) < per:
            return

        signal = close - 2.0 * self._open_p + self._open_2p

        if self.random:
            signal = -signal

        if self.Position == 0:
            if signal > 0.0:
                self.BuyMarket()
                self._stop_price = close - sl
            else:
                self.SellMarket()
                self._stop_price = close + sl
            return

        if self.Position > 0:
            if close <= self._stop_price:
                if signal < 0.0:
                    self.SellMarket()
                    self.SellMarket()
                    self._stop_price = close + sl
                else:
                    self.SellMarket()
            else:
                self._stop_price = max(self._stop_price, close - sl)
        elif self.Position < 0:
            if close >= self._stop_price:
                if signal > 0.0:
                    self.BuyMarket()
                    self.BuyMarket()
                    self._stop_price = close - sl
                else:
                    self.BuyMarket()
            else:
                self._stop_price = min(self._stop_price, close + sl)

    def CreateClone(self):
        return after_effects_strategy()
