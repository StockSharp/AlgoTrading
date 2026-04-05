import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal as NetDecimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage as SMA, ExponentialMovingAverage as EMA, Highest, Lowest
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

class exp_muv_nor_diff_cloud_strategy(Strategy):
    def __init__(self):
        super(exp_muv_nor_diff_cloud_strategy, self).__init__()
        self._ma_period = self.Param("MaPeriod", 14)
        self._momentum_period = self.Param("MomentumPeriod", 1)
        self._k_period = self.Param("KPeriod", 14)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))
        self._prev_sma = None
        self._prev_ema = None

    @property
    def MaPeriod(self): return self._ma_period.Value
    @MaPeriod.setter
    def MaPeriod(self, v): self._ma_period.Value = v
    @property
    def MomentumPeriod(self): return self._momentum_period.Value
    @MomentumPeriod.setter
    def MomentumPeriod(self, v): self._momentum_period.Value = v
    @property
    def KPeriod(self): return self._k_period.Value
    @KPeriod.setter
    def KPeriod(self, v): self._k_period.Value = v
    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, v): self._candle_type.Value = v

    def OnStarted2(self, time):
        super(exp_muv_nor_diff_cloud_strategy, self).OnStarted2(time)
        self._sma_high = Highest()
        self._sma_high.Length = self.KPeriod
        self._sma_low = Lowest()
        self._sma_low.Length = self.KPeriod
        self._ema_high = Highest()
        self._ema_high.Length = self.KPeriod
        self._ema_low = Lowest()
        self._ema_low.Length = self.KPeriod
        self._prev_sma = None
        self._prev_ema = None
        sma = SMA()
        sma.Length = self.MaPeriod
        ema = EMA()
        ema.Length = self.MaPeriod
        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(sma, ema, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, sma_value, ema_value):
        if candle.State != CandleStates.Finished: return
        sv = float(sma_value)
        ev = float(ema_value)
        t = candle.OpenTime
        if self._prev_sma is None or self._prev_ema is None:
            self._prev_sma = sv
            self._prev_ema = ev
            return
        sma_mom = sv - self._prev_sma
        ema_mom = ev - self._prev_ema

        sma_max_r = process_float(self._sma_high, sma_mom, t, True)
        sma_min_r = process_float(self._sma_low, sma_mom, t, True)
        ema_max_r = process_float(self._ema_high, ema_mom, t, True)
        ema_min_r = process_float(self._ema_low, ema_mom, t, True)

        if not self._sma_high.IsFormed or not self._sma_low.IsFormed:
            self._prev_sma = sv
            self._prev_ema = ev
            return
        if not self._ema_high.IsFormed or not self._ema_low.IsFormed:
            self._prev_sma = sv
            self._prev_ema = ev
            return

        sma_max = float(sma_max_r)
        sma_min = float(sma_min_r)
        ema_max = float(ema_max_r)
        ema_min = float(ema_min_r)

        sma_range = sma_max - sma_min
        ema_range = ema_max - ema_min

        # Normalize: value==max => 100, value==min => -100
        if sma_range > 0:
            sma_norm = 100.0 - 200.0 * (sma_max - sma_mom) / sma_range
        else:
            sma_norm = 100.0
        if ema_range > 0:
            ema_norm = 100.0 - 200.0 * (ema_max - ema_mom) / ema_range
        else:
            ema_norm = 100.0

        # Use tolerance for float comparison (CS uses exact decimal)
        eps = 1e-6
        if sma_norm >= 100.0 - eps or ema_norm >= 100.0 - eps:
            if self.Position <= 0:
                self.BuyMarket()
        elif sma_norm <= -100.0 + eps or ema_norm <= -100.0 + eps:
            if self.Position >= 0:
                self.SellMarket()

        self._prev_sma = sv
        self._prev_ema = ev

    def OnReseted(self):
        super(exp_muv_nor_diff_cloud_strategy, self).OnReseted()
        self._prev_sma = None
        self._prev_ema = None

    def CreateClone(self):
        return exp_muv_nor_diff_cloud_strategy()
