# ZeroLag MACD-Kreuzungs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt auf Basis eines Kreuzungssignals zwischen der MACD-Linie und ihrer Signallinie. Sie wurde aus dem MetaTrader-Expertenberater **ZeroLagEA-AIP v0.0.4** konvertiert. Die Strategie operiert nur während der konfigurierten Sitzungsstunden und kann optional verlangen, dass die Kreuzung auf dem aktuellen Balken erfolgt.

## Details

- **Einstiegskriterien**:
  - **Long**: MACD-Linie kreuzt über die Signallinie.
  - **Short**: MACD-Linie kreuzt unter die Signallinie.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Entgegengesetzte Kreuzung oder erzwungener Ausstieg am angegebenen Tag und zur angegebenen Stunde.
- **Stops**: Keine.
- **Filter**:
  - Sitzungsstunden definiert durch `StartHour` und `EndHour`.
  - Optionale frische Kreuzungsanforderung (`UseFreshSignal`).

## Parameter

- `FastEmaLength` = 2
- `SlowEmaLength` = 34
- `SignalEmaLength` = 2
- `UseFreshSignal` = true
- `Volume` = 2
- `StartHour` = 9
- `EndHour` = 15
- `KillDay` = 5
- `KillHour` = 21
- `CandleType` = 1-Minuten-Kerzen
