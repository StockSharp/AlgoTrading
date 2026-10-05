import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, ExponentialMovingAverage, Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class fvg_positioning_average_with_200ema_auto_trading_strategy(Strategy):
    """
    FVG positioning average with 200 EMA strategy.
    A bullish fair value gap is a candle whose low is above the high two candles back by more than AtrMultiplier ATRs, a bearish gap
    the mirror. The levels of the last FvgLookback bullish and bearish gaps are averaged. A close crossing above the bearish average
    while both averages are above the EMA goes long, a close crossing below the bullish average while both are below the EMA goes short.
    The stop sits at the lowest low (highest high) of LookbackPeriod candles and the target at RiskReward times that risk.
    """

    ATR_LENGTH = 200

    def __init__(self):
        super(fvg_positioning_average_with_200ema_auto_trading_strategy, self).__init__()
        self._fvg_lookback = self.Param("FvgLookback", 30).SetGreaterThanZero().SetDisplay("FVG Lookback", "Number of recent gaps of each direction that are averaged", "FVG")
        self._atr_multiplier = self.Param("AtrMultiplier", 0.25).SetNotNegative().SetDisplay("ATR Multiplier", "Minimum gap size in ATRs", "FVG")
        self._lookback_period = self.Param("LookbackPeriod", 20).SetGreaterThanZero().SetDisplay("Lookback Period", "Candles used for the stop at the recent low or high", "Risk")
        self._ema_period = self.Param("EmaPeriod", 200).SetGreaterThanZero().SetDisplay("EMA Period", "Trend EMA period", "Indicators")
        self._risk_reward = self.Param("RiskReward", 1.5).SetGreaterThanZero().SetDisplay("Risk Reward", "Target distance as a multiple of the stop distance", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._bull_levels = []
        self._bear_levels = []
        self._high1 = None
        self._high2 = None
        self._low1 = None
        self._low2 = None
        self._prev_close = None
        self._prev_bull_avg = None
        self._prev_bear_avg = None
        self._stop_price = Decimal(0)
        self._take_price = Decimal(0)

    def OnReseted(self):
        super(fvg_positioning_average_with_200ema_auto_trading_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(fvg_positioning_average_with_200ema_auto_trading_strategy, self).OnStarted2(time)

        self._reset_state()

        atr = AverageTrueRange()
        atr.Length = self.ATR_LENGTH
        ema = ExponentialMovingAverage()
        ema.Length = self._ema_period.Value
        highest = Highest()
        highest.Length = self._lookback_period.Value
        lowest = Lowest()
        lowest.Length = self._lookback_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(atr, ema, highest, lowest, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _add_level(self, levels, level):
        levels.append(level)
        while len(levels) > self._fvg_lookback.Value:
            levels.pop(0)

    def _process_candle(self, candle, atr, ema, highest, lowest):
        if candle.State != CandleStates.Finished:
            return

        if self._high2 is not None and self._low2 is not None:
            min_gap = atr * Decimal(self._atr_multiplier.Value)
            if candle.LowPrice > self._high2 and candle.LowPrice - self._high2 > min_gap:
                self._add_level(self._bull_levels, candle.LowPrice)
            elif candle.HighPrice < self._low2 and self._low2 - candle.HighPrice > min_gap:
                self._add_level(self._bear_levels, candle.HighPrice)

        self._high2 = self._high1
        self._low2 = self._low1
        self._high1 = candle.HighPrice
        self._low1 = candle.LowPrice

        bull_avg = sum(self._bull_levels, Decimal(0)) / Decimal(len(self._bull_levels)) if self._bull_levels else None
        bear_avg = sum(self._bear_levels, Decimal(0)) / Decimal(len(self._bear_levels)) if self._bear_levels else None

        prev_close = self._prev_close
        prev_bull_avg = self._prev_bull_avg
        prev_bear_avg = self._prev_bear_avg
        self._prev_close = candle.ClosePrice
        self._prev_bull_avg = bull_avg
        self._prev_bear_avg = bear_avg

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._manage_position(candle):
            return

        if bull_avg is None or bear_avg is None or prev_close is None:
            return

        close = candle.ClosePrice
        rr = Decimal(self._risk_reward.Value)
        cross_up = prev_bear_avg is not None and prev_close <= prev_bear_avg and close > bear_avg
        cross_down = prev_bull_avg is not None and prev_close >= prev_bull_avg and close < bull_avg

        if cross_up and bull_avg > ema and bear_avg > ema and self.Position <= 0 and lowest < close:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._stop_price = lowest
            self._take_price = close + (close - lowest) * rr
        elif cross_down and bull_avg < ema and bear_avg < ema and self.Position >= 0 and highest > close:
            self.SellMarket(self.Volume + abs(self.Position))
            self._stop_price = highest
            self._take_price = close - (highest - close) * rr

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
        return fvg_positioning_average_with_200ema_auto_trading_strategy()
