import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class bollinger_rsi_ma_strategy(Strategy):
    def __init__(self):
        super(bollinger_rsi_ma_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(30) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._bb_period = self.Param("BbPeriod", 20) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._band_percent = self.Param("BandPercent", 0.01) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._signal_cooldown_candles = self.Param("SignalCooldownCandles", 6) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._candles_since_trade = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(bollinger_rsi_ma_strategy, self).OnReseted()
        self._candles_since_trade = 0.0

    def OnStarted(self, time):
        super(bollinger_rsi_ma_strategy, self).OnStarted(time)

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.rsi_period
        self._ma = SimpleMovingAverage()
        self._ma.Length = self.bb_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._rsi, self._ma, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return bollinger_rsi_ma_strategy()
