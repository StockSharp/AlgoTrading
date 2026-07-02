# Strategie von Aussie Surfer Ltd
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Aussie Surfer Ltd Strategy** ist eine StockSharp High-Level-API-Portierung des MetaTrader 5-Expertenberaters „Aussie Surfer Ltd“ (MQL-Ordner `43278`). Die Strategie kombiniert schnelle Bollinger-Bandumkehrungen mit einem Alligator-Trendfilter, um die im ursprünglichen EA verwendete diskretionäre Einrichtung zu automatisieren. Trades werden mit dem für die Strategie konfigurierten Primärinstrument getätigt und standardmäßig anhand einer 15-minütigen Kerzenserie ausgewertet.

## Indikatoren und Daten
- **Bollinger Bänder (Schlusskurs, Standardlänge 5, Breite 2,5)** – erkennen, wann sich der Markt vorübergehend außerhalb der Bänder ausdehnt und wieder hineinschnappt.
- **Geglätteter gleitender Durchschnitt (Länge 21)** – reproduziert die „Zähne“-Linie Alligator, um die Trendverlangsamung zu beurteilen.
- **Medianpreis jeder Kerze ((Hoch + Tief) / 2)** – speist die Alligator-Berechnung, sodass die Steigung mit der ursprünglichen Implementierung übereinstimmt.

Die Strategie abonniert einen einzelnen Kerzenstrom. Die Indikatorwerte werden nur von fertigen Kerzen bestimmt, wodurch sichergestellt wird, dass Signale auf der Grundlage bestätigter Daten generiert werden.

## Handelslogik
1. **Eintrag einrichten**
   - Wenn die vorherige Kerze über dem unteren Bollinger-Band öffnete und die aktuelle Kerze unter dem vor zwei Balken beobachteten Bandwert öffnete, wird eine **Long-Position** eröffnet (nachdem jegliches Short-Engagement abgeflacht wurde). Dies stellt die EA-Logik wieder her, bei der der Preis das untere Band durchbricht und sofort wieder hinein springt.
   - Wenn die vorherige Kerze unterhalb des oberen Bollinger-Bandes öffnete und die aktuelle Kerze über dem vor zwei Balken beobachteten Bandwert öffnete, wird eine **Short**-Position eröffnet (nachdem jegliches Long-Engagement abgeflacht wurde).
2. **Alligator-basierter Exit**
   - Die Zahnlinie Alligator wird ein und zwei Balken zurück überwacht. Eine Long-Position wird immer dann aufgelöst, wenn die Steigung nach unten geht (der Wert vor zwei Balken ist größer als der Wert vor einem Balken). Eine Short-Position wird geschlossen, wenn der Hang nach oben dreht.
3. **Risikoschichten**
   - Bei der Eingabe werden ein fester Pip-Stop-Loss und ein Take-Profit angewendet. Beide sind optional und können deaktiviert werden, indem ihr Pip-Abstand auf Null gesetzt wird.
   - Ein optionaler Trailing-Stop richtet den Stop-Loss neu auf das Hoch (für Long-Positionen) oder das Tief (für Short-Positionen) der zuvor abgeschlossenen Kerze abzüglich/plus der konfigurierten Pip-Distanz aus. Die Trailing-Logik ist nur aktiv, wenn der Stop-Loss aktiviert ist und `EnableTrailingStop` auf `true` gesetzt ist.

## Risikomanagement
- **Stop-Loss** – wandelt den konfigurierten Pip-Abstand mithilfe der Wertpapierpreisstufe in Preiseinheiten um.
- **Take-Profit** – wird einmal bei der Eingabe berechnet und statisch gehalten, bis entweder erreicht oder die Position durch eine andere Regel geschlossen wird.
- **Trailing Stop** – erhöht den Stop-Loss, wenn bei der vorherigen Kerze ein günstigeres Hoch (für Long-Positionen) oder Tief (für Short-Positionen) erscheint.
- **Umkehrabwicklung** – Wenn ein Signal eintrifft, während eine entgegengesetzte Position offen ist, sendet die Strategie eine Marktorder in der Größe, um das neue Engagement in einer einzigen Transaktion vollständig umzukehren und aufzubauen.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `OrderVolume` | Basishandelsgröße in Lots oder Kontrakten. | `0.30` |
| `StopLossPips` | Schutzstoppabstand in Pips. `0` deaktiviert den Stopp. | `46` |
| `TakeProfitPips` | Gewinnzielentfernung in Pips. `0` deaktiviert das Ziel. | `0` |
| `EnableTrailingStop` | Aktiviert Pip-basiertes Trailing, wenn ein Stop-Loss aktiv ist. | `true` |
| `BollingerPeriod` | Länge des Bollinger Bands-Fensters. | `5` |
| `BollingerDeviation` | Standardabweichungsmultiplikator für die Bänder. | `2.5` |
| `TeethPeriod` | Geglättete gleitende Durchschnittslänge für die Zahnlinie Alligator. | `21` |
| `CandleType` | Für Berechnungen verwendete Kerzenserie (standardmäßig 15-Minuten-Zeitrahmen). | `15m` Kerzen |

Alle numerischen Parameter umfassen Optimierungsmetadaten, sodass sie über den Strategy Analyzer optimiert werden können.

## Implementierungshinweise
- Es werden nur fertige Kerzen verarbeitet; Unvollendete Daten werden ignoriert, um die zeitgesteuerte MetaTrader-Ausführung nachzuahmen, die zu Beginn jedes neuen Balkens ausgeführt wurde.
- Die Trailing-Logik erfordert eine positive Stop-Loss-Distanz. Bei der Initialisierung wird eine Ausnahme ausgelöst, wenn die Trailing-Option ohne Stopp aktiviert ist.
- Indikatorinstanzen werden automatisch gezeichnet, wenn ein Diagrammbereich verfügbar ist, um zu überprüfen, ob der Port StockSharp mit der Vorlage MetaTrader übereinstimmt.

## Nutzung
1. Laden Sie die Strategie in ein StockSharp-Terminal oder eine Backtesting-Umgebung.
2. Konfigurieren Sie die Handelssicherheit und passen Sie die Parameter (insbesondere Pip-Abstände) an die Vertragsspezifikationen des Brokers an.
3. Starten Sie die Strategie. Es abonniert die konfigurierte Kerzenserie, wertet Einträge zu jeder fertigen Kerze aus und verwaltet die Position anhand der beschriebenen Regeln.

Stellen Sie beim Live-Handel sicher, dass der Broker Marktaufträge unterstützt und dass das Symbol `PriceStep` verfügbar ist, damit die Pip-Umrechnungen korrekt sind.
