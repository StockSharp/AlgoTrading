import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class macd_enhanced_mtf_with_stop_loss_strategy(Strategy):
    """
    MACD crossover with ATR-based stop loss.
    """

    def __init__(self):
        super(macd_enhanced_mtf_with_stop_loss_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 8).SetDisplay("Fast", "Fast EMA", "MACD")
        self._slow_length = self.Param("SlowLength", 17).SetDisplay("Slow", "Slow EMA", "MACD")
        self._atr_length = self.Param("AtrLength", 14).SetDisplay("ATR", "ATR period", "Risk")
        self._stop_atr_mult = self.Param("StopAtrMult", 3.0).SetDisplay("SL Mult", "ATR stop mult", "Risk")
        self._cooldown_bars = self.Param("CooldownBars", 10).SetDisplay("Cooldown", "Bars between signals", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candles", "General")

        self._prev_fast_above = False
        self._is_init = False
        self._stop_price = 0.0
        self._bars_from_signal = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(macd_enhanced_mtf_with_stop_loss_strategy, self).OnReseted()
        self._prev_fast_above = False
        self._is_init = False
        self._stop_price = 0.0
        self._bars_from_signal = 0

    def OnStarted2(self, time):
        super(macd_enhanced_mtf_with_stop_loss_strategy, self).OnStarted2(time)
        fast = ExponentialMovingAverage()
        fast.Length = self._fast_length.Value
        slow = ExponentialMovingAverage()
        slow.Length = self._slow_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, atr, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_val, slow_val, atr_val):
        if candle.State != CandleStates.Finished:
            return
        fast = float(fast_val)
        slow = float(slow_val)
        atr = float(atr_val)
        if fast == 0 or slow == 0:
            return
        is_fast_above = fast > slow
        if not self._is_init:
            self._prev_fast_above = is_fast_above
            self._is_init = True
            return
        self._bars_from_signal += 1
        close = float(candle.ClosePrice)
        cross_up = is_fast_above and not self._prev_fast_above
        cross_down = not is_fast_above and self._prev_fast_above
        can_signal = self._bars_from_signal >= self._cooldown_bars.Value
        if self.Position > 0 and self._stop_price > 0 and close <= self._stop_price:
            self.SellMarket()
            self._stop_price = 0
            self._bars_from_signal = 0
            self._prev_fast_above = is_fast_above
            return
        if self.Position < 0 and self._stop_price > 0 and close >= self._stop_price:
            self.BuyMarket()
            self._stop_price = 0
            self._bars_from_signal = 0
            self._prev_fast_above = is_fast_above
            return
        if can_signal and cross_up and self.Position <= 0:
            self.BuyMarket()
            self._stop_price = close - atr * self._stop_atr_mult.Value if atr > 0 else 0
            self._bars_from_signal = 0
        elif can_signal and cross_down and self.Position >= 0:
            self.SellMarket()
            self._stop_price = close + atr * self._stop_atr_mult.Value if atr > 0 else 0
            self._bars_from_signal = 0
        self._prev_fast_above = is_fast_above

    def CreateClone(self):
        return macd_enhanced_mtf_with_stop_loss_strategy()
