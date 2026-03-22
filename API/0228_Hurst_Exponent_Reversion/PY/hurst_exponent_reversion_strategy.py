import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Random, Math, Int32
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

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = float(candle.ClosePrice)
        sma = float(sma_val)

        # Replicate CS CalculateSimplifiedHurst
        # C# (int)long truncates to low 32 bits with sign
        ticks = int(candle.OpenTime.Ticks)
        ticks_masked = ticks & 0xFFFFFFFF
        if ticks_masked >= 0x80000000:
            ticks_masked -= 0x100000000
        rand = Random(Int32(ticks_masked))
        hurst_value = 0.3 + rand.NextDouble() * 0.4

        if hurst_value < 0.5:
            if close < sma and self.Position <= 0:
                self.BuyMarket(self.Volume)
            elif close > sma and self.Position >= 0:
                self.SellMarket(self.Volume + Math.Abs(self.Position))

    def CreateClone(self):
        return hurst_exponent_reversion_strategy()
