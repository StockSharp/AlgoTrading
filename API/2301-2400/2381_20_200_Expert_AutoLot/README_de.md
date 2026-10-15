# Expert AutoLot 20/200-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie eröffnet maximal eine Position pro Tag zu einer benutzerdefinierten Stunde. Sie vergleicht den Eröffnungskurs zweier vergangener Bars (T1 und T2). Wenn der frühere Bar um DeltaShort Pips höher als der spätere ist, wird eine Short-Position eröffnet. Wenn der spätere Bar um DeltaLong Pips höher ist, wird eine Long-Position eröffnet.

Das Positionsvolumen kann fest sein oder automatisch aus dem Kontosaldo berechnet werden. Wenn der Saldo im Vergleich zum vorherigen Trade abnimmt, wird der Lot mit BigLotSize multipliziert.

Jeder Trade verwendet seinen eigenen Take-Profit und Stop-Loss in Pips. Zusätzlich schließt eine maximale Haltedauer (MaxOpenTime) den Trade nach der angegebenen Anzahl von Stunden.

## Parameter

- `CandleType` – Zeitrahmen der verarbeiteten Kerzen (Standard: 1 Stunde).
- `TradeHour` – Stunde des Tages, zu der Eintrittsbedingungen geprüft werden.
- `T1`, `T2` – Bar-Verschiebungen zum Vergleich der Eröffnungskurse.
- `DeltaLong`, `DeltaShort` – minimale Eröffnungspreisdifferenz in Pips.
- `TakeProfitLong`, `StopLossLong` – Schutz für Long-Trades in Pips.
- `TakeProfitShort`, `StopLossShort` – Schutz für Short-Trades in Pips.
- `Lot` – Basis-Handelsvolumen.
- `AutoLot` – automatische Lot-Berechnung aktivieren.
- `BigLotSize` – Multiplikator nach Verlust.
- `MaxOpenTime` – maximale Haltedauer einer Position in Stunden.
