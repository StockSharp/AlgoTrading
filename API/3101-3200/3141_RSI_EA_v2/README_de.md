# RSI EA v2-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist ein StockSharp-Port des MetaTrader 5-Expertenberaters **"RSI EA v2"**. Sie automatisiert den Handel rund um Relative Strength Index (RSI)-Schwellenwert-Kreuzungen und spiegelt die Money-Management-, Trailing-Stop- und Handelszeit-Kontrollen des ursprünglichen Beraters wider. Standardmäßig verarbeitet die Strategie Einminuten-Kerzen, aber jeder Kerzentyp kann über Parameter angegeben werden.

## Handelslogik

- **Eintrittsbedingungen**
  - Long-Positionen öffnen sich, wenn RSI über den konfigurierten *Kauflevel* steigt, nachdem er auf der vorherigen abgeschlossenen Kerze darunter lag, und die Handelsstunden neue Orders erlauben.
  - Short-Positionen öffnen sich, wenn RSI unter den konfigurierten *Verkauflevel* fällt, nachdem er zuvor darüber war, und das Handelsfenster offen ist.
  - Wenn bereits eine entgegengesetzte Position existiert, dimensioniert die Strategie die neue Marktorder so, dass sie die aktuelle Exposition flattened und die gewünschte Richtung etabliert (nur Nettopositionen).
- **Austrittsbedingungen**
  - Stop-Loss- und Take-Profit-Niveaus in Pips werden angehängt, sobald eine neue Position erkannt wird.
  - Ein Trailing-Stop spiegelt den ursprünglichen EA wider: aktiviert sich nach *Trailing-Stop + Trailing-Schritt* Preisfortschritt und bewegt sich dann um mindestens den Trailing-Schritt.
  - Optionale „Close by Signal"-Logik schließt Long-Positionen, wenn RSI nach unten durch den Verkauflevel kreuzt, und schließt Short-Positionen, wenn RSI nach oben durch den Kauflevel kreuzt.
  - Stops und Signale werden nur auf abgeschlossenen Kerzen ausgewertet, was das Verhalten in Backtests deterministisch hält.

## Risiko- und Trade-Management

- **Stop-Loss / Take-Profit** – in Pips definiert, in Preisschritte umgerechnet, die die Instrumentenpräzision respektieren (einschließlich 3/5-Stellen-Forex-Symbole).
- **Trailing-Stop** – deaktiviert wenn Abstand null ist. Ein positiver Trailing-Schritt ist erforderlich, wenn der Trailing-Abstand ungleich null ist.
- **Positionsgröße** – entweder ein festes Volumen oder ein automatisches Volumen aus Risikoprozentsatz und Stop-Abstand berechnet. Risiko-Sizing erfordert Zugang zu Portfolio-Eigenkapital und validen Preisschritt-Metadaten.
- **Handelsfenster** – optionaler täglicher Filter durch inklusive Start- und exklusive End-Stunden (0–23). Wenn Start gleich End ist, gilt der Markt als geschlossen.

## Parameter

| Name | Beschreibung |
| ---- | ------------ |
| `OpenBuy` / `OpenSell` | Schaltet Long- oder Short-Eintritte unabhängig ein/aus. |
| `CloseBySignal` | Aktiviert Ausstiege bei entgegengesetzten RSI-Kreuzungen. |
| `StopLossPips` | Stop-Loss-Abstand in Pips (0 deaktiviert den Stop). |
| `TakeProfitPips` | Take-Profit-Abstand in Pips (0 deaktiviert das Ziel). |
| `TrailingStopPips` | Trailing-Stop-Abstand in Pips. Muss null sein, wenn kein Trailing gewünscht. |
| `TrailingStepPips` | Zusätzlicher Fortschritt (in Pips) vor dem Verschieben des Trailing-Stops. Muss positiv sein, wenn Trailing aktiv. |
| `RsiPeriod` | RSI-Indikatorlänge. |
| `RsiBuyLevel` / `RsiSellLevel` | Schwellenwerte für Long- und Short-Eintritte/Ausstiege. |
| `UseRiskSizing` | Wechselt zwischen festem Volumen und risikoprozentualer Positionsgröße. |
| `FixedVolume` | Basis-Ordergröße für Fixvolumen-Modus oder Fallback wenn Risiko-Sizing nicht berechnet werden kann. |
| `RiskPercent` | Prozentsatz des Portfolio-Eigenkapitals pro Trade. Nur wenn `UseRiskSizing` wahr ist und positiver Stop-Abstand vorhanden. |
| `UseTimeControl` | Aktiviert den täglichen Handelsfensterfilter. |
| `StartHour` / `EndHour` | Inklusive Start- und exklusive End-Stunde (0–23) des Handelsfensters. |
| `CandleType` | Kerzen-Datentyp für Indikatorberechnungen. |

## Implementierungshinweise

- Verwendet die High-Level-Kerzen-Abonnement-API mit `RSI`-Indikator-Binding.
- Konvertiert Pip-Abstände mithilfe der Instrumentenpräzision (`PriceStep` und `Decimals`), um MetaTrader's 3/5-Stellen-Logik zu entsprechen.
- Normalisiert Ordervolumen auf den Volumen-Step und die Grenzen des Instruments (Min/Max-Volumen).
- Trailing-Logik aktualisiert nur interne Stop-Referenzen; Ausstiege werden mit Marktorders durchgeführt, wenn Niveaus verletzt werden.
- Hält separaten Zustand für Long- und Short-Positionen, um Trailing- und Schutzniveaus zwischen Kerzen zu erhalten.

## Verwendung

1. Hängen Sie die Strategie an einen StockSharp-Connector mit geeigneten Instrument- und Portfolio-Metadaten.
2. Konfigurieren Sie Schwellenwerte, Pip-Abstände und optionales Zeitfenster für den gewünschten Markt.
3. Aktivieren Sie risikobasiertes Sizing wenn Portfolio-Informationen verfügbar sind; andernfalls deaktiviert lassen für ein festes Lot.
4. Starten Sie die Strategie – sie wartet auf abgeschlossene Kerzen, wendet die RSI-Logik an und verwaltet aktive Positionen gemäß den konfigurierten Schutzmechanismen.
