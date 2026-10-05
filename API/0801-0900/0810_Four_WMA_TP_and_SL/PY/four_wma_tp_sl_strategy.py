import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import SimpleMovingAverage, ExponentialMovingAverage, WeightedMovingAverage, HullMovingAverage, SmoothedMovingAverage
from StockSharp.Algo.Strategies import Strategy

MA_SMA = 0
MA_EMA = 1
MA_WMA = 2
MA_HMA = 3
MA_RMA = 4

DIRECTION_LONG = 0
DIRECTION_SHORT = 1
DIRECTION_BOTH = 2

ALT_LONG_MA1 = 0
ALT_LONG_MA2 = 1
ALT_SHORT_MA1 = 2
ALT_SHORT_MA2 = 3

class four_wma_tp_sl_strategy(Strategy):
    """
    Four WMA strategy with TP and SL.
    A long opens when Long MA1 crosses above Long MA2 and a short when Short MA1 crosses below Short MA2, in the directions Direction
    allows; an opposite entry reverses the position. With EnableTpSl percent take profit and stop loss protect each position. With
    EnableAltExit a long also closes when the close crosses below the moving average chosen by AltExitMaOption and a short when it
    crosses above it.
    """

    def __init__(self):
        super(four_wma_tp_sl_strategy, self).__init__()
        self._long_ma1_length = self.Param("LongMa1Length", 10).SetGreaterThanZero().SetDisplay("Long MA1", "Length of Long MA1", "Moving Averages")
        self._long_ma2_length = self.Param("LongMa2Length", 20).SetGreaterThanZero().SetDisplay("Long MA2", "Length of Long MA2", "Moving Averages")
        self._short_ma1_length = self.Param("ShortMa1Length", 30).SetGreaterThanZero().SetDisplay("Short MA1", "Length of Short MA1", "Moving Averages")
        self._short_ma2_length = self.Param("ShortMa2Length", 40).SetGreaterThanZero().SetDisplay("Short MA2", "Length of Short MA2", "Moving Averages")
        self._ma_type = self.Param("MaType", MA_WMA).SetDisplay("MA Type", "Moving average type (0 Sma, 1 Ema, 2 Wma, 3 Hma, 4 Rma)", "Moving Averages")
        self._enable_tp_sl = self.Param("EnableTpSl", True).SetDisplay("Enable TP/SL", "Use percent take profit and stop loss", "Risk")
        self._take_profit_percent = self.Param("TakeProfitPercent", 1.0).SetNotNegative().SetDisplay("Take Profit %", "Take profit percentage from entry price", "Risk")
        self._stop_loss_percent = self.Param("StopLossPercent", 1.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._direction = self.Param("Direction", DIRECTION_BOTH).SetDisplay("Direction", "Allowed trade directions (0 Long, 1 Short, 2 Both)", "Trading")
        self._enable_alt_exit = self.Param("EnableAltExit", False).SetDisplay("Enable Alt Exit", "Close positions on a close crossing the chosen moving average", "Exit")
        self._alt_exit_ma_option = self.Param("AltExitMaOption", ALT_LONG_MA1).SetDisplay("Alt Exit MA", "Moving average used by the alternate exit (0 LongMa1, 1 LongMa2, 2 ShortMa1, 3 ShortMa2)", "Exit")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev = None

    def OnReseted(self):
        super(four_wma_tp_sl_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(four_wma_tp_sl_strategy, self).OnStarted2(time)

        self._reset_state()

        ma_type = int(self._ma_type.Value)
        long_ma1 = self._create_ma(ma_type, self._long_ma1_length.Value)
        long_ma2 = self._create_ma(ma_type, self._long_ma2_length.Value)
        short_ma1 = self._create_ma(ma_type, self._short_ma1_length.Value)
        short_ma2 = self._create_ma(ma_type, self._short_ma2_length.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(long_ma1, long_ma2, short_ma1, short_ma2, self._process_candle).Start()

        if self._enable_tp_sl.Value:
            self.StartProtection(
                Unit(Decimal(self._take_profit_percent.Value), UnitTypes.Percent),
                Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent),
                useMarketOrders=True, isLocalStop=True)

            # The stop and target have to see prices between candles, not only at their close.
            for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
                quotes = Subscription(DataType.Level1, self.Security)
                quotes.MarketData.BuildField = field
                self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, long_ma1)
            self.DrawIndicator(area, long_ma2)
            self.DrawIndicator(area, short_ma1)
            self.DrawIndicator(area, short_ma2)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _create_ma(self, ma_type, length):
        if ma_type == MA_SMA:
            ma = SimpleMovingAverage()
        elif ma_type == MA_EMA:
            ma = ExponentialMovingAverage()
        elif ma_type == MA_HMA:
            ma = HullMovingAverage()
        elif ma_type == MA_RMA:
            ma = SmoothedMovingAverage()
        else:
            ma = WeightedMovingAverage()
        ma.Length = length
        return ma

    def _process_candle(self, candle, long_ma1_value, long_ma2_value, short_ma1_value, short_ma2_value):
        if candle.State != CandleStates.Finished:
            return

        current = (float(long_ma1_value), float(long_ma2_value), float(short_ma1_value), float(short_ma2_value), float(candle.ClosePrice))
        prev = self._prev
        self._prev = current

        if prev is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        long_ma1, long_ma2, short_ma1, short_ma2, close = current
        last_long_ma1, last_long_ma2, last_short_ma1, last_short_ma2, last_close = prev

        direction = int(self._direction.Value)
        allow_long = direction != DIRECTION_SHORT
        allow_short = direction != DIRECTION_LONG

        long_signal = last_long_ma1 <= last_long_ma2 and long_ma1 > long_ma2
        short_signal = last_short_ma1 >= last_short_ma2 and short_ma1 < short_ma2

        if allow_long and long_signal and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            return

        if allow_short and short_signal and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            return

        if not self._enable_alt_exit.Value or self.Position == 0:
            return

        option = int(self._alt_exit_ma_option.Value)
        if option == ALT_LONG_MA2:
            exit_ma, last_exit_ma = long_ma2, last_long_ma2
        elif option == ALT_SHORT_MA1:
            exit_ma, last_exit_ma = short_ma1, last_short_ma1
        elif option == ALT_SHORT_MA2:
            exit_ma, last_exit_ma = short_ma2, last_short_ma2
        else:
            exit_ma, last_exit_ma = long_ma1, last_long_ma1

        if self.Position > 0 and last_close >= last_exit_ma and close < exit_ma:
            self.SellMarket(self.Position)
        elif self.Position < 0 and last_close <= last_exit_ma and close > exit_ma:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return four_wma_tp_sl_strategy()
