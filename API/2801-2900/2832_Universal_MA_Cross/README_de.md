# Universal MA Cross-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Universal MA Cross-Strategie** ist eine direkte Konvertierung des ursprünglichen MQL5-Expert-Advisors "UniversalMACrossEA" in das StockSharp-High-Level-Strategie-Framework. Der Algorithmus vergleicht einen schnellen und einen langsamen gleitenden Durchschnitt, die mit verschiedenen Berechnungsmethoden und Preisquellen konfiguriert werden können. Optionale Filter steuern, wie Signale bestätigt werden, ob Trades sofort umgekehrt werden, wie das Risikomanagement durchgeführt wird und wann die Strategie handeln darf.

## Handelslogik
### Indikatorverarbeitung
* Zwei gleitende Durchschnitte werden auf der ausgewählten Kerzenserie berechnet. Jeder Durchschnitt kann seine eigene Periode, Glättungsmethode (SMA, EMA, SMMA oder LWMA) und seinen eigenen Preistyp (Schlusskurs, Eröffnung, Hoch, Tief, Median, Typisch oder Gewichtet) verwenden.
* Der Parameter **MinCrossDistance** erfordert, dass der schnelle und langsame Durchschnitt an der Kreuzungskerze um mindestens die angegebene Anzahl von Preiseinheiten divergieren.
* Wenn **ConfirmedOnEntry** aktiviert ist, wird die Kreuzung an der vorherigen abgeschlossenen Kerze validiert (entspricht der Verwendung von Kerzenindizes 2 und 1 im ursprünglichen EA). Wenn deaktiviert, wird die aktuelle fertige Kerze mit der vorherigen Kerze verglichen, was das "Tick-Modus"-Verhalten der MQL-Version repliziert.
* Das Setzen von **ReverseCondition** tauscht die bullischen und bärischen Signale, sodass die Regeln invertiert werden können, ohne Indikatoreinstellungen zu ändern.

### Einstiegsregeln
1. Für einen Long-Einstieg muss der schnelle Durchschnitt den langsamen um mindestens **MinCrossDistance** nach oben kreuzen. Für einen Short-Einstieg muss der schnelle Durchschnitt den langsamen um diese Distanz nach unten kreuzen.
2. Wenn **StopAndReverse** aktiviert ist und ein entgegengesetztes Signal eintrifft, wird die aktive Position geschlossen, bevor neue Orders berücksichtigt werden.
3. Wenn **OneEntryPerBar** wahr ist, merkt sich die Strategie die Kerzenzeit des letzten Einstiegs und verweigert das Öffnen eines weiteren Trades während derselben Kerze.
4. Das Volumen jeder Order wird durch den Parameter **Volume** konfiguriert.

### Positionsverwaltung
* Stop-Loss- und Take-Profit-Niveaus werden in Preiseinheiten gemessen. Sie werden ignoriert, wenn **PureSar** wahr ist, was dem "Pure SAR"-Modus des ursprünglichen Experten entspricht.
* Die Trailing-Stop-Logik aktiviert sich, nachdem sich der Preis um **TrailingStop + TrailingStep** vom Einstiegspreis bewegt hat. Jede zusätzliche Bewegung von mindestens **TrailingStep** Punkten strafft den Stop um die angegebene **TrailingStop**-Distanz. Trailing läuft nicht im "Pure SAR"-Modus.
* Schutz-Niveaus werden bei jeder fertigen Kerze überwacht. Wenn der Kerzenbereich das Stop-Loss- oder Take-Profit-Niveau verletzt, wird die Position per Marktorder geschlossen.

### Session-Filter
* Wenn **UseHourTrade** aktiviert ist, handelt die Strategie nur, wenn die Eröffnungsstunde der Kerze zwischen **StartHour** und **EndHour** (einschließlich) liegt. Das Trailing-Stop-Management läuft außerhalb dieses Intervalls weiter, aber keine neuen Einstiege oder Stop-and-Reverse-Aktionen werden ausgeführt.

## Parameter
| Parameter | Beschreibung |
|-----------|--------------|
| `FastMaPeriod`, `SlowMaPeriod` | Perioden der schnellen und langsamen gleitenden Durchschnitte. |
| `FastMaType`, `SlowMaType` | Methoden des gleitenden Durchschnitts: Simple, Exponential, Smoothed (RMA) oder Linear Weighted. |
| `FastPriceType`, `SlowPriceType` | Preisquellen für die Durchschnitte. |
| `StopLoss`, `TakeProfit` | Schutzabstände in absoluten Preiseinheiten. Auf 0 setzen zum Deaktivieren. |
| `TrailingStop`, `TrailingStep` | Trailing-Stop-Offset und minimale Zusatzbewegung vor dem Verschieben des Stops. |
| `MinCrossDistance` | Mindestabstand zwischen den Durchschnitten an der Kreuzungskerze. |
| `ReverseCondition` | Bullische und bärische Regeln tauschen. |
| `ConfirmedOnEntry` | Nur abgeschlossene Kerzen zur Validierung verwenden. |
| `OneEntryPerBar` | Höchstens einen Einstieg pro Kerze erlauben. |
| `StopAndReverse` | Aktuelle Position schließen und bei entgegengesetzten Signalen umkehren. |
| `PureSar` | Stop-Loss-, Take-Profit- und Trailing-Logik deaktivieren. |
| `UseHourTrade`, `StartHour`, `EndHour` | Zeitfilter für Handelssessions (Stunden 0–23). |
| `Volume` | Ordervolumen für jede Position. |
| `CandleType` | Kerzendatentyp für Berechnungen. |

## Konvertierungshinweise
* Schutzorders werden intern durch Überprüfung von Kerzenhochs und -tiefs behandelt, da StockSharp-Strategien auf finalisierten Kerzen statt auf rohen Tick-Ereignissen operieren. Dies spiegelt das Verhalten des ursprünglichen Experten wider, während es innerhalb der High-Level-API bleibt.
* Trailing-Stop-Anpassungen folgen der MQL-Implementierung und erfordern eine Bewegung von **TrailingStop + TrailingStep**, bevor der Stop verschoben wird.
* Es wird keine Python-Version in dieser Konvertierung bereitgestellt, wie angefordert.
