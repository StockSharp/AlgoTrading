import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy

class js_ma_day_strategy(Strategy):
    """
    Daily SMA strategy comparing manual moving average to open price.
    Trades when MA crosses daily open. Uses StartProtection for SL/TP.
    """

    def __init__(self):
        super(js_ma_day_strategy, self).__init__()
        self._ma_period = self.Param("MaPeriod", 5) \
            .SetDisplay("MA Period", "SMA period on daily candles", "Parameters")
        self._reverse = self.Param("Reverse", False) \
            .SetDisplay("Reverse Signals", "Reverse entry direction", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles for moving average", "General")

        self._prices = []
        self._prev_ma = None
        self._prev_open = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(js_ma_day_strategy, self).OnReseted()
        self._prices = []
        self._prev_ma = None
        self._prev_open = None

    def OnStarted2(self, time):
        super(js_ma_day_strategy, self).OnStarted2(time)

        self.StartProtection(
            Unit(2000.0, UnitTypes.Absolute),
            Unit(1000.0, UnitTypes.Absolute))

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        open_price = float(candle.OpenPrice)
        period = self._ma_period.Value

        self._prices.append(close)
        if len(self._prices) > period:
            self._prices.pop(0)

        if len(self._prices) < period:
            return

        ma = sum(self._prices) / len(self._prices)

        if self._prev_ma is not None and self._prev_open is not None:
            buy_cond = ma > open_price and self._prev_ma <= self._prev_open
            sell_cond = ma < open_price and self._prev_ma >= self._prev_open

            reverse = self._reverse.Value

            if buy_cond:
                if not reverse and self.Position <= 0:
                    self.BuyMarket()
                elif reverse and self.Position >= 0:
                    self.SellMarket()
            elif sell_cond:
                if not reverse and self.Position >= 0:
                    self.SellMarket()
                elif reverse and self.Position <= 0:
                    self.BuyMarket()

        self._prev_ma = ma
        self._prev_open = open_price

    def CreateClone(self):
        return js_ma_day_strategy()
