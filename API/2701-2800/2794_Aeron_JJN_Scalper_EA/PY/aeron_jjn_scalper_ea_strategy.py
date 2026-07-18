import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class aeron_jjn_scalper_ea_strategy(Strategy):

    def __init__(self):
        super(aeron_jjn_scalper_ea_strategy, self).__init__()
        self._atr_length = self.Param("AtrLength", 14)
        self._body_min_atr = self.Param("BodyMinAtr", 1.5)
        self._cooldown_bars = self.Param("CooldownBars", 10)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))

        self._prev_open = 0.0
        self._prev_close = 0.0
        self._has_prev = False
        self._cooldown = 0

    @property
    def AtrLength(self):
        return self._atr_length.Value

    @property
    def BodyMinAtr(self):
        return self._body_min_atr.Value

    @property
    def CooldownBars(self):
        return self._cooldown_bars.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(aeron_jjn_scalper_ea_strategy, self).OnStarted2(time)

        self._has_prev = False
        self._cooldown = 0

        atr = AverageTrueRange()
        atr.Length = self.AtrLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(atr, self._process_candle).Start()

        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(1, UnitTypes.Percent)
        )

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, atr_val):
        if candle.State != CandleStates.Finished:
            return

        atr_value = float(atr_val)

        if atr_value <= 0:
            self._save_prev(candle)
            return

        if self._cooldown > 0:
            self._cooldown -= 1

        if self._has_prev and self._cooldown == 0 and float(self.Position) == 0:
            prev_body = abs(self._prev_close - self._prev_open)
            min_body = atr_value * float(self.BodyMinAtr)

            if float(candle.ClosePrice) > float(candle.OpenPrice) and self._prev_close < self._prev_open and prev_body >= min_body:
                self.BuyMarket()
                self._cooldown = self.CooldownBars
            elif float(candle.ClosePrice) < float(candle.OpenPrice) and self._prev_close > self._prev_open and prev_body >= min_body:
                self.SellMarket()
                self._cooldown = self.CooldownBars

        self._save_prev(candle)

    def _save_prev(self, candle):
        self._prev_open = float(candle.OpenPrice)
        self._prev_close = float(candle.ClosePrice)
        self._has_prev = True

    def OnReseted(self):
        super(aeron_jjn_scalper_ea_strategy, self).OnReseted()
        self._has_prev = False
        self._cooldown = 0
        self._prev_open = 0.0
        self._prev_close = 0.0

    def CreateClone(self):
        return aeron_jjn_scalper_ea_strategy()
