# Exp FisherCG-Oszillator-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie portiert den **Exp_FisherCGOscillator** MetaTrader 5-Expertenberater auf die High-Level-API von StockSharp. Sie bildet den Fisher Center of Gravity-Oszillator und seine Triggerlinie nach, wertet Signale auf einer konfigurierbaren historischen Bar aus und reproduziert den originalen Stop/Take-Workflow mit StockSharp-Orders und Risikohelferklassen.

## Funktionsweise

1. **Indikator-Pipeline** – jede abgeschlossene Kerze wird durch den Fisher CG-Oszillator geleitet: Medianpreise speisen eine Center-of-Gravity-Schleife, Werte werden über die letzten `Length` Bars normalisiert, und eine Fisher-Transformation erzeugt die Oszillatorlinie. Die Triggerlinie ist einfach der um eine Bar verzögerte Oszillator.
2. **Signalextraktion** – die Strategie untersucht zwei historische Lesungen, die durch `SignalBar` definiert werden. Sie eröffnet einen Long, wenn der ältere Oszillatorwert (`SignalBar + 1`) über seinem Trigger liegt, während der neuere Wert (`SignalBar`) wieder über den Trigger kreuzt und eine bullische Wende signalisiert. Shorts spiegeln diese Logik auf der bärischen Seite.
3. **Ausstiegsbehandlung** – Long-Ausstiege erfolgen, sobald der ältere Oszillator unter seinen Trigger fällt, während Short-Ausstiege ausgelöst werden, wenn er über den Trigger steigt, was den sofortigen Schließindikatoren des EA entspricht. Entgegengesetzte Einstiege schließen die aktive Position vor der Umkehr.
4. **Bar-für-Bar-Verarbeitung** – alles läuft auf abgeschlossenen Kerzen aus `CandleType`; es werden keine Intrabar-Trades generiert, was deterministische Backtests sicherstellt und dem "neue Bar"-Gate des EA entspricht.

## Risikomanagement und Positionsgrößenbestimmung

- **Stops/Ziele** – `StopLossPoints` und `TakeProfitPoints` werden in Instrumentschritten ausgedrückt und über `Security.PriceStep` in absolute Preisabstände übersetzt.
- **Volumenkontrolle** – `SizingMode = FixedVolume` sendet das konstante `FixedVolume`. `SizingMode = PortfolioShare` konvertiert `DepositShare` des aktuellen Portfoliowerts in Kontrakte unter Verwendung des letzten Schlusskurses und `VolumeStep`.
- **Einzelposition** – die Strategie flacht immer ab, bevor sie auf die entgegengesetzte Seite eintritt, um gleichzeitig gehedgte Positionen zu vermeiden.

## Parameter

| Parameter | Beschreibung |
| --- | --- |
| `CandleType` | Abonnierter Zeitrahmen für Kerzen und Indikatorberechnungen. |
| `Length` | Fisher CG-Oszillatorperiode (auch für das Normalisierungsfenster verwendet). |
| `SignalBar` | Anzahl der geschlossenen Kerzen zurück, die zum Lesen von Signalen verwendet werden; `1` entspricht dem EA-Standard. |
| `AllowLongEntry` / `AllowShortEntry` | Long-/Short-Einstiege umschalten. |
| `AllowLongExit` / `AllowShortExit` | Automatische Ausstiege für Long-/Short-Positionen umschalten. |
| `StopLossPoints` / `TakeProfitPoints` | Schutz-Stop- und Zielabstände in Preisschritten. Auf `0` setzen zum Deaktivieren. |
| `FixedVolume` | Volumen im festen Größenmodus. |
| `DepositShare` | Portfolioanteil pro Trade im `PortfolioShare`-Modus. |
| `SizingMode` | Wählt zwischen festem Volumen und anteilsbasierter Positionsgrößenbestimmung. |

## Verwendungshinweise

- Stimmen Sie `CandleType` und `SignalBar` auf den vom ursprünglichen Indikator verwendeten Zeitrahmen ab (standardmäßig H8 und Bar-Verschiebung von 1).
- Erlauben Sie eine kurze Aufwärmphase, damit der Oszillator genug Historie aufbauen kann; die Strategie ignoriert Trades, bis der Indikator vollständig initialisiert ist.
- Stops und Ziele operieren auf dem Kerzenschluss. Passen Sie Punktwerte an die Tick-Größe Ihres Instruments an.
- Wenn `PortfolioShare`-Sizing ausgewählt ist, stellen Sie sicher, dass die Portfoliobewertung verfügbar ist; andernfalls fällt die Strategie auf das feste Volumen zurück.

## Unterschiede vs. originalem EA

- Orders werden als Marktorders ohne den Slippage-Parameter `Deviation_` gesendet; StockSharp verarbeitet die Ausführung mit seinen eigenen Slippage-Einstellungen.
- Geldmanagement ist auf zwei Größenmodi vereinfacht (`FixedVolume` und `PortfolioShare`). Die Verlustprozentsatz-Optionen des EA werden absichtlich weggelassen.
- Ausstehende Order-Zeitstempel (`UpSignalTime`/`DnSignalTime`) werden nicht verwendet. Signale werden sofort auf der verarbeiteten Kerze ausgeführt.
