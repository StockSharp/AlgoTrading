import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class bitex_one_market_maker_strategy(Strategy):
    def __init__(self):
        super(bitex_one_market_maker_strategy, self).__init__()

        self._sma_period = self.Param("SmaPeriod", 100) \
            .SetDisplay("SMA Period", "SMA period for mean reversion", "Indicator")
        self._stop_loss_points = self.Param("StopLossPoints", 200) \
            .SetDisplay("SMA Period", "SMA period for mean reversion", "Indicator")
        self._take_profit_points = self.Param("TakeProfitPoints", 400) \
            .SetDisplay("SMA Period", "SMA period for mean reversion", "Indicator")

        self._sma = None
        self._entry_price = 0.0
        self._cooldown = 0.0

    def OnReseted(self):
        super(bitex_one_market_maker_strategy, self).OnReseted()
        self._sma = None
        self._entry_price = 0.0
        self._cooldown = 0.0

    def OnStarted(self, time):
        super(bitex_one_market_maker_strategy, self).OnStarted(time)

        self.__sma = SimpleMovingAverage()
        self.__sma.Length = self.sma_period

        subscription = self.SubscribeCandles(TimeSpan.FromMinutes(5)
        subscription.Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return bitex_one_market_maker_strategy()
