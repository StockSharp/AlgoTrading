import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex, ExponentialMovingAverage as EMA, WilliamsR
from StockSharp.Algo.Strategies import Strategy


class rabbit_m2_regime_swing_strategy(Strategy):
    def __init__(self):
        super(rabbit_m2_regime_swing_strategy, self).__init__()

        self._cci_sell_level = self.Param("CciSellLevel", 101) \
            .SetDisplay("CCI Sell Level", "CCI threshold confirming a short signal", "CCI")
        self._cci_buy_level = self.Param("CciBuyLevel", 99) \
            .SetDisplay("CCI Sell Level", "CCI threshold confirming a short signal", "CCI")
        self._cci_period = self.Param("CciPeriod", 14) \
            .SetDisplay("CCI Sell Level", "CCI threshold confirming a short signal", "CCI")
        self._donchian_period = self.Param("DonchianPeriod", 100) \
            .SetDisplay("CCI Sell Level", "CCI threshold confirming a short signal", "CCI")
        self._max_trades = self.Param("MaxTrades", 1) \
            .SetDisplay("CCI Sell Level", "CCI threshold confirming a short signal", "CCI")
        self._big_win_target = self.Param("BigWinTarget", 15) \
            .SetDisplay("CCI Sell Level", "CCI threshold confirming a short signal", "CCI")
        self._volume_increment = self.Param("VolumeIncrement", 0.01) \
            .SetDisplay("CCI Sell Level", "CCI threshold confirming a short signal", "CCI")
        self._wpr_period = self.Param("WprPeriod", 50) \
            .SetDisplay("CCI Sell Level", "CCI threshold confirming a short signal", "CCI")
        self._fast_ema_period = self.Param("FastEmaPeriod", 40) \
            .SetDisplay("CCI Sell Level", "CCI threshold confirming a short signal", "CCI")
        self._slow_ema_period = self.Param("SlowEmaPeriod", 80) \
            .SetDisplay("CCI Sell Level", "CCI threshold confirming a short signal", "CCI")
        self._take_profit_points = self.Param("TakeProfitPoints", 50) \
            .SetDisplay("CCI Sell Level", "CCI threshold confirming a short signal", "CCI")
        self._stop_loss_points = self.Param("StopLossPoints", 50) \
            .SetDisplay("CCI Sell Level", "CCI threshold confirming a short signal", "CCI")
        self._initial_volume = self.Param("InitialVolume", 0.01) \
            .SetDisplay("CCI Sell Level", "CCI threshold confirming a short signal", "CCI")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(2) \
            .SetDisplay("CCI Sell Level", "CCI threshold confirming a short signal", "CCI")

        self._base_volume = 0.0
        self._profit_threshold = 0.0
        self._last_realized_pn_l = 0.0
        self._previous_wpr = None
        self._previous_upper_band = None
        self._previous_lower_band = None
        self._long_regime_enabled = False
        self._short_regime_enabled = False
        self._stop_distance = 0.0
        self._take_distance = 0.0
        self._active_stop = 0.0
        self._active_take = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(rabbit_m2_regime_swing_strategy, self).OnReseted()
        self._base_volume = 0.0
        self._profit_threshold = 0.0
        self._last_realized_pn_l = 0.0
        self._previous_wpr = None
        self._previous_upper_band = None
        self._previous_lower_band = None
        self._long_regime_enabled = False
        self._short_regime_enabled = False
        self._stop_distance = 0.0
        self._take_distance = 0.0
        self._active_stop = 0.0
        self._active_take = 0.0

    def OnStarted(self, time):
        super(rabbit_m2_regime_swing_strategy, self).OnStarted(time)

        self._wpr = WilliamsR()
        self._wpr.Length = self.wpr_period
        self._cci = CommodityChannelIndex()
        self._cci.Length = self.cci_period
        self._donchian = DonchianChannels()
        self._donchian.Length = self.donchian_period
        self._ema_fast = EMA()
        self._ema_fast.Length = self.fast_ema_period
        self._ema_slow = EMA()
        self._ema_slow.Length = self.slow_ema_period

        trend_subscription = self.SubscribeCandles(Trendself.candle_type)
        trend_subscription.BindEx(self._ema_fast, self._ema_slow, self._process_candle).Start()

        primary_subscription = self.SubscribeCandles(self.candle_type)
        primary_subscription.BindEx(self._wpr, self._cci, self._donchian, self._process_candle_1).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return rabbit_m2_regime_swing_strategy()
