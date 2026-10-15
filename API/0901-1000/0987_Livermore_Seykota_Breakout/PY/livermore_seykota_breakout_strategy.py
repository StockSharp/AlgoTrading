import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange, SimpleMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class livermore_seykota_breakout_strategy(Strategy):
    """
    Livermore Seykota breakout strategy.
    Tracks the most recent confirmed pivot high and pivot low (PivotLength bars on each side). A close above the last pivot high goes long
    when price is above the main EMA, the fast EMA is above the slow EMA and volume is above its average; a close below the last pivot low
    goes short under the mirrored conditions. Positions exit on an ATR stop placed at entry or on an ATR trailing stop.
    """

    def __init__(self):
        super(livermore_seykota_breakout_strategy, self).__init__()
        self._main_ema_length = self.Param("MainEmaLength", 50).SetGreaterThanZero().SetDisplay("Main EMA", "Main EMA trend filter length", "Indicators")
        self._fast_ema_length = self.Param("FastEmaLength", 20).SetGreaterThanZero().SetDisplay("Fast EMA", "Fast EMA length", "Indicators")
        self._slow_ema_length = self.Param("SlowEmaLength", 200).SetGreaterThanZero().SetDisplay("Slow EMA", "Slow EMA length", "Indicators")
        self._pivot_length = self.Param("PivotLength", 3).SetGreaterThanZero().SetDisplay("Pivot Length", "Bars on each side that confirm a pivot", "Indicators")
        self._atr_length = self.Param("AtrLength", 14).SetGreaterThanZero().SetDisplay("ATR Length", "ATR period", "Indicators")
        self._stop_atr_multiplier = self.Param("StopAtrMultiplier", 3.0).SetNotNegative().SetDisplay("Stop ATR Mult", "ATR multiplier of the initial stop", "Risk")
        self._trail_atr_multiplier = self.Param("TrailAtrMultiplier", 2.0).SetNotNegative().SetDisplay("Trail ATR Mult", "ATR multiplier of the trailing stop", "Risk")
        self._volume_sma_length = self.Param("VolumeSmaLength", 20).SetGreaterThanZero().SetDisplay("Volume SMA", "Volume SMA length", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._volume_sma = None
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._highs = []
        self._lows = []
        self._pivot_high = None
        self._pivot_low = None
        self._stop_price = None
        self._trail_price = None

    def OnReseted(self):
        super(livermore_seykota_breakout_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(livermore_seykota_breakout_strategy, self).OnStarted2(time)

        self._reset_state()

        main_ema = ExponentialMovingAverage()
        main_ema.Length = self._main_ema_length.Value
        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self._fast_ema_length.Value
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self._slow_ema_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value
        self._volume_sma = SimpleMovingAverage()
        self._volume_sma.Length = self._volume_sma_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(main_ema, fast_ema, slow_ema, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, main_ema)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, main_ema, fast_ema, slow_ema, atr):
        if candle.State != CandleStates.Finished:
            return

        volume_input = DecimalIndicatorValue(self._volume_sma, candle.TotalVolume, candle.OpenTime)
        volume_input.IsFinal = True
        volume_value = self._volume_sma.Process(volume_input)
        self._update_pivots(candle)

        trail_mult = Decimal(self._trail_atr_multiplier.Value)
        stop_mult = Decimal(self._stop_atr_multiplier.Value)

        if self.Position > 0 and self._stop_price is not None:
            if trail_mult > 0:
                level = candle.ClosePrice - atr * trail_mult
                self._trail_price = level if self._trail_price is None else max(self._trail_price, level)
            exit_level = self._stop_price if self._trail_price is None else max(self._stop_price, self._trail_price)
            if candle.LowPrice <= exit_level:
                self.SellMarket(self.Position)
                self._stop_price = None
                self._trail_price = None
                return
        elif self.Position < 0 and self._stop_price is not None:
            if trail_mult > 0:
                level = candle.ClosePrice + atr * trail_mult
                self._trail_price = level if self._trail_price is None else min(self._trail_price, level)
            exit_level = self._stop_price if self._trail_price is None else min(self._stop_price, self._trail_price)
            if candle.HighPrice >= exit_level:
                self.BuyMarket(-self.Position)
                self._stop_price = None
                self._trail_price = None
                return

        if not volume_value.IsFormed or self._pivot_high is None or self._pivot_low is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = candle.ClosePrice
        volume_strong = candle.TotalVolume > volume_value.GetValue[Decimal](None)
        up_trend = close > main_ema and fast_ema > slow_ema
        down_trend = close < main_ema and fast_ema < slow_ema

        if close > self._pivot_high and up_trend and volume_strong and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._stop_price = close - atr * stop_mult if stop_mult > 0 else Decimal.MinValue
            self._trail_price = None
        elif close < self._pivot_low and down_trend and volume_strong and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._stop_price = close + atr * stop_mult if stop_mult > 0 else Decimal.MaxValue
            self._trail_price = None

    def _update_pivots(self, candle):
        pivot_length = self._pivot_length.Value
        window = pivot_length * 2 + 1

        self._highs.append(candle.HighPrice)
        self._lows.append(candle.LowPrice)

        if len(self._highs) > window:
            self._highs.pop(0)
            self._lows.pop(0)

        if len(self._highs) < window:
            return

        # The middle bar is a pivot once PivotLength bars on each side have closed.
        center_high = self._highs[pivot_length]
        center_low = self._lows[pivot_length]

        if center_high == max(self._highs):
            self._pivot_high = center_high

        if center_low == min(self._lows):
            self._pivot_low = center_low

    def CreateClone(self):
        return livermore_seykota_breakout_strategy()
