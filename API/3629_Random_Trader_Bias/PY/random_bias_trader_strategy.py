import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class random_bias_trader_strategy(Strategy):
    def __init__(self):
        super(random_bias_trader_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(60) \
            .SetDisplay("Candle Type", "Candle type", "Data")
        self._reward_risk_ratio = self.Param("RewardRiskRatio", 3) \
            .SetDisplay("Candle Type", "Candle type", "Data")
        self._atr_multiplier = self.Param("AtrMultiplier", 3) \
            .SetDisplay("Candle Type", "Candle type", "Data")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("Candle Type", "Candle type", "Data")

        self._random = None
        self._entry_price = 0.0
        self._direction = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(random_bias_trader_strategy, self).OnReseted()
        self._random = None
        self._entry_price = 0.0
        self._direction = 0.0

    def OnStarted(self, time):
        super(random_bias_trader_strategy, self).OnStarted(time)

        self._atr = AverageTrueRange()
        self._atr.Length = self.atr_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._atr, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return random_bias_trader_strategy()
