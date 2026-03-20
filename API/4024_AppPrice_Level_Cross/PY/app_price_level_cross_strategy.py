import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Unit, UnitTypes


class app_price_level_cross_strategy(Strategy):
    def __init__(self):
        super(app_price_level_cross_strategy, self).__init__()

        self._app_price = self.Param("AppPrice", 65000) \
            .SetDisplay("App Price", "Reference level that generates trades when the close crosses it", "Trading")
        self._buy_only = self.Param("BuyOnly", True) \
            .SetDisplay("App Price", "Reference level that generates trades when the close crosses it", "Trading")
        self._fixed_volume = self.Param("FixedVolume", 0.1) \
            .SetDisplay("App Price", "Reference level that generates trades when the close crosses it", "Trading")
        self._stop_loss_points = self.Param("StopLossPoints", 140) \
            .SetDisplay("App Price", "Reference level that generates trades when the close crosses it", "Trading")
        self._take_profit_points = self.Param("TakeProfitPoints", 180) \
            .SetDisplay("App Price", "Reference level that generates trades when the close crosses it", "Trading")
        self._enable_money_management = self.Param("EnableMoneyManagement", False) \
            .SetDisplay("App Price", "Reference level that generates trades when the close crosses it", "Trading")
        self._lot_balance_percent = self.Param("LotBalancePercent", 10) \
            .SetDisplay("App Price", "Reference level that generates trades when the close crosses it", "Trading")
        self._min_lot = self.Param("MinLot", 0.1) \
            .SetDisplay("App Price", "Reference level that generates trades when the close crosses it", "Trading")
        self._max_lot = self.Param("MaxLot", 5) \
            .SetDisplay("App Price", "Reference level that generates trades when the close crosses it", "Trading")
        self._lot_precision = self.Param("LotPrecision", 1) \
            .SetDisplay("App Price", "Reference level that generates trades when the close crosses it", "Trading")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1) \
            .SetDisplay("App Price", "Reference level that generates trades when the close crosses it", "Trading")

        self._previous_close = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(app_price_level_cross_strategy, self).OnReseted()
        self._previous_close = None

    def OnStarted(self, time):
        super(app_price_level_cross_strategy, self).OnStarted(time)

        self._dummy_sma = SimpleMovingAverage()
        self._dummy_sma.Length = 2

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._dummy_sma, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return app_price_level_cross_strategy()
