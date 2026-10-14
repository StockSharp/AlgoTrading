import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, HullMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class rsi_hull_ma_strategy(Strategy):
    """
    RSI Hull MA strategy.
    RSI below RsiOversold with a rising HullPeriod Hull moving average goes long and RSI above RsiOverbought with a falling one goes short,
    reversing an opposite position. A long closes once RSI returns to the neutral 50 level and a short likewise. The stop lies
    AtrMultiplier ATR from the entry close and is checked on candle closes.
    """

    def __init__(self):
        super(rsi_hull_ma_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 14).SetGreaterThanZero().SetDisplay("RSI Period", "Period of RSI", "Indicators")
        self._rsi_oversold = self.Param("RsiOversold", 30.0).SetDisplay("RSI Oversold", "RSI level for longs", "Indicators")
        self._rsi_overbought = self.Param("RsiOverbought", 70.0).SetDisplay("RSI Overbought", "RSI level for shorts", "Indicators")
        self._hull_period = self.Param("HullPeriod", 9).SetGreaterThanZero().SetDisplay("Hull Period", "Period of the Hull moving average", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero().SetDisplay("ATR Period", "Period of the stop ATR", "Risk")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0).SetNotNegative().SetDisplay("ATR Multiplier", "Stop distance from the entry in ATRs", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_hull = None
        self._stop_price = Decimal(0)

    def OnReseted(self):
        super(rsi_hull_ma_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(rsi_hull_ma_strategy, self).OnStarted2(time)

        self._reset_state()

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value
        hull = HullMovingAverage()
        hull.Length = self._hull_period.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(rsi, hull, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, hull)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, rsi)

    def _process_candle(self, candle, rsi_value, hull_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not hull_value.IsFormed:
            return

        hull = hull_value.GetValue[Decimal](None)
        previous = self._prev_hull
        self._prev_hull = hull

        if previous is None or not rsi_value.IsFormed or not atr_value.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        rsi = rsi_value.GetValue[Decimal](None)
        atr = atr_value.GetValue[Decimal](None)
        close = candle.ClosePrice

        # RSI 50 is the middle of the neutral zone.
        stop_atr = Decimal(self._atr_multiplier.Value)
        neutral = Decimal(50)
        if rsi < Decimal(self._rsi_oversold.Value) and hull > previous and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._stop_price = close - stop_atr * atr
        elif rsi > Decimal(self._rsi_overbought.Value) and hull < previous and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._stop_price = close + stop_atr * atr
        elif self.Position > 0 and (rsi >= neutral or (stop_atr > 0 and close <= self._stop_price)):
            self.SellMarket(self.Position)
        elif self.Position < 0 and (rsi <= neutral or (stop_atr > 0 and close >= self._stop_price)):
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return rsi_hull_ma_strategy()
