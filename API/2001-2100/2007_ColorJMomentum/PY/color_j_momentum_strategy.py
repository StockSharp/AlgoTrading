import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, OrderStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import Momentum, JurikMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import process_float


class color_j_momentum_strategy(Strategy):
    """Jurik-smoothed momentum signals on completed candles, with native protection."""

    def __init__(self):
        super(color_j_momentum_strategy, self).__init__()
        self._momentum_length = self.Param("MomentumLength", 8).SetGreaterThanZero() \
            .SetDisplay("Momentum Length", "Momentum lookback", "Indicator")
        self._jma_length = self.Param("JmaLength", 8).SetGreaterThanZero() \
            .SetDisplay("JMA Length", "Smoothing length", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(8))) \
            .SetDisplay("Candle Type", "Signal candle timeframe", "General")
        self._stop_loss_percent = self.Param("StopLossPercent", 1.0).SetNotNegative() \
            .SetDisplay("Stop Loss %", "Native percent risk distance", "Risk")
        self._take_profit_percent = self.Param("TakeProfitPercent", 2.0).SetNotNegative() \
            .SetDisplay("Take Profit %", "Native percent target distance", "Risk")
        self._enable_stop_loss = self.Param("EnableStopLoss", True) \
            .SetDisplay("Enable Stop Loss", "Activate initial stop", "Risk")
        self._enable_long = self.Param("EnableLong", True) \
            .SetDisplay("Enable Long", "Allow long entries", "Trading")
        self._enable_short = self.Param("EnableShort", True) \
            .SetDisplay("Enable Short", "Allow short entries", "Trading")
        self._reset_history()

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def _reset_history(self):
        self._jma = None
        self._previous = self._before_previous = None
        self._pending = None

    def OnReseted(self):
        super(color_j_momentum_strategy, self).OnReseted()
        self._reset_history()

    def OnStarted2(self, time):
        super(color_j_momentum_strategy, self).OnStarted2(time)
        self._reset_history()
        momentum = Momentum()
        momentum.Length = int(self._momentum_length.Value)
        self._jma = JurikMovingAverage()
        self._jma.Length = int(self._jma_length.Value)
        self.Indicators.Add(self._jma)
        self.SubscribeCandles(self.CandleType).Bind(momentum, self.ProcessCandle).Start()
        self.StartProtection(
            takeProfit=Unit(self._take_profit_percent.Value, UnitTypes.Percent),
            stopLoss=Unit(self._stop_loss_percent.Value if self._enable_stop_loss.Value else 0.0, UnitTypes.Percent),
            useMarketOrders=True)

    def ProcessCandle(self, candle, momentum):
        if candle.State != CandleStates.Finished:
            return
        result = process_float(self._jma, momentum, candle.OpenTime, True)
        if not result.IsFormed:
            return
        value = float(result)
        up = self._before_previous is not None and self._previous < self._before_previous and value > self._previous
        down = self._before_previous is not None and self._previous > self._before_previous and value < self._previous
        self._before_previous, self._previous = self._previous, value

        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        if self._pending is not None and self._pending.State not in (OrderStates.Done, OrderStates.Failed):
            return
        if up:
            if self.Position < 0:
                self._pending = self.BuyMarket(-self.Position + (self.Volume if self._enable_long.Value else 0))
            elif self.Position == 0 and self._enable_long.Value:
                self._pending = self.BuyMarket(self.Volume)
        elif down:
            if self.Position > 0:
                self._pending = self.SellMarket(self.Position + (self.Volume if self._enable_short.Value else 0))
            elif self.Position == 0 and self._enable_short.Value:
                self._pending = self.SellMarket(self.Volume)

    def CreateClone(self):
        return color_j_momentum_strategy()
