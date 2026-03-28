import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class pause_trading_on_consecutive_loss_strategy(Strategy):
    def __init__(self):
        super(pause_trading_on_consecutive_loss_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60))).SetDisplay("Candle Type", "Timeframe for momentum entries", "General")
        self._consecutive_losses = self.Param("ConsecutiveLosses", 3).SetGreaterThanZero().SetDisplay("Consecutive Losses", "Losses before pausing", "Risk")
        self._pause_bars = self.Param("PauseBars", 8).SetGreaterThanZero().SetDisplay("Pause Bars", "Number of bars to pause after loss streak", "Risk")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(pause_trading_on_consecutive_loss_strategy, self).OnReseted()
        self._previous_close = None
        self._loss_streak = 0
        self._pause_countdown = 0
        self._entry_price = 0
        self._entry_direction = None

    def OnStarted(self, time):
        super(pause_trading_on_consecutive_loss_strategy, self).OnStarted(time)
        self._previous_close = None
        self._loss_streak = 0
        self._pause_countdown = 0
        self._entry_price = 0
        self._entry_direction = None

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

        if self._previous_close is None:
            self._previous_close = close
            return

        momentum_threshold = float(self._previous_close) * 0.003

        if self._pause_countdown > 0:
            self._pause_countdown -= 1
            self._previous_close = close
            return

        if self.Position != 0:
            should_exit = False
            if self.Position > 0 and close < self._previous_close - momentum_threshold:
                should_exit = True
            elif self.Position < 0 and close > self._previous_close + momentum_threshold:
                should_exit = True

            if should_exit:
                is_loss = False
                if self._entry_direction == "buy" and close < self._entry_price:
                    is_loss = True
                elif self._entry_direction == "sell" and close > self._entry_price:
                    is_loss = True

                if is_loss:
                    self._loss_streak += 1
                    if self._loss_streak >= self._consecutive_losses.Value:
                        self._pause_countdown = self._pause_bars.Value
                        self._loss_streak = 0
                else:
                    self._loss_streak = 0

                if self.Position > 0:
                    self.SellMarket()
                elif self.Position < 0:
                    self.BuyMarket()
                self._entry_direction = None

        if self.Position == 0 and self._entry_direction is None:
            if close > self._previous_close + momentum_threshold:
                self.BuyMarket()
                self._entry_price = close
                self._entry_direction = "buy"
            elif close < self._previous_close - momentum_threshold:
                self.SellMarket()
                self._entry_price = close
                self._entry_direction = "sell"

        self._previous_close = close

    def CreateClone(self):
        return pause_trading_on_consecutive_loss_strategy()
