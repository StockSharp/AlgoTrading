import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class ma_crossover_tp_sl_5_ema_filter_strategy(Strategy):
    """
    MA crossover with TP/SL and EMA filter.
    """

    def __init__(self):
        super(ma_crossover_tp_sl_5_ema_filter_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 8).SetDisplay("Fast MA", "Fast MA period", "Indicators")
        self._slow_length = self.Param("SlowLength", 21).SetDisplay("Slow MA", "Slow MA period", "Indicators")
        self._ema_length = self.Param("EmaLength", 5).SetDisplay("EMA Filter", "EMA filter length", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Candles", "General")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._initialized = False
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ma_crossover_tp_sl_5_ema_filter_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._initialized = False
        self._cooldown = 0

    def OnStarted(self, time):
        super(ma_crossover_tp_sl_5_ema_filter_strategy, self).OnStarted(time)
        fast = ExponentialMovingAverage()
        fast.Length = self._fast_length.Value
        slow = ExponentialMovingAverage()
        slow.Length = self._slow_length.Value
        ema = ExponentialMovingAverage()
        ema.Length = self._ema_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, ema, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_val, slow_val, ema_val):
        if candle.State != CandleStates.Finished:
            return
        f = float(fast_val)
        s = float(slow_val)
        if not self._initialized:
            self._prev_fast = f
            self._prev_slow = s
            self._initialized = True
            return
        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_fast = f
            self._prev_slow = s
            return
        cross_up = self._prev_fast <= self._prev_slow and f > s
        cross_down = self._prev_fast >= self._prev_slow and f < s
        if cross_up and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown = 12
        elif cross_down and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown = 12
        self._prev_fast = f
        self._prev_slow = s

    def CreateClone(self):
        return ma_crossover_tp_sl_5_ema_filter_strategy()
