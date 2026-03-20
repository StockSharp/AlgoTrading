import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class vortex_cross_ma_confirmation_strategy(Strategy):
    """Manual Vortex indicator crossover with SMA trend confirmation."""
    def __init__(self):
        super(vortex_cross_ma_confirmation_strategy, self).__init__()
        self._vortex_len = self.Param("VortexLength", 14).SetGreaterThanZero().SetDisplay("Vortex Length", "Vortex period", "General")
        self._sma_len = self.Param("SmaLength", 9).SetGreaterThanZero().SetDisplay("SMA Length", "MA confirmation period", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(vortex_cross_ma_confirmation_strategy, self).OnReseted()
        self._vm_plus = []
        self._vm_minus = []
        self._true_ranges = []
        self._prev_high = None
        self._prev_low = None
        self._prev_close_val = None
        self._prev_vip = 0
        self._prev_vim = 0

    def OnStarted(self, time):
        super(vortex_cross_ma_confirmation_strategy, self).OnStarted(time)
        self._vm_plus = []
        self._vm_minus = []
        self._true_ranges = []
        self._prev_high = None
        self._prev_low = None
        self._prev_close_val = None
        self._prev_vip = 0
        self._prev_vim = 0

        sma = SimpleMovingAverage()
        sma.Length = self._sma_len.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(sma, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, sma_val):
        if candle.State != CandleStates.Finished:
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        sv = float(sma_val)
        vlen = self._vortex_len.Value

        if self._prev_high is None:
            self._prev_high = high
            self._prev_low = low
            self._prev_close_val = close
            return

        # Vortex movement
        vmp = abs(high - self._prev_low)
        vmm = abs(low - self._prev_high)
        tr = max(high - low, max(abs(high - self._prev_close_val), abs(low - self._prev_close_val)))

        self._vm_plus.append(vmp)
        self._vm_minus.append(vmm)
        self._true_ranges.append(tr)

        while len(self._vm_plus) > vlen:
            self._vm_plus.pop(0)
            self._vm_minus.pop(0)
            self._true_ranges.pop(0)

        self._prev_high = high
        self._prev_low = low
        self._prev_close_val = close

        if len(self._vm_plus) < vlen:
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

        long_signal = self._prev_vip < self._prev_vim and vip > vim and close > sv
        short_signal = self._prev_vip > self._prev_vim and vip < vim and close < sv

        if long_signal and self.Position <= 0:
            self.BuyMarket()
        elif short_signal and self.Position >= 0:
            self.SellMarket()

        self._prev_vip = vip
        self._prev_vim = vim

    def CreateClone(self):
        return vortex_cross_ma_confirmation_strategy()
