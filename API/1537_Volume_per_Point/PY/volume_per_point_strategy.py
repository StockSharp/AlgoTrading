import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class volume_per_point_strategy(Strategy):
    def __init__(self):
        super(volume_per_point_strategy, self).__init__()
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "Period for RSI", "Indicators")
        self._rsi_high = self.Param("RsiHigh", 65) \
            .SetDisplay("RSI Above", "Upper RSI threshold", "Filters")
        self._rsi_low = self.Param("RsiLow", 35) \
            .SetDisplay("RSI Below", "Lower RSI threshold", "Filters")
        self._use_rsi_filter = self.Param("UseRsiFilter", True) \
            .SetDisplay("Use RSI Filter", "Enable RSI filtering", "Filters")
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 12) \
            .SetDisplay("Signal Cooldown", "Bars to wait between trades", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

    @property
    def rsi_length(self):
        return self._rsi_length.Value

    @property
    def rsi_high(self):
        return self._rsi_high.Value

    @property
    def rsi_low(self):
        return self._rsi_low.Value

    @property
    def use_rsi_filter(self):
        return self._use_rsi_filter.Value

    @property
    def signal_cooldown_bars(self):
        return self._signal_cooldown_bars.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(volume_per_point_strategy, self).OnReseted()

    def OnStarted(self, time):
        super(volume_per_point_strategy, self).OnStarted(time)
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def on_process(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
        if self._prev_range == 0:
            self._prev_range = candle.HighPrice - candle.LowPrice
            self._prev_volume = candle.TotalVolume
            self._prev_close = candle.ClosePrice
            return
        step = ((Security.PriceStep if Security is not None else None) if (Security.PriceStep if Security is not None else None) is not None else 0.0001)
        range = max(candle.HighPrice - candle.LowPrice, step)
        previous_range = max(self._prev_range, step)
        volume = candle.TotalVolume
        volume_per_point = volume / range
        previous_volume_per_point = self._prev_volume / previous_range
        bullish_impulse = candle.ClosePrice > candle.OpenPrice and candle.ClosePrice > self._prev_close
        bearish_impulse = candle.ClosePrice < candle.OpenPrice and candle.ClosePrice < self._prev_close
        buy_signal = volume_per_point >= previous_volume_per_point * 1.5 and bullish_impulse and (not self.use_rsi_filter or rsi_value <= self.rsi_low)
        sell_signal = volume_per_point >= previous_volume_per_point * 1.5 and bearish_impulse and (not self.use_rsi_filter or rsi_value >= self.rsi_high)
        if self._cooldown_remaining == 0 and buy_signal and self.Position <= 0:
            self.BuyMarket()
            self._cooldown_remaining = self.signal_cooldown_bars
        elif self._cooldown_remaining == 0 and sell_signal and self.Position >= 0:
            self.SellMarket()
            self._cooldown_remaining = self.signal_cooldown_bars
        self._prev_range = range
        self._prev_volume = volume
        self._prev_close = candle.ClosePrice

    def CreateClone(self):
        return volume_per_point_strategy()
