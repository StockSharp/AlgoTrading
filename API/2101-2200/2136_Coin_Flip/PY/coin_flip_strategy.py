import clr
import random

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class coin_flip_strategy(Strategy):
    def __init__(self):
        super(coin_flip_strategy, self).__init__()
        self._martingale = self.Param("Martingale", 1.8) \
            .SetDisplay("Martingale", "Volume multiplier after loss", "General")
        self._max_volume = self.Param("MaxVolume", 1.0) \
            .SetDisplay("Max Volume", "Upper limit for volume", "General")
        self._take_profit = self.Param("TakeProfit", 50) \
            .SetDisplay("Take Profit", "Profit target in steps", "Risk")
        self._stop_loss = self.Param("StopLoss", 25) \
            .SetDisplay("Stop Loss", "Loss limit in steps", "Risk")
        self._trailing_start = self.Param("TrailingStart", 14) \
            .SetDisplay("Trailing Start", "Steps to activate trailing", "Risk")
        self._trailing_stop = self.Param("TrailingStop", 3) \
            .SetDisplay("Trailing Stop", "Trailing distance in steps", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Time frame for analysis", "General")
        self._entry_price = 0.0
        self._current_volume = 0.0
        self._trailing_level = 0.0
        self._is_long = False
        self._last_trade_loss = False

    @property
    def martingale(self):
        return self._martingale.Value

    @property
    def max_volume(self):
        return self._max_volume.Value

    @property
    def take_profit(self):
        return self._take_profit.Value

    @property
    def stop_loss(self):
        return self._stop_loss.Value

    @property
    def trailing_start(self):
        return self._trailing_start.Value

    @property
    def trailing_stop(self):
        return self._trailing_stop.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(coin_flip_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._current_volume = 0.0
        self._trailing_level = 0.0
        self._is_long = False
        self._last_trade_loss = False

    def OnStarted2(self, time):
        super(coin_flip_strategy, self).OnStarted2(time)
        self._current_volume = float(self.Volume)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _exit_position(self, close_price, is_loss):
        if self._is_long:
            self.SellMarket()
        else:
            self.BuyMarket()
        self._last_trade_loss = is_loss
        self._entry_price = 0.0
        self._trailing_level = 0.0

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        tp = int(self.take_profit)
        sl = int(self.stop_loss)
        ts_start = int(self.trailing_start)
        ts_stop = int(self.trailing_stop)
        if self.Position != 0:
            if self._entry_price == 0.0:
                return
            if self._is_long:
                profit_pct = (close - self._entry_price) / self._entry_price * 100.0
            else:
                profit_pct = (self._entry_price - close) / self._entry_price * 100.0
            # Update trailing stop if in profit
            if ts_start > 0 and profit_pct >= ts_start * 0.1:
                if self._is_long:
                    new_level = close * (1.0 - ts_stop * 0.1 / 100.0)
                else:
                    new_level = close * (1.0 + ts_stop * 0.1 / 100.0)
                if self._trailing_level == 0.0:
                    self._trailing_level = new_level
                elif self._is_long and new_level > self._trailing_level:
                    self._trailing_level = new_level
                elif not self._is_long and new_level < self._trailing_level:
                    self._trailing_level = new_level
            # Check TP
            if profit_pct >= tp * 0.1:
                self._exit_position(close, False)
                return
            # Check SL
            if profit_pct <= -sl * 0.1:
                self._exit_position(close, True)
                return
            # Check trailing
            if self._trailing_level != 0.0:
                if (self._is_long and close <= self._trailing_level) or \
                   (not self._is_long and close >= self._trailing_level):
                    self._exit_position(close, close < self._entry_price)
            return
        # No open position - decide direction
        flip = random.randint(0, 1)
        self._is_long = (flip == 0)
        if self._last_trade_loss:
            self._current_volume = min(self._current_volume * float(self.martingale), float(self.max_volume))
        else:
            self._current_volume = float(self.Volume)
        self._entry_price = close
        self._trailing_level = 0.0
        if self._is_long:
            self.BuyMarket()
        else:
            self.SellMarket()

    def CreateClone(self):
        return coin_flip_strategy()
