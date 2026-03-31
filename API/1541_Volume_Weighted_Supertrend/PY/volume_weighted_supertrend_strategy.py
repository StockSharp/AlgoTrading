import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StandardDeviation, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class volume_weighted_supertrend_strategy(Strategy):
    """Volume-weighted supertrend: price StdDev-based supertrend combined with volume supertrend."""
    def __init__(self):
        super(volume_weighted_supertrend_strategy, self).__init__()
        self._period = self.Param("Period", 10).SetGreaterThanZero().SetDisplay("Period", "Supertrend period", "General")
        self._factor = self.Param("Factor", 3).SetGreaterThanZero().SetDisplay("Factor", "Multiplier", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(volume_weighted_supertrend_strategy, self).OnReseted()
        self._prev_upper = None
        self._prev_lower = None
        self._prev_st = None
        self._prev_dir = 1
        self._prev_close = None
        self._volumes = []
        self._prev_vol_upper = None
        self._prev_vol_lower = None
        self._prev_vol_st = None
        self._prev_vol_dir = 1
        self._prev_volume = None

    def OnStarted2(self, time):
        super(volume_weighted_supertrend_strategy, self).OnStarted2(time)
        self._prev_upper = None
        self._prev_lower = None
        self._prev_st = None
        self._prev_dir = 1
        self._prev_close = None
        self._volumes = []
        self._prev_vol_upper = None
        self._prev_vol_lower = None
        self._prev_vol_st = None
        self._prev_vol_dir = 1
        self._prev_volume = None

        std = StandardDeviation()
        std.Length = self._period.Value
        sma = SimpleMovingAverage()
        sma.Length = self._period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(std, sma, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, std_val, sma_val):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        hl2 = (float(candle.HighPrice) + float(candle.LowPrice)) / 2.0
        volume = float(candle.TotalVolume)
        sv = float(std_val)
        period = self._period.Value
        factor = self._factor.Value

        self._volumes.append(volume)
        while len(self._volumes) > period + 1:
            self._volumes.pop(0)

        if sv <= 0 or len(self._volumes) < period:
            self._prev_close = close
            self._prev_volume = volume
            return

        # Price supertrend
        upper = hl2 + factor * sv
        lower = hl2 - factor * sv

        if self._prev_lower is not None:
            lower = lower if (lower > self._prev_lower or (self._prev_close is not None and self._prev_close < self._prev_lower)) else self._prev_lower
        if self._prev_upper is not None:
            upper = upper if (upper < self._prev_upper or (self._prev_close is not None and self._prev_close > self._prev_upper)) else self._prev_upper

        if self._prev_st == self._prev_upper:
            direction = -1 if close > upper else 1
        else:
            direction = 1 if close < lower else -1

        supertrend = lower if direction == -1 else upper
        st_up_start = direction == -1 and self._prev_dir == 1
        st_dn_start = direction == 1 and self._prev_dir == -1
        in_rising = supertrend < close

        # Volume supertrend
        vol_avg = sum(self._volumes) / len(self._volumes)
        vol_std = 0
        if len(self._volumes) > 1:
            sum_sq = sum((v - vol_avg) ** 2 for v in self._volumes)
            vol_std = math.sqrt(sum_sq / len(self._volumes))

        vol_upper = volume + factor * vol_std
        vol_lower = volume - factor * vol_std

        if self._prev_vol_lower is not None:
            vol_lower = vol_lower if (vol_lower > self._prev_vol_lower or (self._prev_volume is not None and self._prev_volume < self._prev_vol_lower)) else self._prev_vol_lower
        if self._prev_vol_upper is not None:
            vol_upper = vol_upper if (vol_upper < self._prev_vol_upper or (self._prev_volume is not None and self._prev_volume > self._prev_vol_upper)) else self._prev_vol_upper

        if self._prev_vol_st == self._prev_vol_upper:
            vol_dir = -1 if volume > vol_upper else 1
        else:
            vol_dir = 1 if volume < vol_lower else -1

        vol_st = vol_lower if vol_dir == -1 else vol_upper
        vol_change_up = self._prev_vol_dir == 1 and vol_dir == -1
        vol_change_dn = self._prev_vol_dir == -1 and vol_dir == 1
        in_rising_vol = vol_st < volume

        buy = (in_rising_vol and st_up_start) or (vol_change_up and in_rising)
        sell = (not in_rising_vol and st_dn_start) or (vol_change_dn and not in_rising)

        if buy and self.Position <= 0:
            self.BuyMarket()
        elif sell and self.Position >= 0:
            self.SellMarket()

        self._prev_upper = upper
        self._prev_lower = lower
        self._prev_st = supertrend
        self._prev_dir = direction
        self._prev_vol_upper = vol_upper
        self._prev_vol_lower = vol_lower
        self._prev_vol_st = vol_st
        self._prev_vol_dir = vol_dir
        self._prev_close = close
        self._prev_volume = volume

    def CreateClone(self):
        return volume_weighted_supertrend_strategy()
