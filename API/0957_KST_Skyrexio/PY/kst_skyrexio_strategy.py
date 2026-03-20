import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import (RateOfChange, SimpleMovingAverage, AverageTrueRange,
                                         ChoppinessIndex, SmoothedMovingAverage, ExponentialMovingAverage)
from StockSharp.Algo.Strategies import Strategy


class kst_skyrexio_strategy(Strategy):
    def __init__(self):
        super(kst_skyrexio_strategy, self).__init__()
        self._atr_stop_loss = self.Param("AtrStopLoss", 1.5) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Stop Loss", "ATR stop-loss multiplier", "Stops")
        self._atr_take_profit = self.Param("AtrTakeProfit", 3.5) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Take Profit", "ATR take-profit multiplier", "Stops")
        self._filter_ma_length = self.Param("FilterMaLength", 200) \
            .SetGreaterThanZero() \
            .SetDisplay("Filter MA Length", "Length of trend filter", "Filter")
        self._chop_threshold = self.Param("ChopThreshold", 50.0) \
            .SetDisplay("Choppiness Threshold", "Threshold for choppiness index", "Choppiness")
        self._chop_length = self.Param("ChopLength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("Choppiness Length", "Choppiness index period", "Choppiness")
        self._roc_len1 = self.Param("RocLen1", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("ROC Length 1", "First ROC length", "KST")
        self._roc_len2 = self.Param("RocLen2", 15) \
            .SetGreaterThanZero() \
            .SetDisplay("ROC Length 2", "Second ROC length", "KST")
        self._roc_len3 = self.Param("RocLen3", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("ROC Length 3", "Third ROC length", "KST")
        self._roc_len4 = self.Param("RocLen4", 30) \
            .SetGreaterThanZero() \
            .SetDisplay("ROC Length 4", "Fourth ROC length", "KST")
        self._sma_len1 = self.Param("SmaLen1", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("SMA Length 1", "First SMA length", "KST")
        self._sma_len2 = self.Param("SmaLen2", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("SMA Length 2", "Second SMA length", "KST")
        self._sma_len3 = self.Param("SmaLen3", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("SMA Length 3", "Third SMA length", "KST")
        self._sma_len4 = self.Param("SmaLen4", 15) \
            .SetGreaterThanZero() \
            .SetDisplay("SMA Length 4", "Fourth SMA length", "KST")
        self._signal_length = self.Param("SignalLength", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("Signal Length", "KST signal SMA length", "KST")
        self._max_entries = self.Param("MaxEntries", 45) \
            .SetGreaterThanZero() \
            .SetDisplay("Max Entries", "Maximum entries per run", "Risk")
        self._cooldown_bars = self.Param("CooldownBars", 120) \
            .SetGreaterThanZero() \
            .SetDisplay("Cooldown Bars", "Minimum bars between entries", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_kst = 0.0
        self._prev_sig = 0.0
        self._stop_loss = 0.0
        self._take_profit = 0.0
        self._entries_executed = 0
        self._bars_since_signal = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(kst_skyrexio_strategy, self).OnReseted()
        self._prev_kst = 0.0
        self._prev_sig = 0.0
        self._stop_loss = 0.0
        self._take_profit = 0.0
        self._entries_executed = 0
        self._bars_since_signal = 0

    def OnStarted(self, time):
        super(kst_skyrexio_strategy, self).OnStarted(time)
        self._entries_executed = 0
        self._bars_since_signal = self._cooldown_bars.Value
        self._roc1 = RateOfChange()
        self._roc1.Length = self._roc_len1.Value
        self._sma1 = SimpleMovingAverage()
        self._sma1.Length = self._sma_len1.Value
        self._roc2 = RateOfChange()
        self._roc2.Length = self._roc_len2.Value
        self._sma2 = SimpleMovingAverage()
        self._sma2.Length = self._sma_len2.Value
        self._roc3 = RateOfChange()
        self._roc3.Length = self._roc_len3.Value
        self._sma3 = SimpleMovingAverage()
        self._sma3.Length = self._sma_len3.Value
        self._roc4 = RateOfChange()
        self._roc4.Length = self._roc_len4.Value
        self._sma4 = SimpleMovingAverage()
        self._sma4.Length = self._sma_len4.Value
        self._signal_sma = SimpleMovingAverage()
        self._signal_sma.Length = self._signal_length.Value
        self._atr = AverageTrueRange()
        self._atr.Length = 14
        self._chop = ChoppinessIndex()
        self._chop.Length = self._chop_length.Value
        self._filter_ma = ExponentialMovingAverage()
        self._filter_ma.Length = self._filter_ma_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._filter_ma, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._filter_ma)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, filter_val):
        if candle.State != CandleStates.Finished:
            return
        self._bars_since_signal += 1
        close = float(candle.ClosePrice)
        fv = float(filter_val)
        atr_result = self._atr.Process(candle)
        chop_result = self._chop.Process(candle)
        if not self._atr.IsFormed or not self._chop.IsFormed:
            return
        atr_v = float(atr_result.ToDecimal())
        chop_v = float(chop_result.ToDecimal())
        from StockSharp.Algo.Indicators import DecimalIndicatorValue
        t = candle.OpenTime
        iv = DecimalIndicatorValue(self._roc1, close, t)
        iv.IsFinal = True
        r1 = float(self._sma1.Process(self._roc1.Process(iv)).ToDecimal())
        iv2 = DecimalIndicatorValue(self._roc2, close, t)
        iv2.IsFinal = True
        r2 = float(self._sma2.Process(self._roc2.Process(iv2)).ToDecimal())
        iv3 = DecimalIndicatorValue(self._roc3, close, t)
        iv3.IsFinal = True
        r3 = float(self._sma3.Process(self._roc3.Process(iv3)).ToDecimal())
        iv4 = DecimalIndicatorValue(self._roc4, close, t)
        iv4.IsFinal = True
        r4 = float(self._sma4.Process(self._roc4.Process(iv4)).ToDecimal())
        if not self._sma1.IsFormed or not self._sma2.IsFormed or not self._sma3.IsFormed or not self._sma4.IsFormed:
            return
        kst = r1 + 2.0 * r2 + 3.0 * r3 + 4.0 * r4
        sig_iv = DecimalIndicatorValue(self._signal_sma, kst, t)
        sig_iv.IsFinal = True
        sig_result = self._signal_sma.Process(sig_iv)
        if not self._signal_sma.IsFormed:
            self._prev_kst = kst
            return
        sig = float(sig_result.ToDecimal())
        chop_cond = chop_v < float(self._chop_threshold.Value)
        cross_up = self._prev_kst <= self._prev_sig and kst > sig
        sl_mult = float(self._atr_stop_loss.Value)
        tp_mult = float(self._atr_take_profit.Value)
        if self._entries_executed < self._max_entries.Value and self._bars_since_signal >= self._cooldown_bars.Value:
            if cross_up and close > fv and chop_cond and self.Position == 0:
                self._stop_loss = float(candle.LowPrice) - sl_mult * atr_v
                self._take_profit = close + tp_mult * atr_v
                self.BuyMarket()
                self._entries_executed += 1
                self._bars_since_signal = 0
        if self.Position > 0 and self._stop_loss > 0.0 and self._take_profit > 0.0:
            if float(candle.LowPrice) <= self._stop_loss or float(candle.HighPrice) >= self._take_profit:
                self.SellMarket()
                self._stop_loss = 0.0
                self._take_profit = 0.0
                self._bars_since_signal = 0
        self._prev_kst = kst
        self._prev_sig = sig

    def CreateClone(self):
        return kst_skyrexio_strategy()
