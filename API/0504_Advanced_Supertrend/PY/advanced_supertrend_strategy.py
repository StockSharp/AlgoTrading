import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SuperTrend, ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class advanced_supertrend_strategy(Strategy):
    def __init__(self):
        super(advanced_supertrend_strategy, self).__init__()
        self._atr_length = self.Param("AtrLength", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Length", "ATR period for SuperTrend", "SuperTrend")
        self._multiplier = self.Param("Multiplier", 3.0) \
            .SetDisplay("Multiplier", "SuperTrend multiplier", "SuperTrend")
        self._ma_length = self.Param("MaLength", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("EMA Length", "EMA period for trend filter", "Filters")
        self._atr_stop_length = self.Param("AtrStopLength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Stop Length", "ATR period for stops", "Risk")
        self._sl_multiplier = self.Param("SlMultiplier", 2.0) \
            .SetDisplay("SL Multiplier", "Stop loss ATR multiplier", "Risk")
        self._tp_multiplier = self.Param("TpMultiplier", 4.0) \
            .SetDisplay("TP Multiplier", "Take profit ATR multiplier", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")
        self._prev_up_trend = False
        self._has_prev = False
        self._entry_price = 0.0
        self._atr_at_entry = 0.0
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
        super(advanced_supertrend_strategy, self).OnReseted()
        self._prev_up_trend = False
        self._has_prev = False
        self._entry_price = 0.0
        self._atr_at_entry = 0.0
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(advanced_supertrend_strategy, self).OnStarted(time)
        st = SuperTrend()
        st.Length = self._atr_length.Value
        st.Multiplier = self._multiplier.Value
        ema = ExponentialMovingAverage()
        ema.Length = self._ma_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_stop_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(st, ema, atr, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, st)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, st_value, ema_value, atr_value):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        if st_value.IsEmpty or ema_value.IsEmpty or atr_value.IsEmpty:
            return
        up_trend = st_value.IsUpTrend
        ema_v = float(ema_value.GetValue[float]())
        atr_v = float(atr_value.GetValue[float]())
        close = float(candle.ClosePrice)
        if not self._has_prev:
            self._prev_up_trend = up_trend
            self._has_prev = True
            return
        sl_mult = float(self._sl_multiplier.Value)
        tp_mult = float(self._tp_multiplier.Value)
        if self.Position > 0 and self._entry_price > 0 and self._atr_at_entry > 0:
            sl = self._entry_price - self._atr_at_entry * sl_mult
            tp = self._entry_price + self._atr_at_entry * tp_mult
            if close <= sl or close >= tp:
                self.SellMarket()
                self._entry_price = 0.0
                self._cooldown_remaining = self.cooldown_bars
                self._prev_up_trend = up_trend
                return
        elif self.Position < 0 and self._entry_price > 0 and self._atr_at_entry > 0:
            sl = self._entry_price + self._atr_at_entry * sl_mult
            tp = self._entry_price - self._atr_at_entry * tp_mult
            if close >= sl or close <= tp:
                self.BuyMarket()
                self._entry_price = 0.0
                self._cooldown_remaining = self.cooldown_bars
                self._prev_up_trend = up_trend
                return
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_up_trend = up_trend
            return
        bullish_flip = up_trend and not self._prev_up_trend
        bearish_flip = not up_trend and self._prev_up_trend
        if bullish_flip and close > ema_v and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._entry_price = close
            self._atr_at_entry = atr_v
            self._cooldown_remaining = self.cooldown_bars
        elif bearish_flip and close < ema_v and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._entry_price = close
            self._atr_at_entry = atr_v
            self._cooldown_remaining = self.cooldown_bars
        self._prev_up_trend = up_trend

    def CreateClone(self):
        return advanced_supertrend_strategy()
