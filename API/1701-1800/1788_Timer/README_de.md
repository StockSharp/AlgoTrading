# Timer-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Timer-Strategie berechnet Ausbruchsniveaus in festen Zeitintervallen neu und handelt, wenn der Preis diese dynamischen Schwellen kreuzt. Die Niveaus werden anhand des Average True Range (ATR) und einem optionalen zusätzlichen Pip-Abstand positioniert. Der Ansatz zielt darauf ab, kurzfristige Ausbrüche in beide Richtungen zu erfassen.

Alle `WaitSeconds` setzt die Strategie:
- **Kaufniveau** bei `close + pipDistance + ATR`.
- **Verkaufsniveau** bei `close - pipDistance - ATR`.

Wenn die nächste abgeschlossene Kerze jenseits eines dieser Niveaus schließt, wird eine Marktorder in der entsprechenden Richtung platziert. Die Position ist durch konfigurierbaren Stop-Loss, Take-Profit und Trailing Stop geschützt.

Der Handel kann mithilfe der Handelszeiteinstellungen auf ein bestimmtes Zeitfenster begrenzt werden.

## Parameter
- `WaitSeconds` – Sekunden zwischen Neuberechnungen der Niveaus.
- `PipDistance` – zusätzlicher Abstand vom aktuellen Preis in Punkten.
- `AtrPeriod` – ATR-Indikatorperiode.
- `TakeProfit` – Take-Profit-Abstand in Punkten.
- `StopLoss` – Stop-Loss-Abstand in Punkten.
- `TrailingStop` – Trailing-Stop-Abstand in Punkten.
- `TradeVolume` – Ordervolumen.
- `CandleType` – Kerzentyp für Berechnungen.
- `UseTradingHours` – Tageszeit-Filter aktivieren.
- `StartTime` – Handelsstartzeit.
- `StopTime` – Handelsendzeit.

## Funktionsweise
1. Anmeldung auf Kerzen und ATR-Berechnung.
2. Bei jeder abgeschlossenen Kerze:
   - Wenn das konfigurierte Zeitintervall abgelaufen ist, werden neue Kauf- und Verkaufsniveaus berechnet.
   - Wenn Handelszeiten aktiviert sind, wird geprüft, ob die aktuelle Zeit im erlaubten Fenster liegt.
   - Kauf- oder Verkaufs-Marktorder wird platziert, wenn der Preis das entsprechende Niveau kreuzt.
3. Stop-Loss, Take-Profit und Trailing Stop werden automatisch von der Strategie-Infrastruktur verwaltet.

## Hinweise
- Die Strategie handelt sowohl Long als auch Short.
- Funktioniert mit jedem Instrument und Zeitrahmen.
- ATR-basierte Niveaus passen sich der Marktvolatilität an und ermöglichen eine flexible Ausbruchserkennung.
