import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, MovingAverageConvergenceDivergenceSignal, RelativeStrengthIndex, SimpleMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class gold_pullback_strategy(Strategy):
    """
    Gold pullback strategy.
    The trend is up when the EmaFastLength EMA is above the EmaSlowLength EMA. A candle that touches the EmaPullbackLength EMA in an
    uptrend, with the MACD (12, 26, 9) line above its signal, the TDI fast line (2-period SMA of RSI 13) above its signal line (7-period
    SMA of the same RSI) and RSI above 50 goes long; the mirrored conditions go short. The stop is the signal candle low (high) minus
    (plus) SlOffset, and the target lies at the same distance on the other side of the entry.
    """

    RSI_LENGTH = 13
    TDI_FAST_LENGTH = 2
    TDI_SIGNAL_LENGTH = 7

    def __init__(self):
        super(gold_pullback_strategy, self).__init__()
        self._ema_fast_length = self.Param("EmaFastLength", 14).SetGreaterThanZero().SetDisplay("EMA Fast Length", "Fast trend EMA length", "Indicators")
        self._ema_slow_length = self.Param("EmaSlowLength", 60).SetGreaterThanZero().SetDisplay("EMA Slow Length", "Slow trend EMA length", "Indicators")
        self._ema_pullback_length = self.Param("EmaPullbackLength", 21).SetGreaterThanZero().SetDisplay("EMA Pullback Length", "Pullback EMA length", "Indicators")
        self._sl_offset = self.Param("SlOffset", 0.1).SetNotNegative().SetDisplay("SL Offset", "Price offset added beyond the signal candle for the stop", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._tdi_fast = None
        self._tdi_signal = None
        self._stop_price = Decimal(0)
        self._take_price = Decimal(0)

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(gold_pullback_strategy, self).OnReseted()
        self._stop_price = Decimal(0)
        self._take_price = Decimal(0)

    def OnStarted2(self, time):
        super(gold_pullback_strategy, self).OnStarted2(time)

        self._stop_price = Decimal(0)
        self._take_price = Decimal(0)

        ema_fast = ExponentialMovingAverage()
        ema_fast.Length = self._ema_fast_length.Value
        ema_slow = ExponentialMovingAverage()
        ema_slow.Length = self._ema_slow_length.Value
        ema_pullback = ExponentialMovingAverage()
        ema_pullback.Length = self._ema_pullback_length.Value
        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = 12
        macd.Macd.LongMa.Length = 26
        macd.SignalMa.Length = 9
        rsi = RelativeStrengthIndex()
        rsi.Length = self.RSI_LENGTH

        self._tdi_fast = SimpleMovingAverage()
        self._tdi_fast.Length = self.TDI_FAST_LENGTH
        self._tdi_signal = SimpleMovingAverage()
        self._tdi_signal.Length = self.TDI_SIGNAL_LENGTH

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(ema_fast, ema_slow, ema_pullback, macd, rsi, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema_fast)
            self.DrawIndicator(area, ema_slow)
            self.DrawIndicator(area, ema_pullback)
            self.DrawOwnTrades(area)

    def _process_value(self, indicator, value, time):
        indicator_input = DecimalIndicatorValue(indicator, value, time)
        indicator_input.IsFinal = True
        return indicator.Process(indicator_input)

    def _process_candle(self, candle, fast_value, slow_value, pullback_value, macd_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        if not rsi_value.IsFormed:
            return

        rsi = rsi_value.GetValue[Decimal](None)
        tdi_fast = self._process_value(self._tdi_fast, rsi, candle.OpenTime)
        tdi_signal = self._process_value(self._tdi_signal, rsi, candle.OpenTime)

        if self._manage_position(candle):
            return

        if not fast_value.IsFormed or not slow_value.IsFormed or not pullback_value.IsFormed or not self._tdi_fast.IsFormed or not self._tdi_signal.IsFormed:
            return

        if not macd_value.IsFormed or macd_value.Macd is None or macd_value.Signal is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading() or self.Position != 0:
            return

        macd = macd_value.Macd
        signal = macd_value.Signal
        ema_fast = fast_value.GetValue[Decimal](None)
        ema_slow = slow_value.GetValue[Decimal](None)
        pullback = pullback_value.GetValue[Decimal](None)
        tdi_ma = tdi_fast.GetValue[Decimal](None)
        tdi_sig = tdi_signal.GetValue[Decimal](None)
        touches = candle.LowPrice <= pullback and candle.HighPrice >= pullback
        close = candle.ClosePrice
        offset = Decimal(self._sl_offset.Value)

        if touches and ema_fast > ema_slow and macd > signal and tdi_ma > tdi_sig and rsi > 50:
            stop = candle.LowPrice - offset
            if stop >= close:
                return
            self.BuyMarket(self.Volume)
            self._stop_price = stop
            self._take_price = close + (close - stop)
        elif touches and ema_fast < ema_slow and macd < signal and tdi_ma < tdi_sig and rsi < 50:
            stop = candle.HighPrice + offset
            if stop <= close:
                return
            self.SellMarket(self.Volume)
            self._stop_price = stop
            self._take_price = close - (stop - close)

    def _manage_position(self, candle):
        if self.Position > 0 and self._stop_price > 0:
            if candle.LowPrice <= self._stop_price or candle.HighPrice >= self._take_price:
                self.SellMarket(self.Position)
                return True
        elif self.Position < 0 and self._stop_price > 0:
            if candle.HighPrice >= self._stop_price or candle.LowPrice <= self._take_price:
                self.BuyMarket(abs(self.Position))
                return True
        return False

    def CreateClone(self):
        return gold_pullback_strategy()
