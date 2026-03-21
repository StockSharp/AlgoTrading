import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class prophet_strategy(Strategy):
    def __init__(self):
        super(prophet_strategy, self).__init__()

        self._x1 = self.Param("X1", 9) \
            .SetDisplay("X1", "Weight applied to |High[1] - Low[2]|", "Signal")
        self._x2 = self.Param("X2", 29) \
            .SetDisplay("X2", "Weight applied to |High[3] - Low[2]|", "Signal")
        self._x3 = self.Param("X3", 94) \
            .SetDisplay("X3", "Weight applied to |High[2] - Low[1]|", "Signal")
        self._x4 = self.Param("X4", 125) \
            .SetDisplay("X4", "Weight applied to |High[2] - Low[3]|", "Signal")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Timeframe for calculations", "General")

        self._candle1 = None
        self._candle2 = None
        self._candle3 = None
        self._entry_price = 0.0

    @property
    def X1(self):
        return self._x1.Value

    @property
    def X2(self):
        return self._x2.Value

    @property
    def X3(self):
        return self._x3.Value

    @property
    def X4(self):
        return self._x4.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(prophet_strategy, self).OnStarted(time)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        # Shift history
        self._candle3 = self._candle2
        self._candle2 = self._candle1
        self._candle1 = {
            'high': float(candle.HighPrice),
            'low': float(candle.LowPrice),
            'close': float(candle.ClosePrice),
        }

        if self._candle1 is None or self._candle2 is None or self._candle3 is None:
            return

        # Manage position - exit on reversal signal
        if self.Position > 0:
            sell_signal = self._calculate_signal(True)
            if sell_signal > 0:
                self.SellMarket()
                self._entry_price = 0.0
        elif self.Position < 0:
            buy_signal = self._calculate_signal(False)
            if buy_signal > 0:
                self.BuyMarket()
                self._entry_price = 0.0

        # Entry
        if self.Position == 0:
            buy_signal = self._calculate_signal(False)
            sell_signal = self._calculate_signal(True)

            if buy_signal > 0 and buy_signal > sell_signal:
                self.BuyMarket()
                self._entry_price = self._candle1['close']
            elif sell_signal > 0 and sell_signal > buy_signal:
                self.SellMarket()
                self._entry_price = self._candle1['close']

    def _calculate_signal(self, is_sell):
        c1 = self._candle1
        c2 = self._candle2
        c3 = self._candle3

        term1 = abs(c1['high'] - c2['low'])
        term2 = abs(c3['high'] - c2['low'])
        term3 = abs(c2['high'] - c1['low'])
        term4 = abs(c2['high'] - c3['low'])

        x1 = int(self.X1)
        x2 = int(self.X2)
        x3 = int(self.X3)
        x4 = int(self.X4)

        if is_sell:
            return (100 - x1) * term1 + (100 - x2) * term2 + (x3 - 100) * term3 + (x4 - 100) * term4
        else:
            return (x1 - 100) * term1 + (x2 - 100) * term2 + (100 - x3) * term3 + (100 - x4) * term4

    def OnReseted(self):
        super(prophet_strategy, self).OnReseted()
        self._candle1 = None
        self._candle2 = None
        self._candle3 = None
        self._entry_price = 0.0

    def CreateClone(self):
        return prophet_strategy()
