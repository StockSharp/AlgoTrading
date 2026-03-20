import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WilliamsR
from StockSharp.Algo.Strategies import Strategy


class ifs_fractals_strategy(Strategy):
    def __init__(self):
        super(ifs_fractals_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(60) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._wpr_period = self.Param("WprPeriod", 14) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._oversold = self.Param("Oversold", -85) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._overbought = self.Param("Overbought", -15) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._signal_cooldown_candles = self.Param("SignalCooldownCandles", 4) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._prev_wpr = 0.0
        self._candles_since_trade = 0.0
        self._has_prev = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ifs_fractals_strategy, self).OnReseted()
        self._prev_wpr = 0.0
        self._candles_since_trade = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(ifs_fractals_strategy, self).OnStarted(time)

        self._wpr = WilliamsR()
        self._wpr.Length = self.wpr_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._wpr, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return ifs_fractals_strategy()
