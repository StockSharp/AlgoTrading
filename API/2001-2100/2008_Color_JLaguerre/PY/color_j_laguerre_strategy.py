import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, OrderStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import RelativeStrengthIndex, JurikMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import process_float


class color_j_laguerre_strategy(Strategy):
    """RSI-JMA oscillator crosses middle and exits at extreme levels on finished candles."""

    def __init__(self):
        super(color_j_laguerre_strategy, self).__init__()
        self._rsi_length = self.Param("RsiLength", 14).SetGreaterThanZero() \
            .SetDisplay("RSI Length", "RSI lookback", "Indicator")
        self._jma_length = self.Param("JmaLength", 14).SetGreaterThanZero() \
            .SetDisplay("JMA Length", "Jurik smoothing of RSI", "Indicator")
        self._high_level = self.Param("HighLevel", 85.0) \
            .SetDisplay("High Level", "Close long at upper threshold", "Levels")
        self._middle_level = self.Param("MiddleLevel", 50.0) \
            .SetDisplay("Middle Level", "Middle crossover threshold", "Levels")
        self._low_level = self.Param("LowLevel", 15.0) \
            .SetDisplay("Low Level", "Close short at lower threshold", "Levels")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative() \
            .SetDisplay("Stop Loss %", "Native percent stop", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._rsi = None
        self._jma = None
        self._previous = None
        self._pending = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(color_j_laguerre_strategy, self).OnReseted()
        self._rsi = self._jma = self._previous = self._pending = None

    def OnStarted2(self, time):
        super(color_j_laguerre_strategy, self).OnStarted2(time)
        self._previous = self._pending = None
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = int(self._rsi_length.Value)
        self._jma = JurikMovingAverage()
        self._jma.Length = int(self._jma_length.Value)
        self.Indicators.Add(self._jma)
        self.SubscribeCandles(self.CandleType).Bind(self._rsi, self.ProcessCandle).Start()
        self.StartProtection(
            takeProfit=Unit(0.0, UnitTypes.Percent),
            stopLoss=Unit(self._stop_loss_percent.Value, UnitTypes.Percent),
            useMarketOrders=True)

    def ProcessCandle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished or not self._rsi.IsFormed:
            return
        smooth = process_float(self._jma, rsi_value, candle.OpenTime, True)
        if not smooth.IsFormed:
            return
        value = float(smooth)
        middle = float(self._middle_level.Value)
        up = self._previous is not None and self._previous <= middle and value > middle
        down = self._previous is not None and self._previous >= middle and value < middle
        self._previous = value

        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        if self._pending is not None and self._pending.State not in (OrderStates.Done, OrderStates.Failed):
            return

        if self.Position > 0 and value >= float(self._high_level.Value):
            self._pending = self.SellMarket(self.Position)
            return
        if self.Position < 0 and value <= float(self._low_level.Value):
            self._pending = self.BuyMarket(-self.Position)
            return

        if up:
            if self.Position < 0:
                self._pending = self.BuyMarket(-self.Position + self.Volume)
            elif self.Position == 0:
                self._pending = self.BuyMarket(self.Volume)
        elif down:
            if self.Position > 0:
                self._pending = self.SellMarket(self.Position + self.Volume)
            elif self.Position == 0:
                self._pending = self.SellMarket(self.Volume)

    def CreateClone(self):
        return color_j_laguerre_strategy()
