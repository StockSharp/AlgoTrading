import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import DecimalIndicatorValue, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Unit, UnitTypes


class rnn_probability_strategy(Strategy):
    def __init__(self):
        super(rnn_probability_strategy, self).__init__()

        self._trade_volume = self.Param("TradeVolume", 1) \
            .SetDisplay("Trade Volume", "Lot size used for each market entry.", "General")
        self._rsi_period = self.Param("RsiPeriod", 9) \
            .SetDisplay("Trade Volume", "Lot size used for each market entry.", "General")
        self._applied_price = self.Param("AppliedPrice", AppliedPriceTypes.Open) \
            .SetDisplay("Trade Volume", "Lot size used for each market entry.", "General")
        self._stop_loss_take_profit_pips = self.Param("StopLossTakeProfitPips", 100) \
            .SetDisplay("Trade Volume", "Lot size used for each market entry.", "General")
        self._weight0 = self.Param("Weight0", 6) \
            .SetDisplay("Trade Volume", "Lot size used for each market entry.", "General")
        self._weight1 = self.Param("Weight1", 96) \
            .SetDisplay("Trade Volume", "Lot size used for each market entry.", "General")
        self._weight2 = self.Param("Weight2", 90) \
            .SetDisplay("Trade Volume", "Lot size used for each market entry.", "General")
        self._weight3 = self.Param("Weight3", 35) \
            .SetDisplay("Trade Volume", "Lot size used for each market entry.", "General")
        self._weight4 = self.Param("Weight4", 64) \
            .SetDisplay("Trade Volume", "Lot size used for each market entry.", "General")
        self._weight5 = self.Param("Weight5", 83) \
            .SetDisplay("Trade Volume", "Lot size used for each market entry.", "General")
        self._weight6 = self.Param("Weight6", 66) \
            .SetDisplay("Trade Volume", "Lot size used for each market entry.", "General")
        self._weight7 = self.Param("Weight7", 50) \
            .SetDisplay("Trade Volume", "Lot size used for each market entry.", "General")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(2) \
            .SetDisplay("Trade Volume", "Lot size used for each market entry.", "General")

        self._rsi = None
        self._rsi_history = new()
        self._pip_size = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(rnn_probability_strategy, self).OnReseted()
        self._rsi = None
        self._rsi_history = new()
        self._pip_size = 0.0

    def OnStarted(self, time):
        super(rnn_probability_strategy, self).OnStarted(time)

        self.__rsi = RelativeStrengthIndex()
        self.__rsi.Length = self.rsi_period

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
        return rnn_probability_strategy()
