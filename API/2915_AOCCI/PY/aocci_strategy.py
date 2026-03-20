import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy


class aocci_strategy(Strategy):
    def __init__(self):
        super(aocci_strategy, self).__init__()

        self._trade_volume = self.Param("TradeVolume", 1) \
            .SetDisplay("Trade Volume", "Base order volume", "Risk")
        self._stop_loss_pips = self.Param("StopLossPips", 50) \
            .SetDisplay("Trade Volume", "Base order volume", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", 50) \
            .SetDisplay("Trade Volume", "Base order volume", "Risk")
        self._trailing_stop_pips = self.Param("TrailingStopPips", 5) \
            .SetDisplay("Trade Volume", "Base order volume", "Risk")
        self._trailing_step_pips = self.Param("TrailingStepPips", 5) \
            .SetDisplay("Trade Volume", "Base order volume", "Risk")
        self._cci_period = self.Param("CciPeriod", 55) \
            .SetDisplay("Trade Volume", "Base order volume", "Risk")
        self._signal_candle_shift = self.Param("SignalCandleShift", 0) \
            .SetDisplay("Trade Volume", "Base order volume", "Risk")
        self._big_jump_pips = self.Param("BigJumpPips", 100) \
            .SetDisplay("Trade Volume", "Base order volume", "Risk")
        self._double_jump_pips = self.Param("DoubleJumpPips", 100) \
            .SetDisplay("Trade Volume", "Base order volume", "Risk")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(15) \
            .SetDisplay("Trade Volume", "Base order volume", "Risk")
        self._higher_candle_type = self.Param("HigherCandleType", TimeSpan.FromHours(1) \
            .SetDisplay("Trade Volume", "Base order volume", "Risk")

        self._ao = None
        self._cci = None
        self._last_ao_value = None
        self._cci_values = new()
        self._max_cci_values = 0.0
        self._recent_candles = new()
        self._max_recent_candles = 0.0
        self._long_stop = None
        self._long_take = None
        self._short_stop = None
        self._short_take = None
        self._long_entry_price = None
        self._short_entry_price = None
        self._pip_size = 0.0
        self._last_higher_close = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(aocci_strategy, self).OnReseted()
        self._ao = None
        self._cci = None
        self._last_ao_value = None
        self._cci_values = new()
        self._max_cci_values = 0.0
        self._recent_candles = new()
        self._max_recent_candles = 0.0
        self._long_stop = None
        self._long_take = None
        self._short_stop = None
        self._short_take = None
        self._long_entry_price = None
        self._short_entry_price = None
        self._pip_size = 0.0
        self._last_higher_close = None

    def OnStarted(self, time):
        super(aocci_strategy, self).OnStarted(time)

        self.__cci = CommodityChannelIndex()
        self.__cci.Length = self.cci_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(_ao, self.__cci, self._process_candle).Start()

        higher_subscription = self.SubscribeCandles(Higherself.candle_type)
        higher_subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return aocci_strategy()
