import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import DonchianChannels
from StockSharp.Algo.Strategies import Strategy


class contrarian_dc_strategy(Strategy):
    """
    Contrarian Donchian Channel strategy.
    The channel spans the previous DonchianPeriod candles. A low at or below the lower band buys and a high at or above the upper
    band sells short. Each trade has a StopLossPercent stop and a target RiskRewardRatio times farther away, and is also closed when
    price reaches the opposite band. After a stop-loss, entries in the same direction pause for PauseCandles candles.
    """

    def __init__(self):
        super(contrarian_dc_strategy, self).__init__()
        self._donchian_period = self.Param("DonchianPeriod", 20).SetGreaterThanZero().SetDisplay("Donchian Period", "Previous candles the channel spans", "Indicators")
        self._risk_reward_ratio = self.Param("RiskRewardRatio", 1.7).SetGreaterThanZero().SetDisplay("Risk/Reward", "Target distance as a multiple of the stop distance", "Risk")
        self._stop_loss_percent = self.Param("StopLossPercent", 0.3).SetGreaterThanZero().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._pause_candles = self.Param("PauseCandles", 3).SetNotNegative().SetDisplay("Pause Candles", "Candles to skip same-direction entries after a stop-loss", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_upper = None
        self._prev_lower = None
        self._stop_price = None
        self._take_price = None
        self._long_pause = 0
        self._short_pause = 0

    def OnReseted(self):
        super(contrarian_dc_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(contrarian_dc_strategy, self).OnStarted2(time)

        self._reset_state()

        donchian = DonchianChannels()
        donchian.Length = self._donchian_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(donchian, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, donchian)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, donchian_value):
        if candle.State != CandleStates.Finished:
            return

        # The channel is measured on the candles before this one.
        upper = self._prev_upper
        lower = self._prev_lower

        if donchian_value.IsFormed and donchian_value.UpperBand is not None and donchian_value.LowerBand is not None:
            self._prev_upper = donchian_value.UpperBand
            self._prev_lower = donchian_value.LowerBand

        if self._long_pause > 0:
            self._long_pause -= 1
        if self._short_pause > 0:
            self._short_pause -= 1

        if upper is None or lower is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position > 0:
            if self._stop_price is not None and candle.LowPrice <= self._stop_price:
                self.SellMarket(self.Position)
                self._long_pause = self._pause_candles.Value
                self._clear_levels()
            elif (self._take_price is not None and candle.HighPrice >= self._take_price) or candle.HighPrice >= upper:
                self.SellMarket(self.Position)
                self._clear_levels()
            return

        if self.Position < 0:
            if self._stop_price is not None and candle.HighPrice >= self._stop_price:
                self.BuyMarket(-self.Position)
                self._short_pause = self._pause_candles.Value
                self._clear_levels()
            elif (self._take_price is not None and candle.LowPrice <= self._take_price) or candle.LowPrice <= lower:
                self.BuyMarket(-self.Position)
                self._clear_levels()
            return

        close = candle.ClosePrice
        stop_distance = close * Decimal(self._stop_loss_percent.Value) / Decimal(100)
        ratio = Decimal(self._risk_reward_ratio.Value)

        if candle.LowPrice <= lower and self._long_pause == 0:
            self.BuyMarket(self.Volume)
            self._stop_price = close - stop_distance
            self._take_price = close + stop_distance * ratio
        elif candle.HighPrice >= upper and self._short_pause == 0:
            self.SellMarket(self.Volume)
            self._stop_price = close + stop_distance
            self._take_price = close - stop_distance * ratio

    def _clear_levels(self):
        self._stop_price = None
        self._take_price = None

    def CreateClone(self):
        return contrarian_dc_strategy()
