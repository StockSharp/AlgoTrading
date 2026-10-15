import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class gold_orb_strategy(Strategy):
    """
    Gold opening range breakout strategy.
    The high and low of the candles opening between AsiaStart and AsiaEnd (UTC) form the Asia range of the day. Between TradeStart and
    TradeEnd a close above the range high goes long and a close below the range low goes short. The stop is one range size away from the
    entry and the target RewardMultiplier range sizes away.
    """

    def __init__(self):
        super(gold_orb_strategy, self).__init__()
        self._asia_start = self.Param("AsiaStart", TimeSpan.Zero).SetDisplay("Asia Start", "Start of the Asia session (UTC)", "Session")
        self._asia_end = self.Param("AsiaEnd", TimeSpan.FromHours(6)).SetDisplay("Asia End", "End of the Asia session (UTC)", "Session")
        self._trade_start = self.Param("TradeStart", TimeSpan.FromHours(6)).SetDisplay("Trade Start", "Start of the trade window (UTC)", "Session")
        self._trade_end = self.Param("TradeEnd", TimeSpan.FromHours(10)).SetDisplay("Trade End", "End of the trade window (UTC)", "Session")
        self._reward_multiplier = self.Param("RewardMultiplier", 2.0).SetGreaterThanZero().SetDisplay("Reward Multiplier", "Target distance in range sizes", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._range_day = None
        self._asia_high = None
        self._asia_low = None
        self._stop_price = Decimal(0)
        self._take_price = Decimal(0)

    def OnReseted(self):
        super(gold_orb_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(gold_orb_strategy, self).OnStarted2(time)

        self._reset_state()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        day = candle.OpenTime.Date
        time_of_day = candle.OpenTime.TimeOfDay

        if self._range_day is None or self._range_day != day:
            self._range_day = day
            self._asia_high = None
            self._asia_low = None

        if self._asia_start.Value <= time_of_day < self._asia_end.Value:
            self._asia_high = candle.HighPrice if self._asia_high is None else Math.Max(self._asia_high, candle.HighPrice)
            self._asia_low = candle.LowPrice if self._asia_low is None else Math.Min(self._asia_low, candle.LowPrice)

        if self._manage_position(candle):
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position != 0 or time_of_day < self._trade_start.Value or time_of_day >= self._trade_end.Value:
            return

        if self._asia_high is None or self._asia_low is None:
            return

        high = self._asia_high
        low = self._asia_low
        rng = high - low
        if rng <= 0:
            return

        close = candle.ClosePrice
        multiplier = Decimal(self._reward_multiplier.Value)

        if close > high:
            self.BuyMarket(self.Volume)
            self._stop_price = close - rng
            self._take_price = close + rng * multiplier
        elif close < low:
            self.SellMarket(self.Volume)
            self._stop_price = close + rng
            self._take_price = close - rng * multiplier

    def _manage_position(self, candle):
        if self.Position > 0 and self._stop_price > 0:
            if candle.LowPrice <= self._stop_price or candle.HighPrice >= self._take_price:
                self.SellMarket(self.Position)
                return True
        elif self.Position < 0 and self._stop_price > 0:
            if candle.HighPrice >= self._stop_price or candle.LowPrice <= self._take_price:
                self.BuyMarket(abs(self.Position))
                return True
        return False

    def CreateClone(self):
        return gold_orb_strategy()
