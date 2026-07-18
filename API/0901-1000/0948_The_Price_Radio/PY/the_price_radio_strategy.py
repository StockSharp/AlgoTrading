import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest, SimpleMovingAverage, IndicatorHelper
from StockSharp.Algo.Strategies import Strategy

class the_price_radio_strategy(Strategy):
    """Ehlers Price Radio: derivative-based amplitude/frequency with hold bars and cooldown."""
    def __init__(self):
        super(the_price_radio_strategy, self).__init__()
        self._length = self.Param("Length", 14).SetGreaterThanZero().SetDisplay("Length", "Lookback period", "General")
        self._max_entries = self.Param("MaxEntries", 45).SetGreaterThanZero().SetDisplay("Max Entries", "Maximum entries per run", "Risk")
        self._hold_bars = self.Param("HoldBars", 180).SetGreaterThanZero().SetDisplay("Hold Bars", "Bars to hold position", "Risk")
        self._cooldown_bars = self.Param("CooldownBars", 240).SetGreaterThanZero().SetDisplay("Cooldown Bars", "Minimum bars between entries", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(the_price_radio_strategy, self).OnReseted()
        self._prev_close = 0
        self._entries_executed = 0
        self._bars_in_pos = 0
        self._bars_since_signal = 0

    def OnStarted2(self, time):
        super(the_price_radio_strategy, self).OnStarted2(time)
        self._prev_close = 0
        self._entries_executed = 0
        self._bars_in_pos = 0
        self._bars_since_signal = self._cooldown_bars.Value

        length = self._length.Value
        self._envelope = Highest()
        self._envelope.Length = 4
        self._am_sma = SimpleMovingAverage()
        self._am_sma.Length = length
        self._deriv_high = Highest()
        self._deriv_high.Length = length
        self._deriv_low = Lowest()
        self._deriv_low.Length = length
        self._fm_sma = SimpleMovingAverage()
        self._fm_sma.Length = length

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        if self._prev_close == 0:
            self._prev_close = close
            return

        deriv = close - self._prev_close
        self._prev_close = close
        t = candle.OpenTime

        abs_deriv = abs(deriv)
        envelope = float(IndicatorHelper.ToDecimal(IndicatorHelper.Process(self._envelope, Decimal(abs_deriv), t, True)))
        am = float(IndicatorHelper.ToDecimal(IndicatorHelper.Process(self._am_sma, Decimal(envelope), t, True)))
        high = float(IndicatorHelper.ToDecimal(IndicatorHelper.Process(self._deriv_high, Decimal(deriv), t, True)))
        low = float(IndicatorHelper.ToDecimal(IndicatorHelper.Process(self._deriv_low, Decimal(deriv), t, True)))

        clamped = min(max(10 * deriv, low), high)
        fm = float(IndicatorHelper.ToDecimal(IndicatorHelper.Process(self._fm_sma, Decimal(clamped), t, True)))

        if self.Position != 0:
            self._bars_in_pos += 1
            if self._bars_in_pos >= self._hold_bars.Value:
                if self.Position > 0:
                    self.SellMarket()
                else:
                    self.BuyMarket()
                self._bars_in_pos = 0
                self._bars_since_signal = 0
            return

        self._bars_in_pos = 0
        self._bars_since_signal += 1

        if self._entries_executed >= self._max_entries.Value or self._bars_since_signal < self._cooldown_bars.Value:
            return

        if deriv > am and deriv > fm:
            self.BuyMarket()
            self._entries_executed += 1
            self._bars_since_signal = 0
        elif deriv < -am and deriv < -fm:
            self.SellMarket()
            self._entries_executed += 1
            self._bars_since_signal = 0

    def CreateClone(self):
        return the_price_radio_strategy()
