import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class magic_wand_stsm_strategy(Strategy):
    """
    Magic Wand STSM: custom SuperTrend with SMA filter and risk/reward targets.
    """

    def __init__(self):
        super(magic_wand_stsm_strategy, self).__init__()
        self._supertrend_period = self.Param("SupertrendPeriod", 10).SetDisplay("ST Period", "SuperTrend ATR period", "SuperTrend")
        self._supertrend_mult = self.Param("SupertrendMultiplier", 3.0).SetDisplay("ST Mult", "SuperTrend multiplier", "SuperTrend")
        self._ma_length = self.Param("MaLength", 50).SetDisplay("MA Length", "SMA filter period", "Indicators")
        self._risk_reward = self.Param("RiskReward", 3.0).SetDisplay("Risk/Reward", "Risk/reward ratio", "Risk")
        self._cooldown_bars = self.Param("CooldownBars", 80).SetDisplay("Cooldown", "Bars between trades", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))).SetDisplay("Candle Type", "Candles", "General")

        self._prev_highest = 0.0
        self._prev_lowest = 0.0
        self._prev_supertrend = 0.0
        self._prev_close = 0.0
        self._is_first = True
        self._stop = 0.0
        self._take = 0.0
        self._bars_from_trade = 80

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(magic_wand_stsm_strategy, self).OnReseted()
        self._prev_highest = 0.0
        self._prev_lowest = 0.0
        self._prev_supertrend = 0.0
        self._prev_close = 0.0
        self._is_first = True
        self._stop = 0.0
        self._take = 0.0
        self._bars_from_trade = self._cooldown_bars.Value

    def OnStarted(self, time):
        super(magic_wand_stsm_strategy, self).OnStarted(time)
        self._prev_highest = 0.0
        self._prev_lowest = 0.0
        self._prev_supertrend = 0.0
        self._prev_close = 0.0
        self._is_first = True
        self._stop = 0.0
        self._take = 0.0
        self._atr = AverageTrueRange()
        self._atr.Length = self._supertrend_period.Value
        self._sma = SimpleMovingAverage()
        self._sma.Length = self._ma_length.Value
        self._bars_from_trade = self._cooldown_bars.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._atr, self._sma, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._sma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, atr_val, sma_val):
        if candle.State != CandleStates.Finished:
            return
        if not self._atr.IsFormed or not self._sma.IsFormed:
            return
        atr = float(atr_val)
        sma = float(sma_val)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        median = (high + low) / 2.0
        mult = float(self._supertrend_mult.Value)
        upper_band = median + mult * atr
        lower_band = median - mult * atr
        if self._is_first:
            self._prev_highest = upper_band
            self._prev_lowest = lower_band
            self._prev_supertrend = upper_band if close <= upper_band else lower_band
            self._prev_close = close
            self._is_first = False
            return
        current_upper = upper_band if (upper_band < self._prev_highest or self._prev_close > self._prev_highest) else self._prev_highest
        current_lower = lower_band if (lower_band > self._prev_lowest or self._prev_close < self._prev_lowest) else self._prev_lowest
        if self._prev_supertrend == self._prev_highest:
            supertrend = current_upper if close <= current_upper else current_lower
        else:
            supertrend = current_lower if close >= current_lower else current_upper
        is_up_trend = close > supertrend
        self._bars_from_trade += 1
        can_enter = self._bars_from_trade >= self._cooldown_bars.Value
        rr = float(self._risk_reward.Value)
        if self.Position == 0 and can_enter:
            if is_up_trend and close > sma:
                self.BuyMarket()
                self._stop = supertrend
                self._take = close + (close - self._stop) * rr
                self._bars_from_trade = 0
            elif not is_up_trend and close < sma:
                self.SellMarket()
                self._stop = supertrend
                self._take = close - (self._stop - close) * rr
                self._bars_from_trade = 0
        elif self.Position > 0:
            if low <= self._stop or close >= self._take:
                self.SellMarket()
                self._bars_from_trade = 0
        elif self.Position < 0:
            if high >= self._stop or close <= self._take:
                self.BuyMarket()
                self._bars_from_trade = 0
        self._prev_highest = current_upper
        self._prev_lowest = current_lower
        self._prev_supertrend = supertrend
        self._prev_close = close

    def CreateClone(self):
        return magic_wand_stsm_strategy()
