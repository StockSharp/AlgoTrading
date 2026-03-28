import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class alliheik_trader_strategy(Strategy):
    """
    Alliheik Trader: uses smoothed Heiken Ashi candles (EMA of OHLC)
    with Alligator jaw (long SMA) as trend filter.
    Entry on HA color change above/below jaw, exit on reversal.
    """

    def __init__(self):
        super(alliheik_trader_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Timeframe.", "General")

        self._smooth_period = self.Param("SmoothPeriod", 6) \
            .SetDisplay("Smooth Period", "EMA period for HA smoothing.", "Indicators")

        self._jaw_period = self.Param("JawPeriod", 144) \
            .SetDisplay("Jaw Period", "SMA period for Alligator jaw.", "Indicators")

        self._prev_ha_open = 0.0
        self._prev_ha_close = 0.0
        self._prev_bullish = False
        self._has_prev = False
        self._entry_price = 0.0

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, v): self._candle_type.Value = v
    @property
    def SmoothPeriod(self): return self._smooth_period.Value
    @SmoothPeriod.setter
    def SmoothPeriod(self, v): self._smooth_period.Value = v
    @property
    def JawPeriod(self): return self._jaw_period.Value
    @JawPeriod.setter
    def JawPeriod(self, v): self._jaw_period.Value = v

    def OnReseted(self):
        super(alliheik_trader_strategy, self).OnReseted()
        self._prev_ha_open = 0.0
        self._prev_ha_close = 0.0
        self._prev_bullish = False
        self._has_prev = False
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(alliheik_trader_strategy, self).OnStarted(time)

        self._prev_ha_open = 0.0
        self._prev_ha_close = 0.0
        self._prev_bullish = False
        self._has_prev = False
        self._entry_price = 0.0

        ema = ExponentialMovingAverage()
        ema.Length = self.SmoothPeriod
        jaw = SimpleMovingAverage()
        jaw.Length = self.JawPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ema, jaw, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, jaw)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, ema_val, jaw_val):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        o = float(candle.OpenPrice)
        h = float(candle.HighPrice)
        l = float(candle.LowPrice)

        # Compute smoothed Heiken Ashi
        ha_close = (o + h + l + close) / 4.0
        if self._prev_ha_open == 0:
            ha_open = (o + close) / 2.0
        else:
            ha_open = (self._prev_ha_open + self._prev_ha_close) / 2.0

        bullish = ha_close > ha_open

        if not self._has_prev:
            self._prev_ha_open = ha_open
            self._prev_ha_close = ha_close
            self._prev_bullish = bullish
            self._has_prev = True
            return

        color_change = bullish != self._prev_bullish

        # Exit on color change
        if self.Position > 0 and color_change and not bullish:
            self.SellMarket()
            self._entry_price = 0.0
        elif self.Position < 0 and color_change and bullish:
            self.BuyMarket()
            self._entry_price = 0.0

        # Entry on color change confirmed by jaw filter
        if self.Position == 0 and color_change:
            if bullish and close > jaw_val:
                self._entry_price = close
                self.BuyMarket()
            elif not bullish and close < jaw_val:
                self._entry_price = close
                self.SellMarket()

        self._prev_ha_open = ha_open
        self._prev_ha_close = ha_close
        self._prev_bullish = bullish

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return alliheik_trader_strategy()
