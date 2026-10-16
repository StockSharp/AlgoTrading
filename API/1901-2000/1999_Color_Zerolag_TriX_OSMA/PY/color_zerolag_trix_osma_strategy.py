import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, OrderStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class _TrixState:
    def __init__(self, period):
        self.period = period
        self.alpha = 2.0 / (period + 1.0)
        self.first = self.second = self.third = self.previous = None
        self.count = 0

    def add(self, close):
        if self.count == 0:
            self.first = self.second = self.third = self.previous = close
            self.count = 1
            return None
        self.first += self.alpha * (close - self.first)
        self.second += self.alpha * (self.first - self.second)
        self.third += self.alpha * (self.second - self.third)
        self.count += 1
        trix = 100.0 * (self.third - self.previous) / self.previous if self.previous != 0 else 0.0
        self.previous = self.third
        return trix if self.count >= 3 * self.period + 1 else None


class color_zerolag_trix_osma_strategy(Strategy):
    """Weighted five-component TRIX OSMA, two smoothing stages and slope reversals."""

    def __init__(self):
        super(color_zerolag_trix_osma_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Signal candle timeframe", "General")
        self._smoothing1 = self.Param("Smoothing1", 9).SetGreaterThanZero() \
            .SetDisplay("Smoothing 1", "Slow-trend EMA period", "Indicator")
        self._smoothing2 = self.Param("Smoothing2", 5).SetGreaterThanZero() \
            .SetDisplay("Smoothing 2", "OSMA-line EMA period", "Indicator")
        self._stop_loss = self.Param("StopLoss", 0.0).SetNotNegative() \
            .SetDisplay("Stop Loss", "Optional absolute stop distance", "Risk")
        self._take_profit = self.Param("TakeProfit", 0.0).SetNotNegative() \
            .SetDisplay("Take Profit", "Optional absolute target distance", "Risk")
        self._periods = []
        self._factors = []
        for index, period in enumerate((5, 8, 13, 21, 34), start=1):
            self._periods.append(self.Param("Period" + str(index), period).SetGreaterThanZero()
                                 .SetDisplay("TRIX Period " + str(index), "Triple EMA period", "Indicator"))
            self._factors.append(self.Param("Factor" + str(index), 1.0)
                                 .SetDisplay("TRIX Factor " + str(index), "Component weight", "Indicator"))
        self._buy_open = self.Param("BuyOpen", True).SetDisplay("Buy Open", "Enable long entries", "Trading")
        self._buy_close = self.Param("BuyClose", True).SetDisplay("Buy Close", "Enable long exits", "Trading")
        self._sell_open = self.Param("SellOpen", True).SetDisplay("Sell Open", "Enable short entries", "Trading")
        self._sell_close = self.Param("SellClose", True).SetDisplay("Sell Close", "Enable short exits", "Trading")
        self._reset_history()

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def _reset_history(self):
        self._trix = [_TrixState(int(p.Value)) for p in self._periods]
        self._slow = self._smoothed_osma = self._previous_osma = self._before_previous_osma = None
        self._pending = None

    def OnReseted(self):
        super(color_zerolag_trix_osma_strategy, self).OnReseted()
        self._reset_history()

    def OnStarted2(self, time):
        super(color_zerolag_trix_osma_strategy, self).OnStarted2(time)
        self._reset_history()
        self.StartProtection(takeProfit=Unit(self._take_profit.Value, UnitTypes.Absolute),
                             stopLoss=Unit(self._stop_loss.Value, UnitTypes.Absolute))
        self.SubscribeCandles(self.CandleType).Bind(self.ProcessCandle).Start()

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        values = [trix.add(float(candle.ClosePrice)) for trix in self._trix]
        if any(x is None for x in values):
            return
        fast = sum(value * float(param.Value) for value, param in zip(values, self._factors))
        slow = fast if self._slow is None else self._slow + 2.0 / (int(self._smoothing1.Value) + 1.0) * (fast - self._slow)
        osma = fast - slow
        value = osma if self._smoothed_osma is None else self._smoothed_osma + 2.0 / (int(self._smoothing2.Value) + 1.0) * (osma - self._smoothed_osma)
        buy = self._before_previous_osma is not None and self._previous_osma < self._before_previous_osma and value > self._previous_osma
        sell = self._before_previous_osma is not None and self._previous_osma > self._before_previous_osma and value < self._previous_osma
        self._before_previous_osma, self._previous_osma, self._smoothed_osma, self._slow = self._previous_osma, value, value, slow
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
        return color_zerolag_trix_osma_strategy()
