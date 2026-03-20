import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import DecimalIndicatorValue, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class blau_ergodic_strategy(Strategy):
    def __init__(self):
        super(blau_ergodic_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(8) \
            .SetDisplay("Candle Type", "Timeframe for calculations", "General")
        self._mode = self.Param("Mode", BlauErgodicModes.Twist) \
            .SetDisplay("Candle Type", "Timeframe for calculations", "General")
        self._momentum_length = self.Param("MomentumLength", 2) \
            .SetDisplay("Candle Type", "Timeframe for calculations", "General")
        self._first_smoothing_length = self.Param("FirstSmoothingLength", 20) \
            .SetDisplay("Candle Type", "Timeframe for calculations", "General")
        self._second_smoothing_length = self.Param("SecondSmoothingLength", 5) \
            .SetDisplay("Candle Type", "Timeframe for calculations", "General")
        self._third_smoothing_length = self.Param("ThirdSmoothingLength", 3) \
            .SetDisplay("Candle Type", "Timeframe for calculations", "General")
        self._signal_smoothing_length = self.Param("SignalSmoothingLength", 3) \
            .SetDisplay("Candle Type", "Timeframe for calculations", "General")
        self._signal_bar = self.Param("SignalBar", 1) \
            .SetDisplay("Candle Type", "Timeframe for calculations", "General")
        self._applied_price = self.Param("AppliedPrices", AppliedPrices.Close) \
            .SetDisplay("Candle Type", "Timeframe for calculations", "General")
        self._allow_buy_entry = self.Param("AllowBuyEntry", True) \
            .SetDisplay("Candle Type", "Timeframe for calculations", "General")
        self._allow_sell_entry = self.Param("AllowSellEntry", True) \
            .SetDisplay("Candle Type", "Timeframe for calculations", "General")
        self._allow_buy_exit = self.Param("AllowBuyExit", True) \
            .SetDisplay("Candle Type", "Timeframe for calculations", "General")
        self._allow_sell_exit = self.Param("AllowSellExit", True) \
            .SetDisplay("Candle Type", "Timeframe for calculations", "General")
        self._stop_loss_points = self.Param("StopLossPoints", 1000) \
            .SetDisplay("Candle Type", "Timeframe for calculations", "General")
        self._take_profit_points = self.Param("TakeProfitPoints", 2000) \
            .SetDisplay("Candle Type", "Timeframe for calculations", "General")

        self._mom_ema1 = null!
        self._mom_ema2 = null!
        self._mom_ema3 = null!
        self._abs_mom_ema1 = null!
        self._abs_mom_ema2 = null!
        self._abs_mom_ema3 = null!
        self._signal = null!
        self._price_history = new()
        self._main_history = new()
        self._signal_history = new()
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(blau_ergodic_strategy, self).OnReseted()
        self._mom_ema1 = null!
        self._mom_ema2 = null!
        self._mom_ema3 = null!
        self._abs_mom_ema1 = null!
        self._abs_mom_ema2 = null!
        self._abs_mom_ema3 = null!
        self._signal = null!
        self._price_history = new()
        self._main_history = new()
        self._signal_history = new()
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(blau_ergodic_strategy, self).OnStarted(time)

        self.__mom_ema1 = ExponentialMovingAverage()
        self.__mom_ema1.Length = self.first_smoothing_length
        self.__mom_ema2 = ExponentialMovingAverage()
        self.__mom_ema2.Length = self.second_smoothing_length
        self.__mom_ema3 = ExponentialMovingAverage()
        self.__mom_ema3.Length = self.third_smoothing_length
        self.__abs_mom_ema1 = ExponentialMovingAverage()
        self.__abs_mom_ema1.Length = self.first_smoothing_length
        self.__abs_mom_ema2 = ExponentialMovingAverage()
        self.__abs_mom_ema2.Length = self.second_smoothing_length
        self.__abs_mom_ema3 = ExponentialMovingAverage()
        self.__abs_mom_ema3.Length = self.third_smoothing_length
        self.__signal = ExponentialMovingAverage()
        self.__signal.Length = self.signal_smoothing_length

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
        return blau_ergodic_strategy()
