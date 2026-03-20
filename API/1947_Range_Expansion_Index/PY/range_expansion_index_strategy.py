import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class range_expansion_index_strategy(Strategy):

    def __init__(self):
        super(range_expansion_index_strategy, self).__init__()

        self._rei_period = self.Param("ReiPeriod", 8) \
            .SetDisplay("REI Period", "Length of REI indicator", "Parameters")
        self._up_level = self.Param("UpLevel", 70.0) \
            .SetDisplay("Up Level", "Upper threshold", "Parameters")
        self._down_level = self.Param("DownLevel", -70.0) \
            .SetDisplay("Down Level", "Lower threshold", "Parameters")
        self._cooldown_bars = self.Param("CooldownBars", 1) \
            .SetDisplay("Cooldown Bars", "Bars to wait after a completed trade", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(8))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._prev_rei = None
        self._bars_since_trade = 0
        self._buffer = []

    @property
    def ReiPeriod(self):
        return self._rei_period.Value

    @ReiPeriod.setter
    def ReiPeriod(self, value):
        self._rei_period.Value = value

    @property
    def UpLevel(self):
        return self._up_level.Value

    @UpLevel.setter
    def UpLevel(self, value):
        self._up_level.Value = value

    @property
    def DownLevel(self):
        return self._down_level.Value

    @DownLevel.setter
    def DownLevel(self, value):
        self._down_level.Value = value

    @property
    def CooldownBars(self):
        return self._cooldown_bars.Value

    @CooldownBars.setter
    def CooldownBars(self, value):
        self._cooldown_bars.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def _compute_rei(self, candle):
        self._buffer.append(candle)

        length = self.ReiPeriod
        need = length + 8
        if len(self._buffer) > need:
            self._buffer.pop(0)

        if len(self._buffer) < need:
            return None

        last = len(self._buffer) - 1
        sub_sum = 0.0
        abs_sum = 0.0

        for i in range(last, last - length, -1):
            hi = float(self._buffer[i].HighPrice)
            hi2 = float(self._buffer[i - 2].HighPrice)
            lo = float(self._buffer[i].LowPrice)
            lo2 = float(self._buffer[i - 2].LowPrice)

            diff1 = hi - hi2
            diff2 = lo - lo2

            cond1 = (float(self._buffer[i - 2].HighPrice) < float(self._buffer[i - 7].ClosePrice)
                     and float(self._buffer[i - 2].HighPrice) < float(self._buffer[i - 8].ClosePrice)
                     and hi < float(self._buffer[i - 5].HighPrice)
                     and hi < float(self._buffer[i - 6].HighPrice))
            num1 = 0.0 if cond1 else 1.0

            cond2 = (float(self._buffer[i - 2].LowPrice) > float(self._buffer[i - 7].ClosePrice)
                     and float(self._buffer[i - 2].LowPrice) > float(self._buffer[i - 8].ClosePrice)
                     and lo > float(self._buffer[i - 5].LowPrice)
                     and lo > float(self._buffer[i - 6].LowPrice))
            num2 = 0.0 if cond2 else 1.0

            sub_sum += num1 * num2 * (diff1 + diff2)
            abs_sum += abs(diff1) + abs(diff2)

        if abs_sum == 0.0:
            return 0.0
        return sub_sum / abs_sum * 100.0

    def OnStarted(self, time):
        super(range_expansion_index_strategy, self).OnStarted(time)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        rei_value = self._compute_rei(candle)
        if rei_value is None:
            return

        if self._bars_since_trade < self.CooldownBars:
            self._bars_since_trade += 1

        if self._prev_rei is not None:
            prev = self._prev_rei
            down_level = float(self.DownLevel)
            up_level = float(self.UpLevel)

            if self._bars_since_trade >= self.CooldownBars:
                pos = self.Position
                if prev < down_level and rei_value >= down_level and pos <= 0:
                    self.BuyMarket(self.Volume + abs(pos))
                    self._bars_since_trade = 0
                elif prev > up_level and rei_value <= up_level and pos >= 0:
                    self.SellMarket(self.Volume + abs(pos))
                    self._bars_since_trade = 0

        self._prev_rei = rei_value

    def OnReseted(self):
        super(range_expansion_index_strategy, self).OnReseted()
        self._prev_rei = None
        self._bars_since_trade = self.CooldownBars
        self._buffer = []

    def CreateClone(self):
        return range_expansion_index_strategy()
