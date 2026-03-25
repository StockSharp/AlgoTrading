import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class v1n1_lonny_breakout_strategy(Strategy):
    def __init__(self):
        super(v1n1_lonny_breakout_strategy, self).__init__()

        self._trend_period = self.Param("TrendPeriod", 20)
        self._rsi_period = self.Param("RsiPeriod", 14)
        self._range_bars = self.Param("RangeBars", 5)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))

        self._prev_ema = 0.0
        self._prev_prev_ema = 0.0
        self._has_prev_ema = False
        self._has_prev_prev_ema = False
        self._highs = []
        self._lows = []
        self._range_ready = False
        self._range_high = 0.0
        self._range_low = 0.0
        self._breakout_up_seen = False
        self._breakout_down_seen = False

    @property
    def TrendPeriod(self):
        return self._trend_period.Value

    @TrendPeriod.setter
    def TrendPeriod(self, value):
        self._trend_period.Value = value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsi_period.Value = value

    @property
    def RangeBars(self):
        return self._range_bars.Value

    @RangeBars.setter
    def RangeBars(self, value):
        self._range_bars.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(v1n1_lonny_breakout_strategy, self).OnStarted(time)

        self._prev_ema = 0.0
        self._prev_prev_ema = 0.0
        self._has_prev_ema = False
        self._has_prev_prev_ema = False
        self._highs = []
        self._lows = []
        self._range_ready = False
        self._range_high = 0.0
        self._range_low = 0.0
        self._breakout_up_seen = False
        self._breakout_down_seen = False

        ema = ExponentialMovingAverage()
        ema.Length = self.TrendPeriod
        self._ema = ema

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.RsiPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ema, self.ProcessCandle).Start()

        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(1, UnitTypes.Percent))

    def ProcessCandle(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return

        ema_val = float(ema_value)
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        rsi_input = DecimalIndicatorValue(self._rsi, candle.ClosePrice, candle.OpenTime)
        rsi_input.IsFinal = True
        rsi_result = self._rsi.Process(rsi_input)

        if not self._rsi.IsFormed or not self._ema.IsFormed:
            self._shift_ema(ema_val)
            return

        rsi_val = float(rsi_result)

        if not self._range_ready:
            self._highs.append(high)
            self._lows.append(low)

            if len(self._highs) >= int(self.RangeBars):
                self._range_high = max(self._highs)
                self._range_low = min(self._lows)
                self._range_ready = True

            self._shift_ema(ema_val)
            return

        if self.Position != 0 or not self._has_prev_ema or not self._has_prev_prev_ema:
            self._shift_ema(ema_val)
            return

        trend_up = self._prev_ema > self._prev_prev_ema
        trend_down = self._prev_ema < self._prev_prev_ema

        if not self._breakout_up_seen and trend_up and rsi_val < 70.0 and close > self._range_high:
            self.BuyMarket()
            self._breakout_up_seen = True
        elif not self._breakout_down_seen and trend_down and rsi_val > 30.0 and close < self._range_low:
            self.SellMarket()
            self._breakout_down_seen = True

        self._shift_ema(ema_val)

    def _shift_ema(self, ema_val):
        if self._has_prev_ema:
            self._prev_prev_ema = self._prev_ema
            self._has_prev_prev_ema = True
        self._prev_ema = ema_val
        self._has_prev_ema = True

    def OnReseted(self):
        super(v1n1_lonny_breakout_strategy, self).OnReseted()
        self._prev_ema = 0.0
        self._prev_prev_ema = 0.0
        self._has_prev_ema = False
        self._has_prev_prev_ema = False
        self._highs = []
        self._lows = []
        self._range_ready = False
        self._range_high = 0.0
        self._range_low = 0.0
        self._breakout_up_seen = False
        self._breakout_down_seen = False

    def CreateClone(self):
        return v1n1_lonny_breakout_strategy()
