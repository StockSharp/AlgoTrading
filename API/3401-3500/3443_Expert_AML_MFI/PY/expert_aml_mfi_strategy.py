import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MoneyFlowIndex
from StockSharp.Algo.Strategies import Strategy

class expert_aml_mfi_strategy(Strategy):
    def __init__(self):
        super(expert_aml_mfi_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._mfi_period = self.Param("MfiPeriod", 14)
        self._mfi_low = self.Param("MfiLow", 40.0)
        self._mfi_high = self.Param("MfiHigh", 60.0)

        self._prev_candle = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def MfiPeriod(self):
        return self._mfi_period.Value

    @MfiPeriod.setter
    def MfiPeriod(self, value):
        self._mfi_period.Value = value

    @property
    def MfiLow(self):
        return self._mfi_low.Value

    @MfiLow.setter
    def MfiLow(self, value):
        self._mfi_low.Value = value

    @property
    def MfiHigh(self):
        return self._mfi_high.Value

    @MfiHigh.setter
    def MfiHigh(self, value):
        self._mfi_high.Value = value

    def OnReseted(self):
        super(expert_aml_mfi_strategy, self).OnReseted()
        self._prev_candle = None

    def OnStarted2(self, time):
        super(expert_aml_mfi_strategy, self).OnStarted2(time)
        self._prev_candle = None

        mfi = MoneyFlowIndex()
        mfi.Length = self.MfiPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(mfi, self._process_candle).Start()

    def _process_candle(self, candle, mfi_value):
        if candle.State != CandleStates.Finished:
            return

        mfi_val = float(mfi_value)

        if self._prev_candle is not None:
            avg_body = (abs(float(candle.ClosePrice) - float(candle.OpenPrice))
                + abs(float(self._prev_candle.ClosePrice) - float(self._prev_candle.OpenPrice))) / 2.0

            if avg_body > 0:
                prev_bearish = float(self._prev_candle.OpenPrice) > float(self._prev_candle.ClosePrice)
                curr_bullish = float(candle.ClosePrice) > float(candle.OpenPrice)
                closes_near = abs(float(candle.ClosePrice) - float(self._prev_candle.ClosePrice)) < avg_body * 0.3

                if prev_bearish and curr_bullish and closes_near and mfi_val < self.MfiLow and self.Position <= 0:
                    self.BuyMarket()

                prev_bullish = float(self._prev_candle.ClosePrice) > float(self._prev_candle.OpenPrice)
                curr_bearish = float(candle.OpenPrice) > float(candle.ClosePrice)
                closes_near2 = abs(float(candle.ClosePrice) - float(self._prev_candle.ClosePrice)) < avg_body * 0.3

                if prev_bullish and curr_bearish and closes_near2 and mfi_val > self.MfiHigh and self.Position >= 0:
                    self.SellMarket()

        self._prev_candle = candle

    def CreateClone(self):
        return expert_aml_mfi_strategy()
