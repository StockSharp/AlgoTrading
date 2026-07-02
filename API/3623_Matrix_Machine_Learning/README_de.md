# Matrix-Strategie für maschinelles Lernen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Matrix Machine Learning ist ein auf neuronalen Netzwerken basierender Ansatz, der ursprünglich für MetaTrader 5 im Rahmen des Bildungsprojekts „MQL5Book“ veröffentlicht wurde. Das Expertenskript sammelt ein Fenster mit Tick-Preisen, wandelt aufeinanderfolgende Preisunterschiede in eine binäre Sequenz um und trainiert ein wiederkehrendes neuronales Hopfield-Netzwerk. Das trainierte Netzwerk wird an einem In-Sample-Segment ausgewertet, an einem Out-of-Sample-Segment validiert und schließlich verwendet, um die Richtung der nächsten Bewegungen abzuleiten. Positionen werden eröffnet, wenn das erste Element des prognostizierten Binärvektors eine bullische (`+1`) oder bärische (`-1`) Richtung zeigt.

Diese C#-Version portiert die ursprüngliche Logik auf die StockSharp-Hochebene API und ersetzt die Tick-Verarbeitung durch fertige Kerzen, um ein stabiles plattformübergreifendes Verhalten sicherzustellen. Bei jedem Kerzenschluss wird das binäre Preismuster aktualisiert, das Hopfield-Netzwerk neu trainiert, die historische Genauigkeit bewertet und eine Online-Prognose für die bevorstehenden Schritte erstellt.

## Details zum Algorithmus
1. Sammeln Sie die letzten `HistoryDepth` Kerzenschlüsse. Die neuesten `ForwardDepth`-Punkte bilden den Out-of-Sample-Satz, während die verbleibenden Werte das Trainingssegment bilden.
2. Konvertieren Sie aufeinanderfolgende Differenzen nahe beieinander in eine binäre Folge: Positive Deltas oder Null-Deltas werden zu `+1`, negative Deltas werden zu `-1`.
3. Trainieren Sie eine Hopfield-Gewichtsmatrix, indem Sie die äußeren Produkte jedes Prädiktor-/Ausgabepaars summieren, wobei die Prädiktorlänge gleich `PredictorLength` und die Antwortlänge gleich `ForecastLength` ist.
4. Bewerten Sie die trainierte Matrix anhand der Trainings- und Forward-Sets. Die Genauigkeitsmetrik stimmt mit dem ursprünglichen Skript überein: Das Skalarprodukt zwischen vorhergesagten und tatsächlichen Antwortvektoren wird gemittelt und auf einen Prozentsatz neu skaliert.
5. Erstellen Sie das neueste Online-Binärmuster und führen Sie die Hopfield-Inferenzschleife aus (Tanh-Aktivierung mit einem Konvergenzschwellenwert). Die erste Prognosekomponente bestimmt die Handelsentscheidung.

## Parameter
- **Verlaufstiefe** – Anzahl der letzten für das Hopfield-Netzwerk gespeicherten Kerzenschließungen. Muss größer als `ForwardDepth` und mindestens `PredictorLength + ForecastLength + 1` sein.
- **Vorwärtstiefe** – Größe des Validierungsfensters, das für Vorwärtsprüfungen reserviert ist. Erfordert mindestens `ForecastLength + 1` Schließungen.
- **Prädiktorlänge** – Länge des vom neuronalen Netzwerk verwendeten binären Eingabevektors.
- **Prognoselänge** – Anzahl zukünftiger Schritte, die vom Netzwerkausgabevektor vorhergesagt werden.
- **Kerzentyp** – StockSharp `DataType` beschreibt die vom Connector angeforderte Kerzenserie.
- **Debug-Protokoll** – wenn aktiviert, werden detaillierte Zwischenvektoren, Beispielvergleiche und Online-Prognosen gedruckt.

## Handelslogik
- Wenn das erste Element der Hopfield-Prognose positiv ist und die Strategie flach oder short ist, wird eine Marktkauforder für `Volume + |Position|` übermittelt, um in eine Long-Position zu wechseln.
- Wenn das erste Element negativ ist und die Strategie flach oder long ist, wird ein Marktverkaufsauftrag für `Volume + |Position|` übermittelt, um in eine Short-Position zu wechseln.
- Nullprognosen werden ignoriert, um unnötige Abwanderung zu vermeiden.

Die Strategie zeichnet automatisch Kerzen und eigene Trades auf, wenn ein Chartbereich verfügbar ist. Das Hopfield-Netzwerk trainiert jede fertige Kerze neu, um die neuronalen Gewichte mit der neuesten Marktstruktur synchronisiert zu halten.
