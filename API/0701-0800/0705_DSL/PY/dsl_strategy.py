import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageTrueRange, RelativeStrengthIndex, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class dsl_strategy(Strategy):
    """
    DSL strategy.
    Discontinued signal lines: the upper line moves toward the close (by DslFastMode ? 2 : 1 divided by the length) only while the
    close is above its SMA, the lower line only while the close is below it. Price lines use Length, the Beluga oscillator is an
    RSI(BelugaLength) with its own lines over BelugaLength. The upper band is the upper line minus ATR(Offset) * BandsWidth and the
    lower band is the lower line plus that distance.
    Long: upper band above the lower line, open and close above the upper line for three candles, and the oscillator crossing above
    its lower line. Short: lower band below the upper line, open and close below the lower line for three candles, and the
    oscillator crossing below its upper line. The stop sits at the band of the entry side and the target RiskReward times the risk away.
    """

    def __init__(self):
        super(dsl_strategy, self).__init__()
        self._length = self.Param("Length", 34).SetGreaterThanZero().SetDisplay("Length", "Period of the price DSL lines", "DSL")
        self._offset = self.Param("Offset", 30).SetGreaterThanZero().SetDisplay("Offset", "ATR period of the band offset", "DSL")
        self._bands_width = self.Param("BandsWidth", 1.0).SetGreaterThanZero().SetDisplay("Bands Width", "ATR multiplier of the bands", "DSL")
        self._risk_reward = self.Param("RiskReward", 1.5).SetGreaterThanZero().SetDisplay("Risk Reward", "Take profit as a multiple of the risk", "Risk")
        self._beluga_length = self.Param("BelugaLength", 10).SetGreaterThanZero().SetDisplay("Beluga Length", "Period of the Beluga oscillator", "Oscillator")
        self._dsl_fast_mode = self.Param("DslFastMode", True).SetDisplay("DSL Fast Mode", "Doubles the speed of the DSL lines", "DSL")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._osc_sma = None
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._upper = None
        self._lower = None
        self._osc_upper = None
        self._osc_lower = None
        self._prev_osc = None
        self._prev_osc_upper = None
        self._prev_osc_lower = None
        self._bars_above = 0
        self._bars_below = 0
        self._stop_price = Decimal(0)
        self._take_price = Decimal(0)

    def OnReseted(self):
        super(dsl_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(dsl_strategy, self).OnStarted2(time)

        self._reset_state()

        sma = SimpleMovingAverage()
        sma.Length = self._length.Value
        atr = AverageTrueRange()
        atr.Length = self._offset.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._beluga_length.Value
        self._osc_sma = SimpleMovingAverage()
        self._osc_sma.Length = self._beluga_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(sma, atr, rsi, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, rsi)

    @staticmethod
    def _update_line(line, value, move, alpha):
        if line is None:
            return value
        return line + alpha * (value - line) if move else line

    def _process_candle(self, candle, sma_value, atr_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        if not sma_value.IsFormed or not atr_value.IsFormed or not rsi_value.IsFormed:
            return

        close = candle.ClosePrice
        speed = Decimal(2) if self._dsl_fast_mode.Value else Decimal(1)

        sma = sma_value.GetValue[Decimal](None)
        alpha = speed / Decimal(self._length.Value)
        upper = self._update_line(self._upper, close, close > sma, alpha)
        lower = self._update_line(self._lower, close, close < sma, alpha)
        self._upper = upper
        self._lower = lower

        osc = rsi_value.GetValue[Decimal](None)
        osc_input = DecimalIndicatorValue(self._osc_sma, osc, candle.OpenTime)
        osc_input.IsFinal = True
        osc_sma_value = self._osc_sma.Process(osc_input)

        if not osc_sma_value.IsFormed:
            return

        osc_sma = osc_sma_value.GetValue[Decimal](None)
        osc_alpha = speed / Decimal(self._beluga_length.Value)
        osc_upper = self._update_line(self._osc_upper, osc, osc > osc_sma, osc_alpha)
        osc_lower = self._update_line(self._osc_lower, osc, osc < osc_sma, osc_alpha)
        self._osc_upper = osc_upper
        self._osc_lower = osc_lower

        prev_osc = self._prev_osc
        prev_osc_upper = self._prev_osc_upper
        prev_osc_lower = self._prev_osc_lower
        self._prev_osc = osc
        self._prev_osc_upper = osc_upper
        self._prev_osc_lower = osc_lower

        self._bars_above = self._bars_above + 1 if candle.OpenPrice > upper and close > upper else 0
        self._bars_below = self._bars_below + 1 if candle.OpenPrice < lower and close < lower else 0

        distance = atr_value.GetValue[Decimal](None) * Decimal(self._bands_width.Value)
        upper_band = upper - distance
        lower_band = lower + distance

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position > 0:
            if candle.LowPrice <= self._stop_price or candle.HighPrice >= self._take_price:
                self.SellMarket(self.Position)
            return

        if self.Position < 0:
            if candle.HighPrice >= self._stop_price or candle.LowPrice <= self._take_price:
                self.BuyMarket(-self.Position)
            return

        if prev_osc is None or prev_osc_upper is None or prev_osc_lower is None:
            return

        osc_cross_up = prev_osc <= prev_osc_lower and osc > osc_lower
        osc_cross_down = prev_osc >= prev_osc_upper and osc < osc_upper
        risk_reward = Decimal(self._risk_reward.Value)

        if upper_band > lower and self._bars_above >= 3 and osc_cross_up and close > upper_band:
            self._stop_price = upper_band
            self._take_price = close + (close - upper_band) * risk_reward
            self.BuyMarket(self.Volume)
        elif lower_band < upper and self._bars_below >= 3 and osc_cross_down and close < lower_band:
            self._stop_price = lower_band
            self._take_price = close - (lower_band - close) * risk_reward
            self.SellMarket(self.Volume)

    def CreateClone(self):
        return dsl_strategy()
