import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import (RelativeStrengthIndex, RateOfChange, AverageTrueRange, SimpleMovingAverage, ExponentialMovingAverage,
                                        WeightedMovingAverage, DoubleExponentialMovingAverage, TripleExponentialMovingAverage,
                                        HullMovingAverage, SmoothedMovingAverage)
from StockSharp.Algo.Strategies import Strategy


class long_and_short_with_multi_indicators_strategy(Strategy):
    """
    Long and short strategy with RSI, ROC and a selectable moving average.
    Goes long when RSI is between the oversold and overbought levels, ROC is positive and price is above the MA. Goes short when a bearish
    trend is confirmed (close below the bearish SMA for BearishTrendDuration bars), ROC is negative and price is below the MA.
    Positions exit on an ATR trailing stop, a long when RSI rises above overbought and a short when RSI falls below oversold.
    MaTypeParam: SMA, EMA, WMA, DEMA, TEMA, HMA or SMMA.
    """

    def __init__(self):
        super(long_and_short_with_multi_indicators_strategy, self).__init__()
        self._rsi_length = self.Param("RsiLength", 5).SetGreaterThanZero().SetDisplay("RSI Length", "RSI period", "Indicators")
        self._rsi_overbought = self.Param("RsiOverbought", 70.0).SetDisplay("RSI Overbought", "RSI overbought level", "Indicators")
        self._rsi_oversold = self.Param("RsiOversold", 44.0).SetDisplay("RSI Oversold", "RSI oversold level", "Indicators")
        self._roc_length = self.Param("RocLength", 4).SetGreaterThanZero().SetDisplay("ROC Length", "ROC period", "Indicators")
        self._ma_length = self.Param("MaLength", 24).SetGreaterThanZero().SetDisplay("MA Length", "Moving average period", "Indicators")
        self._ma_type = self.Param("MaTypeParam", "TEMA").SetDisplay("MA Type", "Moving average type", "Indicators")
        self._atr_length = self.Param("AtrLength", 14).SetGreaterThanZero().SetDisplay("ATR Length", "ATR period", "Risk")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0).SetNotNegative().SetDisplay("ATR Multiplier", "ATR multiplier of the trailing stop", "Risk")
        self._bearish_ma_length = self.Param("BearishMaLength", 200).SetGreaterThanZero().SetDisplay("Bearish MA Length", "SMA period that defines the bearish trend", "Trend")
        self._bearish_trend_duration = self.Param("BearishTrendDuration", 5).SetGreaterThanZero().SetDisplay("Bearish Duration", "Consecutive closes below the bearish SMA", "Trend")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._bearish_bars = 0
        self._trail_price = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(long_and_short_with_multi_indicators_strategy, self).OnReseted()
        self._bearish_bars = 0
        self._trail_price = None

    def _create_ma(self, ma_type, length):
        types = {
            "SMA": SimpleMovingAverage,
            "EMA": ExponentialMovingAverage,
            "WMA": WeightedMovingAverage,
            "DEMA": DoubleExponentialMovingAverage,
            "HMA": HullMovingAverage,
            "SMMA": SmoothedMovingAverage,
        }
        ma = types.get(str(ma_type), TripleExponentialMovingAverage)()
        ma.Length = length
        return ma

    def OnStarted2(self, time):
        super(long_and_short_with_multi_indicators_strategy, self).OnStarted2(time)

        self._bearish_bars = 0
        self._trail_price = None

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        roc = RateOfChange()
        roc.Length = self._roc_length.Value
        ma = self._create_ma(self._ma_type.Value, self._ma_length.Value)
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value
        bearish_ma = SimpleMovingAverage()
        bearish_ma.Length = self._bearish_ma_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(rsi, roc, ma, atr, bearish_ma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma)
            self.DrawIndicator(area, bearish_ma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, rsi_value, roc_value, ma_value, atr_value, bearish_ma_value):
        if candle.State != CandleStates.Finished:
            return

        close = candle.ClosePrice

        if bearish_ma_value.IsFormed:
            self._bearish_bars = self._bearish_bars + 1 if close < bearish_ma_value.GetValue[Decimal](None) else 0

        if not rsi_value.IsFormed or not roc_value.IsFormed or not ma_value.IsFormed or not atr_value.IsFormed:
            return

        rsi = rsi_value.GetValue[Decimal](None)
        roc = roc_value.GetValue[Decimal](None)
        ma = ma_value.GetValue[Decimal](None)
        atr = atr_value.GetValue[Decimal](None)
        mult = Decimal(self._atr_multiplier.Value)
        overbought = Decimal(self._rsi_overbought.Value)
        oversold = Decimal(self._rsi_oversold.Value)

        if self.Position > 0:
            if mult > 0:
                level = close - atr * mult
                self._trail_price = level if self._trail_price is None else max(self._trail_price, level)
            if (self._trail_price is not None and candle.LowPrice <= self._trail_price) or rsi > overbought:
                self.SellMarket(self.Position)
                self._trail_price = None
                return
        elif self.Position < 0:
            if mult > 0:
                level = close + atr * mult
                self._trail_price = level if self._trail_price is None else min(self._trail_price, level)
            if (self._trail_price is not None and candle.HighPrice >= self._trail_price) or rsi < oversold:
                self.BuyMarket(-self.Position)
                self._trail_price = None
                return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        long_signal = rsi > oversold and rsi < overbought and roc > 0 and close > ma
        short_signal = self._bearish_bars >= self._bearish_trend_duration.Value and roc < 0 and close < ma

        if long_signal and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._trail_price = None
        elif short_signal and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._trail_price = None

    def CreateClone(self):
        return long_and_short_with_multi_indicators_strategy()
