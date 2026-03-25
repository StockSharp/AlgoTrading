import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy

class go_risk_managed_strategy(Strategy):
    """
    GO strategy: moving averages of open vs close prices.
    Buys when GO (close_ma - open_ma) crosses above zero.
    Sells when GO crosses below zero. Uses StartProtection for SL/TP.
    """

    def __init__(self):
        super(go_risk_managed_strategy, self).__init__()
        self._ma_period = self.Param("MaPeriod", 10) \
            .SetDisplay("MA Period", "Moving average period", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._open_ma = None
        self._prev_go = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(go_risk_managed_strategy, self).OnReseted()
        self._prev_go = None

    def OnStarted(self, time):
        super(go_risk_managed_strategy, self).OnStarted(time)

        self._open_ma = SimpleMovingAverage()
        self._open_ma.Length = self._ma_period.Value

        close_ma = SimpleMovingAverage()
        close_ma.Length = self._ma_period.Value

        self.Indicators.Add(self._open_ma)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(close_ma, self._process_candle).Start()

        self.StartProtection(
            Unit(2000, UnitTypes.Absolute),
            Unit(1000, UnitTypes.Absolute))

    def _process_candle(self, candle, close_val):
        if candle.State != CandleStates.Finished:
            return

        open_input = DecimalIndicatorValue(self._open_ma, candle.OpenPrice, candle.OpenTime)
        open_input.IsFinal = True
        open_result = self._open_ma.Process(open_input)
        if not self._open_ma.IsFormed:
            return

        open_v = float(open_result)
        close_v = float(close_val)
        go = close_v - open_v

        if self._prev_go is None:
            self._prev_go = go
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_go = go
            return

        if self._prev_go <= 0 and go > 0 and self.Position <= 0:
            self.BuyMarket()
        elif self._prev_go >= 0 and go < 0 and self.Position >= 0:
            self.SellMarket()

        self._prev_go = go

    def CreateClone(self):
        return go_risk_managed_strategy()
