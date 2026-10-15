import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan

from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import (
    SimpleMovingAverage, ExponentialMovingAverage, SmoothedMovingAverage,
    WeightedMovingAverage, JurikMovingAverage, ZeroLagExponentialMovingAverage,
    KaufmanAdaptiveMovingAverage
)
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

_FATL_COEFF = [
    0.4360409450, 0.3658689069, 0.2460452079, 0.1104506886,
    -0.0054034585, -0.0760367731, -0.0933058722, -0.0670110374,
    -0.0190795053, 0.0259609206, 0.0502044896, 0.0477818607,
    0.0249252327, -0.0047706151, -0.0272432537, -0.0338917071,
    -0.0244141482, -0.0055774838, 0.0128149838, 0.0226522218,
    0.0208778257, 0.0100299086, -0.0036771622, -0.0136744850,
    -0.0160483392, -0.0108597376, -0.0016060704, 0.0069480557,
    0.0110573605, 0.0095711419, 0.0040444064, -0.0023824623,
    -0.0067093714, -0.0072003400, -0.0047717710, 0.0005541115,
    0.0007860160, 0.0130129076, 0.0040364019,
]

_SATL_COEFF = [
    0.0982862174, 0.0975682269, 0.0961401078, 0.0940230544,
    0.0912437090, 0.0878391006, 0.0838544303, 0.0793406350,
    0.0743569346, 0.0689666682, 0.0632381578, 0.0572428925,
    0.0510534242, 0.0447468229, 0.0383959950, 0.0320735368,
    0.0258537721, 0.0198005183, 0.0139807863, 0.0084512448,
    0.0032639979, -0.0015350359, -0.0059060082, -0.0098190256,
    -0.0132507215, -0.0161875265, -0.0186164872, -0.0205446727,
    -0.0219739146, -0.0229204861, -0.0234080863, -0.0234566315,
    -0.0231017777, -0.0223796900, -0.0213300463, -0.0199924534,
    -0.0184126992, -0.0166377699, -0.0147139428, -0.0126796776,
    -0.0105938331, -0.0084736770, -0.0063841850, -0.0043466731,
    -0.0023956944, -0.0005535180, 0.0011421469, 0.0026845693,
    0.0040471369, 0.0052380201, 0.0062194591, 0.0070340085,
    0.0076266453, 0.0080376628, 0.0083037666, 0.0083694798,
    0.0082901022, 0.0080741359, 0.0077543820, 0.0073260526,
    0.0068163569, 0.0062325477, 0.0056078229, 0.0049516078,
    0.0161380976,
]

class x_fatl_x_satl_cloud_duplex_strategy(Strategy):
    def __init__(self):
        super(x_fatl_x_satl_cloud_duplex_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))
        self._long_length1 = self.Param("LongLength1", 3)
        self._long_length2 = self.Param("LongLength2", 5)
        self._long_signal_bar = self.Param("LongSignalBar", 1)
        self._long_stop_loss = self.Param("LongStopLoss", 0.0)
        self._long_take_profit = self.Param("LongTakeProfit", 0.0)

        self._long_history = []
        self._long_entry_price = None
        self._price_buffer = [0.0] * len(_SATL_COEFF)
        self._buf_idx = 0
        self._buf_count = 0
        self._fast_smoother = None
        self._slow_smoother = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def LongLength1(self):
        return self._long_length1.Value

    @property
    def LongLength2(self):
        return self._long_length2.Value

    @property
    def LongSignalBar(self):
        return self._long_signal_bar.Value

    @property
    def LongStopLoss(self):
        return self._long_stop_loss.Value

    @property
    def LongTakeProfit(self):
        return self._long_take_profit.Value

    def OnStarted2(self, time):
        super(x_fatl_x_satl_cloud_duplex_strategy, self).OnStarted2(time)

        self._fast_smoother = JurikMovingAverage()
        self._fast_smoother.Length = max(1, self.LongLength1)
        self._slow_smoother = JurikMovingAverage()
        self._slow_smoother.Length = max(1, self.LongLength2)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        price = float(candle.ClosePrice)
        buf_len = len(self._price_buffer)
        self._price_buffer[self._buf_idx] = price
        self._buf_idx = (self._buf_idx + 1) % buf_len
        if self._buf_count < buf_len:
            self._buf_count += 1

        fast_raw = self._compute_filter(_FATL_COEFF)
        slow_raw = self._compute_filter(_SATL_COEFF)

        t = candle.OpenTime
        fv = process_float(self._fast_smoother, fast_raw, t, True)
        sv = process_float(self._slow_smoother, slow_raw, t, True)
        fast = float(fv)
        slow = float(sv)

        self._long_history.insert(0, (fast, slow))
        max_size = max(self.LongSignalBar + 2, 2)
        if len(self._long_history) > max_size:
            self._long_history.pop()

        if self._handle_long_risk(candle):
            return

        if len(self._long_history) <= self.LongSignalBar + 1:
            return

        current = self._long_history[self.LongSignalBar]
        previous = self._long_history[self.LongSignalBar + 1]
        cross_up = current[0] > current[1] and previous[0] <= previous[1]
        cross_down = current[0] < current[1] and previous[0] >= previous[1]

        if cross_down and self.Position > 0:
            self.SellMarket()
            self._long_entry_price = None

        if cross_up and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._long_entry_price = float(candle.ClosePrice)

        if cross_down and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

    def _handle_long_risk(self, candle):
        if self.Position <= 0 or self._long_entry_price is None:
            return False
        entry = self._long_entry_price
        if self.LongStopLoss > 0 and float(candle.LowPrice) <= entry - self.LongStopLoss:
            self.SellMarket()
            self._long_entry_price = None
            return True
        if self.LongTakeProfit > 0 and float(candle.HighPrice) >= entry + self.LongTakeProfit:
            self.SellMarket()
            self._long_entry_price = None
            return True
        return False

    def _compute_filter(self, coefficients):
        if self._buf_count < len(coefficients):
            return 0.0
        total = 0.0
        for i in range(len(coefficients)):
            idx = self._buf_idx - 1 - i
            if idx < 0:
                idx += len(self._price_buffer)
            total += coefficients[i] * self._price_buffer[idx]
        return total

    def OnReseted(self):
        super(x_fatl_x_satl_cloud_duplex_strategy, self).OnReseted()
        self._long_history = []
        self._long_entry_price = None
        self._price_buffer = [0.0] * len(_SATL_COEFF)
        self._buf_idx = 0
        self._buf_count = 0

    def CreateClone(self):
        return x_fatl_x_satl_cloud_duplex_strategy()
