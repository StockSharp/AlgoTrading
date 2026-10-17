import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, OrderStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class _RsiState:
    """Wilder RSI, requiring period changes before the first usable value."""

    def __init__(self, length):
        self.length = length
        self.samples = 0
        self.previous = None
        self.sum_gain = self.sum_loss = self.avg_gain = self.avg_loss = 0.0

    def add(self, close):
        if self.previous is None:
            self.previous = close
            self.samples = 1
            return None
        delta = close - self.previous
        self.previous = close
        gain, loss = max(delta, 0.0), max(-delta, 0.0)
        if self.samples <= self.length:
            self.sum_gain += gain
            self.sum_loss += loss
            self.samples += 1
            if self.samples <= self.length:
                return None
            self.avg_gain = self.sum_gain / self.length
            self.avg_loss = self.sum_loss / self.length
        else:
            self.avg_gain = (self.avg_gain * (self.length - 1) + gain) / self.length
            self.avg_loss = (self.avg_loss * (self.length - 1) + loss) / self.length
        if self.avg_loss == 0:
            return 50.0 if self.avg_gain == 0 else 100.0
        return 100.0 - 100.0 / (1.0 + self.avg_gain / self.avg_loss)


class color_zerolag_rsi_osma_strategy(Strategy):
    """Weighted 5-RSI composite, two EMA smoothing stages, OSMA slope reversals."""

    def __init__(self):
        super(color_zerolag_rsi_osma_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Signal candle timeframe", "General")
        self._smoothing1 = self.Param("Smoothing1", 9).SetGreaterThanZero() \
            .SetDisplay("Smoothing 1", "Slow-trend EMA period", "Indicator")
        self._smoothing2 = self.Param("Smoothing2", 5).SetGreaterThanZero() \
            .SetDisplay("Smoothing 2", "OSMA-line EMA period", "Indicator")
        self._periods = []
        self._factors = []
        for index, period in enumerate((8, 21, 34, 55, 89), start=1):
            self._periods.append(self.Param("RsiPeriod" + str(index), period).SetGreaterThanZero()
                                 .SetDisplay("RSI Period " + str(index), "Wilder RSI length", "Indicator"))
            self._factors.append(self.Param("Factor" + str(index), 1.0)
                                 .SetDisplay("RSI Factor " + str(index), "Component weight", "Indicator"))
        self._allow_buy = self.Param("AllowBuy", True).SetDisplay("Allow Buy", "Allow long entries", "Trading")
        self._allow_sell = self.Param("AllowSell", True).SetDisplay("Allow Sell", "Allow short entries", "Trading")
        self._close_long = self.Param("CloseLong", True).SetDisplay("Close Long", "Allow long exits", "Trading")
        self._close_short = self.Param("CloseShort", True).SetDisplay("Close Short", "Allow short exits", "Trading")
        self._stop_loss = self.Param("StopLoss", 0.0).SetNotNegative() \
            .SetDisplay("Stop Loss", "Optional price-unit stop", "Risk")
        self._take_profit = self.Param("TakeProfit", 0.0).SetNotNegative() \
            .SetDisplay("Take Profit", "Optional price-unit target", "Risk")
        self._reset_history()

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def _reset_history(self):
        self._rsi = [_RsiState(int(p.Value)) for p in self._periods]
        self._slow = self._osma = self._previous = self._before_previous = None
        self._pending = None

    def OnReseted(self):
        super(color_zerolag_rsi_osma_strategy, self).OnReseted()
        self._reset_history()

    def OnStarted2(self, time):
        super(color_zerolag_rsi_osma_strategy, self).OnStarted2(time)
        self._reset_history()
        self.StartProtection(takeProfit=Unit(self._take_profit.Value, UnitTypes.Absolute),
                             stopLoss=Unit(self._stop_loss.Value, UnitTypes.Absolute))
        self.SubscribeCandles(self.CandleType).Bind(self.ProcessCandle).Start()

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        vals = [indicator.add(float(candle.ClosePrice)) for indicator in self._rsi]
        if any(value is None for value in vals):
            return
        fast = sum(value * float(param.Value) for value, param in zip(vals, self._factors))
        slow = fast if self._slow is None else self._slow + 2.0 / (int(self._smoothing1.Value) + 1.0) * (fast - self._slow)
        raw = fast - slow
        osc = raw if self._osma is None else self._osma + 2.0 / (int(self._smoothing2.Value) + 1.0) * (raw - self._osma)
        turn_up = self._before_previous is not None and self._previous < self._before_previous and osc > self._previous
        turn_down = self._before_previous is not None and self._previous > self._before_previous and osc < self._previous
        self._before_previous, self._previous, self._osma, self._slow = self._previous, osc, osc, slow

        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        if self._pending is not None and self._pending.State not in (OrderStates.Done, OrderStates.Failed):
            return
        if turn_up:
            if self.Position < 0 and self._close_short.Value:
                self._pending = self.BuyMarket(-self.Position + (self.Volume if self._allow_buy.Value else 0))
            elif self.Position == 0 and self._allow_buy.Value:
                self._pending = self.BuyMarket(self.Volume)
        elif turn_down:
            if self.Position > 0 and self._close_long.Value:
                self._pending = self.SellMarket(self.Position + (self.Volume if self._allow_sell.Value else 0))
            elif self.Position == 0 and self._allow_sell.Value:
                self._pending = self.SellMarket(self.Volume)

    def CreateClone(self):
        return color_zerolag_rsi_osma_strategy()
