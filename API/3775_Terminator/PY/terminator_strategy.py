import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Sides


class terminator_strategy(Strategy):
    def __init__(self):
        super(terminator_strategy, self).__init__()

        self._take_profit_pips = self.Param("TakeProfitPips", 38) \
            .SetDisplay("Take Profit (pips)", "Distance of the take profit for each entry in pips", "Risk")
        self._lot_size = self.Param("LotSize", 0.1) \
            .SetDisplay("Take Profit (pips)", "Distance of the take profit for each entry in pips", "Risk")
        self._initial_stop_pips = self.Param("InitialStopPips", 0) \
            .SetDisplay("Take Profit (pips)", "Distance of the take profit for each entry in pips", "Risk")
        self._trailing_stop_pips = self.Param("TrailingStopPips", 0) \
            .SetDisplay("Take Profit (pips)", "Distance of the take profit for each entry in pips", "Risk")
        self._max_trades = self.Param("MaxTrades", 1) \
            .SetDisplay("Take Profit (pips)", "Distance of the take profit for each entry in pips", "Risk")
        self._entry_distance_pips = self.Param("EntryDistancePips", 18) \
            .SetDisplay("Take Profit (pips)", "Distance of the take profit for each entry in pips", "Risk")
        self._secure_profit = self.Param("SecureProfit", 10) \
            .SetDisplay("Take Profit (pips)", "Distance of the take profit for each entry in pips", "Risk")
        self._use_account_protection = self.Param("UseAccountProtection", True) \
            .SetDisplay("Take Profit (pips)", "Distance of the take profit for each entry in pips", "Risk")
        self._protect_using_balance = self.Param("ProtectUsingBalance", False) \
            .SetDisplay("Take Profit (pips)", "Distance of the take profit for each entry in pips", "Risk")
        self._orders_to_protect = self.Param("OrdersToProtect", 3) \
            .SetDisplay("Take Profit (pips)", "Distance of the take profit for each entry in pips", "Risk")
        self._reverse_signals = self.Param("ReverseSignals", False) \
            .SetDisplay("Take Profit (pips)", "Distance of the take profit for each entry in pips", "Risk")
        self._manual_trading = self.Param("ManualTrading", False) \
            .SetDisplay("Take Profit (pips)", "Distance of the take profit for each entry in pips", "Risk")
        self._use_money_management = self.Param("UseMoneyManagement", False) \
            .SetDisplay("Take Profit (pips)", "Distance of the take profit for each entry in pips", "Risk")
        self._risk_percent = self.Param("RiskPercent", 1) \
            .SetDisplay("Take Profit (pips)", "Distance of the take profit for each entry in pips", "Risk")
        self._is_standard_account = self.Param("IsStandardAccount", False) \
            .SetDisplay("Take Profit (pips)", "Distance of the take profit for each entry in pips", "Risk")
        self._eur_usd_pip_value = self.Param("EurUsdPipValue", 10) \
            .SetDisplay("Take Profit (pips)", "Distance of the take profit for each entry in pips", "Risk")
        self._gbp_usd_pip_value = self.Param("GbpUsdPipValue", 10) \
            .SetDisplay("Take Profit (pips)", "Distance of the take profit for each entry in pips", "Risk")
        self._usd_chf_pip_value = self.Param("UsdChfPipValue", 8.7) \
            .SetDisplay("Take Profit (pips)", "Distance of the take profit for each entry in pips", "Risk")
        self._usd_jpy_pip_value = self.Param("UsdJpyPipValue", 9.715) \
            .SetDisplay("Take Profit (pips)", "Distance of the take profit for each entry in pips", "Risk")
        self._default_pip_value = self.Param("DefaultPipValue", 5) \
            .SetDisplay("Take Profit (pips)", "Distance of the take profit for each entry in pips", "Risk")
        self._start_year = self.Param("StartYear", 2005) \
            .SetDisplay("Take Profit (pips)", "Distance of the take profit for each entry in pips", "Risk")
        self._start_month = self.Param("StartMonth", 1) \
            .SetDisplay("Take Profit (pips)", "Distance of the take profit for each entry in pips", "Risk")
        self._end_year = self.Param("EndYear", 2030) \
            .SetDisplay("Take Profit (pips)", "Distance of the take profit for each entry in pips", "Risk")
        self._end_month = self.Param("EndMonth", 12) \
            .SetDisplay("Take Profit (pips)", "Distance of the take profit for each entry in pips", "Risk")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Take Profit (pips)", "Distance of the take profit for each entry in pips", "Risk")
        self._macd_fast_length = self.Param("MacdFastLength", 14) \
            .SetDisplay("Take Profit (pips)", "Distance of the take profit for each entry in pips", "Risk")
        self._macd_slow_length = self.Param("MacdSlowLength", 26) \
            .SetDisplay("Take Profit (pips)", "Distance of the take profit for each entry in pips", "Risk")
        self._macd_signal_length = self.Param("MacdSignalLength", 9) \
            .SetDisplay("Take Profit (pips)", "Distance of the take profit for each entry in pips", "Risk")

        self._macd = None
        self._previous_macd = None
        self._previous_previous_macd = None
        self._open_volume = 0.0
        self._average_price = 0.0
        self._open_trades = 0.0
        self._is_long_position = False
        self._last_entry_price = 0.0
        self._last_entry_volume = 0.0
        self._stop_loss_price = None
        self._take_profit_price = None
        self._pip_size = 0.0
        self._pip_value = 0.0
        self._continue_opening = False
        self._current_direction = None
        self._martingale_base_volume = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(terminator_strategy, self).OnReseted()
        self._macd = None
        self._previous_macd = None
        self._previous_previous_macd = None
        self._open_volume = 0.0
        self._average_price = 0.0
        self._open_trades = 0.0
        self._is_long_position = False
        self._last_entry_price = 0.0
        self._last_entry_volume = 0.0
        self._stop_loss_price = None
        self._take_profit_price = None
        self._pip_size = 0.0
        self._pip_value = 0.0
        self._continue_opening = False
        self._current_direction = None
        self._martingale_base_volume = 0.0

    def OnStarted(self, time):
        super(terminator_strategy, self).OnStarted(time)
        self.StartProtection(None, None)


        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(_macd, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return terminator_strategy()
