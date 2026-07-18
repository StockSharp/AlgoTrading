import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import DataType, CandleStates
from System import TimeSpan, Math


class multi_pair_closer_strategy(Strategy):
    def __init__(self):
        super(multi_pair_closer_strategy, self).__init__()

        self._profit_target = self.Param("ProfitTarget", 5.0)
        self._max_loss = self.Param("MaxLoss", 10.0)
        self._min_age_seconds = self.Param("MinAgeSeconds", 60)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30)))
        self._sma_period = self.Param("SmaPeriod", 20)

        self._sma = None
        self._entry_price = 0.0
        self._entry_time = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(multi_pair_closer_strategy, self).OnStarted2(time)

        self._sma = SimpleMovingAverage()
        self._sma.Length = self._sma_period.Value

        self.SubscribeCandles(self.CandleType).Bind(self._sma, self._process_candle).Start()

    def _process_candle(self, candle, sma_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormed:
            return

        price = float(candle.ClosePrice)
        time = candle.CloseTime
        sma_value = float(sma_val)

        if self.Position != 0 and self._entry_price > 0:
            if self.Position > 0:
                pnl = price - self._entry_price
            else:
                pnl = self._entry_price - price

            can_close = self._min_age_seconds.Value <= 0 or (
                self._entry_time is not None and (time - self._entry_time).TotalSeconds >= self._min_age_seconds.Value)

            if can_close:
                if (self._profit_target.Value > 0 and pnl >= self._profit_target.Value) or \
                   (self._max_loss.Value > 0 and pnl <= -self._max_loss.Value):
                    if self.Position > 0:
                        self.SellMarket(abs(self.Position))
                    else:
                        self.BuyMarket(abs(self.Position))
                    self._entry_price = 0.0
                    self._entry_time = None
                    return

        if self.Position == 0:
            if price > sma_value:
                self.BuyMarket()
                self._entry_price = price
                self._entry_time = time
            elif price < sma_value:
                self.SellMarket()
                self._entry_price = price
                self._entry_time = time

    def OnReseted(self):
        super(multi_pair_closer_strategy, self).OnReseted()
        self._sma = None
        self._entry_price = 0.0
        self._entry_time = None

    def CreateClone(self):
        return multi_pair_closer_strategy()
