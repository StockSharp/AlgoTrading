import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex, ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

TREND_CROSS = 0
TREND_SLOPE = 1


class cci_support_resistance_strategy(Strategy):
    """
    CCI Support Resistance strategy.
    A CCI(CciLength) pivot low confirmed by LeftPivot lower values before it and RightPivot after it sets support at the low of its
    candle; a CCI pivot high sets resistance at the high of its candle. A candle whose low touches support (within Buffer price steps)
    and closes above it goes long, one whose high touches resistance and closes below it goes short, reversing an opposite position.
    With TrendMatter the trend must agree: in Cross mode EMA(FastMaLength) against EMA(SlowMaLength), in Slope mode the change of the
    slow EMA over SlopeLength candles. Each entry freezes a stop Ksl ATRs and a target Ktp ATRs away.
    """

    def __init__(self):
        super(cci_support_resistance_strategy, self).__init__()
        self._cci_length = self.Param("CciLength", 50).SetGreaterThanZero().SetDisplay("CCI Length", "CCI period", "CCI")
        self._left_pivot = self.Param("LeftPivot", 50).SetGreaterThanZero().SetDisplay("Left Pivot", "CCI values before a pivot", "CCI")
        self._right_pivot = self.Param("RightPivot", 50).SetGreaterThanZero().SetDisplay("Right Pivot", "CCI values after a pivot", "CCI")
        self._buffer = self.Param("Buffer", 10.0).SetNotNegative().SetDisplay("Buffer", "Touch tolerance in price steps", "Levels")
        self._trend_matter = self.Param("TrendMatter", True).SetDisplay("Trend Matter", "Require the trend filter", "Trend")
        self._trend_type = self.Param("TrendType", TREND_CROSS).SetDisplay("Trend Type", "Trend filter type (0 = Cross, 1 = Slope)", "Trend")
        self._slow_ma_length = self.Param("SlowMaLength", 100).SetGreaterThanZero().SetDisplay("Slow MA Length", "Slow EMA period", "Trend")
        self._fast_ma_length = self.Param("FastMaLength", 50).SetGreaterThanZero().SetDisplay("Fast MA Length", "Fast EMA period", "Trend")
        self._slope_length = self.Param("SlopeLength", 5).SetGreaterThanZero().SetDisplay("Slope Length", "Candles the slow EMA slope spans", "Trend")
        self._ksl = self.Param("Ksl", 1.1).SetGreaterThanZero().SetDisplay("Ksl", "ATR multiple of the stop", "Risk")
        self._ktp = self.Param("Ktp", 2.2).SetGreaterThanZero().SetDisplay("Ktp", "ATR multiple of the target", "Risk")
        self._atr_length = self.Param("AtrLength", 14).SetGreaterThanZero().SetDisplay("ATR Length", "ATR period", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._history = []
        self._slow_history = []
        self._support = None
        self._resistance = None
        self._stop_price = None
        self._target_price = None

    def OnReseted(self):
        super(cci_support_resistance_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(cci_support_resistance_strategy, self).OnStarted2(time)

        self._reset_state()

        cci = CommodityChannelIndex()
        cci.Length = self._cci_length.Value
        fast_ma = ExponentialMovingAverage()
        fast_ma.Length = self._fast_ma_length.Value
        slow_ma = ExponentialMovingAverage()
        slow_ma.Length = self._slow_ma_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(cci, fast_ma, slow_ma, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ma)
            self.DrawIndicator(area, slow_ma)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, cci)

    def _update_levels(self, cci, candle):
        self._history.append((cci, candle.HighPrice, candle.LowPrice))

        left = self._left_pivot.Value
        size = left + self._right_pivot.Value + 1

        if len(self._history) > size:
            self._history.pop(0)

        if len(self._history) < size:
            return

        pivot = self._history[left]
        is_high = True
        is_low = True

        for i in range(size):
            if not is_high and not is_low:
                break
            if i == left:
                continue
            value = self._history[i][0]
            if value >= pivot[0]:
                is_high = False
            if value <= pivot[0]:
                is_low = False

        if is_high:
            self._resistance = pivot[1]
        if is_low:
            self._support = pivot[2]

    def _process_candle(self, candle, cci_value, fast_value, slow_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if cci_value.IsFormed:
            self._update_levels(cci_value.GetValue[Decimal](None), candle)

        if not slow_value.IsFormed:
            return

        slow = slow_value.GetValue[Decimal](None)
        slope_length = self._slope_length.Value
        self._slow_history.append(slow)

        if len(self._slow_history) > slope_length + 1:
            self._slow_history.pop(0)

        if not fast_value.IsFormed or not atr_value.IsFormed or len(self._slow_history) <= slope_length:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Stop and target frozen at entry.
        if self.Position > 0 and self._stop_price is not None and self._target_price is not None and (candle.LowPrice <= self._stop_price or candle.HighPrice >= self._target_price):
            self.SellMarket(self.Position)
            self._stop_price = None
            self._target_price = None
            return

        if self.Position < 0 and self._stop_price is not None and self._target_price is not None and (candle.HighPrice >= self._stop_price or candle.LowPrice <= self._target_price):
            self.BuyMarket(-self.Position)
            self._stop_price = None
            self._target_price = None
            return

        if not self._trend_matter.Value:
            bullish = True
            bearish = True
        elif self._trend_type.Value == TREND_CROSS:
            fast = fast_value.GetValue[Decimal](None)
            bullish = fast > slow
            bearish = fast < slow
        else:
            slope = slow - self._slow_history[0]
            bullish = slope > 0
            bearish = slope < 0

        step = self.Security.PriceStep if self.Security is not None and self.Security.PriceStep is not None else Decimal(1)
        tolerance = Decimal(self._buffer.Value) * step
        close = candle.ClosePrice
        atr = atr_value.GetValue[Decimal](None)
        ksl = Decimal(self._ksl.Value)
        ktp = Decimal(self._ktp.Value)
        support = self._support
        resistance = self._resistance

        if bullish and support is not None and candle.LowPrice <= support + tolerance and close > support and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._stop_price = close - atr * ksl
            self._target_price = close + atr * ktp
        elif bearish and resistance is not None and candle.HighPrice >= resistance - tolerance and close < resistance and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._stop_price = close + atr * ksl
            self._target_price = close - atr * ktp

    def CreateClone(self):
        return cci_support_resistance_strategy()
