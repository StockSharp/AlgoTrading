import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal, SimpleMovingAverage, StandardDeviation, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy

class macd_breakout_strategy(Strategy):
    """
    MACD Breakout: enters when MACD histogram breaks out of its normal range.
    """

    def __init__(self):
        super(macd_breakout_strategy, self).__init__()
        self._fast_ema = self.Param("FastEmaPeriod", 12).SetDisplay("Fast EMA", "Fast EMA period", "MACD")
        self._slow_ema = self.Param("SlowEmaPeriod", 26).SetDisplay("Slow EMA", "Slow EMA period", "MACD")
        self._signal_period = self.Param("SignalPeriod", 9).SetDisplay("Signal Period", "Signal line period", "MACD")
        self._sma_period = self.Param("SmaPeriod", 20).SetDisplay("SMA Period", "Histogram SMA period", "Indicators")
        self._dev_mult = self.Param("DeviationMultiplier", 2.0).SetDisplay("Dev Mult", "Stddev multiplier", "Breakout")
        self._sl_pct = self.Param("StopLossPercent", 2.0).SetDisplay("SL %", "Stop loss percent", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Timeframe", "General")

        self._macd_hist_sma = None
        self._macd_hist_stddev = None
        self._prev_macd_hist_value = 0.0
        self._prev_macd_hist_sma_value = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(macd_breakout_strategy, self).OnReseted()
        self._prev_macd_hist_value = 0.0
        self._prev_macd_hist_sma_value = 0.0

    def OnStarted(self, time):
        super(macd_breakout_strategy, self).OnStarted(time)
        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self._fast_ema.Value
        macd.Macd.LongMa.Length = self._slow_ema.Value
        macd.SignalMa.Length = self._signal_period.Value

        self._macd_hist_sma = SimpleMovingAverage()
        self._macd_hist_sma.Length = self._sma_period.Value
        self._macd_hist_stddev = StandardDeviation()
        self._macd_hist_stddev.Length = self._sma_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, self._process_candle).Start()
        sl = self._sl_pct.Value
        self.StartProtection(Unit(sl, UnitTypes.Percent), Unit(sl * 1.5, UnitTypes.Percent))
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, macd)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, macd_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        typed_val = macd_value
        if typed_val.Macd is None or typed_val.Signal is None:
            return
        macd_val = float(typed_val.Macd)

        # Process using DecimalIndicatorValue like CS does (no IsFinal)
        div_sma = DecimalIndicatorValue(self._macd_hist_sma, macd_val, candle.ServerTime)
        div_std = DecimalIndicatorValue(self._macd_hist_stddev, macd_val, candle.ServerTime)
        macd_hist_sma_value = float(self._macd_hist_sma.Process(div_sma))
        macd_hist_stddev_value = float(self._macd_hist_stddev.Process(div_std))

        # Store previous values on first call
        if self._prev_macd_hist_value == 0.0 and self._prev_macd_hist_sma_value == 0.0:
            self._prev_macd_hist_value = macd_val
            self._prev_macd_hist_sma_value = macd_hist_sma_value
            return

        # Calculate breakout thresholds
        dm = float(self._dev_mult.Value)
        upper_threshold = macd_hist_sma_value + dm * macd_hist_stddev_value
        lower_threshold = macd_hist_sma_value - dm * macd_hist_stddev_value

        # Trading logic
        if macd_val > upper_threshold and self.Position <= 0:
            self.BuyMarket(self.Volume)
        elif macd_val < lower_threshold and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))
        elif self.Position > 0 and macd_val < macd_hist_sma_value:
            self.SellMarket(Math.Abs(self.Position))
        elif self.Position < 0 and macd_val > macd_hist_sma_value:
            self.BuyMarket(Math.Abs(self.Position))

        self._prev_macd_hist_value = macd_val
        self._prev_macd_hist_sma_value = macd_hist_sma_value

    def CreateClone(self):
        return macd_breakout_strategy()
