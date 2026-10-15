import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class vm_matrix_double_zero_strategy(Strategy):
    """Dual EMA crossover with RSI confirmation and ATR-based stop loss."""
    def __init__(self):
        super(vm_matrix_double_zero_strategy, self).__init__()
        self._fast_len = self.Param("FastEmaLength", 12).SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_len = self.Param("SlowEmaLength", 26).SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._rsi_len = self.Param("RsiLength", 14).SetDisplay("RSI Length", "RSI period", "Indicators")
        self._atr_len = self.Param("AtrLength", 14).SetDisplay("ATR Length", "ATR period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(vm_matrix_double_zero_strategy, self).OnReseted()
        self._prev_fast = 0
        self._prev_slow = 0
        self._entry_price = 0

    def OnStarted2(self, time):
        super(vm_matrix_double_zero_strategy, self).OnStarted2(time)
        self._prev_fast = 0
        self._prev_slow = 0
        self._entry_price = 0

        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self._fast_len.Value
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self._slow_len.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_len.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_len.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(fast_ema, slow_ema, rsi, atr, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_ema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, fast_val, slow_val, rsi_val, atr_val):
        if candle.State != CandleStates.Finished:
            return

        fv = float(fast_val)
        sv = float(slow_val)
        rv = float(rsi_val)
        av = float(atr_val)
        close = float(candle.ClosePrice)

        if self._prev_fast == 0 or self._prev_slow == 0 or av <= 0:
            self._prev_fast = fv
            self._prev_slow = sv
            return

        # Exit conditions
        if self.Position > 0:
            if (fv < sv and self._prev_fast >= self._prev_slow) or close <= self._entry_price - av * 2:
                self.SellMarket()
                self._entry_price = 0
        elif self.Position < 0:
            if (fv > sv and self._prev_fast <= self._prev_slow) or close >= self._entry_price + av * 2:
                self.BuyMarket()
                self._entry_price = 0

        # Entry conditions
        if self.Position == 0:
            if fv > sv and self._prev_fast <= self._prev_slow and rv > 50:
                self._entry_price = close
                self.BuyMarket()
            elif fv < sv and self._prev_fast >= self._prev_slow and rv < 50:
                self._entry_price = close
                self.SellMarket()

        self._prev_fast = fv
        self._prev_slow = sv

    def CreateClone(self):
        return vm_matrix_double_zero_strategy()
