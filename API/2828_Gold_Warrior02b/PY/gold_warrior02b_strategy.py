import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class gold_warrior02b_strategy(Strategy):
    def __init__(self):
        super(gold_warrior02b_strategy, self).__init__()

        self._base_volume = self.Param("BaseVolume", 0.1) \
            .SetDisplay("Volume", "Base trade size", "Trading")
        self._stop_loss_points = self.Param("StopLossPoints", 100) \
            .SetDisplay("Volume", "Base trade size", "Trading")
        self._take_profit_points = self.Param("TakeProfitPoints", 150) \
            .SetDisplay("Volume", "Base trade size", "Trading")
        self._trailing_stop_points = self.Param("TrailingStopPoints", 5) \
            .SetDisplay("Volume", "Base trade size", "Trading")
        self._trailing_step_points = self.Param("TrailingStepPoints", 5) \
            .SetDisplay("Volume", "Base trade size", "Trading")
        self._impulse_period = self.Param("ImpulsePeriod", 21) \
            .SetDisplay("Volume", "Base trade size", "Trading")
        self._zig_zag_depth = self.Param("ZigZagDepth", 12) \
            .SetDisplay("Volume", "Base trade size", "Trading")
        self._zig_zag_deviation = self.Param("ZigZagDeviation", 5) \
            .SetDisplay("Volume", "Base trade size", "Trading")
        self._zig_zag_backstep = self.Param("ZigZagBackstep", 3) \
            .SetDisplay("Volume", "Base trade size", "Trading")
        self._profit_target = self.Param("ProfitTarget", 300) \
            .SetDisplay("Volume", "Base trade size", "Trading")
        self._impulse_sell_threshold = self.Param("ImpulseSellThreshold", -30) \
            .SetDisplay("Volume", "Base trade size", "Trading")
        self._impulse_buy_threshold = self.Param("ImpulseBuyThreshold", 30) \
            .SetDisplay("Volume", "Base trade size", "Trading")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(2) \
            .SetDisplay("Volume", "Base trade size", "Trading")

        self._cci = null!
        self._impulse = null!
        self._last_zig_zag = None
        self._previous_zig_zag = None
        self._search_direction = 0.0
        self._current_extreme = None
        self._bars_since_extreme = 0.0
        self._previous_cci = 0.0
        self._previous_impulse = 0.0
        self._has_previous_cci = False
        self._has_previous_impulse = False
        self._last_trade_time = None
        self._entry_price = 0.0
        self._trailing_stop_price = 0.0
        self._trailing_active = False
        self._max_price_since_entry = 0.0
        self._min_price_since_entry = 0.0
        self._buffer = new()
        self._sum = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(gold_warrior02b_strategy, self).OnReseted()
        self._cci = null!
        self._impulse = null!
        self._last_zig_zag = None
        self._previous_zig_zag = None
        self._search_direction = 0.0
        self._current_extreme = None
        self._bars_since_extreme = 0.0
        self._previous_cci = 0.0
        self._previous_impulse = 0.0
        self._has_previous_cci = False
        self._has_previous_impulse = False
        self._last_trade_time = None
        self._entry_price = 0.0
        self._trailing_stop_price = 0.0
        self._trailing_active = False
        self._max_price_since_entry = 0.0
        self._min_price_since_entry = 0.0
        self._buffer = new()
        self._sum = 0.0

    def OnStarted(self, time):
        super(gold_warrior02b_strategy, self).OnStarted(time)

        self.__cci = CommodityChannelIndex()
        self.__cci.Length = self.impulse_period
        self.__impulse = ImpulseIndicator()
        self.__impulse.Length = self.impulse_period
        self.__impulse.PriceStep = GetPriceStep()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.__cci, self.__impulse, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return gold_warrior02b_strategy()
