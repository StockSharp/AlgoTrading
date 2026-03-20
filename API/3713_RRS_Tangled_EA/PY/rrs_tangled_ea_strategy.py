import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class rrs_tangled_ea_strategy(Strategy):
    def __init__(self):
        super(rrs_tangled_ea_strategy, self).__init__()

        self._min_volume = self.Param("MinVolume", 0.01) \
            .SetDisplay("Minimum Volume", "Lower bound for random position sizing", "Money Management")
        self._max_volume = self.Param("MaxVolume", 0.50) \
            .SetDisplay("Minimum Volume", "Lower bound for random position sizing", "Money Management")
        self._take_profit_pips = self.Param("TakeProfitPips", 50000) \
            .SetDisplay("Minimum Volume", "Lower bound for random position sizing", "Money Management")
        self._stop_loss_pips = self.Param("StopLossPips", 50000) \
            .SetDisplay("Minimum Volume", "Lower bound for random position sizing", "Money Management")
        self._trailing_start_pips = self.Param("TrailingStartPips", 50000) \
            .SetDisplay("Minimum Volume", "Lower bound for random position sizing", "Money Management")
        self._trailing_gap_pips = self.Param("TrailingGapPips", 50) \
            .SetDisplay("Minimum Volume", "Lower bound for random position sizing", "Money Management")
        self._max_spread_pips = self.Param("MaxSpreadPips", 100) \
            .SetDisplay("Minimum Volume", "Lower bound for random position sizing", "Money Management")
        self._max_open_trades = self.Param("MaxOpenTrades", 10) \
            .SetDisplay("Minimum Volume", "Lower bound for random position sizing", "Money Management")
        self._risk_mode = self.Param("RiskManagementMode", RiskModes.BalancePercentage) \
            .SetDisplay("Minimum Volume", "Lower bound for random position sizing", "Money Management")
        self._risk_amount = self.Param("RiskAmount", 5) \
            .SetDisplay("Minimum Volume", "Lower bound for random position sizing", "Money Management")
        self._trade_comment = self.Param("TradeComment", "RRS") \
            .SetDisplay("Minimum Volume", "Lower bound for random position sizing", "Money Management")
        self._notes = self.Param("Notes", "Note For Your Reference") \
            .SetDisplay("Minimum Volume", "Lower bound for random position sizing", "Money Management")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Minimum Volume", "Lower bound for random position sizing", "Money Management")

        self._buy_entries = new()
        self._sell_entries = new()
        self._trade_counter = 0.0
        self._point = 0.0
        self._buy_trailing_stop = None
        self._sell_trailing_stop = None
        self._last_spread = None
        self._initial_balance = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(rrs_tangled_ea_strategy, self).OnReseted()
        self._buy_entries = new()
        self._sell_entries = new()
        self._trade_counter = 0.0
        self._point = 0.0
        self._buy_trailing_stop = None
        self._sell_trailing_stop = None
        self._last_spread = None
        self._initial_balance = 0.0

    def OnStarted(self, time):
        super(rrs_tangled_ea_strategy, self).OnStarted(time)
        self.StartProtection(None, None)


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
        return rrs_tangled_ea_strategy()
