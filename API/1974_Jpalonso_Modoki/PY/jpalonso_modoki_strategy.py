import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class jpalonso_modoki_strategy(Strategy):
    """
    SMA envelope strategy. Buys when price is below the lower envelope,
    sells when above the upper. Uses StartProtection for SL/TP.
    """

    def __init__(self):
        super(jpalonso_modoki_strategy, self).__init__()
        self._sma_period = self.Param("SmaPeriod", 20) \
            .SetDisplay("SMA Period", "Length of the moving average", "Envelopes")
        self._deviation = self.Param("Deviation", 0.35) \
            .SetDisplay("Deviation %", "Envelope deviation from SMA in percent", "Envelopes")
        self._take_profit = self.Param("TakeProfit", 3000.0) \
            .SetDisplay("Take Profit", "Take profit in points", "Risk Management")
        self._stop_loss = self.Param("StopLoss", 5000.0) \
            .SetDisplay("Stop Loss", "Stop loss in points", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(jpalonso_modoki_strategy, self).OnStarted(time)

        tp = Unit(self._take_profit.Value, UnitTypes.Absolute)
        sl = Unit(self._stop_loss.Value, UnitTypes.Absolute)
        self.StartProtection(tp, sl)

        sma = SimpleMovingAverage()
        sma.Length = self._sma_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self._process_candle).Start()

    def _process_candle(self, candle, ma_val):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        ma = float(ma_val)
        dev = self._deviation.Value
        upper = ma * (1.0 + dev / 100.0)
        lower = ma * (1.0 - dev / 100.0)
        close = float(candle.ClosePrice)

        if close <= lower and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif close >= upper and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

    def CreateClone(self):
        return jpalonso_modoki_strategy()
