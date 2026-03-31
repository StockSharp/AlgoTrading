import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class color_j_laguerre_strategy(Strategy):
    """
    Strategy based on color-coded Laguerre oscillator approximated by RSI.
    """

    def __init__(self):
        super(color_j_laguerre_strategy, self).__init__()
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "Period for RSI", "Indicators")
        self._high_level = self.Param("HighLevel", 85.0) \
            .SetDisplay("High Level", "Upper threshold", "Levels")
        self._middle_level = self.Param("MiddleLevel", 50.0) \
            .SetDisplay("Middle Level", "Central threshold", "Levels")
        self._low_level = self.Param("LowLevel", 15.0) \
            .SetDisplay("Low Level", "Lower threshold", "Levels")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_rsi = 0.0
        self._has_prev = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(color_j_laguerre_strategy, self).OnReseted()
        self._prev_rsi = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(color_j_laguerre_strategy, self).OnStarted2(time)

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, self.on_process).Start()

        self.StartProtection(
            takeProfit=Unit(4, UnitTypes.Percent),
            stopLoss=Unit(self._stop_loss_percent.Value, UnitTypes.Percent)
        )

    def on_process(self, candle, rsi_val):
        if candle.State != CandleStates.Finished:
            return

        if not self._has_prev:
            self._prev_rsi = rsi_val
            self._has_prev = True
            return

        middle = self._middle_level.Value
        high = self._high_level.Value
        low = self._low_level.Value

        if self._prev_rsi <= middle and rsi_val > middle and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif self._prev_rsi >= middle and rsi_val < middle and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        if self.Position > 0 and rsi_val >= high:
            self.SellMarket()
        elif self.Position < 0 and rsi_val <= low:
            self.BuyMarket()

        self._prev_rsi = rsi_val

    def CreateClone(self):
        return color_j_laguerre_strategy()
