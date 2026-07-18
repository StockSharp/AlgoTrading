import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class money_fixed_risk_strategy(Strategy):
    def __init__(self):
        super(money_fixed_risk_strategy, self).__init__()

        self._atr_multiplier = self.Param("AtrMultiplier", 1.5)
        self._atr_period = self.Param("AtrPeriod", 14)
        self._candle_interval = self.Param("CandleInterval", 10)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))

        self._candle_counter = 0
        self._stop_price = 0.0
        self._take_profit_price = 0.0
        self._entry_price = 0.0

    @property
    def AtrMultiplier(self):
        return self._atr_multiplier.Value

    @AtrMultiplier.setter
    def AtrMultiplier(self, value):
        self._atr_multiplier.Value = value

    @property
    def AtrPeriod(self):
        return self._atr_period.Value

    @AtrPeriod.setter
    def AtrPeriod(self, value):
        self._atr_period.Value = value

    @property
    def CandleInterval(self):
        return self._candle_interval.Value

    @CandleInterval.setter
    def CandleInterval(self, value):
        self._candle_interval.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(money_fixed_risk_strategy, self).OnStarted2(time)

        self._candle_counter = 0
        self._stop_price = 0.0
        self._take_profit_price = 0.0
        self._entry_price = 0.0

        atr = AverageTrueRange()
        atr.Length = self.AtrPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(atr, self.ProcessCandle).Start()

        self.StartProtection(
            Unit(2000.0, UnitTypes.Absolute),
            Unit(1000.0, UnitTypes.Absolute))

    def ProcessCandle(self, candle, atr_value):
        if candle.State != CandleStates.Finished:
            return

        price = float(candle.ClosePrice)
        low = float(candle.LowPrice)
        high = float(candle.HighPrice)
        atr_val = float(atr_value)

        if self.Position > 0 and self._stop_price > 0.0:
            if low <= self._stop_price or high >= self._take_profit_price:
                self.SellMarket()
                self._stop_price = 0.0
                self._take_profit_price = 0.0
                self._entry_price = 0.0

        if self.Position < 0 and self._stop_price > 0.0:
            if high >= self._stop_price or low <= self._take_profit_price:
                self.BuyMarket()
                self._stop_price = 0.0
                self._take_profit_price = 0.0
                self._entry_price = 0.0

        self._candle_counter += 1

        if self._candle_counter < int(self.CandleInterval):
            return

        self._candle_counter = 0

        if self.Position != 0:
            return

        if atr_val <= 0.0:
            return

        stop_distance = atr_val * float(self.AtrMultiplier)

        go_long = self._entry_price == 0.0 or price > self._entry_price

        if go_long:
            self.BuyMarket()
            self._entry_price = price
            self._stop_price = price - stop_distance
            self._take_profit_price = price + stop_distance
        else:
            self.SellMarket()
            self._entry_price = price
            self._stop_price = price + stop_distance
            self._take_profit_price = price - stop_distance

    def OnReseted(self):
        super(money_fixed_risk_strategy, self).OnReseted()
        self._candle_counter = 0
        self._stop_price = 0.0
        self._take_profit_price = 0.0
        self._entry_price = 0.0

    def CreateClone(self):
        return money_fixed_risk_strategy()
