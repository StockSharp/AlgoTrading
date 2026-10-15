import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import HullMovingAverage, CommodityChannelIndex, ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class double_cci_confirmed_hull_moving_average_reversal_strategy(Strategy):
    """
    Double CCI Confirmed Hull MA Reversal strategy.
    Long only: buys when the close crosses above the Hull Moving Average while both the fast and the slow CCI are above zero.
    At entry the stop is set StopLossAtrMultiplier ATRs below the close and the trailing activation level TrailingActivationMultiplier
    ATRs above it. The long closes when a candle low reaches the stop, or, once a high has reached the activation level, when the
    close falls below the trailing EMA.
    """

    def __init__(self):
        super(double_cci_confirmed_hull_moving_average_reversal_strategy, self).__init__()
        self._stop_loss_atr_multiplier = self.Param("StopLossAtrMultiplier", 1.75).SetGreaterThanZero().SetDisplay("Stop ATR Mult", "Stop distance in ATRs below the entry close", "Risk")
        self._trailing_activation_multiplier = self.Param("TrailingActivationMultiplier", 2.25).SetGreaterThanZero().SetDisplay("Trailing Activation Mult", "Profit in ATRs that activates the trailing EMA exit", "Risk")
        self._fast_cci_period = self.Param("FastCciPeriod", 25).SetGreaterThanZero().SetDisplay("Fast CCI", "Fast CCI period", "Indicators")
        self._slow_cci_period = self.Param("SlowCciPeriod", 50).SetGreaterThanZero().SetDisplay("Slow CCI", "Slow CCI period", "Indicators")
        self._hull_ma_length = self.Param("HullMaLength", 34).SetGreaterThanZero().SetDisplay("Hull MA Length", "Hull Moving Average period", "Indicators")
        self._trailing_ema_length = self.Param("TrailingEmaLength", 20).SetGreaterThanZero().SetDisplay("Trailing EMA", "Trailing EMA period", "Risk")
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero().SetDisplay("ATR Period", "ATR period", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_close = None
        self._prev_hma = None
        self._stop_price = Decimal(0)
        self._activation_price = Decimal(0)
        self._trailing_active = False

    def OnReseted(self):
        super(double_cci_confirmed_hull_moving_average_reversal_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(double_cci_confirmed_hull_moving_average_reversal_strategy, self).OnStarted2(time)

        self._reset_state()

        hma = HullMovingAverage()
        hma.Length = self._hull_ma_length.Value
        fast_cci = CommodityChannelIndex()
        fast_cci.Length = self._fast_cci_period.Value
        slow_cci = CommodityChannelIndex()
        slow_cci.Length = self._slow_cci_period.Value
        trailing_ema = ExponentialMovingAverage()
        trailing_ema.Length = self._trailing_ema_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(hma, fast_cci, slow_cci, trailing_ema, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, hma)
            self.DrawIndicator(area, trailing_ema)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, fast_cci)
                self.DrawIndicator(oscillators, slow_cci)

    def _process_candle(self, candle, hma_value, fast_cci_value, slow_cci_value, ema_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not hma_value.IsFormed or not fast_cci_value.IsFormed or not slow_cci_value.IsFormed or not ema_value.IsFormed or not atr_value.IsFormed:
            return

        hma = hma_value.GetValue[Decimal](None)
        fast_cci = fast_cci_value.GetValue[Decimal](None)
        slow_cci = slow_cci_value.GetValue[Decimal](None)
        trailing_ema = ema_value.GetValue[Decimal](None)
        atr = atr_value.GetValue[Decimal](None)
        close = candle.ClosePrice

        prev_close = self._prev_close
        prev_hma = self._prev_hma

        self._prev_close = close
        self._prev_hma = hma

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position > 0:
            if candle.LowPrice <= self._stop_price:
                self.SellMarket(self.Position)
                return

            if candle.HighPrice >= self._activation_price:
                self._trailing_active = True

            if self._trailing_active and close < trailing_ema:
                self.SellMarket(self.Position)

            return

        if prev_close is None or prev_hma is None:
            return

        cross_up = prev_close <= prev_hma and close > hma

        if self.Position == 0 and cross_up and fast_cci > 0 and slow_cci > 0:
            self._stop_price = close - atr * Decimal(self._stop_loss_atr_multiplier.Value)
            self._activation_price = close + atr * Decimal(self._trailing_activation_multiplier.Value)
            self._trailing_active = False
            self.BuyMarket(self.Volume)

    def CreateClone(self):
        return double_cci_confirmed_hull_moving_average_reversal_strategy()
