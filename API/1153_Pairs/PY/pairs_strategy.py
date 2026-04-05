import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, InvalidOperationException
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class pairs_strategy(Strategy):
    def __init__(self):
        super(pairs_strategy, self).__init__()

        self._reference_security = self.Param("ReferenceSecurity", None) \
            .SetDisplay("Reference Security", "Security used for pair comparison", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._reference_up = False
        self._previous_high = 0.0

    @property
    def reference_security(self):
        return self._reference_security.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(pairs_strategy, self).OnReseted()
        self._reference_up = False
        self._previous_high = 0.0

    def OnStarted2(self, time):
        super(pairs_strategy, self).OnStarted2(time)

        if self.reference_security is None:
            raise InvalidOperationException("ReferenceSecurity must be specified.")

        self.StartProtection(None, None)

        self.SubscribeCandles(self.candle_type, True, self.reference_security) \
            .Bind(self._process_reference).Start()

        self.SubscribeCandles(self.candle_type) \
            .Bind(self._process_main).Start()

    def _process_reference(self, candle):
        if candle.State != CandleStates.Finished:
            return
        self._reference_up = float(candle.ClosePrice) > float(candle.OpenPrice)

    def _process_main(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        opn = float(candle.OpenPrice)

        if self.Position <= 0 and self._reference_up and close < opn and self.IsFormedAndOnlineAndAllowTrading():
            self.BuyMarket()

        if self.Position > 0 and close > self._previous_high and self.IsFormedAndOnlineAndAllowTrading():
            self.SellMarket()

        self._previous_high = float(candle.HighPrice)

    def CreateClone(self):
        return pairs_strategy()
