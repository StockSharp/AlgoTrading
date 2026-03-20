import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex, ExponentialMovingAverage, ExponentialMovingAverage as EMA, WilliamsR
from StockSharp.Algo.Strategies import Strategy


class rabbit_m3_strategy(Strategy):
    def __init__(self):
        super(rabbit_m3_strategy, self).__init__()

        self._fast_ema_period = self.Param("FastEmaPeriod", 33) \
            .SetDisplay("Fast EMA Period", "Length of the fast trend filter (H1 EMA)", "Trend Filter")
        self._slow_ema_period = self.Param("SlowEmaPeriod", 70) \
            .SetDisplay("Fast EMA Period", "Length of the fast trend filter (H1 EMA)", "Trend Filter")
        self._williams_period = self.Param("WilliamsPeriod", 62) \
            .SetDisplay("Fast EMA Period", "Length of the fast trend filter (H1 EMA)", "Trend Filter")
        self._williams_sell_level = self.Param("WilliamsSellLevel", -20) \
            .SetDisplay("Fast EMA Period", "Length of the fast trend filter (H1 EMA)", "Trend Filter")
        self._williams_buy_level = self.Param("WilliamsBuyLevel", -80) \
            .SetDisplay("Fast EMA Period", "Length of the fast trend filter (H1 EMA)", "Trend Filter")
        self._cci_period = self.Param("CciPeriod", 26) \
            .SetDisplay("Fast EMA Period", "Length of the fast trend filter (H1 EMA)", "Trend Filter")
        self._cci_sell_level = self.Param("CciSellLevel", 101) \
            .SetDisplay("Fast EMA Period", "Length of the fast trend filter (H1 EMA)", "Trend Filter")
        self._cci_buy_level = self.Param("CciBuyLevel", 99) \
            .SetDisplay("Fast EMA Period", "Length of the fast trend filter (H1 EMA)", "Trend Filter")
        self._donchian_length = self.Param("DonchianLength", 410) \
            .SetDisplay("Fast EMA Period", "Length of the fast trend filter (H1 EMA)", "Trend Filter")
        self._max_open_positions = self.Param("MaxOpenPositions", 1) \
            .SetDisplay("Fast EMA Period", "Length of the fast trend filter (H1 EMA)", "Trend Filter")
        self._take_profit_pips = self.Param("TakeProfitPips", 360) \
            .SetDisplay("Fast EMA Period", "Length of the fast trend filter (H1 EMA)", "Trend Filter")
        self._stop_loss_pips = self.Param("StopLossPips", 20) \
            .SetDisplay("Fast EMA Period", "Length of the fast trend filter (H1 EMA)", "Trend Filter")
        self._entry_volume = self.Param("EntryVolume", 0.01) \
            .SetDisplay("Fast EMA Period", "Length of the fast trend filter (H1 EMA)", "Trend Filter")
        self._big_win_threshold = self.Param("BigWinThreshold", 4) \
            .SetDisplay("Fast EMA Period", "Length of the fast trend filter (H1 EMA)", "Trend Filter")
        self._volume_increment = self.Param("VolumeIncrement", 0.01) \
            .SetDisplay("Fast EMA Period", "Length of the fast trend filter (H1 EMA)", "Trend Filter")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1) \
            .SetDisplay("Fast EMA Period", "Length of the fast trend filter (H1 EMA)", "Trend Filter")

        self._fast_ema = null!
        self._slow_ema = null!
        self._cci = null!
        self._williams = null!
        self._donchian = null!
        self._pip_size = 0.0
        self._current_volume = 0.0
        self._current_big_win_target = 0.0
        self._previous_williams = None
        self._current_donchian_upper = None
        self._current_donchian_lower = None
        self._previous_donchian_upper = None
        self._previous_donchian_lower = None
        self._trend_direction = None
        self._allow_buy = False
        self._allow_sell = False
        self._long_active = False
        self._short_active = False
        self._long_entry_price = 0.0
        self._short_entry_price = 0.0
        self._long_stop_price = 0.0
        self._long_take_profit_price = 0.0
        self._short_stop_price = 0.0
        self._short_take_profit_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(rabbit_m3_strategy, self).OnReseted()
        self._fast_ema = null!
        self._slow_ema = null!
        self._cci = null!
        self._williams = null!
        self._donchian = null!
        self._pip_size = 0.0
        self._current_volume = 0.0
        self._current_big_win_target = 0.0
        self._previous_williams = None
        self._current_donchian_upper = None
        self._current_donchian_lower = None
        self._previous_donchian_upper = None
        self._previous_donchian_lower = None
        self._trend_direction = None
        self._allow_buy = False
        self._allow_sell = False
        self._long_active = False
        self._short_active = False
        self._long_entry_price = 0.0
        self._short_entry_price = 0.0
        self._long_stop_price = 0.0
        self._long_take_profit_price = 0.0
        self._short_stop_price = 0.0
        self._short_take_profit_price = 0.0

    def OnStarted(self, time):
        super(rabbit_m3_strategy, self).OnStarted(time)
        self.StartProtection(None, None)

        self.__fast_ema = EMA()
        self.__fast_ema.Length = self.fast_ema_period
        self.__slow_ema = EMA()
        self.__slow_ema.Length = self.slow_ema_period
        self.__cci = CommodityChannelIndex()
        self.__cci.Length = self.cci_period
        self.__williams = WilliamsR()
        self.__williams.Length = self.williams_period
        self.__donchian = DonchianChannels()
        self.__donchian.Length = self.donchian_length

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return rabbit_m3_strategy()
