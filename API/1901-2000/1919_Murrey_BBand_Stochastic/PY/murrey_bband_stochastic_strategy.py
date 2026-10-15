import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest, BollingerBands, StochasticOscillator
from StockSharp.Algo.Strategies import Strategy

class murrey_bband_stochastic_strategy(Strategy):
    """
    Murrey Math reversal filtered by Bollinger Bands and Stochastic.
    """

    def __init__(self):
        super(murrey_bband_stochastic_strategy, self).__init__()
        self._frame = self.Param("Frame", 64).SetDisplay("Frame", "Murrey frame", "General")
        self._entry_margin_pct = self.Param("EntryMarginPct", 2.0).SetDisplay("Entry Margin %", "Entry margin", "General")
        self._bb_period = self.Param("BbPeriod", 50).SetDisplay("BB Period", "Bollinger period", "Indicators")
        self._bb_dev = self.Param("BbDeviation", 2.0).SetDisplay("BB Dev", "Bollinger deviation", "Indicators")
        self._stoch_k = self.Param("StochK", 14).SetDisplay("Stoch K", "Stochastic K", "Indicators")
        self._stoch_d = self.Param("StochD", 3).SetDisplay("Stoch D", "Stochastic D", "Indicators")
        self._stoch_os = self.Param("StochOversold", 30.0).SetDisplay("Stoch OS", "Oversold level", "Signals")
        self._stoch_ob = self.Param("StochOverbought", 70.0).SetDisplay("Stoch OB", "Overbought level", "Signals")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candles", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(murrey_bband_stochastic_strategy, self).OnReseted()

    def OnStarted2(self, time):
        super(murrey_bband_stochastic_strategy, self).OnStarted2(time)
        highest = Highest()
        highest.Length = self._frame.Value
        lowest = Lowest()
        lowest.Length = self._frame.Value
        bb = BollingerBands()
        bb.Length = self._bb_period.Value
        bb.Width = self._bb_dev.Value
        stoch = StochasticOscillator()
        stoch.K.Length = self._stoch_k.Value
        stoch.D.Length = self._stoch_d.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(highest, lowest, bb, stoch, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bb)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, high_value, low_value, bb_value, stoch_value):
        if candle.State != CandleStates.Finished:
            return
        if not high_value.IsFormed or not low_value.IsFormed or not bb_value.IsFormed or not stoch_value.IsFormed:
            return
        n_high = float(high_value)
        n_low = float(low_value)
        rng = n_high - n_low
        if rng <= 0:
            return
        if n_high <= 250000 and n_high > 25000:
            fractal = 100000
        elif n_high <= 25000 and n_high > 2500:
            fractal = 10000
        elif n_high <= 2500 and n_high > 250:
            fractal = 1000
        elif n_high <= 250 and n_high > 25:
            fractal = 100
        elif n_high <= 25 and n_high > 6.25:
            fractal = 12.5
        elif n_high <= 6.25 and n_high > 3.125:
            fractal = 6.25
        elif n_high <= 3.125 and n_high > 1.5625:
            fractal = 3.125
        elif n_high <= 1.5625 and n_high > 0.390625:
            fractal = 1.5625
        elif n_high > 250000:
            fractal = 1000000
        else:
            fractal = 0.1953125
        log_val = math.log(fractal / rng, 2)
        s = math.floor(log_val)
        octave = fractal * (0.5 ** s)
        if octave <= 0:
            return
        minimum = math.floor(n_low / octave) * octave
        maximum = minimum + 2.0 * octave
        if maximum > n_high:
            maximum = minimum + octave
        diff = maximum - minimum
        if diff <= 0:
            return
        level0 = minimum
        level1 = minimum + diff / 8.0
        level4 = minimum + diff / 2.0
        level7 = minimum + diff * 7.0 / 8.0
        level8 = maximum
        close = float(candle.ClosePrice)
        margin = close * float(self._entry_margin_pct.Value) / 100.0
        bb_low = bb_value.LowBand
        bb_up = bb_value.UpBand
        if bb_low is None or bb_up is None:
            return
        lower = float(bb_low)
        upper = float(bb_up)
        stoch_k = stoch_value.K
        if stoch_k is None:
            return
        k_val = float(stoch_k)
        os_level = float(self._stoch_os.Value)
        ob_level = float(self._stoch_ob.Value)
        if self.Position <= 0 and k_val < os_level and close <= level1 + margin and close < upper:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif self.Position >= 0 and k_val > ob_level and close >= level7 - margin and close > lower:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        elif self.Position > 0 and (close >= level8 or close >= level4):
            self.SellMarket()
        elif self.Position < 0 and (close <= level0 or close <= level4):
            self.BuyMarket()

    def CreateClone(self):
        return murrey_bband_stochastic_strategy()
