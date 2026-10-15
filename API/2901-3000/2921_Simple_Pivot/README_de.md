# Simple-Pivot-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie reproduziert den MetaTrader 5-Expertenberater "SimplePivot". Sie bewertet kontinuierlich die Beziehung zwischen dem Eröffnungskurs der aktuellen Bar und dem Pivot-Level der vorherigen Bar und hält dabei immer eine einzige direktionale Position. Wenn sich die Richtung ändert, schließt die Strategie die bestehende Position und eröffnet sofort eine in der entgegengesetzten Richtung.

## Übersicht

- **Marktregime**: Always-in-the-Market Swing-Trading.
- **Instrumente**: Jedes Instrument, das Kerzendaten für den ausgewählten Zeitrahmen bereitstellt.
- **Zeitrahmen**: Konfigurierbar über den Parameter *Candle Type* (standardmäßig 1-Stunden-Kerzen).
- **Orders**: Marktorders dimensioniert durch den Parameter *Volume*.

## Funktionsweise

### Pivot-Berechnung

1. Warten auf mindestens eine abgeschlossene Kerze zur Initialisierung der Berechnung.
2. Berechnung des Pivots der vorherigen Kerze als arithmetisches Mittel ihrer Hoch- und Tiefpreise.
3. Beibehaltung des vorherigen Hochs und Tiefs, damit der Pivot für die nächste Bar sofort produziert werden kann, wenn eine neue Kerze abgeschlossen ist.

### Direktionale Entscheidung

1. Die Standardausrichtung ist Long (Kauf).
2. Wenn die aktuelle Kerze unterhalb des vorherigen Hochs eröffnet, aber über dem Pivot bleibt, wechselt die Ausrichtung zu Short (Verkauf).
3. Wenn die gewünschte Richtung gegenüber dem letzten ausgeführten Trade unverändert ist, wird die bestehende Position beibehalten und keine neuen Orders werden gesendet.

### Positionsmanagement

1. Wenn die gewünschte Richtung vom aktuellen Trade abweicht, wird die laufende Position durch eine entgegengesetzte Marktorder flachgestellt.
2. Nach dem Flachstellen etabliert eine Marktorder in Höhe von *Volume* das neue direktionale Engagement.
3. Der Prozess wiederholt sich bei jeder abgeschlossenen Kerze und stellt sicher, dass die Strategie immer entweder Long oder Short ist.

## Parameter

- **Volume**: Handelsgröße für jeden Einstieg. Bestimmt auch die Größe der Schließorder, wenn die Strategie die Richtung wechselt.
- **Candle Type**: Datentyp der Kerzen, die für Pivot- und Einstiegsberechnungen verwendet werden. Der Standard ist ein 1-Stunden-Zeitrahmen, aber jeder verfügbare Zeitrahmen kann ausgewählt werden.

## Zusätzliche Hinweise

- Die Logik reagiert auf vollständig abgeschlossene Kerzen (`CandleStates.Finished`), um wiederholte Signale zu vermeiden, während eine Kerze noch gebildet wird.
- Es sind keine Stops oder Gewinnziele definiert; Ausstiege erfolgen nur, wenn die Pivot-Regel einen Richtungswechsel anfordert.
- Da die Strategie immer im Markt ist, sollten Risikokontrollen wie Überwachung des maximalen Drawdowns oder Session-Filter extern gehandhabt werden, sofern erforderlich.
