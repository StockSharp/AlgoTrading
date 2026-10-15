import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SuperTrend, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class engulfing_with_trend_strategy(Strategy):
    """
    Engulfing with trend strategy.
    A bullish candle whose body engulfs the body of the preceding bearish candle goes long while SuperTrend is up; a bearish
    engulfing candle goes short while SuperTrend is down. The engulfing body must be at least EngulfingThreshold percent of its
    range and the engulfed candle must not be a boring candle (body below BoringThreshold percent of its range). The stop is the
    pattern extreme offset by one ATR and the target lies StopLevel percent of that risk away from the entry.
    """

    def __init__(self):
        super(engulfing_with_trend_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._atr_period = self.Param("AtrPeriod", 10).SetGreaterThanZero().SetDisplay("ATR Period", "ATR period of SuperTrend and the stop offset", "SuperTrend")
        self._atr_multiplier = self.Param("AtrMultiplier", 3.0).SetGreaterThanZero().SetDisplay("ATR Multiplier", "SuperTrend ATR multiplier", "SuperTrend")
        self._boring_threshold = self.Param("BoringThreshold", 25.0).SetNotNegative().SetDisplay("Boring Threshold %", "Body percentage of the range below which a candle is boring", "Pattern")
        self._engulfing_threshold = self.Param("EngulfingThreshold", 50.0).SetNotNegative().SetDisplay("Engulfing Threshold %", "Minimum body percentage of the engulfing candle", "Pattern")
        self._stop_level = self.Param("StopLevel", 200.0).SetGreaterThanZero().SetDisplay("Stop Level %", "Target distance in percent of the risk", "Risk")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev = None
        self._stop_price = 0.0
        self._take_price = 0.0

    def OnReseted(self):
        super(engulfing_with_trend_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(engulfing_with_trend_strategy, self).OnStarted2(time)

        self._reset_state()

        super_trend = SuperTrend()
        super_trend.Length = self._atr_period.Value
        super_trend.Multiplier = Decimal(float(self._atr_multiplier.Value))
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(super_trend, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, super_trend)
            self.DrawOwnTrades(area)

    @staticmethod
    def _body_percent(o, h, l, c):
        rng = h - l
        return abs(c - o) / rng * 100.0 if rng > 0 else 0.0

    def _process_candle(self, candle, super_trend_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        o = float(candle.OpenPrice)
        h = float(candle.HighPrice)
        l = float(candle.LowPrice)
        c = float(candle.ClosePrice)

        prev = self._prev
        self._prev = (o, h, l, c)

        if not super_trend_value.IsFormed or not atr_value.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position > 0:
            if l <= self._stop_price or h >= self._take_price:
                self.SellMarket(self.Position)
            return

        if self.Position < 0:
            if h >= self._stop_price or l <= self._take_price:
                self.BuyMarket(-self.Position)
            return

        if prev is None:
            return

        po, ph, pl, pc = prev
        is_up_trend = bool(super_trend_value.IsUpTrend)
        atr = float(atr_value.GetValue[Decimal](None))

        strong_body = self._body_percent(o, h, l, c) >= float(self._engulfing_threshold.Value)
        prev_not_boring = self._body_percent(po, ph, pl, pc) >= float(self._boring_threshold.Value)

        bullish_engulfing = pc < po and c > o and o <= pc and c >= po
        bearish_engulfing = pc > po and c < o and o >= pc and c <= po

        stop_level = float(self._stop_level.Value) / 100.0

        if is_up_trend and bullish_engulfing and strong_body and prev_not_boring:
            stop = min(l, pl) - atr
            risk = c - stop
            if risk <= 0:
                return
            self._stop_price = stop
            self._take_price = c + risk * stop_level
            self.BuyMarket(self.Volume)
        elif not is_up_trend and bearish_engulfing and strong_body and prev_not_boring:
            stop = max(h, ph) + atr
            risk = stop - c
            if risk <= 0:
                return
            self._stop_price = stop
            self._take_price = c - risk * stop_level
            self.SellMarket(self.Volume)

    def CreateClone(self):
        return engulfing_with_trend_strategy()
