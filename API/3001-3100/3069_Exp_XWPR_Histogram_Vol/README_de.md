# Exp XWPR Histogramm Vol Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist eine C#-Konvertierung des MetaTrader-Experten **Exp_XWPR_Histogram_Vol**. Sie handelt auf den Farbänderungen des
benutzerdefinierten Indikators XWPR Histogramm Vol, der den Williams %R-Oszillator mit dem Kerzenvolumen multipliziert und das Ergebnis glättet. Der
Port behält das ursprüngliche Zwei-Slot-Geldmanagement-Schema (primäres und sekundäres Volumen) bei und reproduziert dieselben farbgesteuerten
Einstiegs- und Ausstiegsregeln unter Verwendung der StockSharp-High-Level-API.

Der Algorithmus verarbeitet nur abgeschlossene Kerzen. Bei jeder neuen Kerze prüft er die Histogrammfarbe eine konfigurierbare Anzahl von Kerzen
zurück in der Vergangenheit und reagiert, wenn die Farbübergänge die bullischen oder bärischen Schwellenwerte des Indikators überschreiten.

## Indikatorlogik
1. Williams %R (`WprPeriod`) wird um +50 verschoben und mit dem ausgewählten Kerzenvolumen (`VolumeMode`) multipliziert.
2. Sowohl der gewichtete Williams %R als auch das Rohvolumen durchlaufen identische Glättungsfilter (`SmoothingMethod`,
   `SmoothingLength`, `SmoothingPhase`).
3. Vier dynamische Niveaus werden aus dem geglätteten Volumen abgeleitet: `HighLevel2`, `HighLevel1`, `LowLevel1` und `LowLevel2`.
4. Histogrammfarben entsprechen den durch diese Niveaus definierten Zonen:
   - **0** – Histogramm über `HighLevel2` (stark bullisch).
   - **1** – Histogramm zwischen `HighLevel1` und `HighLevel2` (moderat bullisch).
   - **2** – Histogramm zwischen `LowLevel1` und `HighLevel1` (neutral).
   - **3** – Histogramm zwischen `LowLevel2` und `LowLevel1` (moderat bärisch).
   - **4** – Histogramm unter `LowLevel2` (stark bärisch).

## Signalregeln
Die Strategie liest zwei historische Farben pro Auswertung: Balken `SignalBar + 1` (älter) und Balken `SignalBar` (neuer).

- **Primäres Long öffnen (Volumen = `PrimaryVolume`)** wenn die ältere Balkenfarbe `1` ist und die neuere Balkenfarbe sich zu `2`, `3` oder
  `4` bewegt. Die Bewegung fordert gleichzeitig das Schließen von Short-Positionen an.
- **Sekundäres Long öffnen (Volumen = `SecondaryVolume`)** wenn die ältere Balkenfarbe `0` ist und die neuere Balkenfarbe zu etwas
  anderem als `0` wird. Das gleiche Signal schließt auch Shorts.
- **Primäres Short öffnen (Volumen = `PrimaryVolume`)** wenn die ältere Balkenfarbe `3` ist und die neuere Balkenfarbe auf `0`, `1`
  oder `2` steigt, während auch Longs geschlossen werden.
- **Sekundäres Short öffnen (Volumen = `SecondaryVolume`)** wenn die ältere Balkenfarbe `4` ist und die neuere Balkenfarbe zu
  `0`, `1`, `2` oder `3` wird, wobei erneut Long-Exits erzwungen werden.
- **Longs schließen** immer wenn die ältere Farbe `3` oder `4` (bärische Zone) ist.
- **Shorts schließen** immer wenn die ältere Farbe `0` oder `1` (bullische Zone) ist.

Für jede Richtung werden zwei unabhängige Positionsslots geführt. Ein Signal löst nur eine Order aus, wenn der entsprechende Slot
derzeit inaktiv ist und das relevante Einstiegs-Flag (`AllowLongEntry`, `AllowShortEntry`) es erlaubt.

## Risikomanagement
- `StopLossSteps` und `TakeProfitSteps` werden über `StartProtection` in StockSharp-Schutzorders übersetzt. Die Werte werden
  in Instrumenten-Preisschritten ausgedrückt.
- `DeviationSteps` wird für Kompatibilität mit der MQL-Eingabeliste beibehalten. StockSharp-Marktorders verwenden es nicht.

## Parameter
| Name | Beschreibung |
|------|--------------|
| `CandleType` | Zeitrahmen für die dem Indikator zugeführten Kerzen. |
| `PrimaryVolume`, `SecondaryVolume` | Von den Level-1- und Level-2-Slots angewendete Volumina. |
| `AllowLongEntry`, `AllowShortEntry` | Öffnen neuer Long- oder Short-Positionen aktivieren. |
| `AllowLongExit`, `AllowShortExit` | Schließen von Long- oder Short-Exposure bei Ausstiegssignalen aktivieren. |
| `StopLossSteps`, `TakeProfitSteps` | Optionale Schutzabstände in Preisschritten (0 deaktiviert den jeweiligen Schutz). |
| `DeviationSteps` | Für Kompatibilität reserviert; hat keinen Einfluss auf StockSharp-Orders. |
| `SignalBar` | Anzahl geschlossener Kerzen zur Verschiebung der Signalauswertung (0 = letzte abgeschlossene Kerze). |
| `WprPeriod` | Rückschauperiode für die Williams %R-Berechnung. |
| `VolumeMode` | Wählt zwischen Tick-Anzahl (`Tick`) oder Realvolumen (`Real`) im Histogramm. |
| `HighLevel2`, `HighLevel1` | Multiplikatoren, die die oberen bullischen Schwellenwerte definieren. |
| `LowLevel1`, `LowLevel2` | Multiplikatoren, die die unteren bärischen Schwellenwerte definieren. |
| `SmoothingMethod` | Moving-Average-Typ für sowohl das Histogramm als auch das Basisvolumen. |
| `SmoothingLength` | Länge der Glättungsfilter. |
| `SmoothingPhase` | Phase für Jurik-basierte Glätter (von anderen Methoden ignoriert). |

## Verwendungshinweise
- Die Strategie handelt ein einzelnes Wertpapier, das von `GetWorkingSecurities()` zurückgegeben wird, und verwendet Marktorders für alle Aktionen.
- Signale werden einmal pro abgeschlossener Kerze ausgewertet. Der zusätzliche Historiebuffer verhindert doppelte Orders auf demselben Balken.
- Die beiden Einstiegs-Slots agieren unabhängig. Deaktivieren Sie einen Slot, indem Sie das entsprechende Volumen auf `0` setzen oder das
  `Allow*Entry`-Flag deaktivieren.
- Die Konvertierung repliziert keine MetaTrader-Magic-Numbers oder Margin-Modi. Die Portfolio-Größenbestimmung wird vollständig durch die
  Parameter `PrimaryVolume` und `SecondaryVolume` gesteuert.
