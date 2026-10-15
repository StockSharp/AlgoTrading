# CGOscillator X2 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

Die **CGOscillator X2 Strategie** ist ein Multi-Zeitrahmen-Trendfolgesystem, das den Schwerpunkt-Oszillator (Center of Gravity) verwendet, um Pullbacks zu handeln. Die Strategie bewertet die Steigung des Oszillators auf einem höheren Zeitrahmen, um den dominanten Trend zu bestimmen, und wartet auf einen korrektiven Haken auf einem niedrigeren Zeitrahmen, bevor sie in Richtung des Trends einsteigt. Optionale Stop-Loss- und Take-Profit-Abstände in absoluten Preiseinheiten können zur Risikoverwaltung nach einer Eröffnung verwendet werden.

## Handelslogik

1. **Trenderkennung (höherer Zeitrahmen)**
   - Der CG-Oszillator (Center of Gravity) wird auf dem Trend-Zeitrahmen mit dem konfigurierten `TrendLength` berechnet.
   - Wenn der aktuelle CG-Wert über seinem Signal liegt (vorheriger Wert), betrachtet die Strategie den Markt als bullisch; wenn er unter dem Signal liegt, gilt der Markt als bearisch.
2. **Signalgenerierung (niedrigerer Zeitrahmen)**
   - Eine zweite CG-Oszillator-Instanz mit eigener Länge arbeitet auf dem Signal-Zeitrahmen.
   - Die Strategie überwacht die zwei jüngsten abgeschlossenen Kerzen. Ein bullischer Haken (aktueller CG >= Signal, während vorheriger CG < vorheriges Signal) signalisiert, dass ein Pullback in einem Abwärtstrend endete. Ein bearischer Haken (aktueller CG <= Signal, während vorheriger CG > vorheriges Signal) zeigt einen Pullback in einem Aufwärtstrend an.
3. **Einstiege und Ausstiege**
   - Long-Einstiege sind nur erlaubt, wenn der höhere Zeitrahmen einen Aufwärtstrend zeigt und der letzte Swing im niedrigeren Zeitrahmen einen bearischen Haken (überverkaufter Pullback) anzeigt. Shorts folgen der gespiegelten Logik für Abwärtstrends.
   - Positionen können geschlossen werden, wenn der höhere Zeitrahmen-Trend dreht oder wenn der jüngste Haken gegen die offene Position geht, abhängig von den Boolean-Parametern.
4. **Risikokontrollen**
   - Optionale absolute Stop-Loss- und Take-Profit-Abstände werden nach jedem Markteinstieg angewendet. Wenn der Preis diese Niveaus innerhalb der aktuellen Kerze kreuzt, wird die Position sofort vor der Verarbeitung neuer Signale geschlossen.

## Parameter

| Name | Beschreibung |
| ---- | ----------- |
| `TrendCandleType` | Kerzentyp (Zeitrahmen) für den höheren Zeitrahmen CG-Oszillator. |
| `SignalCandleType` | Kerzentyp für den niedrigeren Zeitrahmen Signal-Oszillator. |
| `TrendLength` | Länge des CG-Oszillators auf dem Trend-Zeitrahmen. |
| `SignalLength` | Länge des CG-Oszillators auf dem Signal-Zeitrahmen. |
| `BuyOpen` | Aktiviert oder deaktiviert Long-Einstiege aligned mit dem höheren Zeitrahmen-Trend. |
| `SellOpen` | Aktiviert oder deaktiviert Short-Einstiege aligned mit dem höheren Zeitrahmen-Trend. |
| `BuyClose` | Schließt Long-Positionen, wenn der höhere Zeitrahmen-Trend bearisch wird. |
| `SellClose` | Schließt Short-Positionen, wenn der höhere Zeitrahmen-Trend bullisch wird. |
| `BuyCloseSignal` | Schließt Long-Positionen, wenn der jüngste niedrigere Zeitrahmen-Haken bearisch ist. |
| `SellCloseSignal` | Schließt Short-Positionen, wenn der jüngste niedrigere Zeitrahmen-Haken bullisch ist. |
| `StopLoss` | Absoluter Preisabstand für den Schutz-Stop (0 deaktiviert den Stop). |
| `TakeProfit` | Absoluter Preisabstand für das Gewinnziel (0 deaktiviert das Ziel). |

## Indikatordetails

Der benutzerdefinierte **CenterOfGravityOscillatorIndicator** repliziert den MT5 CG-Oszillator:
- Der Medianpreis `(Hoch + Tief) / 2` wird als Eingabe verwendet.
- Eine gewichtete Summe der letzten `Length` Medianwerte bildet den CG-Wert.
- Die Signallinie ist einfach der vorherige CG-Wert und liefert einen Ein-Bar-Lag für die Hakenerkennung.

## Verwendungshinweise

- Die `Volume`-Eigenschaft der Strategie setzen, um die Basisordergröße zu steuern. Umkehrungen addieren automatisch den absoluten Wert der aktuellen Position, damit die neue Position in der gewünschten Richtung eröffnet wird.
- Da die Strategie nur mit abgeschlossenen Kerzen arbeitet, ist sie widerstandsfähig gegen Intrabar-Rauschen, reagiert aber auf den Schlusskurs jeder Kerze.
- Die Stop-Loss- und Take-Profit-Parameter verwenden absolute Preiseinheiten; sie an die Tick-Größe und das Volatilitätsprofil des Instruments anpassen.
- Die Strategie kann an jedes von StockSharp unterstützte Instrument angehängt werden, sobald die geeigneten Kerzentypen konfiguriert sind.
