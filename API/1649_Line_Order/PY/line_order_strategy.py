import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class line_order_strategy(Strategy):
    def __init__(self):
        super(line_order_strategy, self).__init__()
        self._ma_length = self.Param("MaLength", 20) \
            .SetDisplay("MA Length", "Moving average period for line", "Indicators")
        self._stop_loss_pct = self.Param("StopLossPct", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
        self._take_profit_pct = self.Param("TakeProfitPct", 3.0) \
            .SetDisplay("Take Profit %", "Take profit percentage", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._entry_price = 0.0
        self._prev_close = 0.0
        self._prev_ma = 0.0
        self._has_prev = False

    @property
    def ma_length(self):
        return self._ma_length.Value

    @property
    def stop_loss_pct(self):
        return self._stop_loss_pct.Value

    @property
    def take_profit_pct(self):
        return self._take_profit_pct.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(line_order_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._prev_close = 0.0
        self._prev_ma = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(line_order_strategy, self).OnStarted(time)
        ma = SimpleMovingAverage()
        ma.Length = self.ma_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ma, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, ma_val):
        if candle.State != CandleStates.Finished:
            return
        close = candle.ClosePrice
        if not self._has_prev:
            self._prev_close = close
            self._prev_ma = ma_val
            self._has_prev = True
            return
        # Check stop-loss / take-profit for existing positions
        if self.Position > 0 and self._entry_price > 0:
            if (close <= self._entry_price * (1 - self.stop_loss_pct / 100.0) or
                    close >= self._entry_price * (1 + self.take_profit_pct / 100.0)):
                self.SellMarket()
                self._entry_price = 0
                self._prev_close = close
                self._prev_ma = ma_val
                return
        elif self.Position < 0 and self._entry_price > 0:
            if (close >= self._entry_price * (1 + self.stop_loss_pct / 100.0) or
                    close <= self._entry_price * (1 - self.take_profit_pct / 100.0)):
                self.BuyMarket()
                self._entry_price = 0
                self._prev_close = close
                self._prev_ma = ma_val
                return
        # Cross above MA line -> buy
        if self._prev_close <= self._prev_ma and close > ma_val:
            if self.Position < 0:
                self.BuyMarket()
                self._entry_price = 0
            if self.Position <= 0:
                self.BuyMarket()
                self._entry_price = close
        # Cross below MA line -> sell
        elif self._prev_close >= self._prev_ma and close < ma_val:
            if self.Position > 0:
                self.SellMarket()
                self._entry_price = 0
            if self.Position >= 0:
                self.SellMarket()
                self._entry_price = close
        self._prev_close = close
        self._prev_ma = ma_val

    def CreateClone(self):
        return line_order_strategy()
