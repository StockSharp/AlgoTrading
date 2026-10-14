import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Ichimoku, HullMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

PRICE_OPEN = 0
PRICE_HIGH = 1
PRICE_LOW = 2
PRICE_CLOSE = 3

class ichimoku_daily_candle_x_hull_ma_x_macd_strategy(Strategy):
    """
    Ichimoku daily candle X Hull MA X MACD strategy.
    A Hull moving average of HmaPeriod and an HMA-based MACD (HMA(MacdFastLength) - HMA(MacdSlowLength), signal HMA(MacdSignalLength))
    are computed on the PriceSource price. A long requires a rising HMA, the close above the previous HMA value, the current daily
    candle's PriceSource price above the previous day's, Senkou A above Senkou B and the MACD line above its signal; a short requires
    all of them reversed. The opposite signal reverses the position; there are no stops.
    """

    def __init__(self):
        super(ichimoku_daily_candle_x_hull_ma_x_macd_strategy, self).__init__()
        self._hma_period = self.Param("HmaPeriod", 14).SetGreaterThanZero().SetDisplay("HMA Period", "Hull moving average period", "Indicators")
        self._conversion_period = self.Param("ConversionPeriod", 9).SetGreaterThanZero().SetDisplay("Conversion Period", "Ichimoku conversion line period", "Ichimoku")
        self._base_period = self.Param("BasePeriod", 26).SetGreaterThanZero().SetDisplay("Base Period", "Ichimoku base line period", "Ichimoku")
        self._span_period = self.Param("SpanPeriod", 52).SetGreaterThanZero().SetDisplay("Span Period", "Ichimoku leading span B period", "Ichimoku")
        self._macd_fast_length = self.Param("MacdFastLength", 12).SetGreaterThanZero().SetDisplay("MACD Fast", "Fast HMA period of the MACD", "MACD")
        self._macd_slow_length = self.Param("MacdSlowLength", 26).SetGreaterThanZero().SetDisplay("MACD Slow", "Slow HMA period of the MACD", "MACD")
        self._macd_signal_length = self.Param("MacdSignalLength", 9).SetGreaterThanZero().SetDisplay("MACD Signal", "Signal HMA period of the MACD", "MACD")
        self._price_source = self.Param("PriceSource", PRICE_OPEN).SetDisplay("Price Source", "0 Open, 1 High, 2 Low, 3 Close", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._hma = None
        self._macd_fast = None
        self._macd_slow = None
        self._macd_signal = None
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_hma = None
        self._current_day = None
        self._current_day_price = None
        self._previous_day_price = None

    def OnReseted(self):
        super(ichimoku_daily_candle_x_hull_ma_x_macd_strategy, self).OnReseted()
        self._reset_state()

    @staticmethod
    def _make_hma(length):
        hma = HullMovingAverage()
        hma.Length = length
        return hma

    def OnStarted2(self, time):
        super(ichimoku_daily_candle_x_hull_ma_x_macd_strategy, self).OnStarted2(time)

        self._reset_state()

        ichimoku = Ichimoku()
        ichimoku.Tenkan.Length = int(self._conversion_period.Value)
        ichimoku.Kijun.Length = int(self._base_period.Value)
        ichimoku.SenkouB.Length = int(self._span_period.Value)

        self._hma = self._make_hma(int(self._hma_period.Value))
        self._macd_fast = self._make_hma(int(self._macd_fast_length.Value))
        self._macd_slow = self._make_hma(int(self._macd_slow_length.Value))
        self._macd_signal = self._make_hma(int(self._macd_signal_length.Value))

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(ichimoku, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ichimoku)
            self.DrawIndicator(area, self._hma)
            self.DrawOwnTrades(area)

    def _get_price(self, candle):
        source = int(self._price_source.Value)
        if source == PRICE_HIGH:
            return candle.HighPrice
        if source == PRICE_LOW:
            return candle.LowPrice
        if source == PRICE_CLOSE:
            return candle.ClosePrice
        return candle.OpenPrice

    def _update_daily(self, candle):
        day = candle.OpenTime.Date

        if self._current_day != day:
            if self._current_day is not None:
                self._previous_day_price = self._current_day_price
            self._current_day = day
            self._current_day_price = self._get_price(candle)
            return

        # The daily candle is built from the intraday candles of the same day.
        source = int(self._price_source.Value)
        if source == PRICE_CLOSE:
            self._current_day_price = candle.ClosePrice
        elif source == PRICE_HIGH:
            self._current_day_price = max(self._current_day_price, candle.HighPrice)
        elif source == PRICE_LOW:
            self._current_day_price = min(self._current_day_price, candle.LowPrice)

    @staticmethod
    def _process(indicator, value, time):
        result = process_value(indicator, value, time, True)
        if indicator.IsFormed and not result.IsEmpty:
            return to_decimal(result)
        return None

    def _process_candle(self, candle, ichimoku_value):
        if candle.State != CandleStates.Finished:
            return

        self._update_daily(candle)

        price = self._get_price(candle)
        t = candle.OpenTime

        hma = self._process(self._hma, price, t)
        fast = self._process(self._macd_fast, price, t)
        slow = self._process(self._macd_slow, price, t)

        macd = fast - slow if fast is not None and slow is not None else None
        signal = self._process(self._macd_signal, macd, t) if macd is not None else None

        if hma is None:
            return

        prev_hma = self._prev_hma
        self._prev_hma = hma

        if prev_hma is None or macd is None or signal is None:
            return

        if not ichimoku_value.IsFormed or ichimoku_value.SenkouA is None or ichimoku_value.SenkouB is None:
            return

        if self._previous_day_price is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        senkou_a = ichimoku_value.SenkouA
        senkou_b = ichimoku_value.SenkouB
        close = candle.ClosePrice
        day_price = self._current_day_price
        prev_day_price = self._previous_day_price

        long_signal = hma > prev_hma and close > prev_hma and day_price > prev_day_price and senkou_a > senkou_b and macd > signal
        short_signal = hma < prev_hma and close < prev_hma and day_price < prev_day_price and senkou_a < senkou_b and macd < signal

        if long_signal and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif short_signal and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return ichimoku_daily_candle_x_hull_ma_x_macd_strategy()
