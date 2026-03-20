import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import DecimalIndicatorValue, ExponentialMovingAverage, SimpleMovingAverage, SmoothedMovingAverage, WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Sides


class ea_moving_average_strategy(Strategy):
    def __init__(self):
        super(ea_moving_average_strategy, self).__init__()

        self._maximum_risk = self.Param("MaximumRisk", 0.02) \
            .SetDisplay("Maximum Risk", "Risk per trade as part of equity", "Risk")
        self._decrease_factor = self.Param("DecreaseFactor", 3) \
            .SetDisplay("Maximum Risk", "Risk per trade as part of equity", "Risk")
        self._buy_open_period = self.Param("BuyOpenPeriod", 30) \
            .SetDisplay("Maximum Risk", "Risk per trade as part of equity", "Risk")
        self._buy_open_shift = self.Param("BuyOpenShift", 3) \
            .SetDisplay("Maximum Risk", "Risk per trade as part of equity", "Risk")
        self._buy_open_method = self.Param("BuyOpenMethod", MaMethods.Exponential) \
            .SetDisplay("Maximum Risk", "Risk per trade as part of equity", "Risk")
        self._buy_open_price = self.Param("BuyOpenPrice", MaPriceTypes.Close) \
            .SetDisplay("Maximum Risk", "Risk per trade as part of equity", "Risk")
        self._buy_close_period = self.Param("BuyClosePeriod", 14) \
            .SetDisplay("Maximum Risk", "Risk per trade as part of equity", "Risk")
        self._buy_close_shift = self.Param("BuyCloseShift", 3) \
            .SetDisplay("Maximum Risk", "Risk per trade as part of equity", "Risk")
        self._buy_close_method = self.Param("BuyCloseMethod", MaMethods.Exponential) \
            .SetDisplay("Maximum Risk", "Risk per trade as part of equity", "Risk")
        self._buy_close_price = self.Param("BuyClosePrice", MaPriceTypes.Close) \
            .SetDisplay("Maximum Risk", "Risk per trade as part of equity", "Risk")
        self._sell_open_period = self.Param("SellOpenPeriod", 30) \
            .SetDisplay("Maximum Risk", "Risk per trade as part of equity", "Risk")
        self._sell_open_shift = self.Param("SellOpenShift", 0) \
            .SetDisplay("Maximum Risk", "Risk per trade as part of equity", "Risk")
        self._sell_open_method = self.Param("SellOpenMethod", MaMethods.Exponential) \
            .SetDisplay("Maximum Risk", "Risk per trade as part of equity", "Risk")
        self._sell_open_price = self.Param("SellOpenPrice", MaPriceTypes.Close) \
            .SetDisplay("Maximum Risk", "Risk per trade as part of equity", "Risk")
        self._sell_close_period = self.Param("SellClosePeriod", 20) \
            .SetDisplay("Maximum Risk", "Risk per trade as part of equity", "Risk")
        self._sell_close_shift = self.Param("SellCloseShift", 2) \
            .SetDisplay("Maximum Risk", "Risk per trade as part of equity", "Risk")
        self._sell_close_method = self.Param("SellCloseMethod", MaMethods.Exponential) \
            .SetDisplay("Maximum Risk", "Risk per trade as part of equity", "Risk")
        self._sell_close_price = self.Param("SellClosePrice", MaPriceTypes.Close) \
            .SetDisplay("Maximum Risk", "Risk per trade as part of equity", "Risk")
        self._use_buy = self.Param("UseBuy", True) \
            .SetDisplay("Maximum Risk", "Risk per trade as part of equity", "Risk")
        self._use_sell = self.Param("UseSell", True) \
            .SetDisplay("Maximum Risk", "Risk per trade as part of equity", "Risk")
        self._consider_price_last_out = self.Param("ConsiderPriceLastOut", True) \
            .SetDisplay("Maximum Risk", "Risk per trade as part of equity", "Risk")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1) \
            .SetDisplay("Maximum Risk", "Risk per trade as part of equity", "Risk")

        self._buy_open_ma = None
        self._buy_close_ma = None
        self._sell_open_ma = None
        self._sell_close_ma = None
        self._buy_open_buffer = new()
        self._buy_close_buffer = new()
        self._sell_open_buffer = new()
        self._sell_close_buffer = new()
        self._last_exit_price = 0.0
        self._last_entry_price = 0.0
        self._last_entry_side = None
        self._signed_position = 0.0
        self._consecutive_losses = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ea_moving_average_strategy, self).OnReseted()
        self._buy_open_ma = None
        self._buy_close_ma = None
        self._sell_open_ma = None
        self._sell_close_ma = None
        self._buy_open_buffer = new()
        self._buy_close_buffer = new()
        self._sell_open_buffer = new()
        self._sell_close_buffer = new()
        self._last_exit_price = 0.0
        self._last_entry_price = 0.0
        self._last_entry_side = None
        self._signed_position = 0.0
        self._consecutive_losses = 0.0

    def OnStarted(self, time):
        super(ea_moving_average_strategy, self).OnStarted(time)


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
        return ea_moving_average_strategy()
