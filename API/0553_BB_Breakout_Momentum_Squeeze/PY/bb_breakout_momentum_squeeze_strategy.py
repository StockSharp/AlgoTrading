import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, UnitTypes, Unit
from StockSharp.Algo.Indicators import BollingerBands, KeltnerChannels, AverageTrueRange, SimpleMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class bb_breakout_momentum_squeeze_strategy(Strategy):
    def __init__(self):
        super(bb_breakout_momentum_squeeze_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._bb_length = self.Param("BbLength", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("BB Breakout Length", "Length for Bollinger breakout calculation", "BB Breakout")
        self._bb_mult = self.Param("BbMultiplier", 1.0) \
            .SetDisplay("BB Breakout Mult", "Bollinger breakout multiplier", "BB Breakout")
        self._threshold = self.Param("Threshold", 0.0) \
            .SetDisplay("Threshold", "Middle line threshold", "BB Breakout")
        self._squeeze_length = self.Param("SqueezeLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Squeeze Length", "Length for squeeze calculation", "Squeeze")
        self._squeeze_bb_mult = self.Param("SqueezeBbMultiplier", 2.0) \
            .SetDisplay("Bollinger Mult", "Bollinger Band std multiplier for squeeze", "Squeeze")
        self._kc_mult = self.Param("KcMultiplier", 2.0) \
            .SetDisplay("Keltner Mult", "Keltner Channel multiplier", "Squeeze")
        self._atr_length = self.Param("AtrLength", 30) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Length", "ATR calculation length", "ATR")
        self._atr_mult = self.Param("AtrMultiplier", 1.4) \
            .SetDisplay("ATR Multiplier", "ATR stop multiplier", "ATR")
        self._rr_ratio = self.Param("RrRatio", 1.5) \
            .SetDisplay("RR Ratio", "Risk reward ratio", "Risk")

        self._prev_bull = None
        self._prev_bear = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(bb_breakout_momentum_squeeze_strategy, self).OnReseted()
        self._prev_bull = None
        self._prev_bear = None

    def _make_iv(self, ind, val, t):
        iv = DecimalIndicatorValue(ind, val, t)
        iv.IsFinal = True
        return iv

    def OnStarted2(self, time):
        super(bb_breakout_momentum_squeeze_strategy, self).OnStarted2(time)

        self._bb_breakout = BollingerBands()
        self._bb_breakout.Length = self._bb_length.Value
        self._bb_breakout.Width = self._bb_mult.Value

        self._squeeze_bb = BollingerBands()
        self._squeeze_bb.Length = self._squeeze_length.Value
        self._squeeze_bb.Width = self._squeeze_bb_mult.Value

        self._keltner = KeltnerChannels()
        self._keltner.Length = self._squeeze_length.Value
        self._keltner.Multiplier = self._kc_mult.Value

        self._atr = AverageTrueRange()
        self._atr.Length = self._atr_length.Value

        bb_len = self._bb_length.Value
        self._bull_num = SimpleMovingAverage()
        self._bull_num.Length = bb_len
        self._bull_den = SimpleMovingAverage()
        self._bull_den.Length = bb_len
        self._bear_num = SimpleMovingAverage()
        self._bear_num.Length = bb_len
        self._bear_den = SimpleMovingAverage()
        self._bear_den.Length = bb_len
        self._upper_band_ma = SimpleMovingAverage()
        self._upper_band_ma.Length = 3
        self._lower_band_ma = SimpleMovingAverage()
        self._lower_band_ma.Length = 3

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._bb_breakout, self._squeeze_bb, self._keltner, self._atr, self._process_candle).Start()

        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(1, UnitTypes.Percent)
        )

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, bb_breakout_iv, squeeze_bb_iv, keltner_iv, atr_iv):
        if candle.State != CandleStates.Finished:
            return

        bb_upper = bb_breakout_iv.UpBand
        bb_lower = bb_breakout_iv.LowBand
        if bb_upper is None or bb_lower is None:
            return
        breakout_upper = float(bb_upper)
        breakout_lower = float(bb_lower)

        sq_upper = squeeze_bb_iv.UpBand
        sq_lower = squeeze_bb_iv.LowBand
        if sq_upper is None or sq_lower is None:
            return
        squeeze_upper = float(sq_upper)
        squeeze_lower = float(sq_lower)

        kc_upper_val = keltner_iv.Upper
        kc_lower_val = keltner_iv.Lower
        if kc_upper_val is None or kc_lower_val is None:
            return
        kc_upper = float(kc_upper_val)
        kc_lower = float(kc_lower_val)

        if not atr_iv.IsFormed:
            return
        atr = float(atr_iv)

        close = float(candle.ClosePrice)
        t = candle.ServerTime

        bull_num_val = self._bull_num.Process(self._make_iv(self._bull_num, max(close - breakout_upper, 0.0), t))
        bull_den_val = self._bull_den.Process(self._make_iv(self._bull_den, abs(close - breakout_upper), t))
        bear_num_val = self._bear_num.Process(self._make_iv(self._bear_num, max(breakout_lower - close, 0.0), t))
        bear_den_val = self._bear_den.Process(self._make_iv(self._bear_den, abs(breakout_lower - close), t))

        if not self._bull_num.IsFormed or not self._bull_den.IsFormed or \
           not self._bear_num.IsFormed or not self._bear_den.IsFormed:
            return

        bull_num_f = float(bull_num_val)
        bull_den_f = float(bull_den_val)
        bear_num_f = float(bear_num_val)
        bear_den_f = float(bear_den_val)

        bull = 0.0 if bull_den_f == 0 else bull_num_f / bull_den_f * 100.0
        bear = 0.0 if bear_den_f == 0 else bear_num_f / bear_den_f * 100.0

        threshold = float(self._threshold.Value)

        bull_cross = self._prev_bull is not None and self._prev_bull <= threshold and bull > threshold
        bear_cross = self._prev_bear is not None and self._prev_bear <= threshold and bear > threshold

        self._prev_bull = bull
        self._prev_bear = bear

        atr_mult = float(self._atr_mult.Value)
        upper_band_val = self._upper_band_ma.Process(self._make_iv(self._upper_band_ma, close + atr * atr_mult, t))
        lower_band_val = self._lower_band_ma.Process(self._make_iv(self._lower_band_ma, close - atr * atr_mult, t))

        if not self._upper_band_ma.IsFormed or not self._lower_band_ma.IsFormed:
            return

        if bull_cross and self.Position == 0:
            self.BuyMarket()
        elif bear_cross and self.Position == 0:
            self.SellMarket()

    def CreateClone(self):
        return bb_breakout_momentum_squeeze_strategy()
