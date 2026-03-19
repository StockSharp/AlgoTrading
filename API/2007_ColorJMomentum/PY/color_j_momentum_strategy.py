import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import Momentum
from StockSharp.Algo.Strategies import Strategy

class color_j_momentum_strategy(Strategy):
    """
    Strategy based on smoothed momentum direction changes.
    Opens long when momentum turns up, short when momentum turns down.
    """

    def __init__(self):
        super(color_j_momentum_strategy, self).__init__()
        self._momentum_length = self.Param("MomentumLength", 8) \
            .SetDisplay("Momentum Length", "Period for momentum", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for candles", "Parameters")
        self._stop_loss_percent = self.Param("StopLossPercent", 1.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk management")
        self._take_profit_percent = self.Param("TakeProfitPercent", 2.0) \
            .SetDisplay("Take Profit %", "Take profit percentage", "Risk management")
        self._enable_long = self.Param("EnableLong", True) \
            .SetDisplay("Enable Long", "Allow long entries", "General")
        self._enable_short = self.Param("EnableShort", True) \
            .SetDisplay("Enable Short", "Allow short entries", "General")

        self._prev_mom = 0.0
        self._prev_prev_mom = 0.0
        self._count = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(color_j_momentum_strategy, self).OnReseted()
        self._prev_mom = 0.0
        self._prev_prev_mom = 0.0
        self._count = 0

    def OnStarted(self, time):
        super(color_j_momentum_strategy, self).OnStarted(time)

        momentum = Momentum()
        momentum.Length = self._momentum_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(momentum, self.on_process).Start()

        self.StartProtection(
            takeProfit=Unit(self._take_profit_percent.Value, UnitTypes.Percent),
            stopLoss=Unit(self._stop_loss_percent.Value, UnitTypes.Percent)
        )

    def on_process(self, candle, mom_val):
        if candle.State != CandleStates.Finished:
            return

        self._count += 1
        if self._count < 3:
            self._prev_prev_mom = self._prev_mom
            self._prev_mom = mom_val
            return

        was_decreasing = self._prev_mom < self._prev_prev_mom
        now_increasing = mom_val > self._prev_mom
        was_increasing = self._prev_mom > self._prev_prev_mom
        now_decreasing = mom_val < self._prev_mom

        if was_decreasing and now_increasing and self._enable_long.Value and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif was_increasing and now_decreasing and self._enable_short.Value and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_prev_mom = self._prev_mom
        self._prev_mom = mom_val

    def CreateClone(self):
        return color_j_momentum_strategy()
