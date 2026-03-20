import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageDirectionalIndex
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Unit, UnitTypes


class ytg_adx_level_cross_strategy(Strategy):
    def __init__(self):
        super(ytg_adx_level_cross_strategy, self).__init__()

        self._adx_period = self.Param("AdxPeriod", 14) \
            .SetDisplay("ADX Period", "Period for the Average Directional Index", "Indicators")
        self._level_plus = self.Param("LevelPlus", 15) \
            .SetDisplay("ADX Period", "Period for the Average Directional Index", "Indicators")
        self._level_minus = self.Param("LevelMinus", 15) \
            .SetDisplay("ADX Period", "Period for the Average Directional Index", "Indicators")
        self._shift = self.Param("Shift", 1) \
            .SetDisplay("ADX Period", "Period for the Average Directional Index", "Indicators")
        self._take_profit_points = self.Param("TakeProfitPoints", 500) \
            .SetDisplay("ADX Period", "Period for the Average Directional Index", "Indicators")
        self._stop_loss_points = self.Param("StopLossPoints", 500) \
            .SetDisplay("ADX Period", "Period for the Average Directional Index", "Indicators")
        self._trade_volume = self.Param("TradeVolume", 0.1) \
            .SetDisplay("ADX Period", "Period for the Average Directional Index", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1) \
            .SetDisplay("ADX Period", "Period for the Average Directional Index", "Indicators")

        self._adx = None
        self._plus_di_history = []
        self._minus_di_history = []

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ytg_adx_level_cross_strategy, self).OnReseted()
        self._adx = None
        self._plus_di_history = []
        self._minus_di_history = []

    def OnStarted(self, time):
        super(ytg_adx_level_cross_strategy, self).OnStarted(time)

        self.__adx = AverageDirectionalIndex()
        self.__adx.Length = self.adx_period

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
        return ytg_adx_level_cross_strategy()
