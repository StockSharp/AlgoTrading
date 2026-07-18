import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy

class frank_ud_strategy(Strategy):
    """
    Frank Ud: Grid/hedging strategy with direction based on candle body size.
    Opens position when body exceeds step distance, uses StartProtection for SL/TP.
    """

    def __init__(self):
        super(frank_ud_strategy, self).__init__()
        self._take_profit = self.Param("TakeProfit", 5000.0) \
            .SetDisplay("Take Profit", "Take profit distance from avg price", "Risk")
        self._stop_loss = self.Param("StopLoss", 5000.0) \
            .SetDisplay("Stop Loss", "Stop loss distance from avg price", "Risk")
        self._step_distance = self.Param("StepDistance", 300.0) \
            .SetDisplay("Step Distance", "Price distance for adding entries", "Grid")
        self._max_entries = self.Param("MaxEntries", 1) \
            .SetDisplay("Max Entries", "Maximum martingale entries", "Grid")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Candle type for calculations", "General")

        self._last_signal = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(frank_ud_strategy, self).OnReseted()
        self._last_signal = 0

    def OnStarted2(self, time):
        super(frank_ud_strategy, self).OnStarted2(time)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        tp = self._take_profit.Value
        sl = self._stop_loss.Value
        self.StartProtection(
            Unit(tp, UnitTypes.Absolute),
            Unit(sl, UnitTypes.Absolute))

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        body_size = abs(float(candle.ClosePrice) - float(candle.OpenPrice))
        if body_size < self._step_distance.Value:
            return

        direction = 1 if float(candle.ClosePrice) > float(candle.OpenPrice) else -1
        if direction == self._last_signal:
            return

        if direction > 0 and self.Position <= 0:
            self.BuyMarket()
            self._last_signal = 1
        elif direction < 0 and self.Position >= 0:
            self.SellMarket()
            self._last_signal = -1

    def CreateClone(self):
        return frank_ud_strategy()
