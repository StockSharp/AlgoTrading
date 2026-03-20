import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class rsi_trader_strategy(Strategy):
    def __init__(self):
        super(rsi_trader_strategy, self).__init__()

        self._rsi_period = self.Param("RsiPeriod", 14)
        self._short_rsi_ma_period = self.Param("ShortRsiMaPeriod", 12)
        self._long_rsi_ma_period = self.Param("LongRsiMaPeriod", 60)
        self._short_price_ma_period = self.Param("ShortPriceMaPeriod", 12)
        self._long_price_ma_period = self.Param("LongPriceMaPeriod", 60)
        self._reverse = self.Param("Reverse", False)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))

        self._closes = []
        self._rsi_values = []

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsi_period.Value = value

    @property
    def ShortRsiMaPeriod(self):
        return self._short_rsi_ma_period.Value

    @ShortRsiMaPeriod.setter
    def ShortRsiMaPeriod(self, value):
        self._short_rsi_ma_period.Value = value

    @property
    def LongRsiMaPeriod(self):
        return self._long_rsi_ma_period.Value

    @LongRsiMaPeriod.setter
    def LongRsiMaPeriod(self, value):
        self._long_rsi_ma_period.Value = value

    @property
    def ShortPriceMaPeriod(self):
        return self._short_price_ma_period.Value

    @ShortPriceMaPeriod.setter
    def ShortPriceMaPeriod(self, value):
        self._short_price_ma_period.Value = value

    @property
    def LongPriceMaPeriod(self):
        return self._long_price_ma_period.Value

    @LongPriceMaPeriod.setter
    def LongPriceMaPeriod(self, value):
        self._long_price_ma_period.Value = value

    @property
    def Reverse(self):
        return self._reverse.Value

    @Reverse.setter
    def Reverse(self, value):
        self._reverse.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(rsi_trader_strategy, self).OnStarted(time)

        self._closes = []
        self._rsi_values = []

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

        self.StartProtection(
            Unit(2000.0, UnitTypes.Absolute),
            Unit(1000.0, UnitTypes.Absolute))

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        self._closes.append(close)

        max_cache = max(int(self.LongPriceMaPeriod), max(int(self.LongRsiMaPeriod) + int(self.RsiPeriod), 300))
        while len(self._closes) > max_cache:
            self._closes.pop(0)

        rsi = self._calculate_rsi()
        if rsi is None:
            return

        self._rsi_values.append(rsi)
        while len(self._rsi_values) > max_cache:
            self._rsi_values.pop(0)

        if len(self._rsi_values) < int(self.LongRsiMaPeriod) or len(self._closes) < int(self.LongPriceMaPeriod):
            return

        short_rsi = self._average_last(self._rsi_values, int(self.ShortRsiMaPeriod))
        long_rsi = self._average_last(self._rsi_values, int(self.LongRsiMaPeriod))
        short_price = self._average_last(self._closes, int(self.ShortPriceMaPeriod))
        long_price = self._weighted_average_last(self._closes, int(self.LongPriceMaPeriod))

        go_long = short_price > long_price and short_rsi > long_rsi
        go_short = short_price < long_price and short_rsi < long_rsi
        sideways = not go_long and not go_short

        if sideways and self.Position != 0:
            if self.Position > 0:
                self.SellMarket()
            elif self.Position < 0:
                self.BuyMarket()
            return

        if self.Position != 0:
            return

        if go_long:
            if self.Reverse:
                self.SellMarket()
            else:
                self.BuyMarket()
        elif go_short:
            if self.Reverse:
                self.BuyMarket()
            else:
                self.SellMarket()

    def _calculate_rsi(self):
        rsi_period = int(self.RsiPeriod)
        if len(self._closes) <= rsi_period:
            return None

        gain_sum = 0.0
        loss_sum = 0.0
        start = len(self._closes) - rsi_period

        for i in range(start, len(self._closes)):
            change = self._closes[i] - self._closes[i - 1]
            if change > 0.0:
                gain_sum += change
            else:
                loss_sum -= change

        average_gain = gain_sum / rsi_period
        average_loss = loss_sum / rsi_period

        if average_loss == 0.0:
            return 100.0

        rs = average_gain / average_loss
        return 100.0 - 100.0 / (1.0 + rs)

    def _average_last(self, values, length):
        total = 0.0
        start = len(values) - length
        for i in range(start, len(values)):
            total += values[i]
        return total / length

    def _weighted_average_last(self, values, length):
        weighted_sum = 0.0
        weight_sum = 0.0
        start = len(values) - length
        weight = 1.0

        for i in range(start, len(values)):
            weighted_sum += values[i] * weight
            weight_sum += weight
            weight += 1.0

        if weight_sum > 0.0:
            return weighted_sum / weight_sum
        return values[-1]

    def OnReseted(self):
        super(rsi_trader_strategy, self).OnReseted()
        self._closes = []
        self._rsi_values = []

    def CreateClone(self):
        return rsi_trader_strategy()
