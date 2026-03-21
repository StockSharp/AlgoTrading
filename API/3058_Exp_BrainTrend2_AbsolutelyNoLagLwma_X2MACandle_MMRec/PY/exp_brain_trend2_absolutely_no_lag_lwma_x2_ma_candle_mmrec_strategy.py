import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Sides
from StockSharp.Algo.Indicators import AverageTrueRange, WeightedMovingAverage, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class exp_brain_trend2_absolutely_no_lag_lwma_x2_ma_candle_mmrec_strategy(Strategy):
    def __init__(self):
        super(exp_brain_trend2_absolutely_no_lag_lwma_x2_ma_candle_mmrec_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(6))) \
            .SetDisplay("Candle Type", "Primary candle series for the strategy", "General")
        self._atr_period = self.Param("AtrPeriod", 7) \
            .SetDisplay("ATR Period", "Average True Range lookback", "Indicators")
        self._lwma_length = self.Param("LwmaLength", 7) \
            .SetDisplay("LWMA Length", "Linear weighted moving average length", "Indicators")
        self._fast_ma_length = self.Param("FastMaLength", 9) \
            .SetDisplay("Fast EMA", "Fast EMA length", "Indicators")
        self._slow_ma_length = self.Param("SlowMaLength", 21) \
            .SetDisplay("Slow EMA", "Slow EMA length", "Indicators")
        self._stop_loss_atr_multiplier = self.Param("StopLossAtrMultiplier", 2.0) \
            .SetDisplay("Stop Loss (ATR)", "Multiplier for protective stop", "Risk Management")
        self._take_profit_atr_multiplier = self.Param("TakeProfitAtrMultiplier", 3.0) \
            .SetDisplay("Take Profit (ATR)", "Multiplier for profit target", "Risk Management")

        self._allow_long_signal = False
        self._allow_short_signal = False
        self._long_entry_price = None
        self._short_entry_price = None

    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def AtrPeriod(self):
        return self._atr_period.Value
    @property
    def LwmaLength(self):
        return self._lwma_length.Value
    @property
    def FastMaLength(self):
        return self._fast_ma_length.Value
    @property
    def SlowMaLength(self):
        return self._slow_ma_length.Value
    @property
    def StopLossAtrMultiplier(self):
        return self._stop_loss_atr_multiplier.Value
    @property
    def TakeProfitAtrMultiplier(self):
        return self._take_profit_atr_multiplier.Value

    def OnReseted(self):
        super(exp_brain_trend2_absolutely_no_lag_lwma_x2_ma_candle_mmrec_strategy, self).OnReseted()
        self._allow_long_signal = False
        self._allow_short_signal = False
        self._long_entry_price = None
        self._short_entry_price = None

    def OnStarted(self, time):
        super(exp_brain_trend2_absolutely_no_lag_lwma_x2_ma_candle_mmrec_strategy, self).OnStarted(time)
        self._allow_long_signal = False
        self._allow_short_signal = False
        self._long_entry_price = None
        self._short_entry_price = None
        atr = AverageTrueRange()
        atr.Length = self.AtrPeriod
        lwma = WeightedMovingAverage()
        lwma.Length = self.LwmaLength
        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self.FastMaLength
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self.SlowMaLength
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(atr, lwma, fast_ema, slow_ema, self._on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, lwma)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_ema)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, atr_value, lwma_value, fast_ema_value, slow_ema_value):
        if candle.State != CandleStates.Finished:
            return

        av = float(atr_value)
        lv = float(lwma_value)
        fv = float(fast_ema_value)
        sv = float(slow_ema_value)
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        bullish_filter = close > lv and fv > sv
        bearish_filter = close < lv and fv < sv

        if not bullish_filter:
            self._allow_long_signal = True
        if not bearish_filter:
            self._allow_short_signal = True

        if bullish_filter and self.Position <= 0 and self._allow_long_signal:
            self.BuyMarket()
            self._allow_long_signal = False
            self._allow_short_signal = False
        elif bearish_filter and self.Position >= 0 and self._allow_short_signal:
            self.SellMarket()
            self._allow_short_signal = False
            self._allow_long_signal = False

        sl_mult = float(self.StopLossAtrMultiplier)
        tp_mult = float(self.TakeProfitAtrMultiplier)

        if self.Position > 0 and self._long_entry_price is not None:
            stop_price = self._long_entry_price - av * sl_mult
            target_price = self._long_entry_price + av * tp_mult
            if low <= stop_price:
                self.SellMarket()
                self._long_entry_price = None
            elif high >= target_price:
                self.SellMarket()
                self._long_entry_price = None
        elif self.Position < 0 and self._short_entry_price is not None:
            stop_price = self._short_entry_price + av * sl_mult
            target_price = self._short_entry_price - av * tp_mult
            if high >= stop_price:
                self.BuyMarket()
                self._short_entry_price = None
            elif low <= target_price:
                self.BuyMarket()
                self._short_entry_price = None

    def OnOwnTradeReceived(self, trade):
        super(exp_brain_trend2_absolutely_no_lag_lwma_x2_ma_candle_mmrec_strategy, self).OnOwnTradeReceived(trade)
        if trade.Order.Side == Sides.Buy:
            self._long_entry_price = float(trade.Trade.Price) if trade.Trade is not None else None
            if self.Position <= 0:
                self._short_entry_price = None
        elif trade.Order.Side == Sides.Sell:
            self._short_entry_price = float(trade.Trade.Price) if trade.Trade is not None else None
            if self.Position >= 0:
                self._long_entry_price = None
        if self.Position == 0:
            self._long_entry_price = None
            self._short_entry_price = None

    def CreateClone(self):
        return exp_brain_trend2_absolutely_no_lag_lwma_x2_ma_candle_mmrec_strategy()
