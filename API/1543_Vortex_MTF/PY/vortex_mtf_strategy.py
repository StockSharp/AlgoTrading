import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class vortex_mtf_strategy(Strategy):
    def __init__(self):
        super(vortex_mtf_strategy, self).__init__()
        self._length = self.Param("Length", 14) \
            .SetDisplay("Vortex Length", "Period of the Vortex indicator", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(2))) \
            .SetDisplay("Candle Type", "Timeframe for Vortex calculation", "General")
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 4) \
            .SetDisplay("Signal Cooldown Bars", "Closed candles to wait before a new Vortex crossover entry", "General")
        self._vm_plus = []
        self._vm_minus = []
        self._true_ranges = []
        self._prev_high = None
        self._prev_low = None
        self._prev_close = None
        self._prev_vip = 0.0
        self._prev_vim = 0.0
        self._cooldown_remaining = 0

    @property
    def length(self):
        return self._length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def signal_cooldown_bars(self):
        return self._signal_cooldown_bars.Value

    def OnReseted(self):
        super(vortex_mtf_strategy, self).OnReseted()
        self._vm_plus = []
        self._vm_minus = []
        self._true_ranges = []
        self._prev_high = None
        self._prev_low = None
        self._prev_close = None
        self._prev_vip = 0.0
        self._prev_vim = 0.0
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(vortex_mtf_strategy, self).OnStarted2(time)
        sma = SimpleMovingAverage()
        sma.Length = 2
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, _dummy):
        if candle.State != CandleStates.Finished:
            return
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
        if self._prev_high is None:
            self._prev_high = high
            self._prev_low = low
            self._prev_close = close
            return
        vmp = abs(high - self._prev_low)
        vmm = abs(low - self._prev_high)
        tr = max(high - low,
            max(abs(high - self._prev_close),
                abs(low - self._prev_close)))
        self._vm_plus.append(vmp)
        self._vm_minus.append(vmm)
        self._true_ranges.append(tr)
        while len(self._vm_plus) > self.length:
            self._vm_plus.pop(0)
            self._vm_minus.pop(0)
            self._true_ranges.pop(0)
        self._prev_high = high
        self._prev_low = low
        self._prev_close = close
        if len(self._vm_plus) < self.length:
            return
        sum_tr = sum(self._true_ranges)
        if sum_tr == 0:
            return
        vip = sum(self._vm_plus) / sum_tr
        vim = sum(self._vm_minus) / sum_tr
        if self._prev_vip == 0 and self._prev_vim == 0:
            self._prev_vip = vip
            self._prev_vim = vim
            return
        if self._cooldown_remaining == 0 and self._prev_vip <= self._prev_vim and vip > vim and self.Position <= 0:
            self.BuyMarket()
            self._cooldown_remaining = self.signal_cooldown_bars
        elif self._cooldown_remaining == 0 and self._prev_vip >= self._prev_vim and vip < vim and self.Position >= 0:
            self.SellMarket()
            self._cooldown_remaining = self.signal_cooldown_bars
        self._prev_vip = vip
        self._prev_vim = vim

    def CreateClone(self):
        return vortex_mtf_strategy()
