import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class volume_by_session_strategy(Strategy):
    def __init__(self):
        super(volume_by_session_strategy, self).__init__()
        self._vol_avg_length = self.Param("VolAvgLength", 20) \
            .SetDisplay("Vol Avg Length", "Volume average period", "Parameters")
        self._vol_mult = self.Param("VolMult", 2.25) \
            .SetDisplay("Vol Multiplier", "Volume spike multiplier", "Parameters")
        self._stop_pct = self.Param("StopPct", 0.5) \
            .SetDisplay("Stop %", "Stop loss percent", "Risk")
        self._tp_pct = self.Param("TpPct", 1.0) \
            .SetDisplay("TP %", "Take profit percent", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 12) \
            .SetDisplay("Signal Cooldown", "Bars to wait between new entries", "Trading")
        self._volumes = []
        self._volume_sum = 0.0
        self._entry_price = 0.0
        self._stop_dist = 0.0
        self._prev_high = None
        self._prev_low = None
        self._cooldown_remaining = 0

    @property
    def vol_avg_length(self):
        return self._vol_avg_length.Value

    @property
    def vol_mult(self):
        return self._vol_mult.Value

    @property
    def stop_pct(self):
        return self._stop_pct.Value

    @property
    def tp_pct(self):
        return self._tp_pct.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def signal_cooldown_bars(self):
        return self._signal_cooldown_bars.Value

    def OnReseted(self):
        super(volume_by_session_strategy, self).OnReseted()
        self._volumes = []
        self._volume_sum = 0.0
        self._entry_price = 0.0
        self._stop_dist = 0.0
        self._prev_high = None
        self._prev_low = None
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(volume_by_session_strategy, self).OnStarted(time)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle):
        if candle.State != CandleStates.Finished:
            return
        vol = float(candle.TotalVolume)
        close = float(candle.ClosePrice)
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
        # TP/SL
        tp_pct = float(self.tp_pct)
        stop_pct = float(self.stop_pct)
        if self.Position > 0 and self._entry_price > 0 and self._stop_dist > 0:
            if close <= self._entry_price - self._stop_dist or close >= self._entry_price + self._stop_dist * (tp_pct / stop_pct):
                self.SellMarket()
                self._entry_price = 0
                self._stop_dist = 0
                self._cooldown_remaining = self.signal_cooldown_bars
        elif self.Position < 0 and self._entry_price > 0 and self._stop_dist > 0:
            if close >= self._entry_price + self._stop_dist or close <= self._entry_price - self._stop_dist * (tp_pct / stop_pct):
                self.BuyMarket()
                self._entry_price = 0
                self._stop_dist = 0
                self._cooldown_remaining = self.signal_cooldown_bars
        if len(self._volumes) >= self.vol_avg_length and self._prev_high is not None and self._prev_low is not None and self._cooldown_remaining == 0 and self.Position == 0:
            avg_vol = self._volume_sum / len(self._volumes)
            high_vol = vol >= avg_vol * float(self.vol_mult)
            bullish_breakout = close > self._prev_high and close > float(candle.OpenPrice)
            bearish_breakout = close < self._prev_low and close < float(candle.OpenPrice)
            price_step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 0.0
            min_body = max(float(candle.ClosePrice) * 0.001, price_step)
            body = abs(float(candle.ClosePrice) - float(candle.OpenPrice))
            if high_vol and bullish_breakout and body >= min_body:
                self.BuyMarket()
                self._entry_price = close
                self._stop_dist = close * stop_pct / 100.0
                self._cooldown_remaining = self.signal_cooldown_bars
            elif high_vol and bearish_breakout and body >= min_body:
                self.SellMarket()
                self._entry_price = close
                self._stop_dist = close * stop_pct / 100.0
                self._cooldown_remaining = self.signal_cooldown_bars
        self._volumes.append(vol)
        self._volume_sum += vol
        if len(self._volumes) > self.vol_avg_length:
            self._volume_sum -= self._volumes[0]
            self._volumes.pop(0)
        self._prev_high = float(candle.HighPrice)
        self._prev_low = float(candle.LowPrice)

    def CreateClone(self):
        return volume_by_session_strategy()
