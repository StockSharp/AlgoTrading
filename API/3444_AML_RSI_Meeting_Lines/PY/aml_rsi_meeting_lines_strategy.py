import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class aml_rsi_meeting_lines_strategy(Strategy):
    def __init__(self):
        super(aml_rsi_meeting_lines_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._rsi_period = self.Param("RsiPeriod", 14)
        self._rsi_low = self.Param("RsiLow", 40.0)
        self._rsi_high = self.Param("RsiHigh", 60.0)

        self._prev_candle = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsi_period.Value = value

    @property
    def RsiLow(self):
        return self._rsi_low.Value

    @RsiLow.setter
    def RsiLow(self, value):
        self._rsi_low.Value = value

    @property
    def RsiHigh(self):
        return self._rsi_high.Value

    @RsiHigh.setter
    def RsiHigh(self, value):
        self._rsi_high.Value = value

    def OnReseted(self):
        super(aml_rsi_meeting_lines_strategy, self).OnReseted()
        self._prev_candle = None

    def OnStarted(self, time):
        super(aml_rsi_meeting_lines_strategy, self).OnStarted(time)
        self._prev_candle = None

        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, self._process_candle).Start()

    def _process_candle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        rsi_val = float(rsi_value)

        if self._prev_candle is not None:
            avg_body = (abs(float(candle.ClosePrice) - float(candle.OpenPrice))
                + abs(float(self._prev_candle.ClosePrice) - float(self._prev_candle.OpenPrice))) / 2.0

            if avg_body > 0:
                prev_bearish = float(self._prev_candle.OpenPrice) > float(self._prev_candle.ClosePrice)
                curr_bullish = float(candle.ClosePrice) > float(candle.OpenPrice)
                closes_near = abs(float(candle.ClosePrice) - float(self._prev_candle.ClosePrice)) < avg_body * 0.3

                if prev_bearish and curr_bullish and closes_near and rsi_val < self.RsiLow and self.Position <= 0:
                    self.BuyMarket()

                prev_bullish = float(self._prev_candle.ClosePrice) > float(self._prev_candle.OpenPrice)
                curr_bearish = float(candle.OpenPrice) > float(candle.ClosePrice)
                closes_near2 = abs(float(candle.ClosePrice) - float(self._prev_candle.ClosePrice)) < avg_body * 0.3

                if prev_bullish and curr_bearish and closes_near2 and rsi_val > self.RsiHigh and self.Position >= 0:
                    self.SellMarket()

        self._prev_candle = candle

    def CreateClone(self):
        return aml_rsi_meeting_lines_strategy()
