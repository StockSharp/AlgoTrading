import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy

MODE_BOTH = 0
MODE_LONG = 1
MODE_SHORT = 2


class liquidity_engulfment_strategy(Strategy):
    """
    Liquidity Engulfment strategy.
    Upper liquidity is the highest high of the previous UpperLookback candles and lower liquidity the lowest low of the previous
    LowerLookback candles. Once a candle touches lower liquidity, the next bullish engulfing candle opens a long; once a candle touches
    upper liquidity, the next bearish engulfing candle opens a short. Mode restricts the allowed side: a signal of the disabled side only
    closes the open position. Stop loss and take profit are set in pips (price steps).
    """

    def __init__(self):
        super(liquidity_engulfment_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._upper_lookback = self.Param("UpperLookback", 10).SetGreaterThanZero().SetDisplay("Upper Lookback", "Candles that define upper liquidity", "Liquidity")
        self._lower_lookback = self.Param("LowerLookback", 10).SetGreaterThanZero().SetDisplay("Lower Lookback", "Candles that define lower liquidity", "Liquidity")
        self._stop_loss_pips = self.Param("StopLossPips", 10.0).SetNotNegative().SetDisplay("Stop Loss Pips", "Stop loss in pips", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", 20.0).SetNotNegative().SetDisplay("Take Profit Pips", "Take profit in pips, 0 disables it", "Risk")
        self._mode = self.Param("Mode", MODE_BOTH).SetDisplay("Mode", "0 Both, 1 Long, 2 Short", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_highest = None
        self._prev_lowest = None
        self._prev_candle = None
        self._upper_touched = False
        self._lower_touched = False

    def OnReseted(self):
        super(liquidity_engulfment_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(liquidity_engulfment_strategy, self).OnStarted2(time)

        self._reset_state()

        highest = Highest()
        highest.Length = self._upper_lookback.Value
        lowest = Lowest()
        lowest.Length = self._lower_lookback.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(highest, lowest, self._process_candle).Start()

        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        tp = float(self._take_profit_pips.Value)
        sl = float(self._stop_loss_pips.Value)
        self.StartProtection(
            Unit(Decimal(tp * step), UnitTypes.Absolute) if tp > 0 else Unit(),
            Unit(Decimal(sl * step), UnitTypes.Absolute) if sl > 0 else Unit(),
            useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, highest)
            self.DrawIndicator(area, lowest)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, highest_value, lowest_value):
        if candle.State != CandleStates.Finished:
            return

        # Liquidity levels come from the candles before this one.
        upper = self._prev_highest
        lower = self._prev_lowest
        prev = self._prev_candle

        self._prev_highest = highest_value.GetValue[Decimal](None) if highest_value.IsFormed else None
        self._prev_lowest = lowest_value.GetValue[Decimal](None) if lowest_value.IsFormed else None
        self._prev_candle = candle

        if upper is not None and candle.HighPrice >= upper:
            self._upper_touched = True

        if lower is not None and candle.LowPrice <= lower:
            self._lower_touched = True

        if prev is None:
            return

        bullish_engulfing = prev.ClosePrice < prev.OpenPrice and candle.ClosePrice > candle.OpenPrice \
            and candle.OpenPrice <= prev.ClosePrice and candle.ClosePrice >= prev.OpenPrice
        bearish_engulfing = prev.ClosePrice > prev.OpenPrice and candle.ClosePrice < candle.OpenPrice \
            and candle.OpenPrice >= prev.ClosePrice and candle.ClosePrice <= prev.OpenPrice

        long_signal = self._lower_touched and bullish_engulfing
        short_signal = self._upper_touched and bearish_engulfing

        if long_signal:
            self._lower_touched = False

        if short_signal:
            self._upper_touched = False

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        mode = self._mode.Value

        if long_signal:
            if mode != MODE_SHORT and self.Position <= 0:
                self.BuyMarket(self.Volume + abs(self.Position))
            elif mode == MODE_SHORT and self.Position < 0:
                self.BuyMarket(-self.Position)
        elif short_signal:
            if mode != MODE_LONG and self.Position >= 0:
                self.SellMarket(self.Volume + abs(self.Position))
            elif mode == MODE_LONG and self.Position > 0:
                self.SellMarket(self.Position)

    def CreateClone(self):
        return liquidity_engulfment_strategy()
