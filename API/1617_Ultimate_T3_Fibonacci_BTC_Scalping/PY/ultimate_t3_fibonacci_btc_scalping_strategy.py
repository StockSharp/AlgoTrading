import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class ultimate_t3_fibonacci_btc_scalping_strategy(Strategy):
    def __init__(self):
        super(ultimate_t3_fibonacci_btc_scalping_strategy, self).__init__()
        self._t3_length = self.Param("T3Length", 33) \
            .SetDisplay("T3 Length", "Main T3 length", "General")
        self._t3_fibo_length = self.Param("T3FiboLength", 19) \
            .SetDisplay("T3 Fibo Length", "Fibonacci T3 length", "General")
        self._use_opposite = self.Param("UseOpposite", True) \
            .SetDisplay("Use Opposite", "Close on opposite signal", "General")
        self._use_trade_management = self.Param("UseTradeManagement", True) \
            .SetDisplay("Use Trade Management", "Enable TP/SL", "General")
        self._take_profit = self.Param("TakeProfit", 15.0) \
            .SetDisplay("Take Profit %", "Take profit percentage", "Risk")
        self._stop_loss = self.Param("StopLoss", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._entry_price = 0.0
        self._prev_t3 = 0.0
        self._prev_t3_fibo = 0.0

    @property
    def t3_length(self):
        return self._t3_length.Value

    @property
    def t3_fibo_length(self):
        return self._t3_fibo_length.Value

    @property
    def use_opposite(self):
        return self._use_opposite.Value

    @property
    def use_trade_management(self):
        return self._use_trade_management.Value

    @property
    def take_profit(self):
        return self._take_profit.Value

    @property
    def stop_loss(self):
        return self._stop_loss.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ultimate_t3_fibonacci_btc_scalping_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._prev_t3 = 0.0
        self._prev_t3_fibo = 0.0

    def OnStarted(self, time):
        super(ultimate_t3_fibonacci_btc_scalping_strategy, self).OnStarted(time)
        t3 = ExponentialMovingAverage()
        t3.Length = self.t3_length
        t3_fibo = ExponentialMovingAverage()
        t3_fibo.Length = self.t3_fibo_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(t3, t3_fibo, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, t3, t3_fibo):
        if candle.State != CandleStates.Finished:
            return
        cross_up = self._prev_t3_fibo <= self._prev_t3 and t3_fibo > t3
        cross_down = self._prev_t3_fibo >= self._prev_t3 and t3_fibo < t3
        self._prev_t3 = t3
        self._prev_t3_fibo = t3_fibo
        if cross_up and self.Position <= 0:
            self.BuyMarket()
            self._entry_price = candle.ClosePrice
        elif cross_down and self.Position >= 0:
            self.SellMarket()
            self._entry_price = candle.ClosePrice
        else:
            if self.use_opposite:
                if self.Position > 0 and cross_down:
                    self.SellMarket()
                elif self.Position < 0 and cross_up:
                    self.BuyMarket()
        if self.use_trade_management and self.Position != 0:
            tp_sign = self.take_profit if self.Position > 0 else -self.take_profit
            sl_sign = self.stop_loss if self.Position > 0 else -self.stop_loss
            tp = self._entry_price * (1 + tp_sign / 100.0)
            sl = self._entry_price * (1 - sl_sign / 100.0)
            if self.Position > 0:
                if candle.ClosePrice >= tp or candle.ClosePrice <= sl:
                    self.SellMarket()
            elif self.Position < 0:
                if candle.ClosePrice <= tp or candle.ClosePrice >= sl:
                    self.BuyMarket()

    def CreateClone(self):
        return ultimate_t3_fibonacci_btc_scalping_strategy()
