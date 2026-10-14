import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import OnBalanceVolume, SimpleMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class obvious_ma_strategy(Strategy):
    """
    OBVious MA strategy.
    On-balance volume is compared with four simple moving averages of itself. A long opens when OBV crosses above the long entry
    average and closes when it crosses below the long exit average. A short opens when OBV crosses below the short entry average
    and closes when it crosses above the short exit average. TradeDirection ("Long", "Short" or "Both") limits the entries.
    """

    def __init__(self):
        super(obvious_ma_strategy, self).__init__()
        self._long_entry_length = self.Param("LongEntryLength", 190).SetGreaterThanZero().SetDisplay("Long Entry Length", "Length of the OBV average for long entries", "Indicators")
        self._long_exit_length = self.Param("LongExitLength", 202).SetGreaterThanZero().SetDisplay("Long Exit Length", "Length of the OBV average for long exits", "Indicators")
        self._short_entry_length = self.Param("ShortEntryLength", 395).SetGreaterThanZero().SetDisplay("Short Entry Length", "Length of the OBV average for short entries", "Indicators")
        self._short_exit_length = self.Param("ShortExitLength", 300).SetGreaterThanZero().SetDisplay("Short Exit Length", "Length of the OBV average for short exits", "Indicators")
        self._trade_direction = self.Param("TradeDirection", "Long").SetDisplay("Trade Direction", "Allowed entries: Long, Short or Both", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_obv = None
        self._prev_long_entry = None
        self._prev_long_exit = None
        self._prev_short_entry = None
        self._prev_short_exit = None

    def OnReseted(self):
        super(obvious_ma_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(obvious_ma_strategy, self).OnStarted2(time)

        self._reset_state()

        obv = OnBalanceVolume()
        self._long_entry_ma = SimpleMovingAverage()
        self._long_entry_ma.Length = self._long_entry_length.Value
        self._long_exit_ma = SimpleMovingAverage()
        self._long_exit_ma.Length = self._long_exit_length.Value
        self._short_entry_ma = SimpleMovingAverage()
        self._short_entry_ma.Length = self._short_entry_length.Value
        self._short_exit_ma = SimpleMovingAverage()
        self._short_exit_ma.Length = self._short_exit_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(obv, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, obv)
                self.DrawIndicator(oscillators, self._long_entry_ma)
                self.DrawIndicator(oscillators, self._long_exit_ma)
                self.DrawIndicator(oscillators, self._short_entry_ma)
                self.DrawIndicator(oscillators, self._short_exit_ma)

    def _process_average(self, ma, obv, time):
        value = DecimalIndicatorValue(ma, obv, time)
        value.IsFinal = True
        result = ma.Process(value)
        if not ma.IsFormed:
            return None
        return result.GetValue[Decimal](None)

    def _process_candle(self, candle, obv):
        if candle.State != CandleStates.Finished:
            return

        # The averages are built on OBV itself, not on price.
        long_entry = self._process_average(self._long_entry_ma, obv, candle.OpenTime)
        long_exit = self._process_average(self._long_exit_ma, obv, candle.OpenTime)
        short_entry = self._process_average(self._short_entry_ma, obv, candle.OpenTime)
        short_exit = self._process_average(self._short_exit_ma, obv, candle.OpenTime)

        prev = self._prev_obv
        prev_long_entry = self._prev_long_entry
        prev_long_exit = self._prev_long_exit
        prev_short_entry = self._prev_short_entry
        prev_short_exit = self._prev_short_exit

        self._prev_obv = obv
        self._prev_long_entry = long_entry
        self._prev_long_exit = long_exit
        self._prev_short_entry = short_entry
        self._prev_short_exit = short_exit

        if prev is None or not self.IsFormedAndOnlineAndAllowTrading():
            return

        direction = str(self._trade_direction.Value).lower()
        allow_long = direction != "short"
        allow_short = direction != "long"

        cross_up_long_entry = long_entry is not None and prev_long_entry is not None and prev <= prev_long_entry and obv > long_entry
        cross_down_long_exit = long_exit is not None and prev_long_exit is not None and prev >= prev_long_exit and obv < long_exit
        cross_down_short_entry = short_entry is not None and prev_short_entry is not None and prev >= prev_short_entry and obv < short_entry
        cross_up_short_exit = short_exit is not None and prev_short_exit is not None and prev <= prev_short_exit and obv > short_exit

        if cross_up_long_entry and allow_long and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif cross_down_short_entry and allow_short and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and cross_down_long_exit:
            self.SellMarket(self.Position)
        elif self.Position < 0 and cross_up_short_exit:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return obvious_ma_strategy()
