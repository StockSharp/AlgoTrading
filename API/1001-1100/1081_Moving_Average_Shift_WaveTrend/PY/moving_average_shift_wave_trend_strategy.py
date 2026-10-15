import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import (SimpleMovingAverage, ExponentialMovingAverage, SmoothedMovingAverage,
                                        WeightedMovingAverage, HullMovingAverage, AverageTrueRange,
                                        DecimalIndicatorValue)
from StockSharp.Algo.Strategies import Strategy


class moving_average_shift_wave_trend_strategy(Strategy):
    """
    Moving Average Shift WaveTrend strategy.
    The oscillator is a Hull MA of the change of the distance between price and a configurable moving average.
    A long opens when price is above the MA, the oscillator is positive and rising, price is above the long-term EMA,
    ATR is above its average, the candle is inside the trading hours and no long wave is active yet. Shorts mirror it.
    A position closes when the oscillator turns against it while price is on the other side of the MA, when a
    percentage trailing stop is hit, or by the percentage stop loss and take profit.
    """

    def __init__(self):
        super(moving_average_shift_wave_trend_strategy, self).__init__()
        self._ma_type = self.Param("MaType", "SMA").SetDisplay("MA Type", "Moving average type: SMA, EMA, SMMA, WMA or HMA", "Indicators")
        self._ma_length = self.Param("MaLength", 40).SetGreaterThanZero().SetDisplay("MA Length", "Moving average length", "Indicators")
        self._osc_length = self.Param("OscLength", 15).SetGreaterThanZero().SetDisplay("Oscillator Length", "Change period and Hull MA length of the oscillator", "Indicators")
        self._take_profit_percent = self.Param("TakeProfitPercent", 1.5).SetNotNegative().SetDisplay("Take Profit %", "Take profit percentage", "Risk")
        self._stop_loss_percent = self.Param("StopLossPercent", 1.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
        self._trail_percent = self.Param("TrailPercent", 1.0).SetNotNegative().SetDisplay("Trail %", "Trailing stop percentage", "Risk")
        self._long_ma_length = self.Param("LongMaLength", 200).SetGreaterThanZero().SetDisplay("Long MA Length", "Long-term trend EMA length", "Filters")
        self._atr_length = self.Param("AtrLength", 14).SetGreaterThanZero().SetDisplay("ATR Length", "ATR length and its average length", "Filters")
        self._start_hour = self.Param("StartHour", 9).SetDisplay("Start Hour", "First trading hour (UTC)", "Time")
        self._end_hour = self.Param("EndHour", 17).SetDisplay("End Hour", "Hour when trading stops (UTC, exclusive)", "Time")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._ma = None
        self._long_ema = None
        self._atr = None
        self._osc = None
        self._atr_average = None
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._diffs = []
        self._prev_osc = None
        self._in_long_wave = False
        self._in_short_wave = False
        self._trail_extreme = 0.0

    def OnReseted(self):
        super(moving_average_shift_wave_trend_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(moving_average_shift_wave_trend_strategy, self).OnStarted2(time)

        self._reset_state()

        self._ma = self._create_ma(str(self._ma_type.Value).upper(), self._ma_length.Value)
        self._long_ema = ExponentialMovingAverage()
        self._long_ema.Length = self._long_ma_length.Value
        self._atr = AverageTrueRange()
        self._atr.Length = self._atr_length.Value
        self._osc = HullMovingAverage()
        self._osc.Length = self._osc_length.Value
        self._atr_average = SimpleMovingAverage()
        self._atr_average.Length = self._atr_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._ma, self._long_ema, self._atr, self._process_candle).Start()

        self.StartProtection(
            Unit(Decimal(self._take_profit_percent.Value), UnitTypes.Percent),
            Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent),
            useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ma)
            self.DrawIndicator(area, self._long_ema)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, self._osc)

    @staticmethod
    def _create_ma(ma_type, length):
        if ma_type == "EMA":
            ma = ExponentialMovingAverage()
        elif ma_type == "SMMA":
            ma = SmoothedMovingAverage()
        elif ma_type == "WMA":
            ma = WeightedMovingAverage()
        elif ma_type == "HMA":
            ma = HullMovingAverage()
        else:
            ma = SimpleMovingAverage()
        ma.Length = length
        return ma

    @staticmethod
    def _feed(indicator, value, time):
        indicator_input = DecimalIndicatorValue(indicator, Decimal(value), time)
        indicator_input.IsFinal = True
        result = indicator.Process(indicator_input)
        if result.IsEmpty:
            return None
        return float(result.GetValue[Decimal](None))

    def _process_candle(self, candle, ma_value, long_ema_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._ma.IsFormed or not self._atr.IsFormed:
            return

        close = float(candle.ClosePrice)
        ma = float(ma_value)
        long_ema = float(long_ema_value)
        atr = float(atr_value)
        time = candle.OpenTime

        atr_average = self._feed(self._atr_average, atr, time)

        # The oscillator smooths how far the price-to-MA distance moved over OscLength candles.
        diff = close - ma
        self._diffs.append(diff)
        if len(self._diffs) <= self._osc_length.Value:
            return

        change = diff - self._diffs.pop(0)
        osc = self._feed(self._osc, change, time)

        if not self._osc.IsFormed or osc is None:
            return

        prev_osc = self._prev_osc
        self._prev_osc = osc

        if prev_osc is None:
            return

        rising = osc > prev_osc
        falling = osc < prev_osc

        # A wave ends when the oscillator crosses back through zero.
        if osc <= 0:
            self._in_long_wave = False
        if osc >= 0:
            self._in_short_wave = False

        if not self._atr_average.IsFormed or atr_average is None or not self._long_ema.IsFormed or not self.IsFormedAndOnlineAndAllowTrading():
            return

        trail = float(self._trail_percent.Value)

        if self.Position > 0:
            self._trail_extreme = max(self._trail_extreme, close)
            trail_hit = trail > 0 and close <= self._trail_extreme * (1.0 - trail / 100.0)
            if (falling and close < ma) or trail_hit:
                self.SellMarket(self.Position)
                return
        elif self.Position < 0:
            self._trail_extreme = close if self._trail_extreme == 0 else min(self._trail_extreme, close)
            trail_hit = trail > 0 and close >= self._trail_extreme * (1.0 + trail / 100.0)
            if (rising and close > ma) or trail_hit:
                self.BuyMarket(-self.Position)
                return

        hour = candle.OpenTime.Hour
        if hour < self._start_hour.Value or hour >= self._end_hour.Value:
            return

        is_volatile = atr > atr_average

        if close > ma and osc > 0 and rising and close > long_ema and is_volatile and not self._in_long_wave and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._in_long_wave = True
            self._trail_extreme = close
        elif close < ma and osc < 0 and falling and close < long_ema and is_volatile and not self._in_short_wave and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._in_short_wave = True
            self._trail_extreme = close

    def CreateClone(self):
        return moving_average_shift_wave_trend_strategy()
