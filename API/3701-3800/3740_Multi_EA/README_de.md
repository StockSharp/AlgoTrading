# Strategie MultiStrategyEA v1.2 (StockSharp Port)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist ein High-Level-StockSharp-Port des MetaTrader-Expertenberaters **MultiStrategyEA v1.2**. Das Original EA fasst sieben Oszillatoren zusammen und verwaltet mehrere Ordnungsgitter. Die StockSharp-Version konzentriert sich auf den Aspekt der Signalerzeugung und handelt eine einzelne Nettoposition, die durch einen Konsens zwischen den Indikatormodulen bestimmt wird. Auftragsverwaltung, Geldverwaltungsprofile, Raster und Wiederherstellungsfunktionen aus dem MT5-Code werden bewusst weggelassen, um die Implementierung an die übergeordnete API von StockSharp anzupassen und die Übersichtlichkeit zu wahren.

## Module
Die Strategie bewertet die folgenden Indikatormodule im ausgewählten Zeitrahmen:

1. **Beschleunigungs-/Verzögerungsoszillator (AC)** – Verwendet die Differenz zwischen dem Awesome-Oszillator und seinem 5-Perioden-SMA. Erfordert, dass der aktuelle Wert den Schwellenwert `AcLevel` überschreitet und relativ zum vorherigen Messwert ansteigt (oder abfällt).
2. **Durchschnittlicher Richtungsindex (ADX)** – Bestätigt Trends, wenn die ADX-Stärke über `AdxTrendLevel` liegt und die vorherrschende Richtungsbewegung auch `AdxDirectionalLevel` überschreitet.
3. **Toller Oszillator (AO)** – Erkennt Impulsausbrüche, wenn der Oszillator `AoLevel` überschreitet und in die gleiche Richtung weiterläuft.
4. **DeMarker** – Zeigt mögliche Umkehrungen an, wenn der Oszillator überverkaufte (`100 - DeMarkerThreshold`) oder überkaufte (`DeMarkerThreshold`) Bereiche verlässt.
5. **Force-Index + Bollinger-Bänder** – Erfordert, dass der Preis ein Bollinger-Band berührt, während der Force-Index (im Port genau wie im MT5-Skript skaliert) ein Momentum über `ForceConfirmationLevel` bestätigt. Ein optionales `BandDistanceFilter` weist Signale zurück, wenn die Bandbreite, gemessen in Pips, zu schmal oder zu breit ist.
6. **Money Flow Index (MFI)** – Ähnlich wie DeMarker; reagiert auf überkaufte und überverkaufte Zonen, die durch `MfiThreshold` bestimmt werden.
7. **MACD + Stochastic** – Erfordert, dass sowohl MACD (`MacdLevel`) als auch Stochastic (`StochasticLevel`) die gleiche Richtungsneigung bestätigen. MACD muss über/unter dem Füllstand und über/unter seiner Signallinie liegen. Stochastic muss über/unter dem Schwellenwert und über/unter der Signallinie liegen.

Jedes Modul gibt eine **Kauf**-, **Verkaufs**- oder **Neutral**-Stimme basierend auf der letzten fertigen Kerze ab.

## Konsenslogik
- Wenn `TradeAllStrategies` **true** ist (Standard), wartet die Strategie, bis mindestens `RequiredConfirmations` bullische Stimmen und **null** bärische Stimmen erscheinen, bevor sie eine Long-Position eingeht. Die gleiche Logik gilt auch für Kurzfilme.
- Wenn `TradeAllStrategies` **falsch** ist, reicht eine einzige bullische oder bärische Stimme für den Handel aus.
- Wenn `CloseInReverse` aktiviert ist, schließt die Strategie sofort eine entgegengesetzte Position, bevor sie eine neue eröffnet.

Die Implementierung betreibt nur eine Gesamtposition und versucht nicht, die ursprüngliche Auftragsbuchhaltung pro Modul von EA wiederherzustellen.

## Risikomanagement
- `StopLossPips` und `TakeProfitPips` werden mithilfe des `PriceStep` des Instruments in Preisversätze umgewandelt. Bei Symbolen mit 3 oder 5 Dezimalstellen wird die Pip-Größe automatisch mit 10 multipliziert, um das FX-Pip-Verhalten nachzuahmen.
- Stopps und Ziele werden bei jeder fertigen Kerze anhand der Kerzenhochs/-tiefs überprüft. Wenn einer der Schwellenwerte erreicht wird, wird die gesamte Position geschlossen.

## Unterschiede zum MT5 Expert Advisor
- Keine Raster-, Martingal- oder Wiederherstellungsfunktionen. Die Positionsgröße wird über den Parameter `Volume` festgelegt.
- Close-Signal-Varianten (`CloseOrdersType`-Optionen in MT5) sind nicht implementiert; Ausstiege basieren auf einem globalen Stop-Loss/Take-Profit oder dem optionalen Reverse-on-Oposite-Signal-Verhalten.
- Die Indikatorkonfiguration in StockSharp spiegelt die Hauptidee jedes Moduls wider, unterstützt jedoch nur die gängigste Interpretation anstelle der vielen Modusaufzählungen, die im Originalskript zu finden sind.
- Geldverwaltungsblöcke (automatisches Lot, Kontoschutz, symbolspezifische Pip-Bewertung) sind für diesen High-Level-Port nicht möglich.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `CandleType` | Von jedem Indikatormodul verwendete Datenreihe. |
| `Volume` | Gehandeltes Nettovolumen, wenn ein Konsenssignal erscheint. |
| `TradeAllStrategies` | Ermöglicht Konsensabstimmung; andernfalls löst jede einzelne Stimme einen Handel aus. |
| `RequiredConfirmations` | Anzahl der passenden bullischen oder bärischen Stimmen, die erforderlich sind, wenn ein Konsens ermöglicht wird. |
| `CloseInReverse` | Schließen Sie eine bestehende Position, bevor Sie die gegenüberliegende Seite öffnen. |
| `StopLossPips` / `TakeProfitPips` | Schutzstopp und Gewinnziel, gemessen in Pips. |
| `UseAcModule`, `AcLevel` | Umschalter und Schwellenwert für das Accelerator Oscillator-Modul. |
| `UseAdxModule`, `AdxPeriod`, `AdxTrendLevel`, `AdxDirectionalLevel` | ADX-Konfiguration. |
| `UseAoModule`, `AoLevel` | Tolle Oszillatorkonfiguration. |
| `UseDeMarkerModule`, `DeMarkerPeriod`, `DeMarkerThreshold` | Einstellungen des DeMarker-Oszillators. |
| `UseForceBollingerModule`, `BollingerPeriod`, `BollingerDeviation`, `ForceConfirmationLevel`, `BandDistanceFilter` | Index + Bollinger Bandfiltereinstellungen erzwingen. |
| `UseMfiModule`, `MfiPeriod`, `MfiThreshold` | Einstellungen für den Geldflussindex. |
| `UseMacdStochasticModule`, `MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod`, `MacdLevel`, `StochasticPeriod`, `StochasticSignalPeriod`, `StochasticSlowing`, `StochasticLevel` | Kombinationskonfiguration aus MACD und Stochastic. |

## Nutzungshinweise
1. Hängen Sie die Strategie an ein Instrument mit ausreichend historischen Daten an, damit alle Indikatoren gebildet werden können.
2. Konfigurieren Sie den Zeitrahmen und die Modulschwellenwerte entsprechend den gewünschten Marktbedingungen. Die Standardwerte replizieren die in den MT5-EA-Eingaben verwendeten Werte.
3. Die Konsenslogik hängt davon ab, wie viele Module aktiv sind. Wenn Sie Module deaktivieren, sollten Sie erwägen, `RequiredConfirmations` entsprechend zu senken.
4. Da die Strategie eine einzelne Nettoposition handelt, eignet sie sich für den Einsatz in Designer-, Runner- oder anderen StockSharp-High-Level-Umgebungen ohne zusätzliches Portfolio-Routing.

## Haftungsausschluss
Dieser Port konzentriert sich auf die Signalparität und nicht auf die Reproduktion des gesamten Risiko- und Geldmanagement-Stacks des ursprünglichen MetaTrader-Experten. Die vereinfachte Architektur erleichtert das Testen, Erweitern oder Integrieren in StockSharp-basierte Lösungen, die Ergebnisse weichen jedoch von der MT5-Version ab, als komplexe Funktionen (Raster, Wiederherstellungslose, Teilabschlüsse) der Hauptleistungstreiber waren.
