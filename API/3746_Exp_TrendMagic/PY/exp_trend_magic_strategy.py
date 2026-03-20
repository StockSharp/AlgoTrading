import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, CommodityChannelIndex, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class exp_trend_magic_strategy(Strategy):
    def __init__(self):
        super(exp_trend_magic_strategy, self).__init__()

        self._money_management = self.Param("MoneyManagement", 0.1) \
            .SetDisplay("Money Management", "Share of capital used per trade", "Trading")
        self._margin_mode = self.Param("MarginMode", MarginModeOptions.Lot) \
            .SetDisplay("Money Management", "Share of capital used per trade", "Trading")
        self._stop_loss_points = self.Param("StopLossPoints", 1000) \
            .SetDisplay("Money Management", "Share of capital used per trade", "Trading")
        self._take_profit_points = self.Param("TakeProfitPoints", 2000) \
            .SetDisplay("Money Management", "Share of capital used per trade", "Trading")
        self._deviation_points = self.Param("DeviationPoints", 10) \
            .SetDisplay("Money Management", "Share of capital used per trade", "Trading")
        self._allow_buy_entry = self.Param("AllowBuyEntry", True) \
            .SetDisplay("Money Management", "Share of capital used per trade", "Trading")
        self._allow_sell_entry = self.Param("AllowSellEntry", True) \
            .SetDisplay("Money Management", "Share of capital used per trade", "Trading")
        self._allow_buy_exit = self.Param("AllowBuyExit", True) \
            .SetDisplay("Money Management", "Share of capital used per trade", "Trading")
        self._allow_sell_exit = self.Param("AllowSellExit", True) \
            .SetDisplay("Money Management", "Share of capital used per trade", "Trading")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Money Management", "Share of capital used per trade", "Trading")
        self._cci_period = self.Param("CciPeriod", 50) \
            .SetDisplay("Money Management", "Share of capital used per trade", "Trading")
        self._cci_price = self.Param("CciPrice", AppliedPriceModes.Median) \
            .SetDisplay("Money Management", "Share of capital used per trade", "Trading")
        self._atr_period = self.Param("AtrPeriod", 5) \
            .SetDisplay("Money Management", "Share of capital used per trade", "Trading")
        self._signal_bar = self.Param("SignalBar", 1) \
            .SetDisplay("Money Management", "Share of capital used per trade", "Trading")

        self._cci = None
        self._atr = None
        self._color_history = None
        self._previous_trend_magic_value = None
        self._entry_price = None
        self._candle_time_frame = None
        self._next_long_trade_allowed = None
        self._next_short_trade_allowed = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(exp_trend_magic_strategy, self).OnReseted()
        self._cci = None
        self._atr = None
        self._color_history = None
        self._previous_trend_magic_value = None
        self._entry_price = None
        self._candle_time_frame = None
        self._next_long_trade_allowed = None
        self._next_short_trade_allowed = None

    def OnStarted(self, time):
        super(exp_trend_magic_strategy, self).OnStarted(time)

        self.__cci = CommodityChannelIndex()
        self.__cci.Length = self.cci_period
        self.__atr = AverageTrueRange()
        self.__atr.Length = self.atr_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self.__atr, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return exp_trend_magic_strategy()
