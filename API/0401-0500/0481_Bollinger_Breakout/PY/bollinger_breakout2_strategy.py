import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, SimpleMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

RSI_SHORT_LIMIT = 85


class bollinger_breakout2_strategy(Strategy):
    """
    4H Bollinger Breakout strategy.
    A long opens when the close crosses above the lower band with volume above its SMA and price above the trend SMA. A short opens
    when the close crosses below the upper band with volume above its SMA, price below the trend SMA and RSI below 85. A long closes
    when the close crosses above the upper band and a short when it crosses below the lower band.
    """

    def __init__(self):
        super(bollinger_breakout2_strategy, self).__init__()
        self._bollinger_length = self.Param("BollingerLength", 20).SetGreaterThanZero().SetDisplay("Bollinger Length", "Bollinger Bands period", "Bollinger Bands")
        self._bollinger_multiplier = self.Param("BollingerMultiplier", 1.8).SetGreaterThanZero().SetDisplay("Bollinger Multiplier", "Standard deviation multiplier", "Bollinger Bands")
        self._volume_length = self.Param("VolumeLength", 20).SetGreaterThanZero().SetDisplay("Volume Length", "Period of the volume SMA", "Filters")
        self._trend_length = self.Param("TrendLength", 80).SetGreaterThanZero().SetDisplay("Trend Length", "Period of the trend SMA", "Filters")
        self._rsi_length = self.Param("RsiLength", 14).SetGreaterThanZero().SetDisplay("RSI Length", "Period of RSI", "Filters")
        self._use_long_signals = self.Param("UseLongSignals", True).SetDisplay("Use Long Signals", "Allow long trades", "General")
        self._use_short_signals = self.Param("UseShortSignals", True).SetDisplay("Use Short Signals", "Allow short trades", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._volume_sma = None
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_close = None
        self._prev_upper = None
        self._prev_lower = None

    def OnReseted(self):
        super(bollinger_breakout2_strategy, self).OnReseted()
        self._volume_sma = None
        self._reset_state()

    def OnStarted2(self, time):
        super(bollinger_breakout2_strategy, self).OnStarted2(time)

        self._reset_state()

        bollinger = BollingerBands()
        bollinger.Length = self._bollinger_length.Value
        bollinger.Width = Decimal(self._bollinger_multiplier.Value)
        trend_sma = SimpleMovingAverage()
        trend_sma.Length = self._trend_length.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        self._volume_sma = SimpleMovingAverage()
        self._volume_sma.Length = self._volume_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bollinger, trend_sma, rsi, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)
            self.DrawIndicator(area, trend_sma)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, rsi)

    def _process_candle(self, candle, bollinger_value, trend_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        volume_average = process_float(self._volume_sma, candle.TotalVolume, candle.ServerTime, True).GetValue[Decimal](None)

        if not bollinger_value.IsFormed or bollinger_value.UpBand is None or bollinger_value.LowBand is None:
            return

        upper = bollinger_value.UpBand
        lower = bollinger_value.LowBand
        close = candle.ClosePrice
        prev_close = self._prev_close
        prev_upper = self._prev_upper
        prev_lower = self._prev_lower
        self._prev_close = close
        self._prev_upper = upper
        self._prev_lower = lower

        if not self._volume_sma.IsFormed or not trend_value.IsFormed or not rsi_value.IsFormed:
            return

        if prev_close is None or prev_upper is None or prev_lower is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        trend = trend_value.GetValue[Decimal](None)
        rsi = rsi_value.GetValue[Decimal](None)
        volume_high = candle.TotalVolume > volume_average

        cross_above_lower = prev_close <= prev_lower and close > lower
        cross_below_upper = prev_close >= prev_upper and close < upper
        cross_above_upper = prev_close <= prev_upper and close > upper
        cross_below_lower = prev_close >= prev_lower and close < lower

        long_signal = self._use_long_signals.Value and cross_above_lower and volume_high and close > trend
        short_signal = self._use_short_signals.Value and cross_below_upper and volume_high and close < trend and rsi < Decimal(RSI_SHORT_LIMIT)

        if long_signal and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif short_signal and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and cross_above_upper:
            self.SellMarket(self.Position)
        elif self.Position < 0 and cross_below_lower:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return bollinger_breakout2_strategy()
