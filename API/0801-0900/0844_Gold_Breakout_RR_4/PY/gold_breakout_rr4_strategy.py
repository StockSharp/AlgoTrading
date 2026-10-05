import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import DonchianChannels, AverageTrueRange, SimpleMovingAverage, WeightedMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class gold_breakout_rr4_strategy(Strategy):
    """
    Gold breakout RR4 strategy.
    The Donchian channel spans the previous DonchianLength candles. A close above its upper band with volume above the MaVolumeLength
    volume SMA and the Larry Williams Large Trade Index above 50 goes long; a close below the lower band with high volume and LWTI below
    50 goes short. LWTI is the LwtiLength WMA of the close change over LwtiLength candles divided by the ATR, scaled around 50 and smoothed
    with an SMA of LwtiSmooth. Entries are allowed only from StartHour to EndHour (UTC, wrapping over midnight) and once per day. The stop
    sits at the channel middle and the target at RiskReward times that risk.
    """

    def __init__(self):
        super(gold_breakout_rr4_strategy, self).__init__()
        self._donchian_length = self.Param("DonchianLength", 96).SetGreaterThanZero().SetDisplay("Donchian Length", "Donchian channel length", "Indicators")
        self._ma_volume_length = self.Param("MaVolumeLength", 30).SetGreaterThanZero().SetDisplay("Volume MA Length", "Length of the volume average", "Indicators")
        self._lwti_length = self.Param("LwtiLength", 25).SetGreaterThanZero().SetDisplay("LWTI Length", "LWTI length", "Indicators")
        self._lwti_smooth = self.Param("LwtiSmooth", 5).SetGreaterThanZero().SetDisplay("LWTI Smooth", "LWTI smoothing length", "Indicators")
        self._start_hour = self.Param("StartHour", 20).SetRange(0, 23).SetDisplay("Start Hour", "Session start hour (UTC)", "Session")
        self._end_hour = self.Param("EndHour", 8).SetRange(0, 23).SetDisplay("End Hour", "Session end hour (UTC)", "Session")
        self._risk_reward = self.Param("RiskReward", 4.0).SetGreaterThanZero().SetDisplay("Risk Reward", "Target distance as a multiple of the stop distance", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._volume_ma = None
        self._change_wma = None
        self._lwti_sma = None
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._closes = []
        self._prev_upper = None
        self._prev_lower = None
        self._last_trade_day = None
        self._stop_price = Decimal(0)
        self._take_price = Decimal(0)

    def OnReseted(self):
        super(gold_breakout_rr4_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(gold_breakout_rr4_strategy, self).OnStarted2(time)

        self._reset_state()

        donchian = DonchianChannels()
        donchian.Length = self._donchian_length.Value
        atr = AverageTrueRange()
        atr.Length = self._lwti_length.Value

        self._volume_ma = SimpleMovingAverage()
        self._volume_ma.Length = self._ma_volume_length.Value
        self._change_wma = WeightedMovingAverage()
        self._change_wma.Length = self._lwti_length.Value
        self._lwti_sma = SimpleMovingAverage()
        self._lwti_sma.Length = self._lwti_smooth.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(donchian, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, donchian)
            self.DrawOwnTrades(area)

    def _process_value(self, indicator, value, time):
        indicator_input = DecimalIndicatorValue(indicator, value, time)
        indicator_input.IsFinal = True
        return indicator.Process(indicator_input)

    def _process_candle(self, candle, donchian_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        # The channel is measured on the candles before this one.
        upper = self._prev_upper
        lower = self._prev_lower

        if donchian_value.IsFormed and donchian_value.UpperBand is not None and donchian_value.LowerBand is not None:
            self._prev_upper = donchian_value.UpperBand
            self._prev_lower = donchian_value.LowerBand

        volume_avg = self._process_value(self._volume_ma, candle.TotalVolume, candle.OpenTime)
        lwti = self._process_lwti(candle, atr_value)

        if self._manage_position(candle):
            return

        if upper is None or lower is None or lwti is None or not self._volume_ma.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        day = candle.OpenTime.Date
        if self.Position != 0 or not self._in_session(candle.OpenTime.Hour) or self._last_trade_day == day:
            return

        close = candle.ClosePrice
        high_volume = candle.TotalVolume > volume_avg.GetValue[Decimal](None)
        middle = (upper + lower) / Decimal(2)
        rr = Decimal(self._risk_reward.Value)

        if close > upper and high_volume and lwti > 50 and close > middle:
            self.BuyMarket(self.Volume)
            self._stop_price = middle
            self._take_price = close + (close - middle) * rr
            self._last_trade_day = day
        elif close < lower and high_volume and lwti < 50 and close < middle:
            self.SellMarket(self.Volume)
            self._stop_price = middle
            self._take_price = close - (middle - close) * rr
            self._last_trade_day = day

    def _process_lwti(self, candle, atr_value):
        length = self._lwti_length.Value
        self._closes.append(candle.ClosePrice)
        if len(self._closes) > length + 1:
            self._closes.pop(0)

        if len(self._closes) <= length:
            return None

        change = candle.ClosePrice - self._closes[0]
        wma = self._process_value(self._change_wma, change, candle.OpenTime)

        if not self._change_wma.IsFormed or not atr_value.IsFormed:
            return None

        atr = atr_value.GetValue[Decimal](None)
        if atr <= 0:
            return None

        raw = wma.GetValue[Decimal](None) / atr * Decimal(50) + Decimal(50)
        smooth = self._process_value(self._lwti_sma, raw, candle.OpenTime)

        return smooth.GetValue[Decimal](None) if self._lwti_sma.IsFormed else None

    def _in_session(self, hour):
        start = self._start_hour.Value
        end = self._end_hour.Value
        if start <= end:
            return start <= hour < end
        return hour >= start or hour < end

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
        return gold_breakout_rr4_strategy()
