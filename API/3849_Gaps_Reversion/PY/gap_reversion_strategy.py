import clr
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Unit, UnitTypes

class gap_reversion_strategy(Strategy):
    def __init__(self):
        super(gap_reversion_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 20).SetDisplay("EMA Period", "EMA trend filter", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candle timeframe", "General")
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._has_prev = False
    @property
    def ema_period(self): return self._ema_period.Value
    @property
    def candle_type(self): return self._candle_type.Value
    def OnReseted(self):
        super(gap_reversion_strategy, self).OnReseted()
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._has_prev = False
    def OnStarted(self, time):
        super(gap_reversion_strategy, self).OnStarted(time)
        self._has_prev = False
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, self.process_candle).Start()
        self.StartProtection(takeProfit=Unit(2, UnitTypes.Percent), stopLoss=Unit(1, UnitTypes.Percent))
    def process_candle(self, candle, ema):
        if candle.State != CandleStates.Finished:
            return
        if not self._has_prev:
            self._prev_high = float(candle.HighPrice)
            self._prev_low = float(candle.LowPrice)
            self._has_prev = True
            return
        op = float(candle.OpenPrice)
        close = float(candle.ClosePrice)
        ema_val = float(ema)
        if op < self._prev_low and close > ema_val and self.Position == 0:
            self.BuyMarket()
        elif op > self._prev_high and close < ema_val and self.Position == 0:
            self.SellMarket()
        self._prev_high = float(candle.HighPrice)
        self._prev_low = float(candle.LowPrice)
    def CreateClone(self):
        return gap_reversion_strategy()
