import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, ExponentialMovingAverage, SuperTrend, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class MaTypes:
    """Moving average types of the MACD."""
    Simple = 0
    Exponential = 1


class dual_supertrend_macd_strategy(Strategy):
    """
    Dual Supertrend MACD strategy.
    The MACD line is the fast minus the slow moving average of the close (OscillatorMaType) and the histogram is the MACD line minus
    its signal average (SignalMaType). Long: close above both Supertrend lines and a positive histogram. Short: close below both lines
    and a negative histogram. A long closes when the close falls below either line or the histogram turns negative, a short when the
    close rises above either line or the histogram turns positive. TradeDirection limits the sides that may be opened.
    """

    def __init__(self):
        super(dual_supertrend_macd_strategy, self).__init__()
        self._macd_fast = self.Param("MacdFast", 12).SetGreaterThanZero().SetDisplay("MACD Fast", "Fast MACD period", "MACD")
        self._macd_slow = self.Param("MacdSlow", 26).SetGreaterThanZero().SetDisplay("MACD Slow", "Slow MACD period", "MACD")
        self._macd_signal = self.Param("MacdSignal", 9).SetGreaterThanZero().SetDisplay("MACD Signal", "Signal MACD period", "MACD")
        self._oscillator_ma_type = self.Param("OscillatorMaType", MaTypes.Exponential).SetDisplay("Oscillator MA Type", "Moving average type of the MACD line", "MACD")
        self._signal_ma_type = self.Param("SignalMaType", MaTypes.Exponential).SetDisplay("Signal MA Type", "Moving average type of the signal line", "MACD")
        self._atr_period1 = self.Param("AtrPeriod1", 10).SetGreaterThanZero().SetDisplay("ATR Period 1", "ATR period of the first Supertrend", "Supertrend")
        self._factor1 = self.Param("Factor1", 3.0).SetGreaterThanZero().SetDisplay("Factor 1", "Factor of the first Supertrend", "Supertrend")
        self._atr_period2 = self.Param("AtrPeriod2", 20).SetGreaterThanZero().SetDisplay("ATR Period 2", "ATR period of the second Supertrend", "Supertrend")
        self._factor2 = self.Param("Factor2", 5.0).SetGreaterThanZero().SetDisplay("Factor 2", "Factor of the second Supertrend", "Supertrend")
        self._trade_direction = self.Param("TradeDirection", "Both").SetDisplay("Trade Direction", "Sides that may be opened: Long, Short or Both", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._signal_ma = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    @staticmethod
    def _create_ma(ma_type, length):
        ma = SimpleMovingAverage() if ma_type == MaTypes.Simple else ExponentialMovingAverage()
        ma.Length = length
        return ma

    def OnStarted2(self, time):
        super(dual_supertrend_macd_strategy, self).OnStarted2(time)

        fast_ma = self._create_ma(self._oscillator_ma_type.Value, self._macd_fast.Value)
        slow_ma = self._create_ma(self._oscillator_ma_type.Value, self._macd_slow.Value)
        self._signal_ma = self._create_ma(self._signal_ma_type.Value, self._macd_signal.Value)
        super_trend1 = SuperTrend()
        super_trend1.Length = self._atr_period1.Value
        super_trend1.Multiplier = Decimal(self._factor1.Value)
        super_trend2 = SuperTrend()
        super_trend2.Length = self._atr_period2.Value
        super_trend2.Multiplier = Decimal(self._factor2.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(fast_ma, slow_ma, super_trend1, super_trend2, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, super_trend1)
            self.DrawIndicator(area, super_trend2)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_value, slow_value, st1_value, st2_value):
        if candle.State != CandleStates.Finished:
            return

        if not fast_value.IsFormed or not slow_value.IsFormed:
            return

        macd = fast_value.GetValue[Decimal](None) - slow_value.GetValue[Decimal](None)
        signal_input = DecimalIndicatorValue(self._signal_ma, macd, candle.OpenTime)
        signal_input.IsFinal = True
        signal_value = self._signal_ma.Process(signal_input)

        if not signal_value.IsFormed or not st1_value.IsFormed or not st2_value.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        histogram = macd - signal_value.GetValue[Decimal](None)
        st1 = st1_value.GetValue[Decimal](None)
        st2 = st2_value.GetValue[Decimal](None)
        close = candle.ClosePrice

        direction = str(self._trade_direction.Value).lower()
        allow_long = direction == "both" or direction == "long"
        allow_short = direction == "both" or direction == "short"

        long_entry = close > st1 and close > st2 and histogram > 0
        short_entry = close < st1 and close < st2 and histogram < 0
        long_exit = close < st1 or close < st2 or histogram < 0
        short_exit = close > st1 or close > st2 or histogram > 0

        if self.Position > 0:
            if not long_exit:
                return
            if short_entry and allow_short:
                self.SellMarket(self.Volume + self.Position)
            else:
                self.SellMarket(self.Position)
        elif self.Position < 0:
            if not short_exit:
                return
            if long_entry and allow_long:
                self.BuyMarket(self.Volume - self.Position)
            else:
                self.BuyMarket(-self.Position)
        elif long_entry and allow_long:
            self.BuyMarket(self.Volume)
        elif short_entry and allow_short:
            self.SellMarket(self.Volume)

    def CreateClone(self):
        return dual_supertrend_macd_strategy()
