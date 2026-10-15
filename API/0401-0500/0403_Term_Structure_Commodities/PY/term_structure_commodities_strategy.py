import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class term_structure_commodities_strategy(Strategy):
    """Term structure momentum strategy using dual moving average crossover."""

    def __init__(self):
        super(term_structure_commodities_strategy, self).__init__()

        self._fast_period = self.Param("FastPeriod", 10) \
            .SetDisplay("Fast Period", "Fast moving average period", "Parameters")

        self._slow_period = self.Param("SlowPeriod", 30) \
            .SetDisplay("Slow Period", "Slow moving average period", "Parameters")

        self._cooldown_bars = self.Param("CooldownBars", 30) \
            .SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._fast_ema = None
        self._slow_ema = None
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(term_structure_commodities_strategy, self).OnReseted()
        self._fast_ema = None
        self._slow_ema = None
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(term_structure_commodities_strategy, self).OnStarted2(time)

        self._fast_ema = ExponentialMovingAverage()
        self._fast_ema.Length = int(self._fast_period.Value)

        self._slow_ema = ExponentialMovingAverage()
        self._slow_ema.Length = int(self._slow_period.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription \
            .Bind(self._fast_ema, self._slow_ema, self._process_candle) \
            .Start()

    def _process_candle(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return

        if not self._fast_ema.IsFormed or not self._slow_ema.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        fv = float(fast_val)
        sv = float(slow_val)

        if fv > sv and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = int(self._cooldown_bars.Value)
        elif fv < sv and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = int(self._cooldown_bars.Value)

    def CreateClone(self):
        return term_structure_commodities_strategy()
