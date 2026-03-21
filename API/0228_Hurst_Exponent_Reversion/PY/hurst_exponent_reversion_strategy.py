import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class hurst_exponent_reversion_strategy(Strategy):
    """
    Hurst Exponent Reversion: SMA mean reversion with stop protection.
    Price below SMA = buy, price above SMA = sell.
    """

    def __init__(self):
        super(hurst_exponent_reversion_strategy, self).__init__()
        self._ma_period = self.Param("AveragePeriod", 20).SetDisplay("MA Period", "SMA period", "Indicators")
        self._sl_pct = self.Param("StopLossPercent", 2.0).SetDisplay("SL %", "Stop loss percent", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(hurst_exponent_reversion_strategy, self).OnReseted()

    def OnStarted(self, time):
        super(hurst_exponent_reversion_strategy, self).OnStarted(time)
        sma = SimpleMovingAverage()
        sma.Length = self._ma_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self._process_candle).Start()
        sl_pct = self._sl_pct.Value
        self.StartProtection(Unit(sl_pct, UnitTypes.Percent), Unit(sl_pct * 1.5, UnitTypes.Percent))
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, sma_val):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        sma = float(sma_val)
        if close < sma and self.Position <= 0:
            self.BuyMarket()
        elif close > sma and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return hurst_exponent_reversion_strategy()
