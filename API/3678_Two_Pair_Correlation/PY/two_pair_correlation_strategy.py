import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class two_pair_correlation_strategy(Strategy):
    def __init__(self):
        super(two_pair_correlation_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))
        self._price_difference_threshold = self.Param("PriceDifferenceThreshold", 5.0)
        self._minimum_total_profit = self.Param("MinimumTotalProfit", 3.0)
        self._atr_period = self.Param("AtrPeriod", 14)

        self._atr_value = 0.0
        self._entry_price = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def PriceDifferenceThreshold(self):
        return self._price_difference_threshold.Value

    @PriceDifferenceThreshold.setter
    def PriceDifferenceThreshold(self, value):
        self._price_difference_threshold.Value = value

    @property
    def MinimumTotalProfit(self):
        return self._minimum_total_profit.Value

    @MinimumTotalProfit.setter
    def MinimumTotalProfit(self, value):
        self._minimum_total_profit.Value = value

    @property
    def AtrPeriod(self):
        return self._atr_period.Value

    @AtrPeriod.setter
    def AtrPeriod(self, value):
        self._atr_period.Value = value

    def OnReseted(self):
        super(two_pair_correlation_strategy, self).OnReseted()
        self._atr_value = 0.0
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(two_pair_correlation_strategy, self).OnStarted(time)
        self._atr_value = 0.0
        self._entry_price = 0.0

        atr = AverageTrueRange()
        atr.Length = self.AtrPeriod
        sma = SimpleMovingAverage()
        sma.Length = 20

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(atr, sma, self._process_candle).Start()

    def _process_candle(self, candle, atr_value, sma_value):
        if candle.State != CandleStates.Finished:
            return

        atr_val = float(atr_value)
        sma_val = float(sma_value)
        price = float(candle.ClosePrice)
        self._atr_value = atr_val

        # Check profit target
        if self.Position != 0 and self._entry_price > 0:
            if self.Position > 0:
                pnl = price - self._entry_price
            else:
                pnl = self._entry_price - price

            profit_target = max(float(self.MinimumTotalProfit), atr_val * 0.5)
            if profit_target > 0 and pnl >= profit_target:
                if self.Position > 0:
                    self.SellMarket()
                else:
                    self.BuyMarket()
                self._entry_price = 0.0
                return

        if self.Position != 0:
            return

        deviation = price - sma_val
        entry_threshold = max(float(self.PriceDifferenceThreshold), atr_val)

        if deviation > entry_threshold:
            self.SellMarket()
            self._entry_price = price
        elif deviation < -entry_threshold:
            self.BuyMarket()
            self._entry_price = price

    def CreateClone(self):
        return two_pair_correlation_strategy()
