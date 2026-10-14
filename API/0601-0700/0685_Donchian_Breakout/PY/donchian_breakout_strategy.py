import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import DonchianChannels, ExponentialMovingAverage, RelativeStrengthIndex, AverageTrueRange, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

RSI_LENGTH = 14


class donchian_breakout_strategy(Strategy):
    """
    Donchian breakout strategy with trend, volatility and volume filters.
    A close above the EntryLength channel of the previous candles goes long when the close is above the EMA, RSI is above 50,
    ATR is above its AtrSmaLength average and volume is above its VolumeSmaLength average; a close below the channel goes short
    with the mirrored trend filters. An opposite entry breakout reverses the position.
    A long also exits on a close below the ExitLength channel or at an ATR stop of AtrMultiplier times ATR from the entry; shorts mirror this.
    """

    def __init__(self):
        super(donchian_breakout_strategy, self).__init__()
        self._entry_length = self.Param("EntryLength", 20).SetGreaterThanZero().SetDisplay("Entry Length", "Candles of the entry channel", "Donchian")
        self._exit_length = self.Param("ExitLength", 10).SetGreaterThanZero().SetDisplay("Exit Length", "Candles of the exit channel", "Donchian")
        self._atr_length = self.Param("AtrLength", 14).SetGreaterThanZero().SetDisplay("ATR Length", "ATR length", "Risk")
        self._atr_multiplier = self.Param("AtrMultiplier", 1.5).SetNotNegative().SetDisplay("ATR Multiplier", "ATR multiplier of the stop", "Risk")
        self._ema_length = self.Param("EmaLength", 50).SetGreaterThanZero().SetDisplay("EMA Length", "Trend EMA length", "Filters")
        self._volume_sma_length = self.Param("VolumeSmaLength", 20).SetGreaterThanZero().SetDisplay("Volume SMA Length", "Volume average length", "Filters")
        self._atr_sma_length = self.Param("AtrSmaLength", 20).SetGreaterThanZero().SetDisplay("ATR SMA Length", "ATR average length", "Filters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._volume_sma = None
        self._atr_sma = None
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def _reset_state(self):
        self._prev_entry_upper = None
        self._prev_entry_lower = None
        self._prev_exit_upper = None
        self._prev_exit_lower = None
        self._stop_price = None

    def OnReseted(self):
        super(donchian_breakout_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(donchian_breakout_strategy, self).OnStarted2(time)

        self._reset_state()

        entry_channel = DonchianChannels()
        entry_channel.Length = self._entry_length.Value
        exit_channel = DonchianChannels()
        exit_channel.Length = self._exit_length.Value
        ema = ExponentialMovingAverage()
        ema.Length = self._ema_length.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = RSI_LENGTH
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value
        self._volume_sma = SimpleMovingAverage()
        self._volume_sma.Length = self._volume_sma_length.Value
        self._atr_sma = SimpleMovingAverage()
        self._atr_sma.Length = self._atr_sma_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(entry_channel, exit_channel, ema, rsi, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, entry_channel)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, entry_value, exit_value, ema_value, rsi_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        # Channels are measured on the candles before this one.
        entry_upper = self._prev_entry_upper
        entry_lower = self._prev_entry_lower
        exit_upper = self._prev_exit_upper
        exit_lower = self._prev_exit_lower

        if entry_value.IsFormed and entry_value.UpperBand is not None and entry_value.LowerBand is not None:
            self._prev_entry_upper = float(entry_value.UpperBand)
            self._prev_entry_lower = float(entry_value.LowerBand)

        if exit_value.IsFormed and exit_value.UpperBand is not None and exit_value.LowerBand is not None:
            self._prev_exit_upper = float(exit_value.UpperBand)
            self._prev_exit_lower = float(exit_value.LowerBand)

        volume_avg = process_float(self._volume_sma, candle.TotalVolume, candle.ServerTime, True)

        if not atr_value.IsFormed:
            return

        atr_dec = atr_value.GetValue[Decimal](None)
        atr_avg = process_float(self._atr_sma, atr_dec, candle.ServerTime, True)

        if not ema_value.IsFormed or not rsi_value.IsFormed or not volume_avg.IsFormed or not atr_avg.IsFormed:
            return

        if entry_upper is None or entry_lower is None or exit_upper is None or exit_lower is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = float(candle.ClosePrice)
        atr = float(atr_dec)
        ema = float(ema_value.GetValue[Decimal](None))
        rsi = float(rsi_value.GetValue[Decimal](None))
        atr_mult = float(self._atr_multiplier.Value)
        filters_ok = atr > float(atr_avg.GetValue[Decimal](None)) and float(candle.TotalVolume) > float(volume_avg.GetValue[Decimal](None))

        long_signal = close > entry_upper and close > ema and rsi > 50 and filters_ok
        short_signal = close < entry_lower and close < ema and rsi < 50 and filters_ok

        if long_signal and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._stop_price = close - atr * atr_mult if atr_mult > 0 else None
        elif short_signal and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._stop_price = close + atr * atr_mult if atr_mult > 0 else None
        elif self.Position > 0 and (close < exit_lower or (self._stop_price is not None and float(candle.LowPrice) <= self._stop_price)):
            self.SellMarket(self.Position)
            self._stop_price = None
        elif self.Position < 0 and (close > exit_upper or (self._stop_price is not None and float(candle.HighPrice) >= self._stop_price)):
            self.BuyMarket(-self.Position)
            self._stop_price = None

    def CreateClone(self):
        return donchian_breakout_strategy()
