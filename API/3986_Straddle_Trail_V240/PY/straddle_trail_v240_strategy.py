import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage as EMA
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Unit, UnitTypes


class straddle_trail_v240_strategy(Strategy):
    def __init__(self):
        super(straddle_trail_v240_strategy, self).__init__()

        self._channel_period = self.Param("ChannelPeriod", 20) \
            .SetDisplay("Channel Period", "Lookback period for breakout levels", "Parameters")
        self._stop_loss = self.Param("StopLoss", 500) \
            .SetDisplay("Channel Period", "Lookback period for breakout levels", "Parameters")
        self._take_profit = self.Param("TakeProfit", 500) \
            .SetDisplay("Channel Period", "Lookback period for breakout levels", "Parameters")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(2) \
            .SetDisplay("Channel Period", "Lookback period for breakout levels", "Parameters")

        self._highs = new()
        self._lows = new()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(straddle_trail_v240_strategy, self).OnReseted()
        self._highs = new()
        self._lows = new()

    def OnStarted(self, time):
        super(straddle_trail_v240_strategy, self).OnStarted(time)

        self._ema = EMA()
        self._ema.Length = 10

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._ema, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return straddle_trail_v240_strategy()
