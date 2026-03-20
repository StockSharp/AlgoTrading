import clr
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import MoneyFlowIndex
from StockSharp.Algo.Strategies import Strategy

class mfi_histogram_strategy(Strategy):
    def __init__(self):
        super(mfi_histogram_strategy, self).__init__()
        self._mfi_period = self.Param("MfiPeriod", 14).SetDisplay("MFI Period", "Period for Money Flow Index", "MFI")
        self._high_level = self.Param("HighLevel", 60.0).SetDisplay("High Level", "Overbought threshold", "MFI")
        self._low_level = self.Param("LowLevel", 40.0).SetDisplay("Low Level", "Oversold threshold", "MFI")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Timeframe of candles", "General")
        self._prev_mfi = 0.0
    @property
    def mfi_period(self): return self._mfi_period.Value
    @property
    def high_level(self): return self._high_level.Value
    @property
    def low_level(self): return self._low_level.Value
    @property
    def candle_type(self): return self._candle_type.Value
    def OnReseted(self):
        super(mfi_histogram_strategy, self).OnReseted()
        self._prev_mfi = 0.0
    def OnStarted(self, time):
        super(mfi_histogram_strategy, self).OnStarted(time)
        mfi = MoneyFlowIndex()
        mfi.Length = self.mfi_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(mfi, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            area2 = self.CreateChartArea()
            if area2 is not None:
                self.DrawIndicator(area2, mfi)
    def process_candle(self, candle, mfi_value):
        if candle.State != CandleStates.Finished: return
        mv = float(mfi_value)
        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_mfi = mv
            return
        hl = float(self.high_level)
        ll = float(self.low_level)
        if mv > hl and self._prev_mfi <= hl:
            if self.Position <= 0:
                self.BuyMarket()
        elif mv < ll and self._prev_mfi >= ll:
            if self.Position >= 0:
                self.SellMarket()
        self._prev_mfi = mv
    def CreateClone(self): return mfi_histogram_strategy()
