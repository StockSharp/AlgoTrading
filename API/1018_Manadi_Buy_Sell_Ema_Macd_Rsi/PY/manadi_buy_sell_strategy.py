import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class manadi_buy_sell_strategy(Strategy):
    """
    Manadi Buy Sell: EMA crossover with RSI filter and percent-based SL/TP.
    """

    def __init__(self):
        super(manadi_buy_sell_strategy, self).__init__()
        self._fast_ema_length = self.Param("FastEmaLength", 9).SetDisplay("Fast EMA", "Fast EMA", "Indicators")
        self._slow_ema_length = self.Param("SlowEmaLength", 21).SetDisplay("Slow EMA", "Slow EMA", "Indicators")
        self._rsi_length = self.Param("RsiLength", 14).SetDisplay("RSI", "RSI period", "Indicators")
        self._take_profit_pct = self.Param("TakeProfitPercent", 0.15).SetDisplay("TP %", "Take profit percent", "Risk")
        self._stop_loss_pct = self.Param("StopLossPercent", 0.06).SetDisplay("SL %", "Stop loss percent", "Risk")
        self._cooldown_bars = self.Param("CooldownBars", 100).SetDisplay("Cooldown", "Bars between trades", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))).SetDisplay("Candle Type", "Candles", "General")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._stop_price = 0.0
        self._take_profit_price = 0.0
        self._bars_from_trade = 100

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(manadi_buy_sell_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._stop_price = 0.0
        self._take_profit_price = 0.0
        self._bars_from_trade = self._cooldown_bars.Value

    def OnStarted(self, time):
        super(manadi_buy_sell_strategy, self).OnStarted(time)
        fast = ExponentialMovingAverage()
        fast.Length = self._fast_ema_length.Value
        slow = ExponentialMovingAverage()
        slow.Length = self._slow_ema_length.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, rsi, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_val, slow_val, rsi_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        fast = float(fast_val)
        slow = float(slow_val)
        rsi = float(rsi_val)
        if self._prev_fast == 0.0 or self._prev_slow == 0.0:
            self._prev_fast = fast
            self._prev_slow = slow
            return
        close = float(candle.ClosePrice)
        bull_cross = self._prev_fast <= self._prev_slow and fast > slow
        bear_cross = self._prev_fast >= self._prev_slow and fast < slow
        long_cond = bull_cross and rsi < 70 and rsi > 40
        short_cond = bear_cross and rsi > 30 and rsi < 60
        self._bars_from_trade += 1
        can_enter = self._bars_from_trade >= self._cooldown_bars.Value
        sl_pct = float(self._stop_loss_pct.Value)
        tp_pct = float(self._take_profit_pct.Value)
        if can_enter and long_cond and self.Position <= 0:
            self.BuyMarket()
            self._stop_price = close * (1.0 - sl_pct)
            self._take_profit_price = close * (1.0 + tp_pct)
            self._bars_from_trade = 0
        elif can_enter and short_cond and self.Position >= 0:
            self.SellMarket()
            self._stop_price = close * (1.0 + sl_pct)
            self._take_profit_price = close * (1.0 - tp_pct)
            self._bars_from_trade = 0
        elif self.Position > 0:
            if float(candle.LowPrice) <= self._stop_price or float(candle.HighPrice) >= self._take_profit_price:
                self.SellMarket()
                self._bars_from_trade = 0
        elif self.Position < 0:
            if float(candle.HighPrice) >= self._stop_price or float(candle.LowPrice) <= self._take_profit_price:
                self.BuyMarket()
                self._bars_from_trade = 0
        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return manadi_buy_sell_strategy()
