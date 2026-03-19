import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergence
from StockSharp.Algo.Strategies import Strategy

class martingale_with_macd_kdj_opening_conditions_strategy(Strategy):
    """
    Martingale with MACD zero-line crossover entry and percent-based SL/TP.
    """

    def __init__(self):
        super(martingale_with_macd_kdj_opening_conditions_strategy, self).__init__()
        self._take_profit_pct = self.Param("TakeProfitPercent", 4.0).SetDisplay("TP %", "Take profit percent", "Risk")
        self._stop_loss_pct = self.Param("StopLossPercent", 8.0).SetDisplay("SL %", "Stop loss percent", "Risk")
        self._cooldown_bars = self.Param("CooldownBars", 30).SetDisplay("Cooldown", "Bars between trades", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Candles", "General")

        self._prev_macd = 0.0
        self._has_prev = False
        self._entry_price = 0.0
        self._bars_from_trade = 30

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(martingale_with_macd_kdj_opening_conditions_strategy, self).OnReseted()
        self._prev_macd = 0.0
        self._has_prev = False
        self._entry_price = 0.0
        self._bars_from_trade = self._cooldown_bars.Value

    def OnStarted(self, time):
        super(martingale_with_macd_kdj_opening_conditions_strategy, self).OnStarted(time)
        macd = MovingAverageConvergenceDivergence()
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(macd, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, macd)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, macd_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        macd = float(macd_val)
        if not self._has_prev:
            self._prev_macd = macd
            self._has_prev = True
            return
        close = float(candle.ClosePrice)
        self._bars_from_trade += 1
        can_trade = self._bars_from_trade >= self._cooldown_bars.Value
        cross_up = self._prev_macd <= 0 and macd > 0
        cross_down = self._prev_macd >= 0 and macd < 0
        tp_pct = float(self._take_profit_pct.Value) / 100.0
        sl_pct = float(self._stop_loss_pct.Value) / 100.0
        if self.Position == 0 and can_trade:
            if cross_up:
                self.BuyMarket()
                self._entry_price = close
                self._bars_from_trade = 0
            elif cross_down:
                self.SellMarket()
                self._entry_price = close
                self._bars_from_trade = 0
        elif self.Position > 0:
            tp = self._entry_price * (1.0 + tp_pct)
            sl = self._entry_price * (1.0 - sl_pct)
            if close >= tp or close <= sl or (can_trade and cross_down):
                self.SellMarket()
                self._entry_price = 0.0
                self._bars_from_trade = 0
        elif self.Position < 0:
            tp = self._entry_price * (1.0 - tp_pct)
            sl = self._entry_price * (1.0 + sl_pct)
            if close <= tp or close >= sl or (can_trade and cross_up):
                self.BuyMarket()
                self._entry_price = 0.0
                self._bars_from_trade = 0
        self._prev_macd = macd

    def CreateClone(self):
        return martingale_with_macd_kdj_opening_conditions_strategy()
