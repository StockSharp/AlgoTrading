# Daytrading-Impulsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **DayTrading-Strategie** ist eine originalgetreue C#-Konvertierung des klassischen MetaTrader 4-Expertenberaters „DayTrading“, der 2005 von NazFunds veröffentlicht wurde. Der ursprüngliche Roboter wurde für 5-Minuten-Forex-Charts entwickelt und kombiniert mehrere Momentum- und Trendfolgeindikatoren, um kurzfristige Richtungsbewegungen mit einem bescheidenen festen Ziel und optionalem Trailing Stop zu erfassen. Diese StockSharp-Implementierung reproduziert die Kernentscheidungslogik und stellt gleichzeitig jeden wichtigen Schwellenwert als Strategieparameter bereit, sodass er optimiert oder an verschiedene Instrumente angepasst werden kann.

## Indikatorstapel

Die Strategie bewertet vier Indikatoren für die ausgewählte Kerzenserie:

- **Parabolic SAR** (`ParabolicSar`) mit konfigurierbarer Beschleunigung, Inkrement und Obergrenze. Es definiert die grundlegende Trendrichtung und muss unter/über dem Preis spiegeln, um neue Einträge zu ermöglichen.
- **MACD (12, 26, 9)** (`MovingAverageConvergenceDivergenceSignal`). Die MACD-Linie muss bei Long-Positionen unter der Signallinie und bei Short-Positionen darüber liegen, was den ursprünglichen Histogramm-/Signal-Vergleich in MQL widerspiegelt.
- **Stochastic Oszillator (5, 3, 3)** (`StochasticOscillator`). Die %K-Linie muss bei Long-Positionen unter 35 und bei Short-Positionen über 60 bleiben, um sicherzustellen, dass der Markt aus einer überverkauften/überkauften Zone herauskommt.
- **Impuls (14)** (`Momentum`). Ein Wert unter 100 schaltet Long-Trades frei, während ein Wert über 100 Short-Trades zulässt, genau wie im MT4-Skript.

Alle Indikatoren werden über die übergeordnete `BindEx`-Pipeline verarbeitet, sodass keine manuelle Pufferverwaltung oder historische Indexierung erforderlich ist.

## Handelsregeln

### Teilnahmebedingungen

Eine **Long-Position** wird eröffnet, wenn bei der zuletzt abgeschlossenen Kerze alle folgenden Bedingungen zutreffen:

1. Der Parabolic SAR-Punkt liegt auf oder unter dem aktuellen Briefkurs **und** der vorherige Punkt lag über dem aktuellen Punkt (frischer SAR-Umschlag in bullisch).
2. Momentum liegt unter 100.
3. Die MACD-Linie liegt unterhalb ihrer Signallinie.
4. Stochastic %K liegt unter 35.

Eine **Short**-Position wird eröffnet, wenn die symmetrischen Bedingungen erfüllt sind:

1. Der Punkt Parabolic SAR wird auf oder über dem aktuellen Geldkurs gedruckt, **und** der vorherige Punkt lag unter dem aktuellen Punkt (bearish flip).
2. Das Momentum liegt über 100.
3. Die MACD-Linie liegt über ihrer Signallinie.
4. Stochastic %K liegt über 60.

Es kann jeweils nur eine Position offen sein. Immer wenn ein entgegengesetztes Signal auftritt, wird die bestehende Position geschlossen und es erfolgt kein erneuter Einstieg bei derselben Kerze – genau wie in der MetaTrader-Implementierung, wo der `OrdersTotal`-Scan ein sofortiges Neuladen verhindert.

### Exit-Management

- **Stop-Loss / Take-Profit:** Optionale feste Distanzen (in Punkten) werden anhand der Tick-Größe des Instruments in absolute Preise umgewandelt. Sie werden bei jeder Kerze neu bewertet und schließen die Position, wenn sie intrabar überschritten werden.
- **Trailing Stop:** Sobald der Preis um die konfigurierte Anzahl von Punkten steigt, wird ein Trailing Stop aktiviert. Bei Long-Trades liegt der Stop unterhalb des Schlusskurses; Bei Short-Trades bleibt es über dem Schlusskurs. Der Stopp geht nie zurück, sodass der Gewinn schrittweise gesperrt wird.
- **Gegensignal:** Ein gültiges Gegensignal löst die aktuelle Position sofort auf, bevor ein neuer Eintrag berücksichtigt wird.

Es wird keine zusätzliche Raster-, Skalierungs- oder Absicherungslogik hinzugefügt. Die Strategie bleibt so leichtgewichtig und deterministisch wie das Original EA.

## Parameter

| Parameter | Standard | Beschreibung |
| --- | --- | --- |
| `LotSize` | 1 | Volumen jeder Marktorder. Die Eigenschaft `Strategy.Volume` wird beim Start mit diesem Wert synchronisiert. |
| `TrailingStopPoints` | 15 | Nachlaufdistanz in Punkten. Auf Null setzen, um das Nachziehen zu deaktivieren. |
| `TakeProfitPoints` | 20 | Feste Take-Profit-Distanz in Punkten. Auf Null setzen, um das Ziel zu entfernen. |
| `StopLossPoints` | 0 | Schutzstoppabstand in Punkten. Zero reproduziert das ursprüngliche „No Stop“-Verhalten. |
| `SlippagePoints` | 3 | Platzhalter für maximale Ausführungsschlupf (zur Kompatibilität mit der MT4-Eingabe). Wird nicht automatisch erzwungen, wird aber der Vollständigkeit halber beibehalten. |
| `CandleType` | Zeitrahmen von 5 Minuten | Von allen Indikatoren verwendete Kerzenserie. Bleiben Sie bei M5, um der ursprünglichen Empfehlung von EA zu entsprechen. |
| `MacdFastPeriod` | 12 | Schnelle EMA-Länge in der MACD-Berechnung. |
| `MacdSlowPeriod` | 26 | Langsame EMA-Länge in der MACD-Berechnung. |
| `MacdSignalPeriod` | 9 | Signallänge EMA in der MACD-Berechnung. |
| `StochasticLength` | 5 | %K Lookback-Länge für den Stochastic-Oszillator. |
| `StochasticSignal` | 3 | %D Glättungslänge. |
| `StochasticSlow` | 3 | Zusätzliche Verlangsamung auf der %K-Linie. |
| `MomentumPeriod` | 14 | Momentum-Lookback-Länge. |
| `SarAcceleration` | 0,02 | Anfänglicher Beschleunigungsfaktor für Parabolic SAR. |
| `SarStep` | 0,02 | Auf den Beschleunigungsfaktor angewendetes Inkrement nach jedem neuen Extremwert. |
| `SarMaximum` | 0,2 | Maximaler Beschleunigungsfaktor für Parabolic SAR. |

Alle numerischen Parameter können dank der `SetCanOptimize(true)`-Hinweise durch den Optimierungsworkflow von StockSharp optimiert werden.

## Implementierungshinweise

- Geld-/Briefpreise werden aus Live-Level1-Daten abgeleitet, sofern verfügbar; andernfalls fungiert der Kerzenschluss als Fallback, so dass die Logik bei historischen Tests robust bleibt.
- Die Punkteumrechnung hängt vom `Step`/`PriceStep` des Instruments ab. Wenn keine Angabe erfolgt, wird ein konservativer `0.0001`-Fallback verwendet, der einem Standard-Forex-Pip entspricht.
- Das Positionsmanagement spiegelt den MT4 EA wider: Die Strategie ist nie eine Pyramide und hält nie beide Richtungen gleichzeitig.
- Kommentare im Code sind gemäß den Projektrichtlinien auf Englisch, während diese README-Datei eine erweiterte Dokumentation für ein einfacheres Onboarding enthält.

## Nutzungstipps

1. Weisen Sie der Strategie das gewünschte Forex-Paar zu, belassen Sie den Kerzentyp bei 5 Minuten und starten Sie die Strategie. Die Anzeigen werden automatisch aufgewärmt.
2. Erwägen Sie die Aktivierung eines Stop-Loss ungleich Null, wenn Sie Live-Daten verwenden. Das ursprüngliche Skript empfahl den Handel ohne Stop-Loss, aber Trailing-Stops allein reichen möglicherweise nicht zur Risikokontrolle aus.
3. Für algorithmische Portfolios können Sie diese Strategie zu einem `BasketStrategy` hinzufügen und die Kapitalallokation extern verwalten, während Sie weiterhin von den offengelegten Parametern zur Optimierung profitieren.

Diese Dokumentation sorgt zusammen mit den russischen und chinesischen Übersetzungen im selben Ordner für vollständige Transparenz der konvertierten Logik.
