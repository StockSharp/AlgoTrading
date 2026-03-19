import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy

class inverse_reaction_strategy(Strategy):
    """
    Reacts to large single-bar moves expecting mean reversion.
    Buys after large bearish bars, sells after large bullish bars.
    Uses StartProtection for SL/TP.
    """

    def __init__(self):
        super(inverse_reaction_strategy, self).__init__()
        self._stop_loss_points = self.Param("StopLossPoints", 1000.0) \
            .SetDisplay("Stop Loss", "Stop-loss distance in points", "Risk")
        self._take_profit_points = self.Param("TakeProfitPoints", 250.0) \
            .SetDisplay("Take Profit", "Take-profit distance in points", "Risk")
        self._coefficient = self.Param("Coefficient", 1.618) \
            .SetDisplay("Coefficient", "Confidence coefficient", "Signal")
        self._ma_period = self.Param("MaPeriod", 3) \
            .SetDisplay("MA Period", "Moving average length", "Signal")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Primary timeframe", "General")

        self._abs_changes = []

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(inverse_reaction_strategy, self).OnReseted()
        self._abs_changes = []

    def OnStarted(self, time):
        super(inverse_reaction_strategy, self).OnStarted(time)

        step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None:
            step = float(self.Security.PriceStep)
        if step <= 0:
            step = 1.0

        sl = self._stop_loss_points.Value
        tp = self._take_profit_points.Value
        sl_unit = Unit(sl * step, UnitTypes.Absolute) if sl > 0 else None
        tp_unit = Unit(tp * step, UnitTypes.Absolute) if tp > 0 else None
        if sl_unit is not None or tp_unit is not None:
            self.StartProtection(tp_unit, sl_unit)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        open_price = float(candle.OpenPrice)
        change = close - open_price
        abs_change = abs(change)

        period = self._ma_period.Value
        self._abs_changes.append(abs_change)
        if len(self._abs_changes) > period:
            self._abs_changes.pop(0)

        if len(self._abs_changes) < period:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        avg = sum(self._abs_changes) / len(self._abs_changes)
        threshold = avg * self._coefficient.Value

        if self.Position != 0:
            return

        if abs_change > threshold and abs_change > 0:
            if change < 0:
                self.BuyMarket()
            else:
                self.SellMarket()

    def CreateClone(self):
        return inverse_reaction_strategy()
