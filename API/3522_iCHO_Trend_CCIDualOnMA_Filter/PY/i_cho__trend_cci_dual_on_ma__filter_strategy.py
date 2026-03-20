import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy


class i_cho__trend_cci_dual_on_ma__filter_strategy(Strategy):
    def __init__(self):
        super(i_cho__trend_cci_dual_on_ma__filter_strategy, self).__init__()

        self._cci_length = self.Param("CciLength", 14) \
            .SetDisplay("CCI Length", "CCI period", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(60) \
            .SetDisplay("CCI Length", "CCI period", "Indicators")
        self._cci_level = self.Param("CciLevel", 100) \
            .SetDisplay("CCI Length", "CCI period", "Indicators")
        self._signal_cooldown_candles = self.Param("SignalCooldownCandles", 4) \
            .SetDisplay("CCI Length", "CCI period", "Indicators")

        self._prev_cci = None
        self._prev_prev_cci = None
        self._candles_since_trade = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(i_cho__trend_cci_dual_on_ma__filter_strategy, self).OnReseted()
        self._prev_cci = None
        self._prev_prev_cci = None
        self._candles_since_trade = 0.0

    def OnStarted(self, time):
        super(i_cho__trend_cci_dual_on_ma__filter_strategy, self).OnStarted(time)

        self._cci = CommodityChannelIndex()
        self._cci.Length = self.cci_length

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._cci, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return i_cho__trend_cci_dual_on_ma__filter_strategy()
