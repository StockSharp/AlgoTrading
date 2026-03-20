import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Unit, UnitTypes


class balance_drawdown_in_mt4_strategy(Strategy):
    def __init__(self):
        super(balance_drawdown_in_mt4_strategy, self).__init__()

        self._start_balance = self.Param("StartBalance", 1000) \
            .SetDisplay("Start Balance", "Initial balance for drawdown measurement.", "Risk")
        self._stop_loss_points = self.Param("StopLossPoints", 300) \
            .SetDisplay("Start Balance", "Initial balance for drawdown measurement.", "Risk")
        self._take_profit_points = self.Param("TakeProfitPoints", 400) \
            .SetDisplay("Start Balance", "Initial balance for drawdown measurement.", "Risk")
        self._entry_cooldown_days = self.Param("EntryCooldownDays", 5) \
            .SetDisplay("Start Balance", "Initial balance for drawdown measurement.", "Risk")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Start Balance", "Initial balance for drawdown measurement.", "Risk")

        self._max_balance = 0.0
        self._last_drawdown = 0.0
        self._last_price = 0.0
        self._last_entry_date = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(balance_drawdown_in_mt4_strategy, self).OnReseted()
        self._max_balance = 0.0
        self._last_drawdown = 0.0
        self._last_price = 0.0
        self._last_entry_date = None

    def OnStarted(self, time):
        super(balance_drawdown_in_mt4_strategy, self).OnStarted(time)


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
        return balance_drawdown_in_mt4_strategy()
