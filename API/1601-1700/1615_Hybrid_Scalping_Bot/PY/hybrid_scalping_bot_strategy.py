import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class hybrid_scalping_bot_strategy(Strategy):
    def __init__(self):
        super(hybrid_scalping_bot_strategy, self).__init__()
        self._daily_trade_limit = self.Param("DailyTradeLimit", 15) \
            .SetDisplay("Daily Trades", "Maximum trades per day", "General")
        self._take_profit_percent = self.Param("TakeProfitPercent", 0.8) \
            .SetDisplay("Take Profit %", "Take profit percentage", "Risk")
        self._stop_loss_percent = self.Param("StopLossPercent", 0.6) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
        self._use_quick_exit = self.Param("UseQuickExit", True) \
            .SetDisplay("Use Quick Exit", "Exit on RSI or EMA pullback", "General")
        self._use_trailing_stop = self.Param("UseTrailingStop", True) \
            .SetDisplay("Use Trailing Stop", "Trail profit after entry", "General")
        self._trailing_stop_percent = self.Param("TrailingStopPercent", 0.4) \
            .SetDisplay("Trailing Stop %", "Trailing stop percent", "Risk")
        self._signal_sensitivity = self.Param("SignalSensitivity", "Easy") \
            .SetDisplay("Signal Level", "VeryEasy / Easy / Medium / Strong", "General")
        self._use_trend_filter = self.Param("UseTrendFilter", True) \
            .SetDisplay("Use Trend Filter", "Trade only with trend", "General")
        self._use_volume_filter = self.Param("UseVolumeFilter", False) \
            .SetDisplay("Use Volume Filter", "Require high volume", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._entry_price = 0.0
        self._highest_price = 0.0
        self._lowest_price = 0.0
        self._trades_today = 0
        self._last_date = None

    @property
    def daily_trade_limit(self):
        return self._daily_trade_limit.Value

    @property
    def take_profit_percent(self):
        return self._take_profit_percent.Value

    @property
    def stop_loss_percent(self):
        return self._stop_loss_percent.Value

    @property
    def use_quick_exit(self):
        return self._use_quick_exit.Value

    @property
    def use_trailing_stop(self):
        return self._use_trailing_stop.Value

    @property
    def trailing_stop_percent(self):
        return self._trailing_stop_percent.Value

    @property
    def signal_sensitivity(self):
        return self._signal_sensitivity.Value

    @property
    def use_trend_filter(self):
        return self._use_trend_filter.Value

    @property
    def use_volume_filter(self):
        return self._use_volume_filter.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(hybrid_scalping_bot_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._highest_price = 0.0
        self._lowest_price = 0.0
        self._trades_today = 0
        self._last_date = None

    def OnStarted2(self, time):
        super(hybrid_scalping_bot_strategy, self).OnStarted2(time)
        rsi = RelativeStrengthIndex()
        rsi.Length = 14
        ema9 = ExponentialMovingAverage()
        ema9.Length = 9
        ema21 = ExponentialMovingAverage()
        ema21.Length = 21
        ema50 = ExponentialMovingAverage()
        ema50.Length = 50
        volume_sma = SimpleMovingAverage()
        volume_sma.Length = 10
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, ema9, ema21, ema50, volume_sma, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, rsi, ema9, ema21, ema50, avg_volume):
        if candle.State != CandleStates.Finished:
            return
        if candle.OpenTime.Date != self._last_date:
            self._trades_today = 0
            self._last_date = candle.OpenTime.Date
        bullish = candle.ClosePrice > candle.OpenPrice
        bearish = candle.ClosePrice < candle.OpenPrice
        body_ratio = (0 if candle.HighPrice == candle.LowPrice else (candle.ClosePrice - candle.OpenPrice) / (candle.HighPrice - candle.LowPrice))
        strong_bullish = bullish and body_ratio > 0.6
        strong_bearish = bearish and body_ratio > 0.6
        uptrend = ema21 > ema50
        downtrend = ema21 < ema50
        strong_uptrend = ema9 > ema21 and ema21 > ema50
        strong_downtrend = ema9 < ema21 and ema21 < ema50
        volume_ok = not self.use_volume_filter or candle.TotalVolume > avg_volume * 1.2
        sensitivity = self.signal_sensitivity
        if sensitivity == "VeryEasy":
            buy_signal = rsi < 60 and bullish
            sell_signal = rsi > 40 and bearish
        elif sensitivity == "Medium":
            buy_signal = rsi < 30 and bullish and (not self.use_trend_filter or uptrend)
            sell_signal = rsi > 70 and bearish and (not self.use_trend_filter or downtrend)
        elif sensitivity == "Strong":
            buy_signal = rsi < 30 and strong_bullish and (not self.use_trend_filter or strong_uptrend) and volume_ok and candle.ClosePrice > ema21
            sell_signal = rsi > 70 and strong_bearish and (not self.use_trend_filter or strong_downtrend) and volume_ok and candle.ClosePrice < ema21
        else:
            buy_signal = rsi < 30 and bullish
            sell_signal = rsi > 70 and bearish
        can_trade = self._trades_today < self.daily_trade_limit and self.Position == 0
        if buy_signal and can_trade:
            self.BuyMarket()
            self._trades_today += 1
            self._entry_price = candle.ClosePrice
            self._highest_price = candle.ClosePrice
        elif sell_signal and can_trade:
            self.SellMarket()
            self._trades_today += 1
            self._entry_price = candle.ClosePrice
            self._lowest_price = candle.ClosePrice
        if self.Position > 0:
            self._highest_price = max(self._highest_price, candle.HighPrice)
            if self.use_trailing_stop:
                trail = self._highest_price * (1 - self.trailing_stop_percent / 100)
                if candle.ClosePrice <= trail:
                    self.SellMarket()
            if candle.ClosePrice <= self._entry_price * (1 - self.stop_loss_percent / 100):
                self.SellMarket()
            elif candle.ClosePrice >= self._entry_price * (1 + self.take_profit_percent / 100):
                self.SellMarket()
            elif self.use_quick_exit and (rsi > 70 or rsi < 25 or candle.ClosePrice < ema21):
                self.SellMarket()
        elif self.Position < 0:
            self._lowest_price = min(self._lowest_price, candle.LowPrice)
            if self.use_trailing_stop:
                trail = self._lowest_price * (1 + self.trailing_stop_percent / 100)
                if candle.ClosePrice >= trail:
                    self.BuyMarket()
            if candle.ClosePrice >= self._entry_price * (1 + self.stop_loss_percent / 100):
                self.BuyMarket()
            elif candle.ClosePrice <= self._entry_price * (1 - self.take_profit_percent / 100):
                self.BuyMarket()
            elif self.use_quick_exit and (rsi < 30 or rsi > 75 or candle.ClosePrice > ema21):
                self.BuyMarket()

    def CreateClone(self):
        return hybrid_scalping_bot_strategy()
