import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class intelle_city_world_cycle_ath_atl_logarithmic_strategy(Strategy):
    def __init__(self):
        super(intelle_city_world_cycle_ath_atl_logarithmic_strategy, self).__init__()
        self._ath_long_length = self.Param("AthLongLength", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("ATH Long MA", "Length for ATH long moving average", "Strategy Parameters")
        self._ath_short_length = self.Param("AthShortLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("ATH Short MA", "Length for ATH short moving average", "Strategy Parameters")
        self._atl_long_length = self.Param("AtlLongLength", 70) \
            .SetGreaterThanZero() \
            .SetDisplay("ATL Long MA", "Length for ATL long moving average", "Strategy Parameters")
        self._atl_short_length = self.Param("AtlShortLength", 30) \
            .SetGreaterThanZero() \
            .SetDisplay("ATL Short MA", "Length for ATL short moving average", "Strategy Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles for strategy", "Strategy Parameters")
        self._prev_ath_long = 0.0
        self._prev_ath_short = 0.0
        self._prev_atl_long = 0.0
        self._prev_atl_short = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(intelle_city_world_cycle_ath_atl_logarithmic_strategy, self).OnReseted()
        self._prev_ath_long = 0.0
        self._prev_ath_short = 0.0
        self._prev_atl_long = 0.0
        self._prev_atl_short = 0.0

    def OnStarted(self, time):
        super(intelle_city_world_cycle_ath_atl_logarithmic_strategy, self).OnStarted(time)
        ma_ath_long = SimpleMovingAverage()
        ma_ath_long.Length = self._ath_long_length.Value
        ma_ath_short = SimpleMovingAverage()
        ma_ath_short.Length = self._ath_short_length.Value
        ma_atl_long = SimpleMovingAverage()
        ma_atl_long.Length = self._atl_long_length.Value
        ma_atl_short = ExponentialMovingAverage()
        ma_atl_short.Length = self._atl_short_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ma_ath_long, ma_ath_short, ma_atl_long, ma_atl_short, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, ath_long_val, ath_short_val, atl_long_val, atl_short_val):
        if candle.State != CandleStates.Finished:
            return
        ath_long = float(ath_long_val)
        ath_short = float(ath_short_val)
        atl_long = float(atl_long_val)
        atl_short = float(atl_short_val)
        if self._prev_ath_long != 0 and self._prev_ath_short != 0 and self.Position == 0:
            if self._prev_ath_long >= self._prev_ath_short and ath_long < ath_short:
                self.SellMarket()
            if self._prev_atl_long <= self._prev_atl_short and atl_long > atl_short:
                self.BuyMarket()
        self._prev_ath_long = ath_long
        self._prev_ath_short = ath_short
        self._prev_atl_long = atl_long
        self._prev_atl_short = atl_short

    def CreateClone(self):
        return intelle_city_world_cycle_ath_atl_logarithmic_strategy()
