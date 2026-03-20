import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class coin_flipping_strategy(Strategy):
    def __init__(self):
        super(coin_flipping_strategy, self).__init__()

        self._risk_percent = self.Param("RiskPercent", 2) \
            .SetDisplay("Risk %", "Portfolio percentage allocated per trade", "Risk Management")
        self._take_profit_pips = self.Param("TakeProfitPips", 5000) \
            .SetDisplay("Risk %", "Portfolio percentage allocated per trade", "Risk Management")
        self._stop_loss_pips = self.Param("StopLossPips", 3000) \
            .SetDisplay("Risk %", "Portfolio percentage allocated per trade", "Risk Management")
        self._candle_type = self.Param("CandleType", TimeSpan.FromDays(1) \
            .SetDisplay("Risk %", "Portfolio percentage allocated per trade", "Risk Management")

        self._random = None
        self._price_step = 0.0
        self._take_profit_distance = 0.0
        self._stop_loss_distance = 0.0
        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(coin_flipping_strategy, self).OnReseted()
        self._random = None
        self._price_step = 0.0
        self._take_profit_distance = 0.0
        self._stop_loss_distance = 0.0
        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None

    def OnStarted(self, time):
        super(coin_flipping_strategy, self).OnStarted(time)


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
        return coin_flipping_strategy()
