import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class ten_kijun_cross_strategy(Strategy):
    def __init__(self):
        super(ten_kijun_cross_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._tenkan_period = self.Param("TenkanPeriod", 12)
        self._kijun_period = self.Param("KijunPeriod", 34)

        self._highs_tenkan = []
        self._lows_tenkan = []
        self._highs_kijun = []
        self._lows_kijun = []
        self._prev_tenkan = None
        self._prev_kijun = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def TenkanPeriod(self):
        return self._tenkan_period.Value

    @TenkanPeriod.setter
    def TenkanPeriod(self, value):
        self._tenkan_period.Value = value

    @property
    def KijunPeriod(self):
        return self._kijun_period.Value

    @KijunPeriod.setter
    def KijunPeriod(self, value):
        self._kijun_period.Value = value

    def OnReseted(self):
        super(ten_kijun_cross_strategy, self).OnReseted()
        self._highs_tenkan = []
        self._lows_tenkan = []
        self._highs_kijun = []
        self._lows_kijun = []
        self._prev_tenkan = None
        self._prev_kijun = None

    def OnStarted(self, time):
        super(ten_kijun_cross_strategy, self).OnStarted(time)
        self._highs_tenkan = []
        self._lows_tenkan = []
        self._highs_kijun = []
        self._lows_kijun = []
        self._prev_tenkan = None
        self._prev_kijun = None

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        tenkan_period = self.TenkanPeriod
        kijun_period = self.KijunPeriod

        self._highs_tenkan.append(high)
        self._lows_tenkan.append(low)
        while len(self._highs_tenkan) > tenkan_period:
            self._highs_tenkan.pop(0)
            self._lows_tenkan.pop(0)

        self._highs_kijun.append(high)
        self._lows_kijun.append(low)
        while len(self._highs_kijun) > kijun_period:
            self._highs_kijun.pop(0)
            self._lows_kijun.pop(0)

        if len(self._highs_tenkan) < tenkan_period or len(self._highs_kijun) < kijun_period:
            return

        tenkan = (max(self._highs_tenkan) + min(self._lows_tenkan)) / 2.0
        kijun = (max(self._highs_kijun) + min(self._lows_kijun)) / 2.0

        if self._prev_tenkan is None or self._prev_kijun is None:
            self._prev_tenkan = tenkan
            self._prev_kijun = kijun
            return

        cross_up = self._prev_tenkan <= self._prev_kijun and tenkan > kijun
        cross_down = self._prev_tenkan >= self._prev_kijun and tenkan < kijun

        if cross_up:
            if self.Position <= 0:
                self.BuyMarket()
        elif cross_down:
            if self.Position >= 0:
                self.SellMarket()

        self._prev_tenkan = tenkan
        self._prev_kijun = kijun

    def CreateClone(self):
        return ten_kijun_cross_strategy()
