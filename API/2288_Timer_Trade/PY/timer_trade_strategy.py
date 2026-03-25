import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class timer_trade_strategy(Strategy):
    """Alternating buy/sell on each candle with SL/TP management."""
    def __init__(self):
        super(timer_trade_strategy, self).__init__()
        self._stop_loss = self.Param("StopLoss", 300.0).SetGreaterThanZero().SetDisplay("Stop Loss", "Stop loss in price units", "Risk")
        self._take_profit = self.Param("TakeProfit", 200.0).SetGreaterThanZero().SetDisplay("Take Profit", "Take profit in price units", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Timeframe for strategy", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(timer_trade_strategy, self).OnReseted()
        self._is_buy_next = True
        self._entry_price = 0

    def OnStarted(self, time):
        super(timer_trade_strategy, self).OnStarted(time)
        self._is_buy_next = True
        self._entry_price = 0

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        low = float(candle.LowPrice)
        high = float(candle.HighPrice)
        sl = self._stop_loss.Value
        tp = self._take_profit.Value

        # Check SL/TP
        if self.Position > 0:
            if low <= self._entry_price - sl or high >= self._entry_price + tp:
                self.SellMarket()
                self._is_buy_next = not self._is_buy_next
                return
        elif self.Position < 0:
            if high >= self._entry_price + sl or low <= self._entry_price - tp:
                self.BuyMarket()
                self._is_buy_next = not self._is_buy_next
                return

        # Open new position when flat
        if self.Position == 0:
            if self._is_buy_next:
                self.BuyMarket()
            else:
                self.SellMarket()
            self._entry_price = close
            self._is_buy_next = not self._is_buy_next

    def CreateClone(self):
        return timer_trade_strategy()
