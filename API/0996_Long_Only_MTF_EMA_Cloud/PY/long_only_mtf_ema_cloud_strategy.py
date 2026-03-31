import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class long_only_mtf_ema_cloud_strategy(Strategy):
    def __init__(self):
        super(long_only_mtf_ema_cloud_strategy, self).__init__()
        self._short_length = self.Param("ShortLength", 25) \
            .SetGreaterThanZero() \
            .SetDisplay("Short EMA", "Short EMA period", "Indicators")
        self._long_length = self.Param("LongLength", 65) \
            .SetGreaterThanZero() \
            .SetDisplay("Long EMA", "Long EMA period", "Indicators")
        self._stop_loss_percent = self.Param("StopLossPercent", 7.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop loss percent", "Risk")
        self._take_profit_percent = self.Param("TakeProfitPercent", 12.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Take Profit %", "Take profit percent", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(10))) \
            .SetDisplay("Candle Type", "Candles", "General")
        self._prev_short = 0.0
        self._prev_long = 0.0
        self._entry_price = 0.0
        self._is_initialized = False
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(long_only_mtf_ema_cloud_strategy, self).OnReseted()
        self._prev_short = 0.0
        self._prev_long = 0.0
        self._entry_price = 0.0
        self._is_initialized = False
        self._cooldown = 0

    def OnStarted2(self, time):
        super(long_only_mtf_ema_cloud_strategy, self).OnStarted2(time)
        self._prev_short = 0.0
        self._prev_long = 0.0
        self._entry_price = 0.0
        self._is_initialized = False
        self._cooldown = 0
        self._short_ema = ExponentialMovingAverage()
        self._short_ema.Length = self._short_length.Value
        self._long_ema = ExponentialMovingAverage()
        self._long_ema.Length = self._long_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._short_ema, self._long_ema, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._short_ema)
            self.DrawIndicator(area, self._long_ema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, short_val, long_val):
        if candle.State != CandleStates.Finished:
            return
        if not self._short_ema.IsFormed or not self._long_ema.IsFormed:
            return
        sv = float(short_val)
        lv = float(long_val)
        if not self._is_initialized:
            self._prev_short = sv
            self._prev_long = lv
            self._is_initialized = True
            return
        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_short = sv
            self._prev_long = lv
            return
        close = float(candle.ClosePrice)
        crossed_up = self._prev_short <= self._prev_long and sv > lv
        crossed_down = self._prev_short >= self._prev_long and sv < lv
        if crossed_up and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(abs(self.Position))
            self.BuyMarket()
            self._entry_price = close
            self._cooldown = 10
        elif crossed_down and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(abs(self.Position))
            self.SellMarket()
            self._entry_price = close
            self._cooldown = 10
        sl_pct = float(self._stop_loss_percent.Value) / 100.0
        tp_pct = float(self._take_profit_percent.Value) / 100.0
        if self.Position > 0 and self._entry_price > 0.0:
            sl = self._entry_price * (1.0 - sl_pct)
            tp = self._entry_price * (1.0 + tp_pct)
            if close <= sl or close >= tp:
                self.SellMarket(abs(self.Position))
                self._cooldown = 20
        if self.Position < 0 and self._entry_price > 0.0:
            sl = self._entry_price * (1.0 + sl_pct)
            tp = self._entry_price * (1.0 - tp_pct)
            if close >= sl or close <= tp:
                self.BuyMarket(abs(self.Position))
                self._cooldown = 20
        self._prev_short = sv
        self._prev_long = lv

    def CreateClone(self):
        return long_only_mtf_ema_cloud_strategy()
