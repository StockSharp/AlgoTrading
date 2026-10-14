import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, SimpleMovingAverage, Highest, Lowest
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

TREND_LENGTH = 50
RANGE_LENGTH = 20
STRONG_BODY_RATIO = 0.6
CONSOLIDATION_ATR_MULTIPLIER = 2.0
TRAIL_ATR_MULTIPLIER = 2.0


class eliora_gold_1m_heikin_ashi_strategy(Strategy):
    """
    Eliora Gold 1m Heikin Ashi strategy.
    Heikin Ashi candles are built from the one-minute candles. A strong bullish Heikin Ashi candle (body at least
    60% of its range) closing above the trend SMA goes long and a strong bearish one closing below it goes short, when
    the market is not consolidating (the recent high-low range is wider than a multiple of ATR), volatility is
    expanding (ATR above its own average) and at least CooldownBars candles passed since the last trade.
    Positions are exited only by an ATR trailing stop.
    """

    def __init__(self):
        super(eliora_gold_1m_heikin_ashi_strategy, self).__init__()
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero().SetDisplay("ATR Period", "ATR period for filters and the trailing stop", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 5).SetNotNegative().SetDisplay("Cooldown Bars", "Candles to wait after a trade", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._highest = None
        self._lowest = None
        self._atr_average = None
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._ha_open = None
        self._ha_close = None
        self._trail_stop = None
        self._bars_since_trade = None

    def OnReseted(self):
        super(eliora_gold_1m_heikin_ashi_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(eliora_gold_1m_heikin_ashi_strategy, self).OnStarted2(time)

        self._reset_state()

        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value
        trend = SimpleMovingAverage()
        trend.Length = TREND_LENGTH
        self._highest = Highest()
        self._highest.Length = RANGE_LENGTH
        self._lowest = Lowest()
        self._lowest.Length = RANGE_LENGTH
        self._atr_average = SimpleMovingAverage()
        self._atr_average.Length = self._atr_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(atr, trend, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, trend)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, atr_value, trend_value):
        if candle.State != CandleStates.Finished:
            return

        o = float(candle.OpenPrice)
        h = float(candle.HighPrice)
        l = float(candle.LowPrice)
        c = float(candle.ClosePrice)

        ha_close = (o + h + l + c) / 4.0
        if self._ha_open is not None and self._ha_close is not None:
            ha_open = (self._ha_open + self._ha_close) / 2.0
        else:
            ha_open = (o + c) / 2.0
        ha_high = max(h, ha_open, ha_close)
        ha_low = min(l, ha_open, ha_close)
        self._ha_open = ha_open
        self._ha_close = ha_close

        high_result = process_float(self._highest, candle.HighPrice, candle.OpenTime, True)
        low_result = process_float(self._lowest, candle.LowPrice, candle.OpenTime, True)
        atr_avg_result = process_float(self._atr_average, atr_value, candle.OpenTime, True)

        if self._bars_since_trade is not None:
            self._bars_since_trade += 1

        if not high_result.IsFormed or not low_result.IsFormed or not atr_avg_result.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        atr = float(atr_value)
        trend = float(trend_value)
        trail_distance = atr * TRAIL_ATR_MULTIPLIER

        if self.Position > 0:
            candidate = c - trail_distance
            self._trail_stop = candidate if self._trail_stop is None else max(self._trail_stop, candidate)
            if l <= self._trail_stop:
                self.SellMarket(self.Position)
                self._trail_stop = None
                self._bars_since_trade = 0
            return

        if self.Position < 0:
            candidate = c + trail_distance
            self._trail_stop = candidate if self._trail_stop is None else min(self._trail_stop, candidate)
            if h >= self._trail_stop:
                self.BuyMarket(-self.Position)
                self._trail_stop = None
                self._bars_since_trade = 0
            return

        if self._bars_since_trade is not None and self._bars_since_trade < self._cooldown_bars.Value:
            return

        price_range = float(high_result) - float(low_result)
        consolidating = price_range < atr * CONSOLIDATION_ATR_MULTIPLIER
        expanding = atr > float(atr_avg_result)

        if consolidating or not expanding:
            return

        ha_range = ha_high - ha_low
        body = abs(ha_close - ha_open)
        strong = ha_range > 0 and body >= ha_range * STRONG_BODY_RATIO

        if not strong:
            return

        if ha_close > ha_open and ha_close > trend:
            self.BuyMarket(self.Volume)
            self._trail_stop = c - trail_distance
            self._bars_since_trade = 0
        elif ha_close < ha_open and ha_close < trend:
            self.SellMarket(self.Volume)
            self._trail_stop = c + trail_distance
            self._bars_since_trade = 0

    def CreateClone(self):
        return eliora_gold_1m_heikin_ashi_strategy()
