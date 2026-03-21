import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class lube_strategy(Strategy):
    """
    Lube strategy based on friction levels and FIR filter trend.
    Low friction + positive FIR trend = buy, negative = sell.
    """

    def __init__(self):
        super(lube_strategy, self).__init__()
        self._bars_back = self.Param("BarsBack", 20) \
            .SetDisplay("Bars Back", "Bars back for friction", "General")
        self._friction_level = self.Param("FrictionLevel", 50) \
            .SetDisplay("Friction Level", "Stop trade level", "General")
        self._trigger_level = self.Param("TriggerLevel", -10) \
            .SetDisplay("Trigger Level", "Initiate trade level", "General")
        self._range = self.Param("Range", 10) \
            .SetDisplay("Range", "Bars for friction range", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(25))) \
            .SetDisplay("Candle Type", "Candles", "General")

        self._highs = []
        self._lows = []
        self._frictions = []
        self._midf_hist = []
        self._lowf2_hist = []
        self._close_list = []
        self._prev_fir = 0.0
        self._bar_count = 0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(lube_strategy, self).OnReseted()
        self._highs = []
        self._lows = []
        self._frictions = []
        self._midf_hist = []
        self._lowf2_hist = []
        self._close_list = []
        self._prev_fir = 0.0
        self._bar_count = 0
        self._cooldown = 0

    def OnStarted(self, time):
        super(lube_strategy, self).OnStarted(time)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._bar_count += 1
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        bb = self._bars_back.Value

        self._highs.append(high)
        self._lows.append(low)
        while len(self._highs) > bb:
            self._highs.pop(0)
        while len(self._lows) > bb:
            self._lows.pop(0)

        friction = 0.0
        length = min(len(self._highs), len(self._lows))
        for i in range(length):
            if self._highs[i] >= close and self._lows[i] <= close:
                friction += (1.0 + bb) / (i + 1 + bb)

        self._frictions.append(friction)
        while len(self._frictions) > self._range.Value:
            self._frictions.pop(0)

        lowf = min(self._frictions) if self._frictions else 0
        highf = max(self._frictions) if self._frictions else 0

        fl = self._friction_level.Value / 100.0
        tl = self._trigger_level.Value / 100.0
        midf = lowf * (1.0 - fl) + highf * fl
        lowf2 = lowf * (1.0 - tl) + highf * tl

        self._midf_hist.append(midf)
        self._lowf2_hist.append(lowf2)
        if len(self._midf_hist) > 6:
            self._midf_hist.pop(0)
        if len(self._lowf2_hist) > 6:
            self._lowf2_hist.pop(0)

        midf5 = self._midf_hist[0] if len(self._midf_hist) == 6 else midf
        lowf25 = self._lowf2_hist[0] if len(self._lowf2_hist) == 6 else lowf2

        self._close_list.append(close)
        if len(self._close_list) > 4:
            self._close_list.pop(0)
        if len(self._close_list) < 4:
            return

        fir = (4 * self._close_list[3] + 3 * self._close_list[2] + 2 * self._close_list[1] + self._close_list[0]) / 10.0
        trend = 1 if fir > self._prev_fir else -1
        self._prev_fir = fir

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        long_signal = friction < lowf25 and trend == 1
        short_signal = friction < lowf25 and trend == -1
        end_signal = friction > midf5

        if long_signal and self._bar_count > 10 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown = 10
            return

        if short_signal and self._bar_count > 10 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown = 10
            return

        if self.Position > 0 and end_signal:
            self.SellMarket()
            self._cooldown = 10
        elif self.Position < 0 and end_signal:
            self.BuyMarket()
            self._cooldown = 10

    def CreateClone(self):
        return lube_strategy()
