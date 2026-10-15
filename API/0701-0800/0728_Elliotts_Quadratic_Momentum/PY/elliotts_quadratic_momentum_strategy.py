import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SuperTrend
from StockSharp.Algo.Strategies import Strategy


class elliotts_quadratic_momentum_strategy(Strategy):
    """
    Elliott's Quadratic Momentum strategy.
    Four SuperTrend indicators with different ATR lengths and multipliers vote on the trend. When all four are up the
    strategy goes long and when all four are down it goes short. A position closes as soon as any SuperTrend turns
    against it.
    """

    def __init__(self):
        super(elliotts_quadratic_momentum_strategy, self).__init__()
        self._atr_length1 = self.Param("AtrLength1", 7).SetGreaterThanZero().SetDisplay("ATR Length 1", "ATR length of the first SuperTrend", "SuperTrend 1")
        self._multiplier1 = self.Param("Multiplier1", 4.0).SetGreaterThanZero().SetDisplay("Multiplier 1", "Multiplier of the first SuperTrend", "SuperTrend 1")
        self._atr_length2 = self.Param("AtrLength2", 14).SetGreaterThanZero().SetDisplay("ATR Length 2", "ATR length of the second SuperTrend", "SuperTrend 2")
        self._multiplier2 = self.Param("Multiplier2", 3.618).SetGreaterThanZero().SetDisplay("Multiplier 2", "Multiplier of the second SuperTrend", "SuperTrend 2")
        self._atr_length3 = self.Param("AtrLength3", 21).SetGreaterThanZero().SetDisplay("ATR Length 3", "ATR length of the third SuperTrend", "SuperTrend 3")
        self._multiplier3 = self.Param("Multiplier3", 3.5).SetGreaterThanZero().SetDisplay("Multiplier 3", "Multiplier of the third SuperTrend", "SuperTrend 3")
        self._atr_length4 = self.Param("AtrLength4", 28).SetGreaterThanZero().SetDisplay("ATR Length 4", "ATR length of the fourth SuperTrend", "SuperTrend 4")
        self._multiplier4 = self.Param("Multiplier4", 3.382).SetGreaterThanZero().SetDisplay("Multiplier 4", "Multiplier of the fourth SuperTrend", "SuperTrend 4")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _create_supertrend(self, length, multiplier):
        st = SuperTrend()
        st.Length = length
        st.Multiplier = Decimal(multiplier)
        return st

    def OnStarted2(self, time):
        super(elliotts_quadratic_momentum_strategy, self).OnStarted2(time)

        st1 = self._create_supertrend(self._atr_length1.Value, self._multiplier1.Value)
        st2 = self._create_supertrend(self._atr_length2.Value, self._multiplier2.Value)
        st3 = self._create_supertrend(self._atr_length3.Value, self._multiplier3.Value)
        st4 = self._create_supertrend(self._atr_length4.Value, self._multiplier4.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(st1, st2, st3, st4, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, st1)
            self.DrawIndicator(area, st2)
            self.DrawIndicator(area, st3)
            self.DrawIndicator(area, st4)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, v1, v2, v3, v4):
        if candle.State != CandleStates.Finished:
            return

        if not v1.IsFormed or not v2.IsFormed or not v3.IsFormed or not v4.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        ups = [bool(v.IsUpTrend) for v in (v1, v2, v3, v4)]
        all_up = all(ups)
        all_down = not any(ups)

        if all_up and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif all_down and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and not all_up:
            self.SellMarket(self.Position)
        elif self.Position < 0 and not all_down:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return elliotts_quadratic_momentum_strategy()
