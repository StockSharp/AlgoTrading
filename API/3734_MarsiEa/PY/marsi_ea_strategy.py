import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class marsi_ea_strategy(Strategy):
    def __init__(self):
        super(marsi_ea_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Candle Type", "Series used for indicator calculations", "General")
        self._ma_period = self.Param("MaPeriod", 14) \
            .SetDisplay("Candle Type", "Series used for indicator calculations", "General")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("Candle Type", "Series used for indicator calculations", "General")
        self._rsi_overbought = self.Param("RsiOverbought", 55) \
            .SetDisplay("Candle Type", "Series used for indicator calculations", "General")
        self._rsi_oversold = self.Param("RsiOversold", 45) \
            .SetDisplay("Candle Type", "Series used for indicator calculations", "General")
        self._risk_percent = self.Param("RiskPercent", 10) \
            .SetDisplay("Candle Type", "Series used for indicator calculations", "General")
        self._stop_loss_pips = self.Param("StopLossPips", 100) \
            .SetDisplay("Candle Type", "Series used for indicator calculations", "General")
        self._take_profit_pips = self.Param("TakeProfitPips", 300) \
            .SetDisplay("Candle Type", "Series used for indicator calculations", "General")

        self._sma = None
        self._rsi = None
        self._virtual_stop_price = None
        self._virtual_take_price = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(marsi_ea_strategy, self).OnReseted()
        self._sma = None
        self._rsi = None
        self._virtual_stop_price = None
        self._virtual_take_price = None

    def OnStarted(self, time):
        super(marsi_ea_strategy, self).OnStarted(time)

        self.__sma = SimpleMovingAverage()
        self.__sma.Length = self.ma_period
        self.__rsi = RelativeStrengthIndex()
        self.__rsi.Length = self.rsi_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__sma, self.__rsi, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return marsi_ea_strategy()
