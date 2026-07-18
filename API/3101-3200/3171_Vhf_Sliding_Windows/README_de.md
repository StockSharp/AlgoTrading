# Vhf Sliding Windows-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
- Konvertiert vom MetaTrader-5-Expert-Advisor **„VHF EA"** von Vladimir Karputov.
- Verwendet den Vertical Horizontal Filter (VHF)-Indikator, um das Marktregime als Trending oder Ranging einzustufen.
- Funktioniert auf jedem von StockSharp unterstützten Instrument und Zeitrahmen; einfach den Kerzentyp-Parameter an das gewünschte Chart anpassen.

## Handelslogik
1. Die ausgewählte Kerzenserie abonnieren und den VHF-Indikator mit Periode `VhfPeriod` bei jeder abgeschlossenen Kerze berechnen.
2. Zwei gleitende Fenster aktueller VHF-Werte führen:
   - **Hauptfenster (`MainWindowSize`)** – etabliert den gesamten VHF-Bereich und Mittelpunkt.
   - **Arbeitsfenster (`WorkingWindowSize`)** – erkennt kurzfristige Durchbrüche über oder unter der lokalen VHF-Median.
3. Ein bullisches oder bärisches Trend-Regime wird nur bestätigt, wenn der aktuelle VHF-Wert größer als der Mittelpunkt beider Fenster ist.
4. Während im Trend-Regime, den letzten Schlusskurs mit dem Schluss vor `MainWindowSize` Bars vergleichen:
   - Schluss höher als Referenz → Standardverhalten ist Long-Position öffnen/beibehalten.
   - Schluss niedriger als Referenz → Standardverhalten ist Short-Position öffnen/beibehalten.
   - `ReverseSignals` aktivieren, um diese Richtungen umzukehren.
5. Die Strategie schließt alle offenen Positionen, sobald der VHF-Wert wieder in die Ranging-Zone fällt (aktueller VHF liegt nicht über beiden Mittelpunkten).
6. Positionswechsel werden durch Kaufen/Verkaufen von ausreichend Volumen gehandhabt, um sowohl die Gegenseite zu schließen als auch die neue Position in einer einzelnen Marktorder zu eröffnen.

## Parameter
| Parameter | Beschreibung | Standard | Hinweise |
|-----------|-------------|---------|-------|
| `MainWindowSize` | Anzahl der VHF-Werte im primären gleitenden Fenster. | `11` | Muss größer als `WorkingWindowSize` sein. |
| `WorkingWindowSize` | Anzahl der VHF-Werte im sekundären Fenster. | `7` | Bietet schnellere Ausbruchsbestätigung. |
| `VhfPeriod` | Rückblickperiode des Vertical Horizontal Filters. | `9` | Bestimmt die Sensitivität des Indikators. |
| `Volume` | Order-Volumen (Lots) für neue Einstiege. | `1` | Wird zum absoluten aktuellen Positionswert addiert beim Richtungswechsel. |
| `ReverseSignals` | Long/Short-Logik aus Preisrichtung invertieren. | `true` | Entspricht dem Standardverhalten des ursprünglichen EA. |
| `CandleType` | Zeitrahmen und Kerzentyp für Datenabonnement. | `15-Minuten-Zeitrahmen` | Ändern, um die Strategie an andere Charts anzupassen. |

## Geldmanagement und Ausstiege
- Die Strategie handelt immer mit einem festen Volumen definiert durch `Volume`.
- Schützende Stop-Verwaltung wird an den eingebauten StockSharp-Helfer `StartProtection()` delegiert, der unerwartete Restpositionen sicher schließt.
- Keine Stop-Loss- oder Take-Profit-Ziele sind kodiert; Ausstiege beruhen auf dem von VHF erkannten Regimewechsel.

## Implementierungshinweise
- Verwendet die hochrangige Kerzen-Abonnement-API mit Indikator-Bindung, gemäß den Projektrichtlinien.
- Ein benutzerdefinierter Vertical Horizontal Filter-Indikator identisch mit der MQL-Version ist in die Strategie eingebettet.
- Log-Anweisungen beschreiben jeden Positionswechsel und Regimeübergang zur einfacheren Fehlersuche.
