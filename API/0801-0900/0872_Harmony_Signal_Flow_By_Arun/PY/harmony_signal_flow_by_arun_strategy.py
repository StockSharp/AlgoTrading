import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class harmony_signal_flow_by_arun_strategy(Strategy):
    """
    Harmony Signal Flow By Arun strategy.
    An RSI crossing above LowerThreshold buys and an RSI crossing below UpperThreshold sells, reversing an opposite position.
    Longs use BuyStopLoss and BuyTarget, shorts SellStopLoss and SellTarget, all in price steps from the entry. Any open position
    is closed once a day at 15:25 (UTC candle open time).
    """

    SESSION_CLOSE_MINUTES = 15 * 60 + 25

    def __init__(self):
        super(harmony_signal_flow_by_arun_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 5).SetGreaterThanZero().SetDisplay("RSI Period", "RSI period", "Indicators")
        self._lower_threshold = self.Param("LowerThreshold", 30.0).SetDisplay("Lower Threshold", "RSI level whose upward cross buys", "Indicators")
        self._upper_threshold = self.Param("UpperThreshold", 70.0).SetDisplay("Upper Threshold", "RSI level whose downward cross sells", "Indicators")
        self._buy_stop_loss = self.Param("BuyStopLoss", 100.0).SetNotNegative().SetDisplay("Buy Stop Loss", "Long stop loss in price steps", "Risk")
        self._buy_target = self.Param("BuyTarget", 150.0).SetNotNegative().SetDisplay("Buy Target", "Long target in price steps", "Risk")
        self._sell_stop_loss = self.Param("SellStopLoss", 100.0).SetNotNegative().SetDisplay("Sell Stop Loss", "Short stop loss in price steps", "Risk")
        self._sell_target = self.Param("SellTarget", 150.0).SetNotNegative().SetDisplay("Sell Target", "Short target in price steps", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_rsi = None
        self._entry_price = 0.0
        self._last_session_close = None

    def OnReseted(self):
        super(harmony_signal_flow_by_arun_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(harmony_signal_flow_by_arun_strategy, self).OnStarted2(time)

        self._reset_state()

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, rsi)

    def _process_candle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        rsi = float(rsi_value)
        prev_rsi = self._prev_rsi
        self._prev_rsi = rsi

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None:
            step = float(self.Security.PriceStep)

        # Daily close at 15:25.
        open_time = candle.OpenTime
        minutes = open_time.Hour * 60 + open_time.Minute
        day = open_time.Date
        if minutes >= self.SESSION_CLOSE_MINUTES and self._last_session_close != day:
            self._last_session_close = day
            if self.Position > 0:
                self.SellMarket(self.Position)
                return
            if self.Position < 0:
                self.BuyMarket(-self.Position)
                return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        if self.Position > 0:
            sl = float(self._buy_stop_loss.Value)
            tp = float(self._buy_target.Value)
            stop = sl > 0 and low <= self._entry_price - sl * step
            target = tp > 0 and high >= self._entry_price + tp * step
            if stop or target:
                self.SellMarket(self.Position)
                return
        elif self.Position < 0:
            sl = float(self._sell_stop_loss.Value)
            tp = float(self._sell_target.Value)
            stop = sl > 0 and high >= self._entry_price + sl * step
            target = tp > 0 and low <= self._entry_price - tp * step
            if stop or target:
                self.BuyMarket(-self.Position)
                return

        if prev_rsi is None:
            return

        lower = float(self._lower_threshold.Value)
        upper = float(self._upper_threshold.Value)

        if prev_rsi <= lower and rsi > lower and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._entry_price = float(candle.ClosePrice)
        elif prev_rsi >= upper and rsi < upper and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._entry_price = float(candle.ClosePrice)

    def CreateClone(self):
        return harmony_signal_flow_by_arun_strategy()
