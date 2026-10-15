# Momentum M15-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine direkte Portierung des MetaTrader 5-Expert Advisors **Momentum-M15** (Originaldatei `Momentum-M15.mq5`).
Sie handelt 15-Minuten-Kerzen und kombiniert einen verschobenen gleitenden Durchschnitt mit einem Momentum-Oszillator, der auf
Balkenöffnungen ausgewertet wird. Die Logik zielt darauf ab, extremes Momentum gegenzuhandeln, wenn der Preis auf der gegenüberliegenden Seite des verschobenen Durchschnitts liegt, während ein Gap-Wächter und optionaler Trailing Stop die Exposition begrenzen.

## Konvertierungshighlights

* Indikatoren werden mit StockSharp-Komponenten nachgebaut: ein konfigurierbarer gleitender Durchschnitt (Standard geglättet) und der eingebaute
  `Momentum`-Oszillator, der mit dem gewählten Kerzenpreis arbeitet (Standard `Open`).
* Die horizontale MA-Verschiebung von MetaTrader wird durch Puffern von Indikatorwerten emuliert und der Wert `MaShift`
  abgeschlossene Balken zurück abgerufen. Es wird keine benutzerdefinierte Indikatormathematik neu implementiert.
* Momentum-Monotoniéprüfungen verwenden die neuesten Historienwerte und behalten nur so viele Elemente wie von den Einstiegs-
  oder Ausstiegsfenstern benötigt, was die originalen `CheckMO_Up` / `CheckMO_Down`-Helfer widerspiegelt.
* Die Großlücken-Sperrung (`GapLevel`/`GapTimeout`) bleibt erhalten. Preisschrittinformationen werden verwendet, um die punktbasierten
  Schwellenwerte der MQL-Version in StockSharp-Preisschritte umzurechnen.
* Das Trailing-Stop-Management wird intern durch Marktausstiege gehandhabt, wenn der Preis das verfolgte Niveau kreuzt, was der
  MQL-Routine entspricht, die Stop-Loss-Orders einmal pro abgeschlossenem Balken modifizierte.

## Parameter

| Name | Beschreibung | Standard |
| --- | --- | --- |
| `TradeVolume` | Ordergröße für jeden Einstieg. | `0.1` |
| `CandleType` | Primärer Zeitrahmen (standardmäßig 15-Minuten-Kerzen). | `15m` |
| `MaPeriod` | Rückblicklänge des gleitenden Durchschnitts. | `26` |
| `MaShift` | Anzahl der Balken zum horizontalen Verschieben des gleitenden Durchschnitts. | `8` |
| `MaMethod` | Typ des gleitenden Durchschnitts (`Simple`, `Exponential`, `Smoothed`, `Weighted`). | `Smoothed` |
| `MaPrice` | Kerzenpreis für den gleitenden Durchschnitt. | `Low` |
| `MomentumPeriod` | Rückblicklänge des Momentums. | `23` |
| `MomentumPrice` | Kerzenpreis für den Momentum-Oszillator. | `Open` |
| `MomentumThreshold` | Basis-Momentumniveau, das Long/Short-Setups trennt. | `100` |
| `MomentumShift` | Wert, der zu `MomentumThreshold` addiert/subtrahiert wird, um asymmetrische Grenzen zu erstellen. | `-0.2` |
| `MomentumOpenLength` | Balken, die für eine nicht steigende Momentumsequenz vor dem Öffnen von Longs / nicht fallend für Shorts erforderlich sind. | `6` |
| `MomentumCloseLength` | Balken, die für dieselbe monotone Sequenz vor dem Schließen von Positionen erforderlich sind. | `10` |
| `GapLevel` | Minimale positive Lücke (in Preisschritten), die neue Einstiege pausiert. | `30` |
| `GapTimeout` | Anzahl der Balken, für die der Handel nach einer großen Lücke deaktiviert bleibt. | `100` |
| `TrailingStop` | Optionaler Trailing-Stop-Abstand in Preisschritten. | `0` (deaktiviert) |

## Handelsregeln

### Einstiegskriterien

* **Long-Einstiege**
  * Das neueste Momentum liegt unter `MomentumThreshold + MomentumShift` (für die Standard-Verschiebung von `-0.2` ist dies leicht
    unter dem Hauptschwellenwert).
  * Sowohl der vorherige Balkenschluss als auch die aktuelle Balkenöffnung liegen **unter** dem verschobenen gleitenden Durchschnitt.
  * Das Momentum war `MomentumOpenLength` Balken lang nicht steigend (entspricht `CheckMO_Down` im MQL-Quellcode).

* **Short-Einstiege**
  * Das neueste Momentum liegt über `MomentumThreshold - MomentumShift` (mit der Standard-Verschiebung ist dies leicht über 100).
  * Sowohl der vorherige Balkenschluss als auch die aktuelle Balkenöffnung liegen **über** dem verschobenen gleitenden Durchschnitt.
  * Das Momentum war `MomentumOpenLength` Balken lang nicht fallend (entspricht `CheckMO_Up`).

Einstiege werden nur bewertet, wenn keine Position offen ist und der Handel nicht durch den Lückenfilter ausgesetzt ist.

### Ausstiegskriterien

* **Long-Positionen** werden geschlossen, wenn eines der Folgenden zutrifft:
  * Das Momentum war `MomentumCloseLength` Balken lang nicht steigend.
  * Der vorherige Balkenschluss fällt unter den verschobenen gleitenden Durchschnitt.
  * Der Trailing Stop (falls aktiviert) wird berührt. Der Stop folgt dem Kerzentief minus der konfigurierten Distanz in
    Preisschritten.

* **Short-Positionen** werden geschlossen, wenn eines der Folgenden zutrifft:
  * Das Momentum war `MomentumCloseLength` Balken lang nicht fallend.
  * Der vorherige Balkenschluss steigt über den verschobenen gleitenden Durchschnitt.
  * Der Trailing Stop (falls aktiviert) wird berührt. Der Stop folgt dem Kerzenhoch plus der konfigurierten Distanz.

### Gap-Suspensionslogik

Der ursprüngliche Expert Advisor pausierte den Handel nach starken Aufwärtslücken. Die StockSharp-Version misst die Differenz
zwischen der aktuellen Balkenöffnung und dem vorherigen Schluss in Preisschritten:

1. Wenn die Lücke `GapLevel` überschreitet, wird der Sperrtimer auf `GapTimeout` zurückgesetzt.
2. Der Timer wird nach jedem geschlossenen Balken dekrementiert; der Handel wird erst nach Erreichen von null fortgesetzt.

## Hinweise und Annahmen

* Alle Berechnungen verwenden abgeschlossene Kerzen (`CandleStates.Finished`), um mit den StockSharp-High-Level-API-Praktiken
  konform zu bleiben. Daher werden Orders beim nächsten Balken nach der Signalbeobachtung ausgegeben, was damit übereinstimmt, wie
  die ursprüngliche Strategie beim ersten Tick eines neuen Balkens ausgelöst wurde.
* Das MetaTrader-Konzept von "Pips" wird über `Security.PriceStep` approximiert. Fehlt dem Instrument ordentliche Schrittdaten,
  werden der Lückenfilter und Trailing Stop lautlos deaktiviert.
* Gleitende Durchschnittspreise und Momentum-Eingaben können unabhängig voneinander geändert werden, was die Flexibilität der originalen
  Eingangsparameter repliziert.
* Es werden keine automatisierten Stop-Orders registriert; stattdessen reproduzieren Marktausstiege die Stop-Anpassungen, die der MQL-Code über
  `PositionModify` ausgab.

## Nutzungstipps

1. Weisen Sie das gewünschte Wertpapier zu und stellen Sie sicher, dass `CandleType` dem historischen Zeitrahmen entspricht, der während der Backtests verwendet wurde (15-
   Minuten-Balken im Originalskript).
2. Konfigurieren Sie `TradeVolume` auf die Lotgröße, die vom Handelsplatz unterstützt wird.
3. Passen Sie `MomentumOpenLength` / `MomentumCloseLength` an, um zu kontrollieren, wie streng der Momentum-Monotonie-Filter sein soll.
4. Wenn Sie die Standard-"Pip"-Skala genau spiegeln möchten, setzen Sie `TrailingStop` und `GapLevel` entsprechend dem Verhältnis
   zwischen dem Preisschritt der Börse und einem Pip für das Instrument.
