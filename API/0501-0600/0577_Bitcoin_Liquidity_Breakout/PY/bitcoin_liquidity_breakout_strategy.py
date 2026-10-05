import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage, RelativeStrengthIndex, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

RSI_ENTRY_MAX = 65
RSI_EXIT = 70
ATR_SMA_LENGTH = 10


class bitcoin_liquidity_breakout_strategy(Strategy):
    """
    Bitcoin Liquidity Breakout strategy.
    Long only: buys when volume exceeds LiquidityThreshold times its SMA(LiquidityPeriod), the close has risen more than
    PriceChangeThreshold percent from the previous close, SMA(FastMaPeriod) is above SMA(SlowMaPeriod), RSI(RsiPeriod) is below 65
    and ATR(VolatilityPeriod) is above its 10-bar SMA. The long closes when the fast SMA crosses below the slow SMA or RSI rises
    above 70; percent stop loss and take profit protect it (0 disables either).
    """

    def __init__(self):
        super(bitcoin_liquidity_breakout_strategy, self).__init__()
        self._liquidity_threshold = self.Param("LiquidityThreshold", 1.3).SetGreaterThanZero().SetDisplay("Liquidity Threshold", "Multiple of the volume SMA that marks high liquidity", "Entry")
        self._price_change_threshold = self.Param("PriceChangeThreshold", 1.5).SetDisplay("Price Change Threshold %", "Minimum close-to-close rise", "Entry")
        self._volatility_period = self.Param("VolatilityPeriod", 14).SetGreaterThanZero().SetDisplay("Volatility Period", "ATR period", "Indicators")
        self._liquidity_period = self.Param("LiquidityPeriod", 20).SetGreaterThanZero().SetDisplay("Liquidity Period", "Volume SMA period", "Indicators")
        self._fast_ma_period = self.Param("FastMaPeriod", 9).SetGreaterThanZero().SetDisplay("Fast MA Period", "Fast SMA period", "Indicators")
        self._slow_ma_period = self.Param("SlowMaPeriod", 21).SetGreaterThanZero().SetDisplay("Slow MA Period", "Slow SMA period", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 14).SetGreaterThanZero().SetDisplay("RSI Period", "RSI period", "Indicators")
        self._stop_loss_percent = self.Param("StopLossPercent", 0.5).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss in percent", "Risk")
        self._take_profit_percent = self.Param("TakeProfitPercent", 7.0).SetNotNegative().SetDisplay("Take Profit %", "Take profit in percent", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._volume_sma = None
        self._atr_sma = None
        self._prev_close = None
        self._prev_fast = None
        self._prev_slow = None

    def OnReseted(self):
        super(bitcoin_liquidity_breakout_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(bitcoin_liquidity_breakout_strategy, self).OnStarted2(time)

        self._reset_state()
        self._volume_sma = SimpleMovingAverage()
        self._volume_sma.Length = self._liquidity_period.Value
        self._atr_sma = SimpleMovingAverage()
        self._atr_sma.Length = ATR_SMA_LENGTH

        fast = SimpleMovingAverage()
        fast.Length = self._fast_ma_period.Value
        slow = SimpleMovingAverage()
        slow.Length = self._slow_ma_period.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value
        atr = AverageTrueRange()
        atr.Length = self._volatility_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, rsi, atr, self._process_candle).Start()

        tp = self._take_profit_percent.Value
        sl = self._stop_loss_percent.Value
        take = Unit(Decimal(tp), UnitTypes.Percent) if tp > 0 else Unit()
        stop = Unit(Decimal(sl), UnitTypes.Percent) if sl > 0 else Unit()
        self.StartProtection(take, stop, useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, rsi)

    def _process_candle(self, candle, fast, slow, rsi, atr):
        if candle.State != CandleStates.Finished:
            return

        volume_average = process_float(self._volume_sma, candle.TotalVolume, candle.ServerTime, True).GetValue[Decimal](None)
        atr_average = process_float(self._atr_sma, atr, candle.ServerTime, True).GetValue[Decimal](None)

        close = candle.ClosePrice
        prev_close = self._prev_close
        prev_fast = self._prev_fast
        prev_slow = self._prev_slow

        self._prev_close = close
        self._prev_fast = fast
        self._prev_slow = slow

        if not self._volume_sma.IsFormed or not self._atr_sma.IsFormed or prev_close is None or prev_close <= 0:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position > 0:
            cross_down = prev_fast is not None and prev_slow is not None and prev_fast >= prev_slow and fast < slow
            if cross_down or rsi > RSI_EXIT:
                self.SellMarket(self.Position)
            return

        price_change = (close - prev_close) / prev_close * Decimal(100)

        if (self.Position == 0
                and candle.TotalVolume > volume_average * Decimal(self._liquidity_threshold.Value)
                and price_change > Decimal(self._price_change_threshold.Value)
                and fast > slow
                and rsi < RSI_ENTRY_MAX
                and atr > atr_average):
            self.BuyMarket(self.Volume)

    def CreateClone(self):
        return bitcoin_liquidity_breakout_strategy()
