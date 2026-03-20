import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, ExponentialMovingAverage as EMA, MoneyFlowIndex, MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Unit, UnitTypes


class trend_follower_rainbow_strategy(Strategy):
    def __init__(self):
        super(trend_follower_rainbow_strategy, self).__init__()

        self._order_volume = self.Param("OrderVolume", 1) \
            .SetDisplay("Order Volume", "Base order volume", "Trading")
        self._take_profit_points = self.Param("TakeProfitPoints", 17) \
            .SetDisplay("Order Volume", "Base order volume", "Trading")
        self._stop_loss_points = self.Param("StopLossPoints", 30) \
            .SetDisplay("Order Volume", "Base order volume", "Trading")
        self._trailing_stop_points = self.Param("TrailingStopPoints", 45) \
            .SetDisplay("Order Volume", "Base order volume", "Trading")
        self._trading_start_hour = self.Param("TradingStartHour", 1) \
            .SetDisplay("Order Volume", "Base order volume", "Trading")
        self._trading_end_hour = self.Param("TradingEndHour", 23) \
            .SetDisplay("Order Volume", "Base order volume", "Trading")
        self._fast_ema_length = self.Param("FastEmaLength", 4) \
            .SetDisplay("Order Volume", "Base order volume", "Trading")
        self._slow_ema_length = self.Param("SlowEmaLength", 8) \
            .SetDisplay("Order Volume", "Base order volume", "Trading")
        self._macd_fast_length = self.Param("MacdFastLength", 5) \
            .SetDisplay("Order Volume", "Base order volume", "Trading")
        self._macd_slow_length = self.Param("MacdSlowLength", 35) \
            .SetDisplay("Order Volume", "Base order volume", "Trading")
        self._macd_signal_length = self.Param("MacdSignalLength", 5) \
            .SetDisplay("Order Volume", "Base order volume", "Trading")
        self._laguerre_gamma = self.Param("LaguerreGamma", 0.7) \
            .SetDisplay("Order Volume", "Base order volume", "Trading")
        self._laguerre_buy_threshold = self.Param("LaguerreBuyThreshold", 0.15) \
            .SetDisplay("Order Volume", "Base order volume", "Trading")
        self._laguerre_sell_threshold = self.Param("LaguerreSellThreshold", 0.75) \
            .SetDisplay("Order Volume", "Base order volume", "Trading")
        self._mfi_period = self.Param("MfiPeriod", 14) \
            .SetDisplay("Order Volume", "Base order volume", "Trading")
        self._mfi_buy_level = self.Param("MfiBuyLevel", 40) \
            .SetDisplay("Order Volume", "Base order volume", "Trading")
        self._mfi_sell_level = self.Param("MfiSellLevel", 60) \
            .SetDisplay("Order Volume", "Base order volume", "Trading")
        self._rainbow_group1_base = self.Param("RainbowGroup1Base", 5) \
            .SetDisplay("Order Volume", "Base order volume", "Trading")
        self._rainbow_group2_base = self.Param("RainbowGroup2Base", 13) \
            .SetDisplay("Order Volume", "Base order volume", "Trading")
        self._rainbow_group3_base = self.Param("RainbowGroup3Base", 21) \
            .SetDisplay("Order Volume", "Base order volume", "Trading")
        self._rainbow_group4_base = self.Param("RainbowGroup4Base", 34) \
            .SetDisplay("Order Volume", "Base order volume", "Trading")
        self._rainbow_group5_base = self.Param("RainbowGroup5Base", 55) \
            .SetDisplay("Order Volume", "Base order volume", "Trading")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(15) \
            .SetDisplay("Order Volume", "Base order volume", "Trading")

        self._ema_fast = null!
        self._ema_slow = null!
        self._macd = null!
        self._laguerre = null!
        self._mfi = null!
        self._previous_fast_ema = None
        self._previous_slow_ema = None
        self._previous_laguerre = None
        self._point_value = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(trend_follower_rainbow_strategy, self).OnReseted()
        self._ema_fast = null!
        self._ema_slow = null!
        self._macd = null!
        self._laguerre = null!
        self._mfi = null!
        self._previous_fast_ema = None
        self._previous_slow_ema = None
        self._previous_laguerre = None
        self._point_value = 0.0

    def OnStarted(self, time):
        super(trend_follower_rainbow_strategy, self).OnStarted(time)

        self.__ema_fast = EMA()
        self.__ema_fast.Length = self.fast_ema_length
        self.__ema_slow = EMA()
        self.__ema_slow.Length = self.slow_ema_length
        self.__laguerre = AdaptiveLaguerreFilter()
        self.__laguerre.Gamma = self.laguerre_gamma
        self.__mfi = MoneyFlowIndex()
        self.__mfi.Length = self.mfi_period

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
        return trend_follower_rainbow_strategy()
