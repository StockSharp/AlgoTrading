import clr
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
from System import TimeSpan
from StockSharp.Messages import DataType

def tf(minutes):
    """Return TimeFrame candle type for the given number of minutes."""
    return DataType.TimeFrame(TimeSpan.FromMinutes(minutes))
