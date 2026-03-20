import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class sweet_spot_extreme_strategy(Strategy):
    def __init__(self):
        super(sweet_spot_extreme_strategy, self).__init__()

        self._ema_period = self.Param("EmaPeriod", 50) \
            .SetDisplay("EMA Period", "Trend EMA period", "Indicators")
        self._cci_period = self.Param("CciPeriod", 14) \
            .SetDisplay("EMA Period", "Trend EMA period", "Indicators")
        self._buy_cci_level = self.Param("BuyCciLevel", -50) \
            .SetDisplay("EMA Period", "Trend EMA period", "Indicators")
        self._sell_cci_level = self.Param("SellCciLevel", 50) \
            .SetDisplay("EMA Period", "Trend EMA period", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("EMA Period", "Trend EMA period", "Indicators")

        self._prev_ema = 0.0
        self._prev_prev_ema = 0.0
        self._has_prev_ema = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(sweet_spot_extreme_strategy, self).OnReseted()
        self._prev_ema = 0.0
        self._prev_prev_ema = 0.0
        self._has_prev_ema = False

    def OnStarted(self, time):
        super(sweet_spot_extreme_strategy, self).OnStarted(time)

        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.ema_period
        self._cci = CommodityChannelIndex()
        self._cci.Length = self.cci_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._ema, self._cci, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return sweet_spot_extreme_strategy()
