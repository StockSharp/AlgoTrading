import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import HullMovingAverage
from StockSharp.Algo.Strategies import Strategy

class hull_ma_reversal_strategy(Strategy):
    """
    Hull MA Reversal strategy.
    Enters long when Hull MA changes direction from down to up.
    Enters short when Hull MA changes direction from up to down.
    Uses cooldown to control trade frequency.
    """

    def __init__(self):
        super(hull_ma_reversal_strategy, self).__init__()
        self._hma_period = self.Param("HmaPeriod", 9).SetDisplay("HMA Period", "Period for Hull Moving Average", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._prev_hma = 0.0
        self._prev_prev_hma = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(hull_ma_reversal_strategy, self).OnReseted()
        self._prev_hma = 0.0
        self._prev_prev_hma = 0.0
        self._cooldown = 0

    def OnStarted2(self, time):
        super(hull_ma_reversal_strategy, self).OnStarted2(time)

        self._prev_hma = 0.0
        self._prev_prev_hma = 0.0
        self._cooldown = 0

        hma = HullMovingAverage()
        hma.Length = self._hma_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(hma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, hma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, hma_val):
        if candle.State != CandleStates.Finished:
            return

        hv = float(hma_val)

        if self._prev_hma == 0:
            self._prev_hma = hv
            return

        if self._prev_prev_hma == 0:
            self._prev_prev_hma = self._prev_hma
            self._prev_hma = hv
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_prev_hma = self._prev_hma
            self._prev_hma = hv
            return

        cd = self._cooldown_bars.Value

        # Direction change detection
        dir_changed_up = self._prev_hma < self._prev_prev_hma and hv > self._prev_hma
        dir_changed_down = self._prev_hma > self._prev_prev_hma and hv < self._prev_hma

        if self.Position == 0 and dir_changed_up:
            self.BuyMarket()
            self._cooldown = cd
        elif self.Position == 0 and dir_changed_down:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position > 0 and dir_changed_down:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and dir_changed_up:
            self.BuyMarket()
            self._cooldown = cd

        self._prev_prev_hma = self._prev_hma
        self._prev_hma = hv

    def CreateClone(self):
        return hull_ma_reversal_strategy()
