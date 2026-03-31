import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, StochasticK
from StockSharp.Algo.Strategies import Strategy

class scalper_ema_simple_strategy(Strategy):
    def __init__(self):
        super(scalper_ema_simple_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._fast_ema_period = self.Param("FastEmaPeriod", 20)
        self._slow_ema_period = self.Param("SlowEmaPeriod", 50)
        self._stoch_oversold = self.Param("StochOversold", 10.0)
        self._stoch_overbought = self.Param("StochOverbought", 90.0)

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def FastEmaPeriod(self):
        return self._fast_ema_period.Value

    @FastEmaPeriod.setter
    def FastEmaPeriod(self, value):
        self._fast_ema_period.Value = value

    @property
    def SlowEmaPeriod(self):
        return self._slow_ema_period.Value

    @SlowEmaPeriod.setter
    def SlowEmaPeriod(self, value):
        self._slow_ema_period.Value = value

    @property
    def StochOversold(self):
        return self._stoch_oversold.Value

    @StochOversold.setter
    def StochOversold(self, value):
        self._stoch_oversold.Value = value

    @property
    def StochOverbought(self):
        return self._stoch_overbought.Value

    @StochOverbought.setter
    def StochOverbought(self, value):
        self._stoch_overbought.Value = value

    def OnReseted(self):
        super(scalper_ema_simple_strategy, self).OnReseted()

    def OnStarted2(self, time):
        super(scalper_ema_simple_strategy, self).OnStarted2(time)

        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self.FastEmaPeriod
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self.SlowEmaPeriod
        stoch_k = StochasticK()
        stoch_k.Length = 14

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast_ema, slow_ema, stoch_k, self._process_candle).Start()

    def _process_candle(self, candle, fast_value, slow_value, stoch_value):
        if candle.State != CandleStates.Finished:
            return

        fast_val = float(fast_value)
        slow_val = float(slow_value)
        stoch_val = float(stoch_value)

        # Buy: uptrend (fast > slow) + stochastic oversold
        if fast_val > slow_val and stoch_val < float(self.StochOversold) and self.Position <= 0:
            self.BuyMarket()
        # Sell: downtrend (fast < slow) + stochastic overbought
        elif fast_val < slow_val and stoch_val > float(self.StochOverbought) and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return scalper_ema_simple_strategy()
