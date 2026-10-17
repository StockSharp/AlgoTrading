import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")
from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, OrderStates
from StockSharp.Algo.Strategies import Strategy


class color_zerolag_momentum_osma_strategy(Strategy):
    """Weighted five momentum calculations with the documented t-2/t-3 OSMA turning signal."""

    def __init__(self):
        super(color_zerolag_momentum_osma_strategy, self).__init__()
        self._periods = []
        self._factors = []
        for index, period in enumerate((5, 8, 13, 21, 34), start=1):
            self._periods.append(self.Param("MomentumPeriod" + str(index), period).SetGreaterThanZero()
                                 .SetDisplay("Momentum Period " + str(index), "Price difference lookback", "Indicator"))
            self._factors.append(self.Param("Factor" + str(index), 1.0)
                                 .SetDisplay("Factor " + str(index), "Component weight", "Indicator"))
        self._smoothing1 = self.Param("Smoothing1", 9).SetGreaterThanZero() \
            .SetDisplay("Smoothing 1", "Slow trend EMA", "Indicator")
        self._smoothing2 = self.Param("Smoothing2", 5).SetGreaterThanZero() \
            .SetDisplay("Smoothing 2", "OSMA EMA", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Signal candles", "General")
        self._buy_open = self.Param("BuyOpen", True).SetDisplay("Buy Open", "Enable long entries", "Trading")
        self._sell_open = self.Param("SellOpen", True).SetDisplay("Sell Open", "Enable short entries", "Trading")
        self._buy_close = self.Param("BuyClose", True).SetDisplay("Buy Close", "Enable long exits", "Trading")
        self._sell_close = self.Param("SellClose", True).SetDisplay("Sell Close", "Enable short exits", "Trading")
        self._reset_history()

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def _reset_history(self):
        self._closes = []
        self._slow = self._smoothed = self._last = self._prev2 = self._prev3 = None
        self._pending = None

    def OnReseted(self):
        super(color_zerolag_momentum_osma_strategy, self).OnReseted()
        self._reset_history()

    def OnStarted2(self, time):
        super(color_zerolag_momentum_osma_strategy, self).OnStarted2(time)
        self._reset_history()
        self.SubscribeCandles(self.CandleType).Bind(self.ProcessCandle).Start()

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        max_period = max(int(p.Value) for p in self._periods)
        close = float(candle.ClosePrice)
        self._closes.append(close)
        if len(self._closes) > max_period + 1:
            self._closes.pop(0)
        if len(self._closes) < max_period + 1:
            return
        fast = 0.0
        for param, factor in zip(self._periods, self._factors):
            period = int(param.Value)
            fast += (close - self._closes[-period-1]) * float(factor.Value)
        slow = fast if self._slow is None else self._slow + 2.0 / (int(self._smoothing1.Value) + 1.0) * (fast - self._slow)
        raw_osma = fast - slow
        osma = raw_osma if self._smoothed is None else self._smoothed + 2.0 / (int(self._smoothing2.Value) + 1.0) * (raw_osma - self._smoothed)

        buy = self._prev3 is not None and self._prev2 < self._prev3 and osma > self._prev2
        sell = self._prev3 is not None and self._prev2 > self._prev3 and osma < self._prev2
        self._prev3, self._prev2, self._last = self._prev2, self._last, osma
        self._smoothed, self._slow = osma, slow
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        if self._pending is not None and self._pending.State not in (OrderStates.Done, OrderStates.Failed):
            return
        if buy:
            if self.Position < 0 and self._sell_close.Value:
                self._pending = self.BuyMarket(-self.Position + (self.Volume if self._buy_open.Value else 0))
            elif self.Position == 0 and self._buy_open.Value:
                self._pending = self.BuyMarket(self.Volume)
        elif sell:
            if self.Position > 0 and self._buy_close.Value:
                self._pending = self.SellMarket(self.Position + (self.Volume if self._sell_open.Value else 0))
            elif self.Position == 0 and self._sell_open.Value:
                self._pending = self.SellMarket(self.Volume)

    def CreateClone(self):
        return color_zerolag_momentum_osma_strategy()
