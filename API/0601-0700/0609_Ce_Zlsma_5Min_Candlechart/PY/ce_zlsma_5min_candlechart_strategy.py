import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


def _linear_regression(values, value, length):
    values.append(value)
    if len(values) > length:
        values.pop(0)
    if len(values) < length:
        return None

    # Least squares line over the window, evaluated at its last point.
    sum_x = Decimal(0)
    sum_y = Decimal(0)
    sum_xy = Decimal(0)
    sum_xx = Decimal(0)
    for i in range(length):
        y = values[i]
        x = Decimal(i)
        sum_x += x
        sum_y += y
        sum_xy += x * y
        sum_xx += x * x

    n = Decimal(length)
    denominator = n * sum_xx - sum_x * sum_x
    if denominator == 0:
        return values[-1]

    slope = (n * sum_xy - sum_x * sum_y) / denominator
    intercept = (sum_y - slope * sum_x) / n
    return intercept + slope * Decimal(length - 1)


class ce_zlsma_5min_candlechart_strategy(Strategy):
    """
    CE ZLSMA 5MIN Candlechart strategy.
    Everything is computed on Heikin Ashi candles. The Chandelier Exit trails AtrMultiplier times ATR(AtrPeriod) below the highest close
    and above the lowest close of the last AtrPeriod candles, and its direction turns up when the close rises above the short stop.
    The zero lag LSMA is twice the ZlsmaLength linear regression of the close minus the regression of that regression. Long only:
    buys when the direction turns up and the Heikin Ashi close is above both the ZLSMA and its open, and closes the long when the
    Heikin Ashi close falls below the ZLSMA.
    """

    def __init__(self):
        super(ce_zlsma_5min_candlechart_strategy, self).__init__()
        self._zlsma_length = self.Param("ZlsmaLength", 50).SetGreaterThanZero().SetDisplay("ZLSMA Length", "ZLSMA regression length", "ZLSMA")
        self._atr_period = self.Param("AtrPeriod", 1).SetGreaterThanZero().SetDisplay("ATR Period", "ATR period and Chandelier lookback", "Chandelier Exit")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0).SetGreaterThanZero().SetDisplay("ATR Multiplier", "ATR multiplier of the Chandelier Exit", "Chandelier Exit")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._closes = []
        self._lsmas = []
        self._recent_closes = []
        self._ha_open = None
        self._ha_close = None
        self._prev_ha_close = None
        self._atr = None
        self._atr_count = 0
        self._long_stop = None
        self._short_stop = None
        self._direction = 1

    def OnReseted(self):
        super(ce_zlsma_5min_candlechart_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(ce_zlsma_5min_candlechart_strategy, self).OnStarted2(time)

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

        ha_close = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / Decimal(4)
        if self._ha_open is not None and self._ha_close is not None:
            ha_open = (self._ha_open + self._ha_close) / Decimal(2)
        else:
            ha_open = (candle.OpenPrice + candle.ClosePrice) / Decimal(2)
        ha_high = max(candle.HighPrice, ha_open, ha_close)
        ha_low = min(candle.LowPrice, ha_open, ha_close)
        prev_ha_close = self._prev_ha_close
        self._ha_open = ha_open
        self._ha_close = ha_close
        self._prev_ha_close = ha_close

        # Wilder ATR of the Heikin Ashi candles.
        if prev_ha_close is not None:
            true_range = max(ha_high - ha_low, abs(ha_high - prev_ha_close), abs(ha_low - prev_ha_close))
        else:
            true_range = ha_high - ha_low
        period = self._atr_period.Value
        self._atr_count = min(self._atr_count + 1, period)
        if self._atr is not None:
            self._atr = (self._atr * Decimal(self._atr_count - 1) + true_range) / Decimal(self._atr_count)
        else:
            self._atr = true_range

        length = self._zlsma_length.Value
        lsma = _linear_regression(self._closes, ha_close, length)
        zlsma = None
        if lsma is not None:
            lsma2 = _linear_regression(self._lsmas, lsma, length)
            if lsma2 is not None:
                zlsma = lsma + (lsma - lsma2)

        self._recent_closes.append(ha_close)
        if len(self._recent_closes) > period:
            self._recent_closes.pop(0)

        if len(self._recent_closes) < period or prev_ha_close is None:
            return

        recent = self._recent_closes
        distance = Decimal(self._atr_multiplier.Value) * self._atr
        long_stop = max(recent) - distance
        short_stop = min(recent) + distance
        prev_long_stop = self._long_stop if self._long_stop is not None else long_stop
        prev_short_stop = self._short_stop if self._short_stop is not None else short_stop

        if prev_ha_close > prev_long_stop:
            long_stop = max(long_stop, prev_long_stop)
        if prev_ha_close < prev_short_stop:
            short_stop = min(short_stop, prev_short_stop)

        self._long_stop = long_stop
        self._short_stop = short_stop

        prev_direction = self._direction
        if ha_close > prev_short_stop:
            self._direction = 1
        elif ha_close < prev_long_stop:
            self._direction = -1

        if zlsma is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position > 0:
            if ha_close < zlsma:
                self.SellMarket(self.Position)
            return

        if self.Position == 0 and self._direction == 1 and prev_direction == -1 and ha_close > zlsma and ha_close > ha_open:
            self.BuyMarket(self.Volume)

    def CreateClone(self):
        return ce_zlsma_5min_candlechart_strategy()
