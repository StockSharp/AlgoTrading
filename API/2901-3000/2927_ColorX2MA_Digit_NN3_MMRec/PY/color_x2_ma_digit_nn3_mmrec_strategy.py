import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class color_x2_ma_digit_nn3_mmrec_strategy(Strategy):
    def __init__(self):
        super(color_x2_ma_digit_nn3_mmrec_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._fast_length = self.Param("FastLength", 8) \
            .SetDisplay("Fast Length", "Fast SMA period", "Indicators")
        self._slow_length = self.Param("SlowLength", 21) \
            .SetDisplay("Slow Length", "Slow SMA period", "Indicators")

        self._prev_fast = None
        self._prev_slow = None
        self._prev_signal = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def FastLength(self):
        return self._fast_length.Value

    @property
    def SlowLength(self):
        return self._slow_length.Value

    def OnReseted(self):
        super(color_x2_ma_digit_nn3_mmrec_strategy, self).OnReseted()
        self._prev_fast = None
        self._prev_slow = None
        self._prev_signal = 0

    def OnStarted2(self, time):
        super(color_x2_ma_digit_nn3_mmrec_strategy, self).OnStarted2(time)
        self._prev_fast = None
        self._prev_slow = None
        self._prev_signal = 0

        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self.FastLength
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self.SlowLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast_ema, slow_ema, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_ema)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return
        fv = float(fast_value)
        sv = float(slow_value)
        if self._prev_fast is None or self._prev_slow is None:
            self._prev_fast = fv
            self._prev_slow = sv
            return
        if fv > sv:
            signal = 1
        elif fv < sv:
            signal = -1
        else:
            signal = self._prev_signal
        self._prev_fast = fv
        self._prev_slow = sv
        if signal == self._prev_signal:
            return
        old_signal = self._prev_signal
        self._prev_signal = signal
        if signal == 1 and old_signal <= 0:
            if self.Position < 0:
                self.BuyMarket()
            if self.Position <= 0:
                self.BuyMarket()
        elif signal == -1 and old_signal >= 0:
            if self.Position > 0:
                self.SellMarket()
            if self.Position >= 0:
                self.SellMarket()

    def CreateClone(self):
        return color_x2_ma_digit_nn3_mmrec_strategy()
