import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class is_strategy(Strategy):
    def __init__(self):
        super(is_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles used by the strategy", "General")
        self._reverse = self.Param("Reverse", False) \
            .SetDisplay("Reverse", "Reverse trading direction", "General")
        self._enable_short = self.Param("EnableShort", True) \
            .SetDisplay("Sell On", "Enable short selling", "General")
        self._profit_percent = self.Param("ProfitPercent", 1.5) \
            .SetDisplay("Profit %", "Take profit percent", "Risk")
        self._loss_percent = self.Param("LossPercent", 1.5) \
            .SetDisplay("Loss %", "Stop loss percent", "Risk")
        self._previous_value = 0.0
        self._sma_fast = None
        self._sma_slow = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(is_strategy, self).OnReseted()
        self._previous_value = 0.0
        self._sma_fast = None
        self._sma_slow = None

    def OnStarted2(self, time):
        super(is_strategy, self).OnStarted2(time)

        self._sma_fast = SimpleMovingAverage()
        self._sma_fast.Length = 80
        self._sma_slow = SimpleMovingAverage()
        self._sma_slow.Length = 200

        subscription = self.SubscribeCandles(self.candle_type)
        subscription \
            .Bind(self._sma_fast, self._sma_slow, self.ProcessCandle) \
            .Start()

        self.StartProtection(
            takeProfit=Unit(self._profit_percent.Value, UnitTypes.Percent),
            stopLoss=Unit(self._loss_percent.Value, UnitTypes.Percent),
            isStopTrailing=False)

    def ProcessCandle(self, candle, fast_val, slow_val):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Map price action to signal: 1 = bullish (fast > slow), 2 = bearish (fast < slow)
        hb5 = 1.0 if fast_val > slow_val else 2.0
        ii = 2.0 if self._reverse.Value else 1.0
        i2 = 1.0 if self._reverse.Value else 2.0
        prev = self._previous_value

        if hb5 == ii and prev != ii:
            if self.Position < 0 and self._enable_short.Value:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket()
        elif hb5 == i2 and prev != i2:
            if self.Position > 0:
                self.SellMarket(self.Position)
            if self._enable_short.Value:
                self.SellMarket()

        self._previous_value = hb5

    def CreateClone(self):
        return is_strategy()
