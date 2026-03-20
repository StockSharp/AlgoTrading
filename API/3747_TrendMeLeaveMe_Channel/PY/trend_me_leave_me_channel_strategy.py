import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class trend_me_leave_me_channel_strategy(Strategy):
    def __init__(self):
        super(trend_me_leave_me_channel_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1) \
            .SetDisplay("Candle Type", "Candle aggregation used for trend estimation", "General")
        self._trend_length = self.Param("TrendLength", 100) \
            .SetDisplay("Candle Type", "Candle aggregation used for trend estimation", "General")
        self._buy_step_upper = self.Param("BuyStepUpper", 10) \
            .SetDisplay("Candle Type", "Candle aggregation used for trend estimation", "General")
        self._buy_step_lower = self.Param("BuyStepLower", 50) \
            .SetDisplay("Candle Type", "Candle aggregation used for trend estimation", "General")
        self._sell_step_upper = self.Param("SellStepUpper", 50) \
            .SetDisplay("Candle Type", "Candle aggregation used for trend estimation", "General")
        self._sell_step_lower = self.Param("SellStepLower", 10) \
            .SetDisplay("Candle Type", "Candle aggregation used for trend estimation", "General")
        self._buy_take_profit_steps = self.Param("BuyTakeProfitSteps", 50) \
            .SetDisplay("Candle Type", "Candle aggregation used for trend estimation", "General")
        self._buy_stop_loss_steps = self.Param("BuyStopLossSteps", 30) \
            .SetDisplay("Candle Type", "Candle aggregation used for trend estimation", "General")
        self._sell_take_profit_steps = self.Param("SellTakeProfitSteps", 50) \
            .SetDisplay("Candle Type", "Candle aggregation used for trend estimation", "General")
        self._sell_stop_loss_steps = self.Param("SellStopLossSteps", 30) \
            .SetDisplay("Candle Type", "Candle aggregation used for trend estimation", "General")
        self._buy_volume = self.Param("BuyVolume", 1) \
            .SetDisplay("Candle Type", "Candle aggregation used for trend estimation", "General")
        self._sell_volume = self.Param("SellVolume", 1) \
            .SetDisplay("Candle Type", "Candle aggregation used for trend estimation", "General")

        self._entry_price = 0.0
        self._active_stop = None
        self._active_take = None
        self._active_direction = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(trend_me_leave_me_channel_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._active_stop = None
        self._active_take = None
        self._active_direction = 0.0

    def OnStarted(self, time):
        super(trend_me_leave_me_channel_strategy, self).OnStarted(time)

        self._regression = LinearRegression()
        self._regression.Length = self.trend_length

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._regression, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return trend_me_leave_me_channel_strategy()
