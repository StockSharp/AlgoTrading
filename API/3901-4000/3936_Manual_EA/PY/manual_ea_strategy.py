import clr
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import RelativeStrengthIndex, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class manual_ea_strategy(Strategy):
    def __init__(self):
        super(manual_ea_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 9).SetDisplay("RSI Period", "RSI lookback", "Indicators")
        self._ema_period = self.Param("EmaPeriod", 20).SetDisplay("EMA Period", "EMA filter", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candle timeframe", "General")
        self._prev_rsi = 0.0; self._has_prev = False; self._cooldown = 0

    @property
    def rsi_period(self): return self._rsi_period.Value
    @property
    def ema_period(self): return self._ema_period.Value
    @property
    def candle_type(self): return self._candle_type.Value

    def OnReseted(self):
        super(manual_ea_strategy, self).OnReseted()
        self._prev_rsi = 0.0; self._has_prev = False; self._cooldown = 0

    def OnStarted2(self, time):
        super(manual_ea_strategy, self).OnStarted2(time)
        self._has_prev = False; self._cooldown = 0
        rsi = RelativeStrengthIndex(); rsi.Length = self.rsi_period
        ema = ExponentialMovingAverage(); ema.Length = self.ema_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, ema, self.process_candle).Start()
        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(1, UnitTypes.Percent)
        )

    def process_candle(self, candle, rsi, ema):
        if candle.State != CandleStates.Finished: return
        close = float(candle.ClosePrice)
        rsi_val = float(rsi); ema_val = float(ema)
        if not self._has_prev:
            self._prev_rsi = rsi_val; self._has_prev = True; return
        if self._cooldown > 0:
            self._cooldown -= 1; self._prev_rsi = rsi_val; return
        if self._prev_rsi <= 20 and rsi_val > 20 and close > ema_val and self.Position == 0:
            self.BuyMarket(); self._cooldown = 2
        elif self._prev_rsi >= 80 and rsi_val < 80 and close < ema_val and self.Position == 0:
            self.SellMarket(); self._cooldown = 2
        self._prev_rsi = rsi_val

    def CreateClone(self): return manual_ea_strategy()
