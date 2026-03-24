import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Sides
from StockSharp.Algo.Strategies import Strategy


class double_trading_strategy(Strategy):
    def __init__(self):
        super(double_trading_strategy, self).__init__()
        self._volume1 = self.Param("Volume1", 1.0) \
            .SetDisplay("Volume1", "First volume", "Parameters")
        self._volume2 = self.Param("Volume2", 1.3) \
            .SetDisplay("Volume2", "Second volume", "Parameters")
        self._profit_target = self.Param("ProfitTarget", 20.0) \
            .SetDisplay("Profit Target", "Exit profit", "Risk")
        self._direction1 = self.Param("Direction1", 0) \
            .SetDisplay("Direction1", "First side", "Parameters")
        self._direction2 = self.Param("Direction2", 0) \
            .SetDisplay("Direction2", "Second side", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Candles", "Data")

        self._second_security = None
        self._side1 = Sides.Buy
        self._side2 = Sides.Sell
        self._entry1 = None
        self._entry2 = None
        self._last1 = 0.0
        self._last2 = 0.0

    def get_SecondSecurity(self):
        return self._second_security

    def set_SecondSecurity(self, value):
        self._second_security = value

    SecondSecurity = property(get_SecondSecurity, set_SecondSecurity)

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(double_trading_strategy, self).OnReseted()
        dir1 = self._direction1.Value
        dir2 = self._direction2.Value
        self._side1 = Sides.Sell if dir1 == 2 else Sides.Buy
        self._side2 = Sides.Buy if dir2 == 1 else Sides.Sell
        self._entry1 = None
        self._entry2 = None
        self._last1 = 0.0
        self._last2 = 0.0

    def OnStarted(self, time):
        super(double_trading_strategy, self).OnStarted(time)
        self.StartProtection(None, None)

        sub1 = self.SubscribeCandles(self.candle_type)
        sub1.Bind(self.process_first).Start()

        if self._second_security is not None:
            sub2 = self.SubscribeCandles(self.candle_type, security=self._second_security)
            sub2.Bind(self.process_second).Start()

        dir1 = self._direction1.Value
        dir2 = self._direction2.Value
        self._side1 = Sides.Sell if dir1 == 2 else Sides.Buy
        self._side2 = Sides.Buy if dir2 == 1 else Sides.Sell

        if self._side1 == Sides.Buy:
            self.BuyMarket(float(self._volume1.Value))
        else:
            self.SellMarket(float(self._volume1.Value))

    def process_first(self, candle):
        if candle.State != CandleStates.Finished:
            return
        self._last1 = float(candle.ClosePrice)
        if self._entry1 is None:
            self._entry1 = float(candle.ClosePrice)
        self._check_exit()

    def process_second(self, candle):
        if candle.State != CandleStates.Finished:
            return
        self._last2 = float(candle.ClosePrice)
        if self._entry2 is None:
            self._entry2 = float(candle.ClosePrice)
        self._check_exit()

    def _check_exit(self):
        if self._entry1 is None or self._entry2 is None:
            return
        v1 = float(self._volume1.Value)
        v2 = float(self._volume2.Value)
        if self._side1 == Sides.Buy:
            pnl1 = (self._last1 - self._entry1) * v1
        else:
            pnl1 = (self._entry1 - self._last1) * v1
        if self._side2 == Sides.Buy:
            pnl2 = (self._last2 - self._entry2) * v2
        else:
            pnl2 = (self._entry2 - self._last2) * v2
        if pnl1 + pnl2 >= float(self._profit_target.Value):
            self._exit_positions()

    def _exit_positions(self):
        v1 = float(self._volume1.Value)
        if self._side1 == Sides.Buy:
            self.SellMarket(v1)
        else:
            self.BuyMarket(v1)
        self._entry1 = None
        self._entry2 = None

    def CreateClone(self):
        return double_trading_strategy()
