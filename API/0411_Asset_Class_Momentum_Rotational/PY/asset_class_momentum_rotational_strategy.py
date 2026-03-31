import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RateOfChange, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class asset_class_momentum_rotational_strategy(Strategy):
    """Momentum rotation strategy using ROC and SMA trend filter."""

    def __init__(self):
        super(asset_class_momentum_rotational_strategy, self).__init__()

        self._roc_length = self.Param("RocLength", 14) \
            .SetDisplay("ROC Length", "Rate of change lookback", "Parameters")
        self._sma_period = self.Param("SmaPeriod", 30) \
            .SetDisplay("SMA Period", "SMA period for trend filter", "Parameters")
        self._cooldown_bars = self.Param("CooldownBars", 20) \
            .SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Candle type used for momentum", "General")

        self._roc = None
        self._sma = None
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(asset_class_momentum_rotational_strategy, self).OnReseted()
        self._roc = None
        self._sma = None
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(asset_class_momentum_rotational_strategy, self).OnStarted2(time)
        self._roc = RateOfChange()
        self._roc.Length = int(self._roc_length.Value)
        self._sma = SimpleMovingAverage()
        self._sma.Length = int(self._sma_period.Value)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._roc, self._sma, self._process_candle).Start()

    def _process_candle(self, candle, roc_val, sma_val):
        if candle.State != CandleStates.Finished:
            return

        if not self._roc.IsFormed or not self._sma.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        close = float(candle.ClosePrice)
        rv = float(roc_val)
        sv = float(sma_val)
        cooldown = int(self._cooldown_bars.Value)

        if rv > 0 and close > sv and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif rv < 0 and close < sv and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = cooldown

    def CreateClone(self):
        return asset_class_momentum_rotational_strategy()
