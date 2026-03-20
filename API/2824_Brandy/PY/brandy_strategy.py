import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import DecimalIndicatorValue, ExponentialMovingAverage, SimpleMovingAverage, SmoothedMovingAverage, WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class brandy_strategy(Strategy):
    def __init__(self):
        super(brandy_strategy, self).__init__()

        self._trade_volume = self.Param("TradeVolume", 0.1) \
            .SetDisplay("Trade Volume", "Order size in lots", "General")
        self._stop_loss_pips = self.Param("StopLossPips", 50) \
            .SetDisplay("Trade Volume", "Order size in lots", "General")
        self._take_profit_pips = self.Param("TakeProfitPips", 150) \
            .SetDisplay("Trade Volume", "Order size in lots", "General")
        self._trailing_stop_pips = self.Param("TrailingStopPips", 5) \
            .SetDisplay("Trade Volume", "Order size in lots", "General")
        self._trailing_step_pips = self.Param("TrailingStepPips", 5) \
            .SetDisplay("Trade Volume", "Order size in lots", "General")
        self._ma_close_period = self.Param("MaClosePeriod", 20) \
            .SetDisplay("Trade Volume", "Order size in lots", "General")
        self._ma_close_shift = self.Param("MaCloseShift", 0) \
            .SetDisplay("Trade Volume", "Order size in lots", "General")
        self._ma_close_method = self.Param("MaCloseMethod", MovingAverageMethods.Ema) \
            .SetDisplay("Trade Volume", "Order size in lots", "General")
        self._ma_close_applied_price = self.Param("MaCloseAppliedPrice", AppliedPriceTypes.Close) \
            .SetDisplay("Trade Volume", "Order size in lots", "General")
        self._ma_close_signal_bar = self.Param("MaCloseSignalBar", 0) \
            .SetDisplay("Trade Volume", "Order size in lots", "General")
        self._ma_open_period = self.Param("MaOpenPeriod", 70) \
            .SetDisplay("Trade Volume", "Order size in lots", "General")
        self._ma_open_shift = self.Param("MaOpenShift", 0) \
            .SetDisplay("Trade Volume", "Order size in lots", "General")
        self._ma_open_method = self.Param("MaOpenMethod", MovingAverageMethods.Ema) \
            .SetDisplay("Trade Volume", "Order size in lots", "General")
        self._ma_open_applied_price = self.Param("MaOpenAppliedPrice", AppliedPriceTypes.Close) \
            .SetDisplay("Trade Volume", "Order size in lots", "General")
        self._ma_open_signal_bar = self.Param("MaOpenSignalBar", 0) \
            .SetDisplay("Trade Volume", "Order size in lots", "General")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Trade Volume", "Order size in lots", "General")

        self._ma_open_indicator = None
        self._ma_close_indicator = None
        self._pip_size = 0.0
        self._ma_open_values = []
        self._ma_close_values = []
        self._max_open_queue_size = 0.0
        self._max_close_queue_size = 0.0
        self._entry_price = None
        self._stop_price = None
        self._take_price = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(brandy_strategy, self).OnReseted()
        self._ma_open_indicator = None
        self._ma_close_indicator = None
        self._pip_size = 0.0
        self._ma_open_values = []
        self._ma_close_values = []
        self._max_open_queue_size = 0.0
        self._max_close_queue_size = 0.0
        self._entry_price = None
        self._stop_price = None
        self._take_price = None

    def OnStarted(self, time):
        super(brandy_strategy, self).OnStarted(time)


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
        return brandy_strategy()
