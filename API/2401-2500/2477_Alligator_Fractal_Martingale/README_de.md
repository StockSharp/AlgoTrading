# Alligator Fractal Martingale-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie portiert den MetaTrader-Expert "Alligator(barabashkakvn's edition)" auf die StockSharp High-Level-API. Sie kombiniert Bill Williams' Alligator-Indikator mit Fraktal-Ausbruchsbestätigung, einer Martingale-Mittelungskette und adaptiven Trailing Stops. Die Logik ist für hedging-artigen Ausführungsstil ausgelegt, bei dem die erste Order zum Marktpreis geöffnet wird und zusätzliche Einstiege in vordefinierten Abständen geplant werden, wenn sich der Preis gegen die Position bewegt.

## Handelslogik

- **Alligator-Mund-Öffnung** – die geglätteten gleitenden Durchschnitte der Lippen (grün), Zähne (rot) und Kiefer (blau) werden auf dem Medianpreis verarbeitet. Ein Long-Bias wird aktiviert, wenn die Lippen über dem Kiefer um mindestens `EntrySpread` steigen, während ein Short-Bias die entgegengesetzte Ausrichtung erfordert. Wenn sich der Spread unter `ExitSpread` zusammenzieht, wird der entsprechende Bias deaktiviert.
- **Fraktal-Filter (optional)** – fertige Kerzen werden nach Bill Williams-Fraktalen gescannt. Ein Long-Signal wird nur akzeptiert, wenn ein Aufwärts-Fraktal innerhalb der letzten `FractalLookback` Bars mindestens `FractalBuffer` über dem Schlusskurs liegt. Short-Signale erfordern ein Abwärts-Fraktal unter dem Markt. Deaktivieren Sie den Filter über `UseFractalFilter`, um nur auf Alligator-Signale einzusteigen.
- **Martingale-Mittelung** – nach der initialen Marktorder kann die Strategie `MartingaleSteps` Mittelungsebenen im Abstand von `MartingaleStepDistance` vorbauen. Jede Ebene multipliziert das vorherige Volumen mit `MartingaleMultiplier` (begrenzt durch `MaxVolume`) und wird ausgeführt, sobald der Preis die Ebene berührt.
- **Trailing-Exit-Management** – jede gefüllte Long- oder Short-Position erhält einen synthetischen Stop-Loss und Take-Profit basierend auf `StopLossDistance` und `TakeProfitDistance`. Wenn `EnableTrailing` aktiviert ist, werden Stops um mindestens `TrailingStep` vorwärtsgezogen, wenn sich der Markt zugunsten des Trades bewegt.
- **Alligator-Ausstiege (optional)** – wenn `UseAlligatorExit` wahr ist, wird die Position sofort geschlossen, sobald der Alligator-Mund sich schließt (Bias wechselt von aktiv zu inaktiv).

## Risiko- und Orderhandling

- Die Strategie verwendet den Parameter `Volume` für die erste Marktorder. Jede Martingale-Ebene verwendet das gerundete Volumen und multipliziert es mit dem konfigurierten Faktor, während das Ergebnis unter `MaxVolume` gehalten wird.
- Stops und Ziele werden intern bei jeder fertigen Kerze bewertet, anstatt auf native Börsenorders zu verlassen. Wenn der Kerzenbereich den synthetischen Stop oder das Ziel kreuzt, wird die Position sofort geschlossen.
- Entgegengesetzte Positionen werden geschlossen, bevor eine neue Richtung geöffnet wird, um abgesicherte Exposition innerhalb von StockSharp zu vermeiden.

## Parameter

| Parameter | Beschreibung |
| --- | --- |
| `Volume` | Basis-Ordergröße für den ersten Markteinstieg. |
| `JawLength`, `TeethLength`, `LipsLength` | Länge der geglätteten gleitenden Durchschnitte, die den Alligator-Kiefer, Zähne und Lippen bilden. |
| `JawShift`, `TeethShift`, `LipsShift` | Vorwärtsverschiebung (in Bars) beim Lesen der Alligator-Buffer. |
| `EntrySpread`, `ExitSpread` | Mindest-Spread zum Aktivieren von Trades und Kontraktionsschwelle zum Deaktivieren. |
| `UseAlligatorEntry`, `UseAlligatorExit` | Alligator-basierte Ein- und Ausstiege umschalten. |
| `UseFractalFilter` | Fraktal-Bestätigungsschicht aktivieren oder deaktivieren. |
| `FractalLookback`, `FractalBuffer` | Lookback-Fenster und Sicherheitsmarge für gültige Fraktale. |
| `EnableMartingale`, `MartingaleSteps`, `MartingaleMultiplier`, `MartingaleStepDistance`, `MaxVolume` | Steuern die Mittelungskette. |
| `StopLossDistance`, `TakeProfitDistance`, `EnableTrailing`, `TrailingStep` | Konfigurieren das synthetische Risikomanagement. |
| `AllowMultipleEntries` | Wiederholte Markteinträge erlauben, während eine Position offen ist. |
| `ManualMode` | Wenn wahr, verwaltet der Algorithmus nur offene Trades und erstellt keine neuen. |
| `CandleType` | Quell-Kerzenserie für Indikatorberechnungen. |

## Verwendungshinweise

1. Stellen Sie sicher, dass das ausgewählte Instrument die konfigurierten Preis- und Volumenschritte unterstützt; die Strategie rundet die Werte mit `Security.MinPriceStep` und `Security.VolumeStep`, wenn verfügbar.
2. Die Martingale-Kette wird intern simuliert. Wenn Sie lieber echte Limit-Orders an der Börse verwenden, deaktivieren Sie die Funktion und verwalten Sie die Skalierung extern.
3. Starten Sie die Strategie in einem hedging-kompatiblen Portfolio. Obwohl StockSharp die Nettoposition aggregiert, geht die ursprüngliche Logik davon aus, dass mehrere Positionen in der gleichen Richtung hinzugefügt werden können.
4. Überprüfen Sie die Standard-Pip-basierten Abstände (`0.008` ≈ 80 Pips für vierstellige FX-Notierungen) und passen Sie sie an das gehandelte Instrument an.
