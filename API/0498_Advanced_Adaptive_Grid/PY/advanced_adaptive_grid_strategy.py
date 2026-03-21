import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class advanced_adaptive_grid_strategy(Strategy):
    def __init__(self):
        super(advanced_adaptive_grid_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Length", "RSI period", "Indicators")
        self._rsi_overbought = self.Param("RsiOverbought", 70) \
            .SetDisplay("RSI Overbought", "Overbought level", "Indicators")
        self._rsi_oversold = self.Param("RsiOversold", 30) \
            .SetDisplay("RSI Oversold", "Oversold level", "Indicators")
        self._short_ma_length = self.Param("ShortMaLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Short MA", "Short moving average length", "Trend")
        self._long_ma_length = self.Param("LongMaLength", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("Long MA", "Long moving average length", "Trend")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
        self._take_profit_percent = self.Param("TakeProfitPercent", 3.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Take Profit %", "Take profit percentage", "Risk")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")
        self._entry_price = 0.0
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    @cooldown_bars.setter
    def cooldown_bars(self, value):
        self._cooldown_bars.Value = value

    def OnReseted(self):
        super(advanced_adaptive_grid_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(advanced_adaptive_grid_strategy, self).OnStarted(time)
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        short_ma = SimpleMovingAverage()
        short_ma.Length = self._short_ma_length.Value
        long_ma = SimpleMovingAverage()
        long_ma.Length = self._long_ma_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, short_ma, long_ma, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, short_ma)
            self.DrawIndicator(area, long_ma)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, rsi_val, short_ma_val, long_ma_val):
        if candle.State != CandleStates.Finished:
            return
        current_price = float(candle.ClosePrice)
        rsi_v = float(rsi_val)
        short_v = float(short_ma_val)
        long_v = float(long_ma_val)
        bullish = short_v > long_v
        bearish = short_v < long_v
        sl_pct = float(self._stop_loss_percent.Value)
        tp_pct = float(self._take_profit_percent.Value)
        ob = float(self._rsi_overbought.Value)
        os_level = float(self._rsi_oversold.Value)
        if self.Position > 0 and self._entry_price > 0:
            stop_price = self._entry_price * (1.0 - sl_pct / 100.0)
            tp_price = self._entry_price * (1.0 + tp_pct / 100.0)
            if current_price <= stop_price or current_price >= tp_price:
                self.SellMarket()
                self._entry_price = 0.0
                self._cooldown_remaining = self.cooldown_bars
                return
        elif self.Position < 0 and self._entry_price > 0:
            stop_price = self._entry_price * (1.0 + sl_pct / 100.0)
            tp_price = self._entry_price * (1.0 - tp_pct / 100.0)
            if current_price >= stop_price or current_price <= tp_price:
                self.BuyMarket()
                self._entry_price = 0.0
                self._cooldown_remaining = self.cooldown_bars
                return
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return
        if bullish and rsi_v < os_level and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._entry_price = current_price
            self._cooldown_remaining = self.cooldown_bars
        elif bearish and rsi_v > ob and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._entry_price = current_price
            self._cooldown_remaining = self.cooldown_bars
        elif self.Position > 0 and bearish and rsi_v > ob:
            self.SellMarket()
            self._entry_price = 0.0
            self._cooldown_remaining = self.cooldown_bars
        elif self.Position < 0 and bullish and rsi_v < os_level:
            self.BuyMarket()
            self._entry_price = 0.0
            self._cooldown_remaining = self.cooldown_bars

    def CreateClone(self):
        return advanced_adaptive_grid_strategy()
