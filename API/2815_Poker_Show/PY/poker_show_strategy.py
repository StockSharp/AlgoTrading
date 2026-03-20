import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import DecimalIndicatorValue, ExponentialMovingAverage, SimpleMovingAverage, SmoothedMovingAverage, WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class poker_show_strategy(Strategy):
    def __init__(self):
        super(poker_show_strategy, self).__init__()

        self._combination = self.Param("Combination", PokerCombinations.Couple) \
            .SetDisplay("Poker Combination", "Probability gate for opening trades", "Signals")
        self._trade_volume = self.Param("TradeVolume", 0.1) \
            .SetDisplay("Poker Combination", "Probability gate for opening trades", "Signals")
        self._stop_loss_points = self.Param("StopLossPoints", 50) \
            .SetDisplay("Poker Combination", "Probability gate for opening trades", "Signals")
        self._take_profit_points = self.Param("TakeProfitPoints", 150) \
            .SetDisplay("Poker Combination", "Probability gate for opening trades", "Signals")
        self._enable_buy = self.Param("EnableBuy", True) \
            .SetDisplay("Poker Combination", "Probability gate for opening trades", "Signals")
        self._enable_sell = self.Param("EnableSell", True) \
            .SetDisplay("Poker Combination", "Probability gate for opening trades", "Signals")
        self._distance_points = self.Param("DistancePoints", 50) \
            .SetDisplay("Poker Combination", "Probability gate for opening trades", "Signals")
        self._ma_period = self.Param("MaPeriod", 24) \
            .SetDisplay("Poker Combination", "Probability gate for opening trades", "Signals")
        self._ma_shift = self.Param("MaShift", 0) \
            .SetDisplay("Poker Combination", "Probability gate for opening trades", "Signals")
        self._ma_method = self.Param("MaMethod", MovingAverageMethods.Ema) \
            .SetDisplay("Poker Combination", "Probability gate for opening trades", "Signals")
        self._applied_price = self.Param("AppliedPrice", AppliedPriceses.Close) \
            .SetDisplay("Poker Combination", "Probability gate for opening trades", "Signals")
        self._reverse_signal = self.Param("ReverseSignal", False) \
            .SetDisplay("Poker Combination", "Probability gate for opening trades", "Signals")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1) \
            .SetDisplay("Poker Combination", "Probability gate for opening trades", "Signals")

        self._ma = None
        self._ma_history = []
        self._stop_loss_price = None
        self._take_profit_price = None
        self._price_step = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(poker_show_strategy, self).OnReseted()
        self._ma = None
        self._ma_history = []
        self._stop_loss_price = None
        self._take_profit_price = None
        self._price_step = 0.0

    def OnStarted(self, time):
        super(poker_show_strategy, self).OnStarted(time)


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
        return poker_show_strategy()
