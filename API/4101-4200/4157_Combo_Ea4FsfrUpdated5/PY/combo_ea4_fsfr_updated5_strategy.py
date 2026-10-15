import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class combo_ea4_fsfr_updated5_strategy(Strategy):
    """
    Combo EA: Triple EMA confirmation with RSI filter and ATR stops.
    """

    def __init__(self):
        super(combo_ea4_fsfr_updated5_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._fast_ema_length = self.Param("FastEmaLength", 8) \
            .SetDisplay("Fast EMA", "Fast EMA period.", "Indicators")
        self._slow_ema_length = self.Param("SlowEmaLength", 21) \
            .SetDisplay("Slow EMA", "Slow EMA period.", "Indicators")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "RSI period.", "Indicators")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Length", "ATR period.", "Indicators")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(combo_ea4_fsfr_updated5_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0

    def OnStarted2(self, time):
        super(combo_ea4_fsfr_updated5_strategy, self).OnStarted2(time)
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0

        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self._fast_ema_length.Value
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self._slow_ema_length.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_ema, slow_ema, rsi, atr, self.on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_ema)
            self.DrawOwnTrades(area)

    def on_process(self, candle, fast_val, slow_val, rsi_val, atr_val):
        if candle.State != CandleStates.Finished:
            return
        if self._prev_fast == 0.0 or self._prev_slow == 0.0 or atr_val <= 0:
            self._prev_fast = fast_val
            self._prev_slow = slow_val
            return

        close = float(candle.ClosePrice)

        if self.Position > 0:
            if (fast_val < slow_val and self._prev_fast >= self._prev_slow) or close <= self._entry_price - atr_val * 2:
                self.SellMarket()
                self._entry_price = 0.0
        elif self.Position < 0:
            if (fast_val > slow_val and self._prev_fast <= self._prev_slow) or close >= self._entry_price + atr_val * 2:
                self.BuyMarket()
                self._entry_price = 0.0

        if self.Position == 0:
            if fast_val > slow_val and self._prev_fast <= self._prev_slow and rsi_val > 50:
                self._entry_price = close
                self.BuyMarket()
            elif fast_val < slow_val and self._prev_fast >= self._prev_slow and rsi_val < 50:
                self._entry_price = close
                self.SellMarket()

        self._prev_fast = fast_val
        self._prev_slow = slow_val

    def CreateClone(self):
        return combo_ea4_fsfr_updated5_strategy()
