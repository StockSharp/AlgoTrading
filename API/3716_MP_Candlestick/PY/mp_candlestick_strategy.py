import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class mp_candlestick_strategy(Strategy):
    """Candlestick direction strategy with ATR or fixed stop-loss and risk-reward management."""

    def __init__(self):
        super(mp_candlestick_strategy, self).__init__()

        self._risk_percent = self.Param("RiskPercent", 1.0) \
            .SetNotNegative() \
            .SetDisplay("Risk Percent", "Percentage of portfolio equity risked per trade", "Risk")
        self._risk_reward_ratio = self.Param("RiskRewardRatio", 1.5) \
            .SetGreaterThanZero() \
            .SetDisplay("Risk/Reward Ratio", "Target reward multiple relative to initial risk", "Risk")
        self._max_margin_usage = self.Param("MaxMarginUsage", 30.0) \
            .SetNotNegative() \
            .SetDisplay("Max Margin Usage", "Upper bound for margin consumption percent", "Risk")
        self._stop_loss_pips = self.Param("StopLossPips", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop-Loss Pips", "Fixed stop-loss size in pips", "Risk")
        self._use_auto_sl = self.Param("UseAutoSl", True) \
            .SetDisplay("Use ATR Stop", "If enabled the stop-loss uses ATR * 1.5", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Primary candle series for signals", "Data")

        self._atr = None
        self._entry_price = None
        self._stop_price = None
        self._take_profit_price = None
        self._is_long_position = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def RiskPercent(self):
        return self._risk_percent.Value

    @property
    def RiskRewardRatio(self):
        return self._risk_reward_ratio.Value

    @property
    def StopLossPips(self):
        return self._stop_loss_pips.Value

    @property
    def UseAutoSl(self):
        return self._use_auto_sl.Value

    def OnReseted(self):
        super(mp_candlestick_strategy, self).OnReseted()
        self._atr = None
        self._entry_price = None
        self._stop_price = None
        self._take_profit_price = None
        self._is_long_position = False

    def OnStarted2(self, time):
        super(mp_candlestick_strategy, self).OnStarted2(time)

        self._atr = AverageTrueRange()
        self._atr.Length = 14

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._atr, self._process_candle).Start()

    def _process_candle(self, candle, atr_value):
        if candle.State != CandleStates.Finished:
            return

        atr_v = float(atr_value)

        self._check_risk_levels(candle)

        if self.Position != 0:
            return

        is_bullish = float(candle.ClosePrice) > float(candle.OpenPrice)
        is_bearish = float(candle.ClosePrice) < float(candle.OpenPrice)

        if not is_bullish and not is_bearish:
            return

        price_step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None:
            ps = float(self.Security.PriceStep)
            if ps > 0:
                price_step = ps

        if self.UseAutoSl:
            distance = atr_v * 1.5
        else:
            distance = self.StopLossPips * price_step

        if distance <= 0:
            return

        entry_price = float(candle.ClosePrice)
        rr = float(self.RiskRewardRatio)

        if is_bullish:
            stop_price = entry_price - distance
            take_profit = entry_price + distance * rr
        else:
            stop_price = entry_price + distance
            take_profit = entry_price - distance * rr

        if stop_price <= 0 or take_profit <= 0:
            return

        if is_bullish:
            self.BuyMarket()
        else:
            self.SellMarket()

        self._entry_price = entry_price
        self._stop_price = stop_price
        self._take_profit_price = take_profit
        self._is_long_position = is_bullish

    def _check_risk_levels(self, candle):
        if self.Position > 0:
            if self._stop_price is not None and float(candle.LowPrice) <= self._stop_price:
                self.SellMarket(self.Position)
                self._reset_risk_levels()
                return
            if self._take_profit_price is not None and float(candle.HighPrice) >= self._take_profit_price:
                self.SellMarket(self.Position)
                self._reset_risk_levels()
        elif self.Position < 0:
            if self._stop_price is not None and float(candle.HighPrice) >= self._stop_price:
                self.BuyMarket(abs(self.Position))
                self._reset_risk_levels()
                return
            if self._take_profit_price is not None and float(candle.LowPrice) <= self._take_profit_price:
                self.BuyMarket(abs(self.Position))
                self._reset_risk_levels()

    def _reset_risk_levels(self):
        self._entry_price = None
        self._stop_price = None
        self._take_profit_price = None
        self._is_long_position = False

    def CreateClone(self):
        return mp_candlestick_strategy()
