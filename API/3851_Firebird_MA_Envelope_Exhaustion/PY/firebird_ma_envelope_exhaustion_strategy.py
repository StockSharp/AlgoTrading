import clr
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy

class firebird_ma_envelope_exhaustion_strategy(Strategy):
    def __init__(self):
        super(firebird_ma_envelope_exhaustion_strategy, self).__init__()
        self._bb_period = self.Param("BbPeriod", 10).SetDisplay("BB Period", "Bollinger Bands period", "Indicators")
        self._bb_width = self.Param("BbWidth", 2.0).SetDisplay("BB Width", "Bollinger Bands width", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Candle timeframe", "General")
    @property
    def bb_period(self): return self._bb_period.Value
    @property
    def bb_width(self): return self._bb_width.Value
    @property
    def candle_type(self): return self._candle_type.Value
    def OnReseted(self):
        super(firebird_ma_envelope_exhaustion_strategy, self).OnReseted()
    def OnStarted2(self, time):
        super(firebird_ma_envelope_exhaustion_strategy, self).OnStarted2(time)
        bb = BollingerBands()
        bb.Length = self.bb_period
        bb.Width = self.bb_width
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bb, self.process_candle).Start()
    def process_candle(self, candle, bb_value):
        if candle.State != CandleStates.Finished:
            return
        if not bb_value.IsFinal or bb_value.IsEmpty:
            return
        if bb_value.UpBand is None or bb_value.LowBand is None or bb_value.MovingAverage is None:
            return
        upper = float(bb_value.UpBand)
        lower = float(bb_value.LowBand)
        close = float(candle.ClosePrice)
        if close < lower and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif close > upper and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
    def CreateClone(self):
        return firebird_ma_envelope_exhaustion_strategy()
