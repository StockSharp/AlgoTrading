import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class aml_cci_meeting_lines_strategy(Strategy):
    """
    Meeting Lines + CCI strategy.
    Buys on bullish meeting lines with low CCI, sells on bearish meeting lines with high CCI.
    """

    def __init__(self):
        super(aml_cci_meeting_lines_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._cci_period = self.Param("CciPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("CCI Period", "CCI period", "Indicators")
        self._cci_low = self.Param("CciLow", -50.0) \
            .SetDisplay("CCI Low", "CCI level for bullish entry", "Signals")
        self._cci_high = self.Param("CciHigh", 50.0) \
            .SetDisplay("CCI High", "CCI level for bearish entry", "Signals")

        self._prev_open = None
        self._prev_close = None

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, v): self._candle_type.Value = v
    @property
    def CciPeriod(self): return self._cci_period.Value
    @CciPeriod.setter
    def CciPeriod(self, v): self._cci_period.Value = v
    @property
    def CciLow(self): return self._cci_low.Value
    @CciLow.setter
    def CciLow(self, v): self._cci_low.Value = v
    @property
    def CciHigh(self): return self._cci_high.Value
    @CciHigh.setter
    def CciHigh(self, v): self._cci_high.Value = v

    def OnReseted(self):
        super(aml_cci_meeting_lines_strategy, self).OnReseted()
        self._prev_open = None
        self._prev_close = None

    def OnStarted(self, time):
        super(aml_cci_meeting_lines_strategy, self).OnStarted(time)
        self._prev_open = None
        self._prev_close = None

        cci = CommodityChannelIndex()
        cci.Length = self.CciPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(cci, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, cci_value):
        if candle.State != CandleStates.Finished:
            return

        c_open = float(candle.OpenPrice)
        c_close = float(candle.ClosePrice)

        if self._prev_open is not None and self._prev_close is not None:
            avg_body = (abs(c_close - c_open) + abs(self._prev_close - self._prev_open)) / 2.0

            if avg_body > 0:
                prev_bearish = self._prev_open > self._prev_close
                curr_bullish = c_close > c_open
                closes_near = abs(c_close - self._prev_close) < avg_body * 0.3

                if prev_bearish and curr_bullish and closes_near and cci_value < self.CciLow and self.Position <= 0:
                    self.BuyMarket()

                prev_bullish = self._prev_close > self._prev_open
                curr_bearish = c_open > c_close
                closes_near2 = abs(c_close - self._prev_close) < avg_body * 0.3

                if prev_bullish and curr_bearish and closes_near2 and cci_value > self.CciHigh and self.Position >= 0:
                    self.SellMarket()

        self._prev_open = c_open
        self._prev_close = c_close

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return aml_cci_meeting_lines_strategy()
