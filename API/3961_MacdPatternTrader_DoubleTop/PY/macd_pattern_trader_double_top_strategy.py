import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Unit, UnitTypes


class macd_pattern_trader_double_top_strategy(Strategy):
    def __init__(self):
        super(macd_pattern_trader_double_top_strategy, self).__init__()

        self._fast_period = self.Param("FastPeriod", 5) \
            .SetDisplay("Fast EMA", "Fast moving average length used by MACD", "MACD")
        self._slow_period = self.Param("SlowPeriod", 13) \
            .SetDisplay("Fast EMA", "Fast moving average length used by MACD", "MACD")
        self._signal_period = self.Param("SignalPeriod", 1) \
            .SetDisplay("Fast EMA", "Fast moving average length used by MACD", "MACD")
        self._trigger_level = self.Param("TriggerLevel", 50) \
            .SetDisplay("Fast EMA", "Fast moving average length used by MACD", "MACD")
        self._stop_loss_pips = self.Param("StopLossPips", 100) \
            .SetDisplay("Fast EMA", "Fast moving average length used by MACD", "MACD")
        self._take_profit_pips = self.Param("TakeProfitPips", 300) \
            .SetDisplay("Fast EMA", "Fast moving average length used by MACD", "MACD")
        self._trade_volume = self.Param("TradeVolume", 0.1) \
            .SetDisplay("Fast EMA", "Fast moving average length used by MACD", "MACD")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(30) \
            .SetDisplay("Fast EMA", "Fast moving average length used by MACD", "MACD")

        self._macd = null!
        self._previous_macd = None
        self._previous_macd2 = None
        self._first_peak = None
        self._first_trough = None
        self._sell_pattern_armed = False
        self._buy_pattern_armed = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(macd_pattern_trader_double_top_strategy, self).OnReseted()
        self._macd = null!
        self._previous_macd = None
        self._previous_macd2 = None
        self._first_peak = None
        self._first_trough = None
        self._sell_pattern_armed = False
        self._buy_pattern_armed = False

    def OnStarted(self, time):
        super(macd_pattern_trader_double_top_strategy, self).OnStarted(time)


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
        return macd_pattern_trader_double_top_strategy()
