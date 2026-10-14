import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

DOJI_BODY_PERCENT = 30.0
WICK_PERCENT = 1.0


class enhanced_doji_candle_strategy(Strategy):
    """
    Enhanced doji candle strategy.
    A doji has a body of at most 30% of its range. When flat, a doji goes long if it is bullish with a lower wick of at most 1% of
    its range or the previous candle was bullish, and short if it is bearish with an upper wick of at most 1% or the previous
    candle was bearish. While a position is open any new doji closes it. The stop lies StopLossPips price steps from the entry and
    the take profit RiskRewardRatio times that distance. The SMA is drawn for reference.
    """

    def __init__(self):
        super(enhanced_doji_candle_strategy, self).__init__()
        self._risk_reward_ratio = self.Param("RiskRewardRatio", 2.0).SetGreaterThanZero().SetDisplay("Risk Reward", "Take profit in multiples of the stop distance", "Risk")
        self._stop_loss_pips = self.Param("StopLossPips", 5).SetNotNegative().SetDisplay("Stop Loss Pips", "Stop loss in price steps", "Risk")
        self._sma_period = self.Param("SmaPeriod", 20).SetGreaterThanZero().SetDisplay("SMA Period", "SMA period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(enhanced_doji_candle_strategy, self).OnReseted()
        self._prev = None

    def OnStarted2(self, time):
        super(enhanced_doji_candle_strategy, self).OnStarted2(time)

        self._prev = None

        sma = SimpleMovingAverage()
        sma.Length = self._sma_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self._process_candle).Start()

        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        stop_distance = self._stop_loss_pips.Value * step
        rr = float(self._risk_reward_ratio.Value)
        self.StartProtection(
            Unit(Decimal(stop_distance * rr), UnitTypes.Absolute) if stop_distance > 0 else Unit(),
            Unit(Decimal(stop_distance), UnitTypes.Absolute) if stop_distance > 0 else Unit(),
            useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, sma_value):
        if candle.State != CandleStates.Finished:
            return

        o = float(candle.OpenPrice)
        h = float(candle.HighPrice)
        l = float(candle.LowPrice)
        c = float(candle.ClosePrice)

        prev = self._prev
        self._prev = (o, c)

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        rng = h - l
        if rng <= 0:
            return

        is_doji = abs(c - o) / rng * 100.0 <= DOJI_BODY_PERCENT
        if not is_doji:
            return

        if self.Position > 0:
            self.SellMarket(self.Position)
            return

        if self.Position < 0:
            self.BuyMarket(-self.Position)
            return

        lower_wick = (min(o, c) - l) / rng * 100.0
        upper_wick = (h - max(o, c)) / rng * 100.0
        prev_bullish = prev is not None and prev[1] > prev[0]
        prev_bearish = prev is not None and prev[1] < prev[0]

        if (c > o and lower_wick <= WICK_PERCENT) or prev_bullish:
            self.BuyMarket(self.Volume)
        elif (c < o and upper_wick <= WICK_PERCENT) or prev_bearish:
            self.SellMarket(self.Volume)

    def CreateClone(self):
        return enhanced_doji_candle_strategy()
