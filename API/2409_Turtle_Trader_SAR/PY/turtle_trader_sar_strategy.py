import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class turtle_trader_sar_strategy(Strategy):
    def __init__(self):
        super(turtle_trader_sar_strategy, self).__init__()

        self._short_period = self.Param("ShortPeriod", 20)
        self._stop_multiplier = self.Param("StopMultiplier", 2.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))

        self._highs = []
        self._lows = []
        self._closes = []
        self._stop_price = 0.0

    @property
    def ShortPeriod(self):
        return self._short_period.Value

    @ShortPeriod.setter
    def ShortPeriod(self, value):
        self._short_period.Value = value

    @property
    def StopMultiplier(self):
        return self._stop_multiplier.Value

    @StopMultiplier.setter
    def StopMultiplier(self, value):
        self._stop_multiplier.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(turtle_trader_sar_strategy, self).OnStarted(time)

        self._highs = []
        self._lows = []
        self._closes = []
        self._stop_price = 0.0

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self.ProcessCandle).Start()

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        h = float(candle.HighPrice)
        l = float(candle.LowPrice)
        c = float(candle.ClosePrice)

        self._highs.append(h)
        self._lows.append(l)
        self._closes.append(c)

        period = int(self.ShortPeriod)

        if len(self._highs) < period + 1:
            return

        while len(self._highs) > period + 10:
            self._highs.pop(0)
            self._lows.pop(0)
            self._closes.pop(0)

        length = len(self._highs)
        highest = 0.0
        lowest = float('inf')
        for i in range(length - 1 - period, length - 1):
            if self._highs[i] > highest:
                highest = self._highs[i]
            if self._lows[i] < lowest:
                lowest = self._lows[i]

        atr_period = min(20, length)
        sum_range = 0.0
        for i in range(length - atr_period, length):
            sum_range += self._highs[i] - self._lows[i]
        atr = sum_range / atr_period if atr_period > 0 else 0.0

        price = c

        if self.Position > 0 and self._stop_price > 0.0 and price <= self._stop_price:
            self.SellMarket()
            return
        elif self.Position < 0 and self._stop_price > 0.0 and price >= self._stop_price:
            self.BuyMarket()
            return

        if self.Position != 0:
            return

        if price > highest and atr > 0.0:
            self._stop_price = price - float(self.StopMultiplier) * atr
            self.BuyMarket()
        elif price < lowest and atr > 0.0:
            self._stop_price = price + float(self.StopMultiplier) * atr
            self.SellMarket()

    def OnReseted(self):
        super(turtle_trader_sar_strategy, self).OnReseted()
        self._highs = []
        self._lows = []
        self._closes = []
        self._stop_price = 0.0

    def CreateClone(self):
        return turtle_trader_sar_strategy()
