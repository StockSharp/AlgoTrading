# Sitzungs-Ordersentiment-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Diese Strategie handelt basierend auf dem Ungleichgewicht zwischen Kauf- und Verkaufsaufträgen im Orderbuch. Sie misst Verhältnisse von Auftragsanzahlen und Gesamtvolumina für beide Seiten des Buches und eröffnet eine Position, wenn die Dominanz einer Seite konfigurierbare Schwellenwerte überschreitet. Der Handel ist nur während eines bestimmten Zeitfensters erlaubt.

Nach dem Öffnen einer Position werden die Schwellenwerte reduziert, um die gegenüberliegende Seite zu überwachen. Wenn die gegenüberliegende Seite über diese reduzierten Schwellenwerte wächst, wird die Position geschlossen. Stop-Loss und Take-Profit werden ebenfalls in absoluten Preispunkten angewendet.

## Handelsregeln
- **Long-Einstieg**: Kaufen wenn
  - `BUY volume / SELL volume >= DiffVolumesEx` und `BUY orders / SELL orders >= DiffTradersEx`
  - Eine der Seiten erfüllt `MinTraders` und `MinVolume`
  - Die aktuelle Zeit besteht `CheckTradingTime`
- **Short-Einstieg**: Verkaufen, wenn die obige Logik spiegelbildlich für die Verkaufsseite gilt.
- **Ausstieg**:
  - Long schließen wenn `SELL volume / BUY volume > 1 / DiffVolumes` oder `SELL orders / BUY orders > 1 / DiffTraders`
  - Short schließen wenn `SELL volume / BUY volume < DiffVolumes` oder `SELL orders / BUY orders < DiffTraders`
  - Alle Positionen außerhalb der Handelszeiten schließen
- **Stops**: Verwendet `Stop Loss` und `Take Profit` in Preispunkten.

## Parameter
- `MinVolume` – minimales Gesamtvolumen auf einer Seite des Buches (Standard: 20000)
- `MinTraders` – Mindestanzahl von Aufträgen auf einer Seite (Standard: 1000)
- `DiffVolumesEx` – Volumenverhältnis für den Einstieg (Standard: 2.0)
- `DiffTradersEx` – Auftragsanzahlverhältnis für den Einstieg (Standard: 1.5)
- `MinDiffVolumesEx` – Volumenverhältnis nach Positionseröffnung (Standard: 1.5)
- `MinDiffTradersEx` – Auftragsanzahlverhältnis nach Positionseröffnung (Standard: 1.3)
- `SleepMinutes` – Verzögerung zwischen Orderbuchprüfungen in Minuten (Standard: 5)
- `TpPips` – Take-Profit in Preispunkten (Standard: 500)
- `SlPips` – Stop-Loss in Preispunkten (Standard: 500)

## Hinweise
Die Strategie enthält keine Python-Version.
