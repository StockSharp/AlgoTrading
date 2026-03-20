import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class multi_currency_template_mt5_strategy(Strategy):
    def __init__(self):
        super(multi_currency_template_mt5_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(60) \
            .SetDisplay("Candle Type", "Timeframe for signal generation", "General")
        self._lookback = self.Param("Lookback", 1) \
            .SetDisplay("Candle Type", "Timeframe for signal generation", "General")

        self._prev_candle = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(multi_currency_template_mt5_strategy, self).OnReseted()
        self._prev_candle = None

    def OnStarted(self, time):
        super(multi_currency_template_mt5_strategy, self).OnStarted(time)


        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return multi_currency_template_mt5_strategy()
