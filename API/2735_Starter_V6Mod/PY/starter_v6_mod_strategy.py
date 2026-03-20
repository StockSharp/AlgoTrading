import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex, ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Unit, UnitTypes


class starter_v6_mod_strategy(Strategy):
    def __init__(self):
        super(starter_v6_mod_strategy, self).__init__()

        self._use_manual_volume = self.Param("UseManualVolume", True) \
            .SetDisplay("Manual Volume", "Use manual volume instead of risk-based sizing", "Money Management")
        self._manual_volume = self.Param("ManualVolume", 1) \
            .SetDisplay("Manual Volume", "Use manual volume instead of risk-based sizing", "Money Management")
        self._risk_percent = self.Param("RiskPercent", 5) \
            .SetDisplay("Manual Volume", "Use manual volume instead of risk-based sizing", "Money Management")
        self._stop_loss_pips = self.Param("StopLossPips", 35) \
            .SetDisplay("Manual Volume", "Use manual volume instead of risk-based sizing", "Money Management")
        self._take_profit_pips = self.Param("TakeProfitPips", 10) \
            .SetDisplay("Manual Volume", "Use manual volume instead of risk-based sizing", "Money Management")
        self._trailing_stop_pips = self.Param("TrailingStopPips", 0) \
            .SetDisplay("Manual Volume", "Use manual volume instead of risk-based sizing", "Money Management")
        self._trailing_step_pips = self.Param("TrailingStepPips", 5) \
            .SetDisplay("Manual Volume", "Use manual volume instead of risk-based sizing", "Money Management")
        self._decrease_factor = self.Param("DecreaseFactor", 1.6) \
            .SetDisplay("Manual Volume", "Use manual volume instead of risk-based sizing", "Money Management")
        self._max_losses_per_day = self.Param("MaxLossesPerDay", 3) \
            .SetDisplay("Manual Volume", "Use manual volume instead of risk-based sizing", "Money Management")
        self._equity_cutoff = self.Param("EquityCutoff", 800) \
            .SetDisplay("Manual Volume", "Use manual volume instead of risk-based sizing", "Money Management")
        self._max_open_trades = self.Param("MaxOpenTrades", 10) \
            .SetDisplay("Manual Volume", "Use manual volume instead of risk-based sizing", "Money Management")
        self._grid_step_pips = self.Param("GridStepPips", 30) \
            .SetDisplay("Manual Volume", "Use manual volume instead of risk-based sizing", "Money Management")
        self._long_ema_period = self.Param("LongEmaPeriod", 120) \
            .SetDisplay("Manual Volume", "Use manual volume instead of risk-based sizing", "Money Management")
        self._short_ema_period = self.Param("ShortEmaPeriod", 40) \
            .SetDisplay("Manual Volume", "Use manual volume instead of risk-based sizing", "Money Management")
        self._cci_period = self.Param("CciPeriod", 14) \
            .SetDisplay("Manual Volume", "Use manual volume instead of risk-based sizing", "Money Management")
        self._angle_threshold = self.Param("AngleThreshold", 3) \
            .SetDisplay("Manual Volume", "Use manual volume instead of risk-based sizing", "Money Management")
        self._level_up = self.Param("LevelUp", 0.85) \
            .SetDisplay("Manual Volume", "Use manual volume instead of risk-based sizing", "Money Management")
        self._level_down = self.Param("LevelDown", 0.15) \
            .SetDisplay("Manual Volume", "Use manual volume instead of risk-based sizing", "Money Management")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Manual Volume", "Use manual volume instead of risk-based sizing", "Money Management")

        self._long_ema = None
        self._short_ema = None
        self._cci = None
        self._laguerre_proxy = None
        self._prev_long_ema = None
        self._prev_short_ema = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(starter_v6_mod_strategy, self).OnReseted()
        self._long_ema = None
        self._short_ema = None
        self._cci = None
        self._laguerre_proxy = None
        self._prev_long_ema = None
        self._prev_short_ema = None

    def OnStarted(self, time):
        super(starter_v6_mod_strategy, self).OnStarted(time)

        self.__long_ema = ExponentialMovingAverage()
        self.__long_ema.Length = self.long_ema_period
        self.__short_ema = ExponentialMovingAverage()
        self.__short_ema.Length = self.short_ema_period
        self.__cci = CommodityChannelIndex()
        self.__cci.Length = self.cci_period
        self.__laguerre_proxy = RelativeStrengthIndex()
        self.__laguerre_proxy.Length = 14

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__long_ema, self.__short_ema, self.__cci, self.__laguerre_proxy, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return starter_v6_mod_strategy()
