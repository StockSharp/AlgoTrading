# Exp Digital MACD Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die Exp Digital MACD Strategie recreiert das Verhalten des ursprünglichen MetaTrader 5-Expertenberaters "Exp_Digital_MACD" innerhalb des StockSharp-Frameworks. Das System hört auf abgeschlossene Kerzen aus einem dedizierten Zeitrahmen und reagiert auf die relative Position und Steigung eines MACD-artigen Oszillators. Vier Betriebsmodi reproduzieren die Entscheidungsregeln des Quellcodes:

1. **Breakdown** – handelt Nulllinien-Übergänge des Oszillators.
2. **MACD Twist** – beobachtet eine Umkehr in der Steigung der MACD-Linie.
3. **Signal Twist** – verwendet die Wendung der Signallinie selbst als Bestätigung.
4. **MACD Disposition** – sucht nach dem MACD-Histogramm, das über oder unter seine Signallinie kreuzt.

Da StockSharp den proprietären "Digital MACD"-Filter nicht bereitstellt, verwendet die Strategie den Standard-`MovingAverageConvergenceDivergenceSignal`-Indikator. Die Standardwerte (schnelle EMA 12, langsame EMA 26, Signal 5) approximieren das ursprüngliche Setup, bei dem die Signal-Glättungslänge gleich fünf war. Die Strategie verarbeitet nur abgeschlossene Kerzen und hält eine kurze rollierende Historie in privaten Feldern, um das `SignalBar = 1`-Verhalten der MQL-Implementierung widerzuspiegeln.

## Parameter
- **Mode** – wählt einen der vier oben beschriebenen Handelsalgorithmen aus. Standard: `MacdTwist`.
- **FastPeriod** – Länge der von MACD verwendeten schnellen EMA. Standard: `12`.
- **SlowPeriod** – Länge der von MACD verwendeten langsamen EMA. Standard: `26`.
- **SignalPeriod** – Länge der Signal-Glättungs-EMA. Standard: `5`, um mit dem ursprünglichen Expertenberater übereinzustimmen.
- **CandleType** – Zeitrahmen für das Kerzenabonnement. Standard: `4h`-Kerzen.
- **OrderVolume** – Anzahl der Kontrakte oder Lots bei jeder Marktorder.
- **StopLossPoints / TakeProfitPoints** – Schutzoffsets in Wertpapier-Preisschritten ausgedrückt. Sie werden aktiviert, wenn das Wertpapier einen gültigen `Step`-Wert exponiert; auf null setzen zum Deaktivieren.
- **EnableLongEntry / EnableShortEntry** – Schalter, die das Öffnen neuer Long- oder Short-Positionen erlauben oder verbieten.
- **EnableLongExit / EnableShortExit** – Schalter, die der Strategie erlauben, bestehende Positionen in der entsprechenden Richtung zu schließen.

## Handelslogik
Der Algorithmus arbeitet mit dem Schlusswert jeder Kerze:

- **Breakdown**: Wenn der MACD-Wert zwei Balken zuvor über null war, schließt die Strategie optional Short-Positionen und öffnet einen Long-Trade, wenn der nachfolgende Balken auf oder unter null fällt. Umgekehrt schließt das System bei einem MACD unter null zwei Balken zuvor Longs und öffnet Shorts, wenn der nächste Balken auf oder über die Nulllinie steigt. Dies spiegelt die konträre Nulllinien-Logik im Expertenberater wider.
- **MACD Twist**: Verfolgt drei sequentielle MACD-Werte. Ein Long-Signal erscheint, wenn die Linie einen lokalen Tiefpunkt bildet (value[2] > value[1] und value[0] > value[1]). Ein lokaler Hochpunkt erzeugt ein Short-Signal. Ausstiege folgen dem entgegengesetzten Twist.
- **Signal Twist**: Wendet dieselbe Wendepunktdetektion auf den Signallinien-Buffer an.
- **MACD Disposition**: Arbeitet mit MACD- und Signal-Buffern. Wenn der MACD zuvor über der Signallinie saß, aber die nächste Beobachtung zurück auf oder unter sie fällt, geht die Strategie long und schließt Shorts. Der umgekehrte Übergang führt zu Short-Einstiegen und Long-Ausstiegen.

Jeder Einstieg verwendet eine Marktorder der Größe `OrderVolume + |aktuelle Position|`, sodass eine Umkehr die bestehende Exposition schließt und eine neue Position in einer einzigen Anweisung etabliert. Ausstiegssignale geben Marktorders aus, die nur die offene Position flatten.

## Risikomanagement
`StartProtection` wird aktiviert, sobald die Strategie startet. Wenn `StopLossPoints` oder `TakeProfitPoints` über null gesetzt sind und der Wertpapierschritt bekannt ist, werden die entsprechenden Schutzorders in absoluten Preiswerten konfiguriert. Die Parameter auf null zu halten deaktiviert den automatischen Schutz.

## Implementierungshinweise
- Die Strategie wertet nur die zuletzt abgeschlossene Kerze aus, äquivalent zu `SignalBar = 1` in der MQL-Version.
- Die StockSharp-MACD-Implementierung unterscheidet sich vom proprietären Digital MACD. Benutzer können die EMA-Längen abstimmen, um das ursprüngliche Verhalten bei Bedarf besser anzunähern.
- Alle Kommentare innerhalb der C#-Quelldatei werden wie gewünscht auf Englisch bereitgestellt.

## Verwendung
1. Die Strategie an ein Portfolio und ein Wertpapier anhängen, das den erforderlichen Kerzen-Zeitrahmen liefert.
2. Die Parameter anpassen, um dem gewünschten Symbol und Volatilitätseigenschaften zu entsprechen.
3. Die Strategie starten; sie abonniert automatisch die konfigurierten Kerzen, verarbeitet MACD-Werte und platziert Marktorders gemäß dem ausgewählten Modus.
4. Logs oder optionale Chart-Ausgabe überwachen, um Indikatorwerte und Positionsänderungen zu verfolgen.
