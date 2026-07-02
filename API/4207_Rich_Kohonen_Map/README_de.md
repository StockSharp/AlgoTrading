# Reichhaltige Kohonen-Kartenstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Rich Kohonen Map Strategy ist eine Umsetzung des MetaTrader 4 Expert Advisors „Rich.mq4“. Das ursprüngliche System erstellt eine selbstorganisierende Karte (Kohonen-Netzwerk) über Merkmalsvektoren, die aus Pivot-Berechnungen von Tom DeMark abgeleitet wurden, und klassifiziert den nächsten Balken als Kauf-, Verkaufs- oder Haltegelegenheit. Der StockSharp-Port behält den Lernansatz bei und integriert sich gleichzeitig in die High-Level-Strategie API, wobei er ausschließlich auf abgeschlossene Kerzen und Marktaufträge angewendet wird.

## Marktdaten
- **Instrument** – konfiguriert über das verknüpfte `Security` in der Hostanwendung.
- **Kerzentyp** – Parameter `CandleType` (Standard: 1-stündiger Zeitrahmen). Die Strategie erfordert mindestens sieben fertige Kerzen, bevor Signale erzeugt werden, damit sowohl aktuelle als auch vorherige Merkmalsvektoren zusammengesetzt werden können.

## Handelslogik
1. Behalten Sie ein fortlaufendes Fenster der letzten sieben abgeschlossenen Kerzen bei.
2. Bauen Sie auf jeder fertigen Kerze zwei Sieben-Elemente-Vektoren auf:
   - Der **aktuelle Vektor** verwendet die letzte Eröffnung zusammen mit Tom DeMarks Pivot-Prognosen, die aus den vorherigen fünf Kerzen berechnet wurden.
   - Der **vorherige Vektor** verschiebt das Fenster um einen Balken und stellt den Balken dar, der gerade geschlossen wurde. Dieser Vektor wird für das Training verwendet.
3. Vergleichen Sie den aktuellen Vektor mit drei Kohonen-Karten (Kaufen, Verkaufen, Halten) und zeichnen Sie die euklidische Distanz zu jeder am besten passenden Einheit auf.
4. Wählen Sie die Aktion mit dem kleinsten Abstand aus und legen Sie die Zielposition fest:
   - Kaufen → Long Exposure in Höhe des berechneten Volumens.
   - Verkaufen → Short-Engagement in gleicher Größenordnung.
   - Halten → keine Position.
Die Strategie sendet Marktaufträge für die Differenz zwischen der aktuellen und der Zielposition, sodass das endgültige Engagement mit der Entscheidung übereinstimmt.
5. Berechnen Sie die Bewegung von Eröffnung zu Eröffnung (in Pips) zwischen den letzten beiden Kerzen und trainieren Sie die Karte:
   - Positive Bewegung innerhalb von `[MinPips, MaxPips]` → Füge den vorherigen Vektor zur Kaufkarte hinzu.
   - Negative Bewegung innerhalb von `[-MaxPips, -MinPips]` → Füge den vorherigen Vektor zur Verkaufskarte hinzu.
   - Andernfalls → den Vektor in der Hold-Map speichern.
6. Die Positionsgröße wird dynamisch aus dem Portfoliosaldo bestimmt: `floor(balance / 50) / 10`. Wenn dies Null ergibt, wird stattdessen der Fallback-Parameter `Lots` verwendet.

## Parameter
- `MinPips` – Untergrenze (in Pips) für die Berücksichtigung einer positiven Bewegung von Eröffnung zu Eröffnung als Beispiel für ein Kauftraining.
- `MaxPips` – Obergrenze (in Pips) für Kauf-/Verkaufstrainingsbeispiele.
- `TakeProfit`, `StopLoss` – vom MQL-Experten zu Dokumentationszwecken aufbewahrt. Die High-Level-Implementierung schließt oder kehrt Positionen über Marktaufträge statt durch Anbringen von Stopps um.
- `Lots` – Fallback-Volumen, das angewendet wird, wenn die saldobasierte Formel Null ergibt.
- `Slippage` – reserviert für die manuelle Auftragsoptimierung (wird nicht direkt von den übergeordneten API-Helfern verwendet).
- `MapPath` – Binärdateipfad, der zum Beibehalten der drei Kohonen-Karten zwischen den Läufen verwendet wird.
- `EAName` – optionaler Kommentar, der als Referenz gespeichert wird.
- `CandleType` – Kerzenabonnement, das zur Feature-Extraktion verwendet wird.

## Permanente Kartenspeicherung
Die Strategie speichert die trainierte Karte in einer durch `MapPath` definierten Binärdatei (Standard: `rl.bin` im Arbeitsverzeichnis). Die Datei enthält nacheinander die Kauf-, Verkaufs- und Haltematrizen. Beim Start werden die Matrizen geladen und die Strategie zählt die nicht leeren Zeilen, um das Training ab dem vorherigen Status fortzusetzen. Fehlende Dateien werden ignoriert, was dazu führt, dass die Karten mit einem mit Null gefüllten Speicher beginnen.

## Unterschiede zum ursprünglichen MQL-Experten
- Aufträge werden über StockSharp-Helfer (`BuyMarket` / `SellMarket`) erteilt und zielen auf das endgültige gewünschte Engagement ab, anstatt bei jedem Balken einen vollständigen Abschluss und eine erneute Eröffnung zu erzwingen. Dadurch bleibt das effektive Verhalten erhalten und gleichzeitig werden doppelte Transaktionen in der verwalteten Umgebung reduziert.
- Stop-Loss- und Take-Profit-Level bleiben als Parameter für die Dokumentation bestehen, werden jedoch nicht als separate Orders registriert. Positionsausstiege erfolgen, wenn der Klassifikator die Gegenseite oder die Halteaktion auswählt.
- Für die Dateiverarbeitung werden .NET-I/O-Hilfsprogramme verwendet. Das Kartenformat bleibt kompatibel (Werte mit doppelter Genauigkeit werden identisch angeordnet).

## Nutzungshinweise
- Stellen Sie sicher, dass die ausgewählte Sicherheit einen gültigen `PriceStep` bereitstellt, damit Pip-Differenzen korrekt berechnet werden. Wenn der Schritt fehlt oder Null ist, greift die Strategie auf einen Einheitsschritt zurück.
- Die Kohonen-Karten können groß werden (bis zu 10.000 Kauf-/Verkaufseinträge und 25.000 Halteeinträge). Behalten Sie den Standardpfad auf einem Speichergerät mit ausreichender Kapazität bei (~2,5 MB, wenn es voll ist).
- Da der Algorithmus kontinuierlich trainiert, hilft die Ausführung der Strategie anhand historischer Daten vor der Live-Bereitstellung dabei, die Karte mit repräsentativen Stichproben zu füllen.
