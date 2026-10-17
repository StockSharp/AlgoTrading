import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class daytrading_es_wick_length_strategy(Strategy):
    """Long-only total-wick strategy using a preceding volume-weighted wick average."""

    def __init__(self):
        super(daytrading_es_wick_length_strategy, self).__init__()

        self._ma_length = self.Param("MaLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("MA Length", "Preceding completed candles for wick average", "Indicators")
        self._ma_type = self.Param("MaType", "VolumeWeighted") \
            .SetDisplay("MA Type", "VolumeWeighted or Simple", "Indicators")
        self._ma_offset = self.Param("MaOffset", 10.0) \
            .SetRange(0.0, 100000.0) \
            .SetDisplay("MA Offset", "Additional wick length in security price ticks", "Signals")
        self._hold_periods = self.Param("HoldPeriods", 18) \
            .SetGreaterThanZero() \
            .SetDisplay("Hold Periods", "Completed candles to hold an open long", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Signal candle interval", "General")

        self._samples = []
        self._finished_bars = 0
        self._entry_bar = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @staticmethod
    def calculate_wick_length(open_price, high, low, close):
        return max(0.0, high - max(open_price, close)) + max(0.0, min(open_price, close) - low)

    def OnReseted(self):
        super(daytrading_es_wick_length_strategy, self).OnReseted()
        self._samples = []
        self._finished_bars = 0
        self._entry_bar = 0

    def OnStarted2(self, time):
        super(daytrading_es_wick_length_strategy, self).OnStarted2(time)

        if str(self._ma_type.Value) not in ("VolumeWeighted", "Simple"):
            raise ValueError("MaType must be VolumeWeighted or Simple")

        self._samples = []
        self._finished_bars = 0
        self._entry_bar = 0

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._finished_bars += 1
        wick = self.calculate_wick_length(
            float(candle.OpenPrice), float(candle.HighPrice),
            float(candle.LowPrice), float(candle.ClosePrice)
        )

        ma_length = int(self._ma_length.Value)
        formed = len(self._samples) >= ma_length
        average = 0.0

        if formed:
            sum_wick = sum(sample[0] for sample in self._samples)
            sum_volume = sum(sample[1] for sample in self._samples)
            weighted_wick = sum(sample[0] * sample[1] for sample in self._samples)
            average = (weighted_wick / sum_volume) if (
                str(self._ma_type.Value) == "VolumeWeighted" and sum_volume > 0
            ) else sum_wick / len(self._samples)

        self._samples.append((wick, max(0.0, float(candle.TotalVolume))))
        if len(self._samples) > ma_length:
            self._samples.pop(0)

        if not formed or not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position > 0:
            if self._entry_bar > 0 and self._finished_bars - self._entry_bar >= int(self._hold_periods.Value):
                self.SellMarket(self.Position)
                self._entry_bar = 0
        elif self.Position == 0 and self._entry_bar == 0:
            price_step = float(self.Security.PriceStep) if self.Security is not None else 0.0
            if price_step <= 0:
                return

            if wick > average + float(self._ma_offset.Value) * price_step:
                self.BuyMarket()
                self._entry_bar = self._finished_bars

    def CreateClone(self):
        return daytrading_es_wick_length_strategy()
