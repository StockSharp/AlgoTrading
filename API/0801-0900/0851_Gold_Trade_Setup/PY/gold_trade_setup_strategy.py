import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import KaufmanAdaptiveMovingAverage, SuperTrend, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class gold_trade_setup_strategy(Strategy):
    """
    Gold trade setup strategy.
    A Kaufman adaptive moving average (AmaLength, FastLength, SlowLength) gives the direction and a SuperTrend (AtrPeriod, Factor) the
    flips. When the AMA is rising and the SuperTrend flips to an uptrend the strategy sells; when the AMA is falling and the SuperTrend
    flips to a downtrend it buys. The target is TargetMultiplier ATRs and the stop RiskMultiplier ATRs from the entry.
    """

    def __init__(self):
        super(gold_trade_setup_strategy, self).__init__()
        self._ama_length = self.Param("AmaLength", 14).SetGreaterThanZero().SetDisplay("AMA Length", "AMA efficiency ratio length", "AMA")
        self._fast_length = self.Param("FastLength", 2).SetGreaterThanZero().SetDisplay("Fast Length", "AMA fast smoothing period", "AMA")
        self._slow_length = self.Param("SlowLength", 30).SetGreaterThanZero().SetDisplay("Slow Length", "AMA slow smoothing period", "AMA")
        self._atr_period = self.Param("AtrPeriod", 10).SetGreaterThanZero().SetDisplay("ATR Period", "ATR period of SuperTrend and the exits", "SuperTrend")
        self._factor = self.Param("Factor", 3.0).SetGreaterThanZero().SetDisplay("Factor", "SuperTrend ATR factor", "SuperTrend")
        self._target_multiplier = self.Param("TargetMultiplier", 3.0).SetGreaterThanZero().SetDisplay("Target Multiplier", "Target distance in ATRs", "Risk")
        self._risk_multiplier = self.Param("RiskMultiplier", 1.0).SetGreaterThanZero().SetDisplay("Risk Multiplier", "Stop distance in ATRs", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_ama = None
        self._prev_up_trend = None
        self._stop_price = Decimal(0)
        self._take_price = Decimal(0)

    def OnReseted(self):
        super(gold_trade_setup_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(gold_trade_setup_strategy, self).OnStarted2(time)

        self._reset_state()

        ama = KaufmanAdaptiveMovingAverage()
        ama.Length = self._ama_length.Value
        ama.FastSCPeriod = self._fast_length.Value
        ama.SlowSCPeriod = self._slow_length.Value
        super_trend = SuperTrend()
        super_trend.Length = self._atr_period.Value
        super_trend.Multiplier = Decimal(self._factor.Value)
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(ama, super_trend, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ama)
            self.DrawIndicator(area, super_trend)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ama_value, super_trend_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not ama_value.IsFormed or not super_trend_value.IsFormed or not atr_value.IsFormed:
            return

        ama = ama_value.GetValue[Decimal](None)
        up_trend = bool(super_trend_value.IsUpTrend)
        prev_ama = self._prev_ama
        prev_up_trend = self._prev_up_trend
        self._prev_ama = ama
        self._prev_up_trend = up_trend

        if self._manage_position(candle):
            return

        if prev_ama is None or prev_up_trend is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading() or self.Position != 0:
            return

        atr = atr_value.GetValue[Decimal](None)
        close = candle.ClosePrice
        risk = Decimal(self._risk_multiplier.Value)
        target = Decimal(self._target_multiplier.Value)

        if ama > prev_ama and up_trend and not prev_up_trend:
            self.SellMarket(self.Volume)
            self._stop_price = close + atr * risk
            self._take_price = close - atr * target
        elif ama < prev_ama and not up_trend and prev_up_trend:
            self.BuyMarket(self.Volume)
            self._stop_price = close - atr * risk
            self._take_price = close + atr * target

    def _manage_position(self, candle):
        if self.Position > 0 and self._take_price > 0:
            if candle.LowPrice <= self._stop_price or candle.HighPrice >= self._take_price:
                self.SellMarket(self.Position)
                return True
        elif self.Position < 0 and self._stop_price > 0:
            if candle.HighPrice >= self._stop_price or candle.LowPrice <= self._take_price:
                self.BuyMarket(abs(self.Position))
                return True
        return False

    def CreateClone(self):
        return gold_trade_setup_strategy()
