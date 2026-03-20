import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex, ExponentialMovingAverage, MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy


class smc_trader_camel_cci_macd1_strategy(Strategy):
    def __init__(self):
        super(smc_trader_camel_cci_macd1_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._ema_length = self.Param("EmaLength", 34) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._macd_fast_period = self.Param("MacdFastPeriod", 12) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._macd_slow_period = self.Param("MacdSlowPeriod", 26) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._macd_signal_period = self.Param("MacdSignalPeriod", 9) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._cci_period = self.Param("CciPeriod", 20) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._prev_macd_main = None
        self._prev_macd_signal = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(smc_trader_camel_cci_macd1_strategy, self).OnReseted()
        self._prev_macd_main = None
        self._prev_macd_signal = None

    def OnStarted(self, time):
        super(smc_trader_camel_cci_macd1_strategy, self).OnStarted(time)

        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.ema_length
        self._cci = CommodityChannelIndex()
        self._cci.Length = self.cci_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, self._cci, self._ema, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return smc_trader_camel_cci_macd1_strategy()
