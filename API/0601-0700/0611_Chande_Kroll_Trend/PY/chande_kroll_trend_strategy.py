import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, Highest, Lowest, SimpleMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy

CALC_LINEAR = 0
CALC_EXPONENTIAL = 1
SIZING_LOOKBACK = 1560


class chande_kroll_trend_strategy(Strategy):
    """
    Chande Kroll Trend strategy.
    The high stop is the highest high of the last StopLength candles minus AtrMultiplier times ATR(AtrPeriod), the low stop the lowest
    low plus the same distance. Long only: buys when the close crosses above the low stop while above SMA(SmaLength), and closes the
    long when the close falls below the high stop. The order size is RiskMultiplier percent of the capital divided by the lowest close
    of the last 1560 candles; in Exponential mode the capital is the current equity, in Linear mode the starting capital.
    """

    def __init__(self):
        super(chande_kroll_trend_strategy, self).__init__()
        self._calc_mode = self.Param("CalcMode", CALC_EXPONENTIAL).SetDisplay("Calc Mode", "Position sizing mode (0 = Linear, 1 = Exponential)", "Sizing")
        self._risk_multiplier = self.Param("RiskMultiplier", 5.0).SetGreaterThanZero().SetDisplay("Risk Multiplier", "Percent of the capital committed to a position", "Sizing")
        self._atr_period = self.Param("AtrPeriod", 10).SetGreaterThanZero().SetDisplay("ATR Period", "ATR period", "Stops")
        self._atr_multiplier = self.Param("AtrMultiplier", 3.0).SetGreaterThanZero().SetDisplay("ATR Multiplier", "ATR multiplier of the stops", "Stops")
        self._stop_length = self.Param("StopLength", 21).SetGreaterThanZero().SetDisplay("Stop Length", "Candles of the Donchian extremes", "Stops")
        self._sma_length = self.Param("SmaLength", 21).SetGreaterThanZero().SetDisplay("SMA Length", "Period of the trend SMA", "Trend")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._lowest_close = None
        self._prev_close = None
        self._prev_low_stop = None
        self._initial_capital = Decimal(0)

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(chande_kroll_trend_strategy, self).OnReseted()
        self._lowest_close = None
        self._prev_close = None
        self._prev_low_stop = None
        self._initial_capital = Decimal(0)

    def OnStarted2(self, time):
        super(chande_kroll_trend_strategy, self).OnStarted2(time)

        self._prev_close = None
        self._prev_low_stop = None
        self._initial_capital = Decimal(0)
        portfolio = self.Portfolio
        if portfolio is not None:
            begin = portfolio.BeginValue
            if begin is not None and begin > 0:
                self._initial_capital = begin
            elif portfolio.CurrentValue is not None:
                self._initial_capital = portfolio.CurrentValue
        self._lowest_close = Lowest()
        self._lowest_close.Length = SIZING_LOOKBACK

        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value
        highest = Highest()
        highest.Length = self._stop_length.Value
        lowest = Lowest()
        lowest.Length = self._stop_length.Value
        sma = SimpleMovingAverage()
        sma.Length = self._sma_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(atr, highest, lowest, sma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _get_order_volume(self, lowest_close):
        if self._calc_mode.Value == CALC_EXPONENTIAL:
            capital = self._initial_capital + self.PnL
        else:
            capital = self._initial_capital

        if capital <= 0 or lowest_close <= 0:
            return self.Volume

        volume = capital * Decimal(self._risk_multiplier.Value) / Decimal(100) / lowest_close
        step = self.Security.VolumeStep if self.Security is not None and self.Security.VolumeStep is not None else Decimal(1)
        if step > 0:
            volume = Math.Floor(volume / step) * step

        return volume if volume > 0 else self.Volume

    def _process_candle(self, candle, atr_value, highest_value, lowest_value, sma_value):
        if candle.State != CandleStates.Finished:
            return

        close = candle.ClosePrice

        # The sizing low uses whatever history is available until the full lookback is reached.
        lowest_input = DecimalIndicatorValue(self._lowest_close, close, candle.OpenTime)
        lowest_input.IsFinal = True
        lowest_close = self._lowest_close.Process(lowest_input).GetValue[Decimal](None)

        if not atr_value.IsFormed or not highest_value.IsFormed or not lowest_value.IsFormed:
            return

        distance = Decimal(self._atr_multiplier.Value) * atr_value.GetValue[Decimal](None)
        high_stop = highest_value.GetValue[Decimal](None) - distance
        low_stop = lowest_value.GetValue[Decimal](None) + distance

        pc = self._prev_close
        pl = self._prev_low_stop
        self._prev_close = close
        self._prev_low_stop = low_stop

        if not sma_value.IsFormed or pc is None or pl is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position > 0:
            if close < high_stop:
                self.SellMarket(self.Position)
            return

        if self.Position == 0 and pc <= pl and close > low_stop and close > sma_value.GetValue[Decimal](None):
            self.BuyMarket(self._get_order_volume(lowest_close))

    def CreateClone(self):
        return chande_kroll_trend_strategy()
