# CCI and Martin-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die CCI and Martin-Strategie sucht nach scharfen Umkehrungen nach einer kurzen bärischen oder bullischen Sequenz und bestätigt die Bewegung mit dem Commodity Channel Index. Die Logik repliziert den ursprünglichen MetaTrader 5-Expertenberater unter Verwendung der StockSharp-High-Level-API. Die Strategie arbeitet nur mit fertigen Kerzen und kann auf jedem Instrument betrieben werden, für das CCI-Werte und Preisschritte verfügbar sind.

## Handelsregeln
- **Bullisches Setup**
  - Kerze `-2` und Kerze `-1` müssen beide bärisch sein (Eröffnung größer als Schluss).
  - Kerze `0` muss über ihrer Eröffnung und über der Eröffnung von Kerze `-1` schließen.
  - CCI auf Kerze `-1` muss unter `+5` liegen, unter dem Wert von Kerze `-2`, und sowohl `-2` als auch `-3` müssen eine absteigende Sequenz zeigen. Der aktuelle CCI (Kerze `0`) muss sich nach oben über den vorherigen Wert drehen.
  - Wenn alle Bedingungen zutreffen und keine Position offen ist, tritt die Strategie in einen Long-Trade ein.
- **Bärisches Setup**
  - Kerze `-2` und Kerze `-1` müssen beide bullisch sein (Eröffnung kleiner als Schluss).
  - Kerze `0` muss unter ihrer Eröffnung und unter der Eröffnung von Kerze `-1` schließen.
  - CCI auf Kerze `-1` muss über `-5` liegen, über dem Wert von Kerze `-2`, und sowohl `-2` als auch `-3` müssen eine aufsteigende Sequenz bilden. Der aktuelle CCI (Kerze `0`) muss sich nach unten unter den vorherigen Wert drehen.
  - Wenn alle Bedingungen zutreffen und keine Position offen ist, tritt die Strategie in einen Short-Trade ein.

Der Algorithmus überwacht nur abgeschlossene Kerzen. Die ursprüngliche MQL-Implementierung wartete 40 Sekunden nach der Minuten-Eröffnung, um vorzeitige Signale zu vermeiden; die Verwendung fertiger Kerzen macht diesen Filter unnötig.

## Risikomanagement
- **Stop-Loss**- und **Take-Profit**-Abstände werden in Pips definiert. Sie werden in Preisoffsets konvertiert, indem der Preisschritt des Instruments mit zehn multipliziert wird, wenn der Schritt einer 3- oder 5-stelligen Notierung entspricht, was die ursprüngliche Pip-Berechnung widerspiegelt.
- Der **Trailing-Stop** wird aktiv, nachdem der Preis um die Trailing-Stop-Distanz plus dem Trailing-Schritt vorrückt. Der Stop wird dann bewegt, um die Trailing-Distanz aufrechtzuerhalten, und rückt nur vor, wenn die Preisverbesserung den konfigurierten Schritt überschreitet.
- Wenn Stop-Loss oder Take-Profit auf null gesetzt wird, ist der jeweilige Ausstieg deaktiviert. Trailing erfordert, dass sowohl die Stop-Distanz als auch der Schritt positiv sind.

## Volumenverwaltung
Zwei optionale Positionsgrößen-Engines können die Losgröße nach jedem Trade ändern.
- **Martingale-Skalierung** multipliziert das aktuelle Volumen mit dem Martingale-Koeffizienten, sobald die Anzahl aufeinanderfolgender Verluste den Auslöser erreicht. Die Skalierung stoppt nach der konfigurierten Anzahl von Martingale-Schritten. Jeder profitable Trade setzt das Volumen auf den Anfangswert zurück.
- **Schritt-Anpassungen** erhöhen das Volumen um einen festen Betrag entweder nach Verlusten oder nach Gewinnen, abhängig vom gewählten Modus. Der Zuwachs wird auf den Volumenschritt des Instruments normiert und durch den maximalen Volumenparameter begrenzt. Wenn das Limit überschritten wird oder ein Trade die Auslöserbedingung nicht erfüllt, fällt das Volumen auf die Anfangsgröße zurück.

Der ursprüngliche Expertenberater verbietet die gleichzeitige Aktivierung von Martingale- und Schritt-Logik; der C#-Port erzwingt dieselbe Einschränkung.

## Parameter
- `CandleType` – für die Analyse verwendete Kerzenserie.
- `CciPeriod` – Mittelungslänge für den Commodity Channel Index.
- `InitialVolume` – Basis-Ordergröße vor jeglicher Skalierung.
- `StopLossPips` – Stop-Loss-Abstand in Pips.
- `TakeProfitPips` – Take-Profit-Abstand in Pips.
- `TrailingStopPips` – Trailing-Stop-Abstand in Pips (0 deaktiviert Trailing).
- `TrailingStepPips` – Mindestpreisverbesserung erforderlich, bevor der Trailing-Stop sich bewegt.
- `EnableMartingale` – aktiviert Martingale-Skalierung nach Verlusten.
- `MartingaleCoefficient` – Multiplikator für das aktuelle Volumen bei Martingale-Trades.
- `MartingaleTriggerLosses` – Anzahl aufeinanderfolgender Verlust-Trades vor der Skalierung.
- `MartingaleMaxSteps` – maximale Anzahl von Martingale-Multiplikationen.
- `EnableStepAdjustments` – aktiviert schrittbasierte Volumenerhöhungen.
- `StepVolumeIncrement` – absoluter Zuwachs bei Auslösung der Schrittregel.
- `StepVolumeMax` – Obergrenze für das schrittbasierte Volumen.
- `StepAdjustmentMode` – wählt, ob der Schritterhöhung nach einem Verlust oder nach einem Gewinn ausgelöst wird.

## Hinweise
- Die Strategie geht davon aus, dass Marktorders nahe am angeforderten Preis ausgeführt werden. Protective Logik berechnet Stops bei jeder fertigen Kerze neu, um das tick-basierte Trailing im ursprünglichen EA zu emulieren.
- Wenn der Preisschritt des Instruments nicht der klassischen FX-Notierung entspricht, funktioniert die Pip-Konvertierung trotzdem, aber pip-basierte Abstände können unterschiedliche Geldwerte darstellen.
