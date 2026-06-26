# Exp i-KlPrice Vol Direkt-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die **Exp i-KlPrice Vol Direkt-Strategie** ist eine StockSharp-Adaptation des MetaTrader 5 Expert Advisors
`Exp_i-KlPrice_Vol_Direct`. Das Originalsystem multipliziert einen benutzerdefinierten KlPrice-Oszillator mit dem Volumen,
glättet ihn mit mehreren gleitenden Durchschnittsstufen und reagiert auf Änderungen in der Steigung der resultierenden Linie.
Der Port behält die mehrstufige Verarbeitungskette bei, exponiert die gleichen konfigurierbaren Parameter und führt Trades
durch StockSharp's High-Level-API auf abgeschlossenen Kerzen aus.

Aus der MQL5-Version bewahrte Schlüsselideen:
- **Zweistufige Glättung von Preis und Spanne** – Preisdaten werden durch einen konfigurierbaren gleitenden Durchschnitt
  gefiltert, die Hoch-Tief-Spanne wird separat geglättet.
- **Volumengewichtung** – die Oszillatorausgabe wird vor einem abschließenden Jurik-Filter mit dem ausgewählten Volumenstrom
  multipliziert.
- **Direktionale Farbkarte** – die Strategie überwacht das Vorzeichen der geglätteten Oszillatorsteigung.
- **Signalverzögerung** – `SignalBar` ermöglicht dem Benutzer, zusätzliche geschlossene Kerzen zu fordern.

## Verarbeitungs-Pipeline
1. **Angewendete Preisauswahl** – Wählen aus den gleichen zwölf angewendeten Preisformeln wie der MQL-Indikator.
2. **Primäre Glättung** – `PriceMethod` über `PriceLength` Bars mit optionalem `PricePhase` anwenden.
3. **Spannenglättung** – dasselbe Verfahren für die Kerzenspanne (`High - Low`) mit `RangeMethod`, `RangeLength` und
   `RangePhase` wiederholen.
4. **Oszillatorkonstruktion** – `(Price - (PriceMA - RangeMA)) / (2 * RangeMA) * 100 - 50` berechnen, identisch zur MQL-
   Formel, und mit dem ausgewählten Volumenstrom (`VolumeSource`) multiplizieren.
5. **Abschließender Jurik-Filter** – der volumengewichtete Oszillator und der Rohvolumenstrom werden beide durch Jurik-
   gleitende Durchschnitte mit Periode `ResultLength` geleitet.
6. **Farberkennung** – den neuesten geglätteten Oszillatorwert mit dem vorherigen vergleichen. Steigende Werte färben die Bar
   bullisch (`0`), fallende Werte bearisch (`1`), gleiche Werte erben die vorherige Farbe.

## Handelslogik
### Long-Seite
- **Eintritt**: Wenn die Farbe bei der Signalbar (`SignalBar`) bullisch (`0`) ist und die unmittelbar ältere Farbe bearisch
  (`1`) ist, Long-Position öffnen wenn `AllowLongEntries = true` und die aktuelle Nettoposition nicht positiv ist.
- **Ausstieg**: Wenn die Signalbar-Farbe bullisch ist und `AllowShortExits = true`, offene Short-Positionen schließen.

### Short-Seite
- **Eintritt**: Wenn die Signalbar-Farbe nach bullisch (`0`) bearisch (`1`) wird, Short-Position öffnen wenn
  `AllowShortEntries = true` und die aktuelle Nettoposition nicht negativ ist.
- **Ausstieg**: Wenn die Signalbar-Farbe bearisch ist und `AllowLongExits = true`, bestehende Long-Exposition schließen.

## Parameter-Referenz
| Parameter | Beschreibung | Standard |
|-----------|--------------|----------|
| `CandleType` | Zeitrahmen der analysierten Kerzen. | `H4` |
| `VolumeSource` | Volumenstrom für die Gewichtung (`Tick` oder `Real`). | `Tick` |
| `PriceMethod` / `PriceLength` / `PricePhase` | Primärer Glättungsalgorithmus, Periode und Jurik-Phase für den angewendeten Preis. | `Sma`, `100`, `15` |
| `RangeMethod` / `RangeLength` / `RangePhase` | Glättungsalgorithmus, Periode und Phase für die Kerzenspanne. | `Jjma`, `20`, `100` |
| `ResultLength` | Jurik-Periode für den volumengewichteten Oszillator und den Volumenstrom. | `20` |
| `PriceMode` | Angewendete Preisformel (Close, Open, Median, Demark, TrendFollow0/1, etc.). | `Close` |
| `HighLevel2`, `HighLevel1`, `LowLevel1`, `LowLevel2` | Niveaumultiplikatoren für visuelle Diagnose; ändern keine Signale. | `0`, `0`, `0`, `0` |
| `SignalBar` | Anzahl vollständig geschlossener Kerzen, die vor der Bewertung des Farbwechsels übersprungen werden. | `1` |
| `AllowLongEntries` / `AllowShortEntries` | Berechtigungsflags für das Öffnen von Long/Short-Trades. | `true` |
| `AllowLongExits` / `AllowShortExits` | Berechtigungsflags für das Schließen bestehender Positionen bei entgegengesetzter Farbe. | `true` |
| `StopLossPoints` / `TakeProfitPoints` | Schutzoffsets in Preispunkten, die an `StartProtection` übergeben werden. | `1000`, `2000` |

## Risikomanagement
- Stop-Loss- und Take-Profit-Niveaus werden in `UnitTypes.Point`-Offsets übersetzt und von `StartProtection` verwaltet. Einen
  Wert auf `0` setzen, um den jeweiligen Schutz zu deaktivieren.
- Die Positionsgröße wird vollständig durch `Strategy.Volume` gesteuert.
- Farben werden nur ausgewertet, wenn die Strategie geformt, online und der Handel erlaubt ist.

## Einschränkungen und Unterschiede vs. MQL5
- Exotischere Glättungsannäherungen können leicht von der MT5-Ausgabe abweichen.
- StockSharp-Kerzen exponieren nur das Gesamtvolumen.
- Geldverwaltungsmodi des Original-EA sind nicht portiert.
- Orders werden unmittelbar nach dem Schließen der Signalkerze gesendet.
