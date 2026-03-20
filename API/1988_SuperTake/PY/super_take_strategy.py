import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class super_take_strategy(Strategy):

    def __init__(self):
        super(super_take_strategy, self).__init__()

        self._take_profit = self.Param("TakeProfit", 3000.0) \
            .SetDisplay("Take Profit", "Base take profit distance", "Risk")
        self._stop_loss = self.Param("StopLoss", 5000.0) \
            .SetDisplay("Stop Loss", "Stop loss distance", "Risk")
        self._martin_factor = self.Param("MartinFactor", 1.5) \
            .SetDisplay("Martingale Factor", "Multiplier after losing trade", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._entry_price = 0.0
        self._current_take_profit = 0.0
        self._last_take_profit_distance = 0.0
        self._is_long = False
        self._last_trade_was_loss = False
        self._last_closed_was_buy = None

    @property
    def TakeProfit(self):
        return self._take_profit.Value

    @TakeProfit.setter
    def TakeProfit(self, value):
        self._take_profit.Value = value

    @property
    def StopLoss(self):
        return self._stop_loss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stop_loss.Value = value

    @property
    def MartinFactor(self):
        return self._martin_factor.Value

    @MartinFactor.setter
    def MartinFactor(self, value):
        self._martin_factor.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(super_take_strategy, self).OnStarted(time)

        self._last_take_profit_distance = float(self.TakeProfit)

        self.SubscribeCandles(self.CandleType) \
            .Bind(self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        tp = float(self.TakeProfit)
        sl = float(self.StopLoss)
        mf = float(self.MartinFactor)

        if self.Position != 0:
            if self._is_long:
                profit = close - self._entry_price

                if profit >= self._current_take_profit:
                    self.SellMarket()
                    self._last_trade_was_loss = False
                    self._last_take_profit_distance = self._current_take_profit
                    self._last_closed_was_buy = True
                    self._entry_price = 0.0
                elif profit <= -sl:
                    self.SellMarket()
                    self._last_trade_was_loss = True
                    self._last_take_profit_distance = self._current_take_profit
                    self._last_closed_was_buy = True
                    self._entry_price = 0.0
            else:
                profit = self._entry_price - close

                if profit >= self._current_take_profit:
                    self.BuyMarket()
                    self._last_trade_was_loss = False
                    self._last_take_profit_distance = self._current_take_profit
                    self._last_closed_was_buy = False
                    self._entry_price = 0.0
                elif profit <= -sl:
                    self.BuyMarket()
                    self._last_trade_was_loss = True
                    self._last_take_profit_distance = self._current_take_profit
                    self._last_closed_was_buy = False
                    self._entry_price = 0.0

            return

        open_buy = self._last_closed_was_buy is not True

        if self._last_trade_was_loss:
            self._current_take_profit = self._last_take_profit_distance * mf
        else:
            self._current_take_profit = tp

        if open_buy:
            self.BuyMarket()
            self._entry_price = close
            self._is_long = True
        else:
            self.SellMarket()
            self._entry_price = close
            self._is_long = False

    def OnReseted(self):
        super(super_take_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._current_take_profit = 0.0
        self._last_take_profit_distance = 0.0
        self._is_long = False
        self._last_trade_was_loss = False
        self._last_closed_was_buy = None

    def CreateClone(self):
        return super_take_strategy()
