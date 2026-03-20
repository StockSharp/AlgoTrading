import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, Ichimoku, MovingAverageConvergenceDivergenceSignal, SmoothedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class mc_valute_cloud_strategy(Strategy):
    def __init__(self):
        super(mc_valute_cloud_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Candle Type", "Primary timeframe used for signals", "General")
        self._filter_ma_length = self.Param("FilterMaLength", 3) \
            .SetDisplay("Candle Type", "Primary timeframe used for signals", "General")
        self._blue_ma_length = self.Param("BlueMaLength", 13) \
            .SetDisplay("Candle Type", "Primary timeframe used for signals", "General")
        self._lime_ma_length = self.Param("LimeMaLength", 5) \
            .SetDisplay("Candle Type", "Primary timeframe used for signals", "General")
        self._macd_fast_length = self.Param("MacdFastLength", 12) \
            .SetDisplay("Candle Type", "Primary timeframe used for signals", "General")
        self._macd_slow_length = self.Param("MacdSlowLength", 26) \
            .SetDisplay("Candle Type", "Primary timeframe used for signals", "General")
        self._macd_signal_length = self.Param("MacdSignalLength", 9) \
            .SetDisplay("Candle Type", "Primary timeframe used for signals", "General")
        self._tenkan_length = self.Param("TenkanLength", 12) \
            .SetDisplay("Candle Type", "Primary timeframe used for signals", "General")
        self._kijun_length = self.Param("KijunLength", 20) \
            .SetDisplay("Candle Type", "Primary timeframe used for signals", "General")
        self._senkou_length = self.Param("SenkouLength", 40) \
            .SetDisplay("Candle Type", "Primary timeframe used for signals", "General")
        self._take_profit = self.Param("TakeProfit", 30) \
            .SetDisplay("Candle Type", "Primary timeframe used for signals", "General")
        self._stop_loss = self.Param("StopLoss", 350) \
            .SetDisplay("Candle Type", "Primary timeframe used for signals", "General")

        self._filter_ma = None
        self._blue_ma = None
        self._lime_ma = None
        self._macd = None
        self._ichimoku = None
        self._filter_value = None
        self._blue_value = None
        self._lime_value = None
        self._senkou_a_value = None
        self._senkou_b_value = None
        self._macd_main_value = None
        self._macd_signal_value = None
        self._last_processed_time = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(mc_valute_cloud_strategy, self).OnReseted()
        self._filter_ma = None
        self._blue_ma = None
        self._lime_ma = None
        self._macd = None
        self._ichimoku = None
        self._filter_value = None
        self._blue_value = None
        self._lime_value = None
        self._senkou_a_value = None
        self._senkou_b_value = None
        self._macd_main_value = None
        self._macd_signal_value = None
        self._last_processed_time = None

    def OnStarted(self, time):
        super(mc_valute_cloud_strategy, self).OnStarted(time)
        self.StartProtection(None, None)

        self.__filter_ma = ExponentialMovingAverage()
        self.__filter_ma.Length = self.filter_ma_length
        self.__blue_ma = SmoothedMovingAverage()
        self.__blue_ma.Length = self.blue_ma_length
        self.__lime_ma = SmoothedMovingAverage()
        self.__lime_ma.Length = self.lime_ma_length

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(_ichimoku, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return mc_valute_cloud_strategy()
