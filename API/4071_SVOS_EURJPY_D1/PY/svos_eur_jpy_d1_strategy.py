import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class svos_eur_jpy_d1_strategy(Strategy):
    def __init__(self):
        super(svos_eur_jpy_d1_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Candle Type", "Timeframe for analysis.", "General")
        self._adx_length = self.Param("AdxLength", 14) \
            .SetDisplay("Candle Type", "Timeframe for analysis.", "General")
        self._ema_length = self.Param("EmaLength", 20) \
            .SetDisplay("Candle Type", "Timeframe for analysis.", "General")
        self._adx_threshold = self.Param("AdxThreshold", 25) \
            .SetDisplay("Candle Type", "Timeframe for analysis.", "General")

        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(svos_eur_jpy_d1_strategy, self).OnReseted()
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(svos_eur_jpy_d1_strategy, self).OnStarted(time)

        self._atr = AverageTrueRange()
        self._atr.Length = self.adx_length
        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.ema_length

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._atr, self._ema, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return svos_eur_jpy_d1_strategy()
