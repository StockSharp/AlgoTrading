import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class january_barometer_strategy(Strategy):
    """
    January barometer strategy generalized to any month.
    Measures the return over the first N candles of each evaluation period,
    then goes long if bullish or short if bearish for the remainder.
    Re-evaluates at the start of each new period.
    """

    def __init__(self):
        super(january_barometer_strategy, self).__init__()

        self._measure_candles = self.Param("MeasureCandles", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("Measure Candles", "Number of candles for barometer measurement", "General")

        self._period_candles = self.Param("PeriodCandles", 200) \
            .SetGreaterThanZero() \
            .SetDisplay("Period Candles", "Total candles per evaluation period", "General")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._candle_count = 0
        self._period_open = 0
        self._measure_close = 0
        self._measured = False

    @property
    def MeasureCandles(self):
        return self._measure_candles.Value

    @property
    def PeriodCandles(self):
        return self._period_candles.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(january_barometer_strategy, self).OnReseted()
        self._candle_count = 0
        self._period_open = 0
        self._measure_close = 0
        self._measured = False

    def OnStarted2(self, time):
        super(january_barometer_strategy, self).OnStarted2(time)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._candle_count += 1

        # Start of a new period
        if self._candle_count == 1:
            self._period_open = candle.OpenPrice
            self._measured = False

        # End of measurement window
        if self._candle_count == self.MeasureCandles and not self._measured:
            self._measure_close = candle.ClosePrice
            self._measured = True

            if self._period_open > 0:
                barometer_return = (self._measure_close - self._period_open) / self._period_open
                bullish = barometer_return > 0

                # Enter position based on barometer reading
                if bullish and self.Position <= 0:
                    if self.Position < 0:
                        self.BuyMarket()
                    self.BuyMarket()
                elif not bullish and self.Position >= 0:
                    if self.Position > 0:
                        self.SellMarket()
                    self.SellMarket()

        # End of period: close position and reset for next period
        if self._candle_count >= self.PeriodCandles:
            if self.Position > 0:
                self.SellMarket()
            elif self.Position < 0:
                self.BuyMarket()

            self._candle_count = 0
            self._period_open = 0
            self._measure_close = 0
            self._measured = False

    def CreateClone(self):
        return january_barometer_strategy()
