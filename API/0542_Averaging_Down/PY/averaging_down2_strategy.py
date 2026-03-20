import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class averaging_down2_strategy(Strategy):
    def __init__(self):
        super(averaging_down2_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Length", "RSI calculation length", "Indicators")
        self._ema_length = self.Param("EmaLength", 40) \
            .SetGreaterThanZero() \
            .SetDisplay("EMA Length", "EMA trend filter period", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 350) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Trading")
        self._prev_rsi = 0.0
        self._bar_index = 0
        self._last_trade_bar = 0

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
        super(averaging_down2_strategy, self).OnReseted()
        self._prev_rsi = 0.0
        self._bar_index = 0
        self._last_trade_bar = 0

    def OnStarted(self, time):
        super(averaging_down2_strategy, self).OnStarted(time)
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        ema = ExponentialMovingAverage()
        ema.Length = self._ema_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, ema, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, rsi_val, ema_val):
        if candle.State != CandleStates.Finished:
            return
        self._bar_index += 1
        rsi_v = float(rsi_val)
        ema_v = float(ema_val)
        close = float(candle.ClosePrice)
        cooldown_ok = self._bar_index - self._last_trade_bar > self.cooldown_bars
        long_signal = self._prev_rsi > 0 and self._prev_rsi < 40.0 and rsi_v >= 40.0 and close > ema_v
        short_signal = self._prev_rsi > 0 and self._prev_rsi > 60.0 and rsi_v <= 60.0 and close < ema_v
        if long_signal and self.Position <= 0 and cooldown_ok:
            self.BuyMarket()
            self._last_trade_bar = self._bar_index
        elif short_signal and self.Position >= 0 and cooldown_ok:
            self.SellMarket()
            self._last_trade_bar = self._bar_index
        self._prev_rsi = rsi_v

    def CreateClone(self):
        return averaging_down2_strategy()
