import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import KeltnerChannels
from StockSharp.Algo.Strategies import Strategy

class keltner_channel_breakout_strategy(Strategy):
    """
    Strategy based on Keltner Channel breakout.
    Enters long when price breaks above upper band, short when price breaks below lower band.
    """

    def __init__(self):
        super(keltner_channel_breakout_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 500).SetDisplay("EMA Period", "Period for Exponential Moving Average", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 14).SetDisplay("ATR Period", "Period for Average True Range", "Indicators")
        self._atr_multiplier = self.Param("AtrMultiplier", 10.0).SetDisplay("ATR Multiplier", "Multiplier for ATR to determine channel width", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_close_price = 0.0
        self._prev_upper_band = 0.0
        self._prev_lower_band = 0.0
        self._prev_ema = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(keltner_channel_breakout_strategy, self).OnReseted()
        self._prev_close_price = 0.0
        self._prev_upper_band = 0.0
        self._prev_lower_band = 0.0
        self._prev_ema = 0.0

    def OnStarted2(self, time):
        super(keltner_channel_breakout_strategy, self).OnStarted2(time)

        keltner = KeltnerChannels()
        keltner.Length = self._ema_period.Value
        keltner.Multiplier = self._atr_multiplier.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(keltner, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, keltner)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, keltner_val):
        if candle.State != CandleStates.Finished:
            return

        if keltner_val.Upper is None or keltner_val.Lower is None or keltner_val.Middle is None:
            return

        upper = float(keltner_val.Upper)
        lower = float(keltner_val.Lower)
        middle = float(keltner_val.Middle)

        if self._prev_upper_band == 0:
            self._prev_close_price = float(candle.ClosePrice)
            self._prev_upper_band = upper
            self._prev_lower_band = lower
            self._prev_ema = middle
            return

        close = float(candle.ClosePrice)
        is_upper_breakout = close > self._prev_upper_band and self._prev_close_price <= self._prev_upper_band
        is_lower_breakout = close < self._prev_lower_band and self._prev_close_price >= self._prev_lower_band

        if is_upper_breakout and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif is_lower_breakout and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

        self._prev_close_price = close
        self._prev_upper_band = upper
        self._prev_lower_band = lower
        self._prev_ema = middle

    def CreateClone(self):
        return keltner_channel_breakout_strategy()
