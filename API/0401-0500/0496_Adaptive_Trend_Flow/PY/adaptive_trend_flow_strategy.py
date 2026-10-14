import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, StandardDeviation, SimpleMovingAverage, MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class adaptive_trend_flow_strategy(Strategy):
    """
    Adaptive Trend Flow strategy.
    The basis is the mean of EMA(Length) and EMA(2 * Length) of the typical price; the channel adds and subtracts Sensitivity times
    an EMA(SmoothLength) of the typical price standard deviation over Length bars. A close above the upper band turns the trend up,
    a close below the lower band turns it down. The strategy buys when the trend turns from down to up while the close is above
    SMA(SmaLength) and the MACD line is above its signal (each filter optional), and closes the long when the trend turns down.
    """

    def __init__(self):
        super(adaptive_trend_flow_strategy, self).__init__()
        self._length = self.Param("Length", 2).SetGreaterThanZero().SetDisplay("Length", "Fast EMA and deviation length; the slow EMA uses twice this length", "Trend")
        self._smooth_length = self.Param("SmoothLength", 2).SetGreaterThanZero().SetDisplay("Smooth Length", "EMA length that smooths the deviation", "Trend")
        self._sensitivity = self.Param("Sensitivity", 2.0).SetGreaterThanZero().SetDisplay("Sensitivity", "Deviation multiplier of the channel", "Trend")
        self._use_sma_filter = self.Param("UseSmaFilter", True).SetDisplay("Use SMA Filter", "Require the close above the SMA", "Filters")
        self._sma_length = self.Param("SmaLength", 4).SetGreaterThanZero().SetDisplay("SMA Length", "SMA period of the filter", "Filters")
        self._use_macd_filter = self.Param("UseMacdFilter", True).SetDisplay("Use MACD Filter", "Require the MACD line above its signal", "Filters")
        self._macd_fast_length = self.Param("MacdFastLength", 2).SetGreaterThanZero().SetDisplay("MACD Fast Length", "MACD fast EMA length", "Filters")
        self._macd_slow_length = self.Param("MacdSlowLength", 7).SetGreaterThanZero().SetDisplay("MACD Slow Length", "MACD slow EMA length", "Filters")
        self._macd_signal_length = self.Param("MacdSignalLength", 2).SetGreaterThanZero().SetDisplay("MACD Signal Length", "MACD signal EMA length", "Filters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._fast_ema = None
        self._slow_ema = None
        self._std_dev = None
        self._vol_smooth = None
        self._trend = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(adaptive_trend_flow_strategy, self).OnReseted()
        self._fast_ema = None
        self._slow_ema = None
        self._std_dev = None
        self._vol_smooth = None
        self._trend = 0

    def OnStarted2(self, time):
        super(adaptive_trend_flow_strategy, self).OnStarted2(time)

        length = self._length.Value
        self._trend = 0
        self._fast_ema = ExponentialMovingAverage()
        self._fast_ema.Length = length
        self._slow_ema = ExponentialMovingAverage()
        self._slow_ema.Length = length * 2
        self._std_dev = StandardDeviation()
        self._std_dev.Length = length
        self._vol_smooth = ExponentialMovingAverage()
        self._vol_smooth.Length = self._smooth_length.Value

        sma = SimpleMovingAverage()
        sma.Length = self._sma_length.Value
        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self._macd_fast_length.Value
        macd.Macd.LongMa.Length = self._macd_slow_length.Value
        macd.SignalMa.Length = self._macd_signal_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(sma, macd, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, sma_value, macd_value):
        if candle.State != CandleStates.Finished:
            return

        typical = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / Decimal(3)
        time = candle.ServerTime

        fast = process_value(self._fast_ema, typical, time, True).GetValue[Decimal](None)
        slow = process_value(self._slow_ema, typical, time, True).GetValue[Decimal](None)
        deviation = process_value(self._std_dev, typical, time, True)

        if not self._fast_ema.IsFormed or not self._slow_ema.IsFormed or not self._std_dev.IsFormed:
            return

        smooth_vol = process_value(self._vol_smooth, deviation.GetValue[Decimal](None), time, True).GetValue[Decimal](None)
        if not self._vol_smooth.IsFormed:
            return

        sensitivity = Decimal(self._sensitivity.Value)
        basis = (fast + slow) / Decimal(2)
        upper = basis + smooth_vol * sensitivity
        lower = basis - smooth_vol * sensitivity
        close = candle.ClosePrice

        prev_trend = self._trend
        if close > upper:
            self._trend = 1
        elif close < lower:
            self._trend = -1

        if not sma_value.IsFormed or not macd_value.IsFormed:
            return

        if macd_value.Macd is None or macd_value.Signal is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        sma_ok = not self._use_sma_filter.Value or close > sma_value.GetValue[Decimal](None)
        macd_ok = not self._use_macd_filter.Value or macd_value.Macd > macd_value.Signal

        if prev_trend == -1 and self._trend == 1 and sma_ok and macd_ok and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif prev_trend == 1 and self._trend == -1 and self.Position > 0:
            self.SellMarket(self.Position)

    def CreateClone(self):
        return adaptive_trend_flow_strategy()
