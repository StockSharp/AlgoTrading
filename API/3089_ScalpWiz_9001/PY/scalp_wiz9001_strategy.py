import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy


class scalp_wiz9001_strategy(Strategy):
    def __init__(self):
        super(scalp_wiz9001_strategy, self).__init__()

        self._bands_period = self.Param("BandsPeriod", 20) \
            .SetDisplay("Bands Period", "Bollinger Bands period", "Indicator")
        self._bands_deviation = self.Param("BandsDeviation", 1.5) \
            .SetDisplay("Bands Period", "Bollinger Bands period", "Indicator")
        self._stop_loss_pips = self.Param("StopLossPips", 100) \
            .SetDisplay("Bands Period", "Bollinger Bands period", "Indicator")
        self._take_profit_pips = self.Param("TakeProfitPips", 150) \
            .SetDisplay("Bands Period", "Bollinger Bands period", "Indicator")

        self._bollinger = None
        self._entry_price = 0.0
        self._cooldown = 0.0

    def OnReseted(self):
        super(scalp_wiz9001_strategy, self).OnReseted()
        self._bollinger = None
        self._entry_price = 0.0
        self._cooldown = 0.0

    def OnStarted(self, time):
        super(scalp_wiz9001_strategy, self).OnStarted(time)

        self.__bollinger = BollingerBands()
        self.__bollinger.Length = self.bands_period
        self.__bollinger.Width = self.bands_deviation

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
        return scalp_wiz9001_strategy()
