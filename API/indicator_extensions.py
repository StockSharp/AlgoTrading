import clr
clr.AddReference("System")
clr.AddReference("StockSharp.Algo")

from System import Decimal


def to_float(value):
    """Convert .NET Decimal or indicator value to Python float."""
    try:
        if hasattr(value, "ToDecimal"):
            return float(value.ToDecimal())
        return float(value)
    except Exception:
        try:
            return float(str(value))
        except Exception:
            return value


def process_float(indicator, value, time, is_final):
    """Wrapper for indicator.Process accepting Python float."""
    return indicator.Process(float(value), time, is_final)
