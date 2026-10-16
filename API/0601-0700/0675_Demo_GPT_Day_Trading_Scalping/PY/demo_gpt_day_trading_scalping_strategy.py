import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class demo_gpt_day_trading_scalping_strategy(Strategy):
    """Long-only two-candle volume-confirmed gap strategy, with reference levels."""

    def __init__(self):
        super(demo_gpt_day_trading_scalping_strategy, self).__init__()

        self._ema_length = self.Param("EmaLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("EMA Length", "Reference EMA", "Indicators")
        self._volume_lookback = self.Param("VolumeLookback", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Volume Lookback", "Preceding candles for volume confirmation", "Signals")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Scalping candles", "General")

        self._ema = None
        self._prior_volumes = []
        self._previous_close = None
        self._previous_green = False
        self._previous_red = False
        self._session_date = None
        self._session_high = 0.0
        self._session_low = 0.0
        self._session_close = 0.0
        self._session_price_volume = 0.0
        self._session_volume = 0.0
        self.reference_ema = 0.0
        self.reference_vwap = 0.0
        self.reference_camarilla_h3 = 0.0
        self.reference_camarilla_l3 = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(demo_gpt_day_trading_scalping_strategy, self).OnReseted()
        self._ema = None
        self._prior_volumes = []
        self._previous_close = None
        self._previous_green = False
        self._previous_red = False
        self._session_date = None
        self._session_high = 0.0
        self._session_low = 0.0
        self._session_close = 0.0
        self._session_price_volume = 0.0
        self._session_volume = 0.0
        self.reference_ema = 0.0
        self.reference_vwap = 0.0
        self.reference_camarilla_h3 = 0.0
        self.reference_camarilla_l3 = 0.0

    def OnStarted2(self, time):
        super(demo_gpt_day_trading_scalping_strategy, self).OnStarted2(time)

        self._ema = ExponentialMovingAverage()
        self._ema.Length = int(self._ema_length.Value)
        self._prior_volumes = []
        self._previous_close = None
        self._previous_green = False
        self._previous_red = False
        self._session_date = None
        self._session_price_volume = 0.0
        self._session_volume = 0.0

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._ema, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return

        self.reference_ema = float(ema_value)
        date = candle.OpenTime.Date
        hi = float(candle.HighPrice)
        lo = float(candle.LowPrice)
        close = float(candle.ClosePrice)

        if self._session_date != date:
            if self._session_date is not None:
                range_val = self._session_high - self._session_low
                self.reference_camarilla_h3 = self._session_close + range_val * 1.1 / 4.0
                self.reference_camarilla_l3 = self._session_close - range_val * 1.1 / 4.0

            self._session_date = date
            self._session_high = hi
            self._session_low = lo
            self._session_price_volume = 0.0
            self._session_volume = 0.0

        self._session_high = max(self._session_high, hi)
        self._session_low = min(self._session_low, lo)
        self._session_close = close

        volume = max(0.0, float(candle.TotalVolume))
        self._session_price_volume += ((hi + lo + close) / 3.0) * volume
        self._session_volume += volume
        self.reference_vwap = (
            self._session_price_volume / self._session_volume
            if self._session_volume > 0 else close
        )

        length = int(self._volume_lookback.Value)
        volume_confirmed = (
            len(self._prior_volumes) >= length and
            float(candle.TotalVolume) > sum(self._prior_volumes) / length
        )

        green = close > float(candle.OpenPrice)
        red = close < float(candle.OpenPrice)
        up_gap = self._previous_close is not None and float(candle.OpenPrice) > self._previous_close
        down_gap = self._previous_close is not None and float(candle.OpenPrice) < self._previous_close

        buy = self._previous_green and green and up_gap and volume_confirmed
        exit_signal = self._previous_red and red and down_gap and volume_confirmed

        self._previous_close = close
        self._previous_green = green
        self._previous_red = red
        self._prior_volumes.append(volume)
        if len(self._prior_volumes) > length:
            self._prior_volumes.pop(0)

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position == 0 and buy:
            self.BuyMarket()
        elif self.Position > 0 and exit_signal:
            self.SellMarket(self.Position)

    def CreateClone(self):
        return demo_gpt_day_trading_scalping_strategy()
