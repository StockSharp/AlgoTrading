import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class ma_crossover_tp_sl_strategy(Strategy):
    def __init__(self):
        super(ma_crossover_tp_sl_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 5) \
            .SetGreaterThanZero()
        self._slow_length = self.Param("SlowLength", 12) \
            .SetGreaterThanZero()
        self._take_profit_percent = self.Param("TakeProfitPercent", 10.0) \
            .SetGreaterThanZero()
        self._stop_loss_percent = self.Param("StopLossPercent", 5.0) \
            .SetGreaterThanZero()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._entry_price = 0.0
        self._was_fast_less = False
        self._initialized = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(ma_crossover_tp_sl_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._was_fast_less = False
        self._initialized = False

    def OnStarted(self, time):
        super(ma_crossover_tp_sl_strategy, self).OnStarted(time)
        self._entry_price = 0.0
        self._was_fast_less = False
        self._initialized = False
        self._fast_ma = ExponentialMovingAverage()
        self._fast_ma.Length = self._fast_length.Value
        self._slow_ma = ExponentialMovingAverage()
        self._slow_ma.Length = self._slow_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._fast_ma, self._slow_ma, self.OnProcess).Start()

    def OnProcess(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return
        fv = float(fast)
        sv = float(slow)
        if not self._initialized:
            self._was_fast_less = fv < sv
            self._initialized = True
            return
        is_fast_less = fv < sv
        close = float(candle.ClosePrice)
        if self._was_fast_less and not is_fast_less and self.Position <= 0:
            self._entry_price = close
            self.BuyMarket()
        if self.Position > 0 and self._entry_price > 0.0:
            tp = self._entry_price * (1.0 + float(self._take_profit_percent.Value) / 100.0)
            sl = self._entry_price * (1.0 - float(self._stop_loss_percent.Value) / 100.0)
            if close >= tp or close <= sl or (not self._was_fast_less and is_fast_less):
                self.SellMarket()
                self._entry_price = 0.0
        self._was_fast_less = is_fast_less

    def CreateClone(self):
        return ma_crossover_tp_sl_strategy()
