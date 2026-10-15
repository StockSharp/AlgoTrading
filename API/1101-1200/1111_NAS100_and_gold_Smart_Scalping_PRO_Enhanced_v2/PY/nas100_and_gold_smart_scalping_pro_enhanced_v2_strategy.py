import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

EMA_LENGTH = 9
RSI_LENGTH = 14
ATR_LENGTH = 14
TREND_LENGTH = 200
VOLUME_LENGTH = 20
VOLUME_SPIKE_FACTOR = 1.5


class nas100_and_gold_smart_scalping_pro_enhanced_v2_strategy(Strategy):
    """
    NAS100 and gold smart scalping strategy.
    Between StartHour and EndHour (UTC) a long opens when the close is above EMA9, the session VWAP and the 15 minute EMA200,
    RSI is above 50 and volume spikes above its average; a short is the mirror image. An opposite signal reverses the position.
    The stop loss and take profit are ATR multiples from the entry, optionally trailing, the size risks RiskPercent of equity
    on the stop distance and a new entry waits CooldownMins after the previous one.
    """

    def __init__(self):
        super(nas100_and_gold_smart_scalping_pro_enhanced_v2_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._risk_percent = self.Param("RiskPercent", 1.0).SetGreaterThanZero().SetDisplay("Risk %", "Percent of equity risked per trade", "Risk")
        self._atr_multiplier_sl = self.Param("AtrMultiplierSl", 1.0).SetGreaterThanZero().SetDisplay("ATR SL Mult", "ATR multiplier for the stop loss", "Risk")
        self._atr_multiplier_tp = self.Param("AtrMultiplierTp", 2.0).SetGreaterThanZero().SetDisplay("ATR TP Mult", "ATR multiplier for the take profit", "Risk")
        self._cooldown_mins = self.Param("CooldownMins", 30).SetNotNegative().SetDisplay("Cooldown (min)", "Minutes between entries", "General")
        self._start_hour = self.Param("StartHour", 13).SetRange(0, 23).SetDisplay("Start Hour", "Session start hour (UTC)", "Session")
        self._end_hour = self.Param("EndHour", 20).SetRange(0, 24).SetDisplay("End Hour", "Session end hour (UTC)", "Session")
        self._use_trailing = self.Param("UseTrailing", False).SetDisplay("Use Trailing", "Trail the stop by the ATR stop distance", "Risk")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type), (self.Security, DataType.TimeFrame(TimeSpan.FromMinutes(15)))]

    def _reset_state(self):
        self._trend_ema = None
        self._volumes = []
        self._vwap_date = None
        self._vwap_price_volume = 0.0
        self._vwap_volume = 0.0
        self._last_entry_time = None
        self._stop_price = 0.0
        self._take_price = 0.0
        self._stop_distance = 0.0

    def OnReseted(self):
        super(nas100_and_gold_smart_scalping_pro_enhanced_v2_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(nas100_and_gold_smart_scalping_pro_enhanced_v2_strategy, self).OnStarted2(time)

        self._reset_state()

        ema = ExponentialMovingAverage()
        ema.Length = EMA_LENGTH
        rsi = RelativeStrengthIndex()
        rsi.Length = RSI_LENGTH
        atr = AverageTrueRange()
        atr.Length = ATR_LENGTH
        self._trend_indicator = ExponentialMovingAverage()
        self._trend_indicator.Length = TREND_LENGTH

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, rsi, atr, self._process_candle).Start()

        self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromMinutes(15))).Bind(self._trend_indicator, self._process_trend).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _process_trend(self, candle, value):
        if candle.State == CandleStates.Finished and self._trend_indicator.IsFormed:
            self._trend_ema = float(value)

    def _process_candle(self, candle, ema_value, rsi_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        volume = float(candle.TotalVolume)
        self._volumes.append(volume)
        if len(self._volumes) > VOLUME_LENGTH:
            self._volumes.pop(0)

        # Session VWAP restarts every UTC day.
        open_time = candle.OpenTime
        date = open_time.Date
        if self._vwap_date is None or date != self._vwap_date:
            self._vwap_date = date
            self._vwap_price_volume = 0.0
            self._vwap_volume = 0.0

        typical = (float(candle.HighPrice) + float(candle.LowPrice) + float(candle.ClosePrice)) / 3.0
        self._vwap_price_volume += typical * volume
        self._vwap_volume += volume

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._manage_position(candle):
            return

        atr = float(atr_value)
        if self._trend_ema is None or len(self._volumes) < VOLUME_LENGTH or self._vwap_volume <= 0 or atr <= 0:
            return

        hour = open_time.Hour
        if hour < self._start_hour.Value or hour >= self._end_hour.Value:
            return

        if self._last_entry_time is not None and (open_time - self._last_entry_time).TotalMinutes < self._cooldown_mins.Value:
            return

        close = float(candle.ClosePrice)
        ema = float(ema_value)
        rsi = float(rsi_value)
        vwap = self._vwap_price_volume / self._vwap_volume
        volume_spike = volume > sum(self._volumes) / len(self._volumes) * VOLUME_SPIKE_FACTOR

        long_signal = close > ema and close > vwap and rsi > 50 and close > self._trend_ema and volume_spike
        short_signal = close < ema and close < vwap and rsi < 50 and close < self._trend_ema and volume_spike

        if long_signal and self.Position <= 0:
            self._enter(candle, True, atr)
        elif short_signal and self.Position >= 0:
            self._enter(candle, False, atr)

    def _enter(self, candle, is_long, atr):
        close = float(candle.ClosePrice)
        self._stop_distance = atr * float(self._atr_multiplier_sl.Value)

        volume = self.Volume
        portfolio = self.Portfolio
        equity = float(portfolio.CurrentValue) if portfolio is not None and portfolio.CurrentValue is not None else 0.0
        if equity > 0 and self._stop_distance > 0:
            risk_volume = equity * float(self._risk_percent.Value) / 100.0 / self._stop_distance
            if risk_volume > 0:
                volume = Decimal(risk_volume)

        take_distance = atr * float(self._atr_multiplier_tp.Value)
        if is_long:
            self.BuyMarket(volume + abs(self.Position))
            self._stop_price = close - self._stop_distance
            self._take_price = close + take_distance
        else:
            self.SellMarket(volume + abs(self.Position))
            self._stop_price = close + self._stop_distance
            self._take_price = close - take_distance

        self._last_entry_time = candle.OpenTime

    def _manage_position(self, candle):
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)

        if self.Position > 0:
            if low <= self._stop_price or high >= self._take_price:
                self.SellMarket(self.Position)
                return True
            if self._use_trailing.Value:
                self._stop_price = max(self._stop_price, close - self._stop_distance)
        elif self.Position < 0:
            if high >= self._stop_price or low <= self._take_price:
                self.BuyMarket(-self.Position)
                return True
            if self._use_trailing.Value:
                self._stop_price = min(self._stop_price, close + self._stop_distance)

        return False

    def CreateClone(self):
        return nas100_and_gold_smart_scalping_pro_enhanced_v2_strategy()
