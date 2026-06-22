# Strategie Statistik Wiederholendes Verhalten
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Intraday-Strategie, die untersucht, wie sich Kerzen zur gleichen Tageszeit während der letzten N Handelssitzungen verhalten haben. Für jede neue Kerze werden die akkumulierten bullischen und bearischen Kerzenkörpergrößen aus den Vortagen verglichen. Wenn bullischer Druck dominiert, wird eine Long-Position zum Kerzenöffnung eröffnet, andernfalls geht die Strategie Short. Positionen werden an der nächsten Kerze geschlossen, und ein fester Pip-Stop-Loss imitiert die ursprüngliche MetaTrader-Logik. Die Positionsgröße folgt einem Goldenen-Schnitt-Martingale, das nach Verlusten wächst und nach Gewinnen zurückgesetzt wird.

## Handelslogik

1. Zu Beginn jeder neuen Kerze wird die offene Position der vorherigen Kerze geschlossen.
2. Kerzen der letzten `HistoryDays` Handelstage, die zur gleichen Stunde und Minute geöffnet haben, werden gesucht.
3. Die Kerzenkörper (in Punkten) werden getrennt für bullische und bearische Schlusskurse summiert; Körper kleiner als `MinimumBodyPoints` werden ignoriert.
4. Wenn die bullische Summe die bearische übertrifft → Long-Position mit dem aktuellen Volumen eröffnen.
5. Wenn die bearische Summe die bullische übertrifft → Short-Position eröffnen.
6. Einen Stop-Loss von `StopLossPips` wird über den Mindestpreisschritt des Instruments umgerechnet und angewendet. Der Stop wird gegen Intrabar-Extremwerte geprüft, wenn die Kerze abgeschlossen ist.
7. Wenn der Trade geschlossen wird:
   - Wenn das Ergebnis profitabel ist, wird das Volumen auf `InitialVolume` zurückgesetzt.
   - Andernfalls wird das aktuelle Volumen mit `MartingaleFactor` multipliziert (unter Beachtung des Volumenschritts und der Limits).

## Parameter

- **HistoryDays** *(Standard: 10)* — Anzahl der Vortage für die Statistik.
- **MinimumBodyPoints** *(Standard: 10)* — Kerzen mit einem Körper kleiner als dieser Schwellenwert (in Punkten) werden ignoriert.
- **StopLossPips** *(Standard: 15)* — Pip-Abstand des Schutzstops.
- **InitialVolume** *(Standard: 0.1)* — Anfangsordergröße vor Martingale-Anpassungen.
- **MartingaleFactor** *(Standard: 1.618)* — Multiplikator nach einem Verlust-Trade.
- **CandleType** *(Standard: 1 Stunde)* — Zeitrahmen für die Kerzen.

## Handelsmerkmale

- **Marktseite**: Beide, Long und Short, je nach Statistik.
- **Zeitrahmen**: Konfigurierbar (standardmäßig stündlich) mit exakter Übereinstimmung nach Stunde und Minute.
- **Positionsmanagement**: Jeweils eine Position, die an der nächsten Kerze oder bei Stop-Loss-Auslösung geschlossen wird.
- **Risiko**: Verwendet festen Pip-Stop und Martingale-Sizing, das das Volumen nach aufeinanderfolgenden Verlusten schnell erhöhen kann.
- **Instrumente**: Funktioniert mit Instrumenten, die einen gültigen `MinPriceStep` und Volumengrenzen bereitstellen.

## Implementierungshinweise

- Kerzenkörper werden pro Tagesminute in einer rollierenden Queue gespeichert, die durch `HistoryDays` begrenzt wird.
- Volumina werden auf den Volumenschritt des Instruments normalisiert und durch `MinVolume`/`MaxVolume` begrenzt.
- Die Stop-Loss-Erkennung stützt sich auf abgeschlossene Kerzenextrema, um die Intrabar-Ausführung des ursprünglichen MQL5-Experten zu emulieren.
- Alle Code-Kommentare sind auf Englisch, entsprechend den Repository-Anforderungen.
