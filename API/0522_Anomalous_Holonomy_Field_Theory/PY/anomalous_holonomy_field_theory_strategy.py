import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class anomalous_holonomy_field_theory_strategy(Strategy):
    def __init__(self):
        super(anomalous_holonomy_field_theory_strategy, self).__init__()
        self._signal_threshold = self.Param("SignalThreshold", 0.1) \
            .SetDisplay("Signal Threshold", "Absolute signal level required for trades", "Parameters")
        self._cooldown_bars = self.Param("CooldownBars", 50) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._bar_index = 0
        self._last_trade_bar = -200

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
        super(anomalous_holonomy_field_theory_strategy, self).OnReseted()
        self._bar_index = 0
        self._last_trade_bar = -200

    def OnStarted(self, time):
        super(anomalous_holonomy_field_theory_strategy, self).OnStarted(time)
        ema20 = ExponentialMovingAverage()
        ema20.Length = 20
        ema50 = ExponentialMovingAverage()
        ema50.Length = 50
        rsi = RelativeStrengthIndex()
        rsi.Length = 14
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema20, ema50, rsi, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema20)
            self.DrawIndicator(area, ema50)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, ema20_val, ema50_val, rsi_val):
        if candle.State != CandleStates.Finished:
            return
        self._bar_index += 1
        close = float(candle.ClosePrice)
        e20 = float(ema20_val)
        e50 = float(ema50_val)
        rsi_v = float(rsi_val)
        cooldown_ok = self._bar_index - self._last_trade_bar > self.cooldown_bars
        signal = 0.0
        if close > e20:
            signal += 1.5 if close > e50 else 1.0
        elif close < e20:
            signal -= 1.5 if close < e50 else 1.0
        vol = float(candle.TotalVolume) if candle.TotalVolume != 0 else 0.0
        tp = float(candle.TotalPrice) if hasattr(candle, 'TotalPrice') else 0.0
        vwap = tp / vol if vol != 0 else close
        vwap_dist = (close - vwap) / close * 100.0 if close != 0 else 0.0
        vwap_dist = max(-2.0, min(2.0, vwap_dist))
        signal += vwap_dist
        if rsi_v < 30.0 and signal > 0:
            signal += 1.5
        elif rsi_v < 40.0 and signal > 0:
            signal += 0.5
        elif rsi_v > 70.0 and signal < 0:
            signal -= 1.5
        elif rsi_v > 60.0 and signal < 0:
            signal -= 0.5
        threshold = float(self._signal_threshold.Value)
        if signal >= threshold and self.Position <= 0 and cooldown_ok:
            self.BuyMarket()
            self._last_trade_bar = self._bar_index
        elif signal <= -threshold and self.Position >= 0 and cooldown_ok:
            self.SellMarket()
            self._last_trade_bar = self._bar_index

    def CreateClone(self):
        return anomalous_holonomy_field_theory_strategy()
