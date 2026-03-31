import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class megabar_breakout_strategy(Strategy):
    def __init__(self):
        super(megabar_breakout_strategy, self).__init__()
        self._avg_period = self.Param("AvgPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Average Period", "Rolling average period", "General")
        self._multiplier = self.Param("Multiplier", 1.8) \
            .SetGreaterThanZero() \
            .SetDisplay("Multiplier", "Volume and range multiplier", "General")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Period", "RSI period", "General")
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 8) \
            .SetGreaterThanZero() \
            .SetDisplay("Signal Cooldown Bars", "Minimum bars between entries", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(10))) \
            .SetDisplay("Candle Type", "Candles timeframe", "General")
        self._volumes = []
        self._ranges = []
        self._bars_from_signal = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(megabar_breakout_strategy, self).OnReseted()
        self._volumes = []
        self._ranges = []
        self._bars_from_signal = 0

    def OnStarted2(self, time):
        super(megabar_breakout_strategy, self).OnStarted2(time)
        self._volumes = []
        self._ranges = []
        self._bars_from_signal = self._signal_cooldown_bars.Value
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self._rsi_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._rsi, self.OnProcess).Start()

    def OnProcess(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return
        vol = float(candle.TotalVolume)
        rng = abs(float(candle.ClosePrice) - float(candle.OpenPrice))
        ap = self._avg_period.Value
        self._volumes.append(vol)
        self._ranges.append(rng)
        if len(self._volumes) > ap:
            self._volumes.pop(0)
        if len(self._ranges) > ap:
            self._ranges.pop(0)
        if not self._rsi.IsFormed or len(self._volumes) < ap:
            return
        avg_vol = sum(self._volumes) / len(self._volumes)
        avg_rng = sum(self._ranges) / len(self._ranges)
        mult = float(self._multiplier.Value)
        volume_ok = vol > avg_vol * mult
        range_ok = rng > avg_rng * mult
        rv = float(rsi_value)
        close = float(candle.ClosePrice)
        opn = float(candle.OpenPrice)
        self._bars_from_signal += 1
        cd = self._signal_cooldown_bars.Value
        if self._bars_from_signal >= cd and close > opn and volume_ok and range_ok and rv > 52.0 and self.Position <= 0:
            self.BuyMarket()
            self._bars_from_signal = 0
        elif self._bars_from_signal >= cd and close < opn and volume_ok and range_ok and rv < 48.0 and self.Position >= 0:
            self.SellMarket()
            self._bars_from_signal = 0

    def CreateClone(self):
        return megabar_breakout_strategy()
