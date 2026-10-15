# AIS4-Handelsmaschinenstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **AIS4 Trade Machine Strategy** ist ein manueller Handelsassistent, der den ursprünglichen Expertenberater „AIS4 Trade Machine“ von MetaTrader auf StockSharp portiert. Es behält den Ein-Positions-Workflow des Skripts bei: Der Operator gibt absolute Stop-Loss- und Take-Profit-Werte an, erteilt einen Befehl und die Strategie berechnet die Handelsgröße auf der Grundlage des aktuellen Kontokapitals und der Instrumentenspezifikationen. Nachdem die Marktorder ausgeführt wurde, übermittelt die Strategie sofort gepaarte Schutzorder (Stopp + Limit), sodass die angeforderten Risiko- und Ertragsniveaus auf der Börsenseite durchgesetzt werden.

Die Strategie generiert **keine** automatische Signale. Es ist für eine diskretionäre Ausführung konzipiert, bei der der Benutzer entscheidet, wann und wo er eine Position eingibt oder ändert.

## Manueller Arbeitsablauf
1. Stellen Sie sicher, dass das angeschlossene Instrument `PriceStep`, `StepPrice`, `VolumeStep`, `MinVolume` und `MaxVolume` bereitstellt. Sie müssen das Preisrisiko in Kontraktgröße umrechnen und das Ordervolumen an Börsenlimits anpassen.
2. Stellen Sie vor dem Senden eines Befehls `StopPrice` und `TakePrice` auf die absoluten Preisniveaus ein, die Sie verwenden möchten.
3. Ändern Sie `Command` in `Buy` oder `Sell`. Die Strategie:
   - Überprüft, ob keine andere Position offen ist.
   - Überprüft, ob der angeforderte Stop-Loss und Take-Profit den Mindest-Tick-Abstand einhalten.
   - Berechnet das Risikobudget aus `OrderReserve` × aktuellem Portfolio-Eigenkapital und stellt sicher, dass die Eigenkapitalreserve (`AccountReserve`) eingehalten wird.
   - Schätzt das Ordervolumen aus der Stop-Distanz und dem Tick-Wert des Instruments.
   - Sendet die Marktorder und übermittelt dann gepaarte Schutzaufträge (`SellStop`+`SellLimit` für Long-Positionen, `BuyStop`+`BuyLimit` für Short-Positionen).
4. `Command` wird automatisch auf `Wait` zurückgesetzt, nachdem die Aktion verarbeitet wurde, sodass versehentliche doppelte Ausführungen vermieden werden.

### Verwaltung einer bestehenden Position
- Legen Sie neue Preisniveaus fest (verwenden Sie `0`, um den aktuellen Wert beizubehalten) und ändern Sie `Command` auf `Modify`. Die Strategie storniert die vorherigen Schutzanordnungen und ersetzt sie durch neue, die den aktualisierten Preisen entsprechen.
- Wechseln Sie von `Command` zu `Close`, um die aktive Position zum Marktwert zu liquidieren und alle Schutzaufträge zu stornieren.

## Logik des Risikomanagements
- **AccountReserve** – hält einen Bruchteil des Spitzenkapitals unberührt. Der Handel wird blockiert, solange das verfügbare Eigenkapital (`equity - peak_equity × (1 - AccountReserve)`) kleiner als das angeforderte Risikobudget ist.
- **OrderReserve** – Bruchteil des aktuellen Eigenkapitals, der dem nächsten Trade zugewiesen wird. Das Budget wird mithilfe der Stoppentfernung und des Instrumenten-Tick-Werts (`PriceStep` × `StepPrice`) in eine Kontraktgröße umgewandelt.
- Wenn das berechnete Volumen unter `MinVolume` fällt oder gegen `VolumeStep` verstößt, wird der Befehl abgelehnt und eine Warnung in das Protokoll geschrieben.

## Parameter
| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `Command` | `Wait` | Manueller Befehl zur Ausführung (`Buy`, `Sell`, `Modify`, `Close`). Kehrt nach der Bearbeitung automatisch zu `Wait` zurück. |
| `StopPrice` | `0` | Absolutes Stop-Loss-Niveau. Muss für Long-Positionen unter dem Einstiegspreis und für Short-Positionen über dem Einstiegspreis liegen. |
| `TakePrice` | `0` | Absolutes Take-Profit-Niveau. Muss bei Long-Positionen über dem Einstiegspreis und bei Shorts darunter liegen. |
| `AccountReserve` | `0.20` | Bruchteil des Eigenkapitals, der als Reserve gehalten wird. Höhere Werte erfordern ein größeres Polster, bevor neue Geschäfte akzeptiert werden. |
| `OrderReserve` | `0.04` | Anteil des pro Trade riskierten Eigenkapitals. Wird zur Berechnung der Kontraktgröße aus der Stoppdistanz verwendet. |
| `CandleType` | `1 minute` Zeitrahmen | Kerzenserien werden verwendet, um die neuesten Preise für Validierung und Protokollierung zu beobachten. |

## Hinweise und Einschränkungen
- Es wird jeweils nur eine Position unterstützt, entsprechend dem ursprünglichen Expert Advisor-Design.
- Befehle, die den Mindestpreisabstand, die Kapitalreserve oder Volumenbeschränkungen verletzen, werden ignoriert und eine Warnung wird im Strategieprotokoll aufgezeichnet.
- Schutzaufträge werden bei jeder Änderung oder Neubesetzung ersetzt, um die Volumina mit der tatsächlichen Positionsgröße synchron zu halten.
- Die Strategie basiert auf genauen Marktdaten für `PriceStep`/`StepPrice`. Instrumente, die diese Felder nicht bereitstellen, können über diesen Port nicht sicher gehandelt werden.
