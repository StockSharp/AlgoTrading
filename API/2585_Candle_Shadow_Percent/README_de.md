# Kerzen-Schatten-Prozent-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die **Kerzen-Schatten-Prozent-Strategie** ist eine direkte Portierung des MetaTrader-Expertenberaters *Candle shadow percent*. Sie sucht nach Kerzen, bei denen der obere oder untere Docht einen konfigurierbaren Prozentsatz des Kerzenkörpers erreicht. Wenn ein langer oberer Docht erscheint, eröffnet die Strategie eine Short-Position; wenn ein tiefer unterer Docht erscheint, eröffnet sie eine Long-Position. Die Trade-Richtung ist mit dem ursprünglichen Algorithmus abgestimmt und hält den Risikomanagement-Workflow intakt.

## Konvertierungshinweise
* Der ursprüngliche Experte hing von einem benutzerdefinierten Indikator ab. In der StockSharp-Version werden Docht- und Körperproportionen direkt aus abgeschlossenen Kerzen berechnet, daher gibt es keine externen Indikatorabhängigkeiten.
* Pip-Werte werden aus `Security.PriceStep` abgeleitet. Passen Sie `StopLossPips`, `TakeProfitPips` und `MinBodyPips` an die Tick-Größe des Instruments an.
* Die risikobasierte Positionsgrößenbestimmung spiegelt die MetaTrader `CMoneyFixedMargin`-Logik wider, indem ein Prozentsatz des aktuellen Portfolio-Werts gegen die konfigurierte Stop-Loss-Distanz riskiert wird.

## Kerzen-Qualifikation
Eine Kerze wird für den Trading in Betracht gezogen, wenn:
1. Ihre absolute Körpergröße mindestens `MinBodyPips * Security.PriceStep` beträgt.
2. Der entsprechende Docht positiv ist.
3. Das Docht-zu-Körper-Verhältnis die ausgewählte Schwellenwertlogik erfüllt:
   * **Oberer Docht** (Verkaufs-Setup): `(High − max(Open, Close)) / Body * 100` ist größer oder gleich `TopShadowPercent` wenn `TopShadowIsMinimum = true`, andernfalls muss es kleiner oder gleich diesem Wert sein.
   * **Unterer Docht** (Kauf-Setup): `(min(Open, Close) − Low) / Body * 100` ist größer oder gleich `LowerShadowPercent` wenn `LowerShadowIsMinimum = true`, andernfalls muss es kleiner oder gleich diesem Wert sein.
4. Wenn beide Dochte ihre Schwellenwerte in derselben Kerze erfüllen, behält die Strategie nur die Seite mit dem größeren Dochtverhältnis, um Doppelsignale zu vermeiden.

## Einstiegsregeln
* **Short-Einstieg** – ausgelöst durch ein gültiges oberes Dochtsignal, während die Strategie flat oder long ist. Die Strategie kehrt bei Bedarf das bestehende Long-Engagement um und setzt sofort Schutzorders.
* **Long-Einstieg** – ausgelöst durch ein gültiges unteres Dochtsignal, während die Strategie flat oder short ist. Das bestehende Short-Engagement wird automatisch geschlossen, bevor die neue Long-Position aufgebaut wird.

## Ausstiegsregeln
* **Stop-Loss** – platziert bei `StopLossPips * Security.PriceStep` vom Einstiegspreis entfernt. Long-Positionen verwenden `entry − stopDistance`; Short-Positionen verwenden `entry + stopDistance`.
* **Take-Profit** – optionales Ziel bei `TakeProfitPips * Security.PriceStep` vom Einstieg. Wenn `TakeProfitPips = 0`, ist das Ziel deaktiviert und Positionen verlassen sich ausschließlich auf den Stop-Loss oder ein entgegengesetztes Signal zum Ausstieg.
* Die Strategie überwacht abgeschlossene Kerzen. Wenn ein Kerzenbereich den Stop oder das Ziel berührt, wird die Position beim nächsten Verarbeitungszyklus geschlossen.

## Positionsgrößenbestimmung
* Das Risiko pro Trade wird als `Portfolio.CurrentValue * (RiskPercent / 100)` berechnet. Wenn der Portfolio-Wert nicht verfügbar ist, fällt die Strategie auf das konfigurierte Strategievolumen zurück.
* Die Menge entspricht dem Risikobetrag dividiert durch die Stop-Loss-Distanz. Bei der Umkehr addiert der Algorithmus die absolute Größe des aktuellen Engagements, um eine vollständige Umkehr zu gewährleisten, was dem Verhalten des ursprünglichen MetaTrader-Experten entspricht.

## Parameter
| Parameter | Beschreibung |
|-----------|--------------|
| `CandleType` | Zeitrahmen oder Datentyp für Kerzen-Abonnements. |
| `StopLossPips` | Stop-Loss-Abstand in Pips/Ticks relativ zum Instrument. Muss größer als null sein. |
| `TakeProfitPips` | Take-Profit-Abstand in Pips/Ticks. Null verwenden zum Deaktivieren des Ziels. |
| `RiskPercent` | Prozentualer Anteil des Portfolio-Werts, der pro Trade riskiert wird. |
| `MinBodyPips` | Minimale Kerzenkörpergröße (in Pips/Ticks), die vor der Auswertung der Dochtverhältnisse erforderlich ist. |
| `EnableTopShadow` | Aktiviert Short-Signale basierend auf der Länge des oberen Dochts. |
| `TopShadowPercent` | Schwellenprozentsatz für das obere Docht-zu-Körper-Verhältnis. |
| `TopShadowIsMinimum` | Wenn true, muss das Verhältnis größer oder gleich dem Schwellenwert sein; wenn false, muss es kleiner oder gleich sein. |
| `EnableLowerShadow` | Aktiviert Long-Signale basierend auf der Länge des unteren Dochts. |
| `LowerShadowPercent` | Schwellenprozentsatz für das untere Docht-zu-Körper-Verhältnis. |
| `LowerShadowIsMinimum` | Steuert, ob der untere Dochtschwellenwert als Mindest- oder Höchstbedingung behandelt wird. |

## Verwendungstipps
* Beginnen Sie mit einem Zeitrahmen ähnlich dem ursprünglichen EA (z. B. 5-Minuten-Kerzen) und passen Sie Pip-Abstände für Ihr Instrument an.
* Erhöhen Sie `MinBodyPips`, wenn Rauschen zu viele Signale erzeugt; verringern Sie es, um kleinere Umkehrungen zu erfassen.
* Kombinieren Sie die Strategie mit zusätzlichen Filtern (wie Trendindikatoren) durch Erweitern der Klasse—Bindungen für zusätzliche Indikatoren können innerhalb von `OnStarted` hinzugefügt werden.
* Validieren Sie immer die Tick-Größeninterpretation auf einem Demo-Portfolio, bevor Sie es in der Produktion einsetzen.
