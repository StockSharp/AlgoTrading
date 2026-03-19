import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class nevalyashka_stopup_strategy(Strategy):
    def __init__(self):
        super(nevalyashka_stopup_strategy, self).__init__()
        self._stop_loss = self.Param("StopLoss", 500.0).SetGreaterThanZero().SetDisplay("Stop Loss", "Stop loss in price units", "General")
        self._take_profit = self.Param("TakeProfit", 200.0).SetGreaterThanZero().SetDisplay("Take Profit", "Take profit in price units", "General")
        self._martingale_coeff = self.Param("MartingaleCoeff", 1.5).SetGreaterThanZero().SetDisplay("Martingale Coeff", "Multiplier applied after loss", "Risk")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4).TimeFrame()).SetDisplay("Candle Type", "Type of candles", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(nevalyashka_stopup_strategy, self).OnReseted()
        self._entry_price = 0
        self._current_sl = 0
        self._current_tp = 0
        self._next_is_buy = True

    def OnStarted(self, time):
        super(nevalyashka_stopup_strategy, self).OnStarted(time)
        self._current_sl = self._stop_loss.Value
        self._current_tp = self._take_profit.Value
        self._next_is_buy = True
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

        close = candle.ClosePrice

        if self.Position == 0:
            if self._next_is_buy:
                self.BuyMarket()
            else:
                self.SellMarket()
            self._entry_price = close
            return

        if self.Position > 0:
            if candle.LowPrice <= self._entry_price - self._current_sl:
                self.SellMarket()
                self._on_trade_closed(False)
            elif candle.HighPrice >= self._entry_price + self._current_tp:
                self.SellMarket()
                self._on_trade_closed(True)
        elif self.Position < 0:
            if candle.HighPrice >= self._entry_price + self._current_sl:
                self.BuyMarket()
                self._on_trade_closed(False)
            elif candle.LowPrice <= self._entry_price - self._current_tp:
                self.BuyMarket()
                self._on_trade_closed(True)

    def _on_trade_closed(self, was_profit):
        if was_profit:
            self._current_sl = self._stop_loss.Value
            self._current_tp = self._take_profit.Value
        else:
            self._current_sl *= self._martingale_coeff.Value
            self._current_tp *= self._martingale_coeff.Value
        self._next_is_buy = not self._next_is_buy

    def CreateClone(self):
        return nevalyashka_stopup_strategy()
