import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import TripleExponentialMovingAverage, SuperTrend
from StockSharp.Algo.Strategies import Strategy


class three_kilos_btc_15m_strategy(Strategy):
    """
    Three Kilos BTC 15m strategy.
    Three TEMAs (ShortPeriod, LongPeriod, Long2Period) with a Supertrend filter. A long opens when TEMA2 crosses above TEMA1 while
    TEMA2 is above TEMA3 and the Supertrend is up; a short opens when TEMA1 crosses above TEMA2 while TEMA2 is below TEMA3 and the
    Supertrend is down. Positions close by a percent take profit or stop loss.
    """

    def __init__(self):
        super(three_kilos_btc_15m_strategy, self).__init__()
        self._short_period = self.Param("ShortPeriod", 30).SetGreaterThanZero().SetDisplay("Short Period", "Period of TEMA1", "Indicators")
        self._long_period = self.Param("LongPeriod", 50).SetGreaterThanZero().SetDisplay("Long Period", "Period of TEMA2", "Indicators")
        self._long2_period = self.Param("Long2Period", 140).SetGreaterThanZero().SetDisplay("Long2 Period", "Period of TEMA3", "Indicators")
        self._atr_length = self.Param("AtrLength", 10).SetGreaterThanZero().SetDisplay("ATR Length", "ATR length of the Supertrend", "Supertrend")
        self._multiplier = self.Param("Multiplier", 2.0).SetGreaterThanZero().SetDisplay("Multiplier", "ATR multiplier of the Supertrend", "Supertrend")
        self._take_profit = self.Param("TakeProfit", 1.0).SetNotNegative().SetDisplay("Take Profit %", "Take profit percentage", "Risk")
        self._stop_loss = self.Param("StopLoss", 1.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_tema1 = None
        self._prev_tema2 = None

    def OnReseted(self):
        super(three_kilos_btc_15m_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(three_kilos_btc_15m_strategy, self).OnStarted2(time)

        self._reset_state()

        tema1 = TripleExponentialMovingAverage()
        tema1.Length = self._short_period.Value
        tema2 = TripleExponentialMovingAverage()
        tema2.Length = self._long_period.Value
        tema3 = TripleExponentialMovingAverage()
        tema3.Length = self._long2_period.Value
        super_trend = SuperTrend()
        super_trend.Length = self._atr_length.Value
        super_trend.Multiplier = Decimal(self._multiplier.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(tema1, tema2, tema3, super_trend, self._process_candle).Start()

        self.StartProtection(Unit(Decimal(self._take_profit.Value), UnitTypes.Percent), Unit(Decimal(self._stop_loss.Value), UnitTypes.Percent), useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, tema1)
            self.DrawIndicator(area, tema2)
            self.DrawIndicator(area, tema3)
            self.DrawIndicator(area, super_trend)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, tema1_value, tema2_value, tema3_value, super_trend_value):
        if candle.State != CandleStates.Finished:
            return

        if not tema1_value.IsFormed or not tema2_value.IsFormed or not tema3_value.IsFormed or not super_trend_value.IsFormed:
            return

        tema1 = tema1_value.GetValue[Decimal](None)
        tema2 = tema2_value.GetValue[Decimal](None)
        tema3 = tema3_value.GetValue[Decimal](None)
        is_up_trend = super_trend_value.IsUpTrend

        prev_tema1 = self._prev_tema1
        prev_tema2 = self._prev_tema2
        self._prev_tema1 = tema1
        self._prev_tema2 = tema2

        if prev_tema1 is None or prev_tema2 is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        tema2_crosses_up = prev_tema2 <= prev_tema1 and tema2 > tema1
        tema1_crosses_up = prev_tema1 <= prev_tema2 and tema1 > tema2

        if tema2_crosses_up and tema2 > tema3 and is_up_trend and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif tema1_crosses_up and tema2 < tema3 and not is_up_trend and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return three_kilos_btc_15m_strategy()
