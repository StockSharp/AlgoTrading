# Auto-KDJ-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Auto-KDJ-Strategie ist eine direkte Umsetzung des von *senlin ge* erstellten MetaTrader 4-Expertenberaters `AutoKdj.mq4`. Das System handelt mit einem einzelnen Symbol und wertet den geglätteten stochastischen Oszillator namens **KDJ** (auch %K, %D, %J genannt) aus. Die StockSharp-Implementierung stellt die gleiche Indikatorlogik und die gleichen Geldverwaltungsoptionen wieder her, die im ursprünglichen Expert Advisor verfügbar waren, und nutzt gleichzeitig die hochrangigen API-Funktionen wie Kerzenabonnements, Indikatorbindung und automatische Schutzaufträge.

KDJ basiert auf dem stochastischen Oszillator. Es berechnet zunächst einen Rohwert Stochastic (RSV), glättet ihn in die %K-Linie, glättet %K erneut in die %D-Linie und verwendet ihre Differenz (im Quellcode als *KDC* bezeichnet), um Impulsverschiebungen zu erkennen. Auto KDJ eröffnet jeweils höchstens eine Marktposition und wendet sofort die angeforderten Stop-Loss-/Take-Profit-Schutzmaßnahmen an.

## Indikatorkonstruktion
1. **RSV-Berechnung** – Für jede fertige Kerze werden das höchste Hoch und das niedrigste Tief über `KDJ Length` Kerzen erfasst. RSV wird wie folgt berechnet:
\[
RSV = \frac{\text{Close} - \text{LowestLow}}{\text{HighestHigh} - \text{LowestLow}} \times 100
\]
2. **%K-Glättung** – RSV-Werte werden über `Smooth %K` Perioden gemittelt, um die %K-Linie zu erhalten.
3. **%D-Glättung** – %K-Werte werden über `Smooth %D` Zeiträume gemittelt, um die %D-Linie zu erzeugen.
4. **KDJ-Signal** – Der Algorithmus analysiert `K - D` (den *KDC*-Puffer aus der MQL-Version) und die Steigung von %K, um Ein- und Ausgänge zu generieren.

Diese Pipeline wird mit dem `Stochastic`-Indikator von StockSharp implementiert, indem der Zeitraum und die Glättungsparameter so konfiguriert werden, dass sie die MetaTrader-Puffer widerspiegeln.

## Handelsregeln
Signale werden einmal pro fertiger Kerze ausgewertet. Die Strategie weigert sich, eine weitere Position zu eröffnen, während ein offener Handel oder eine ausstehende Exit-Order vorliegt, was dem Verhalten des Expertenberaters MQL entspricht.

### Teilnahmebedingungen
- **Kaufen**, wenn eine der folgenden Bedingungen zutrifft:
  - `K - D` wechselt von negativ zu positiv.
  - `K - D` ist bereits positiv und %K steigt (`K_current > K_previous`).
- **Verkaufen**, wenn eine der folgenden Bedingungen zutrifft:
  - `K - D` wechselt vom positiven zum negativen Wert.
  - `K - D` ist bereits negativ und %K fällt (`K_current < K_previous`).

### Ausstiegsbedingungen
- **Long schließen**, wenn `K - D` unter Null fällt oder wenn %K zu fallen beginnt.
- **Short schließen**, wenn `K - D` über Null geht oder wenn %K zu steigen beginnt.

Wenn die Position abgeflacht ist, zeichnet die Strategie auf, ob der Handel profitabel war oder nicht. Aufeinanderfolgende Verluste beeinflussen die nächste Positionsgröße genauso wie die `DecreaseFactor`-Logik von MQL EA.

## Money-Management
Der ursprüngliche Expert Advisor bietet einen `whichmethod`-Schalter zur Kombination von Stop-Loss- und Take-Profit-Verhalten sowie eine dynamische Lotgrößenroutine basierend auf der Margin-Nutzung und Verluststrähnen. Der StockSharp-Port reproduziert diese Funktionen als einzelne Parameter:

- **Stop-Loss/Take-Profit-Schalter** – Unabhängige boolesche Flags ermöglichen das Aktivieren oder Deaktivieren jedes Schutzzweigs. Wenn es aktiv ist, hängt `StartProtection` die Schutzausgänge an und übernimmt die Marktausführung.
- **Risikobasiertes Volumen** – Die Auftragsgröße beginnt bei `Base Volume` und kann erhöht werden, um den angeforderten `Maximum Risk`-Anteil des Portfolios abzudecken. Der Margin-Verbrauch wird durch die Vertragsgröße des Instruments und den konfigurierten Hebel angenähert, der die MT4-Berechnung `AccountFreeMargin * MaximumRisk * Leverage / 100000` emuliert.
- **Reduzierung der Verluststrähne** – Nach zwei oder mehr aufeinanderfolgenden Verlustgeschäften wird die nächste Order um `volume * losses / DecreaseFactor` reduziert, was der ursprünglichen Volumenabfallroutine entspricht.

Alle Volumina werden mit den Werten `VolumeStep`, `MinVolume` und `MaxVolume` des Wertpapiers normalisiert, um sicherzustellen, dass die übermittelte Ordergröße handelbar ist.

## Parameter
| Parameter | Beschreibung | Standard | Optimierung |
|-----------|-------------|---------|--------------|
| **Kerzentyp** | Datentyp/Zeitrahmen der Eingabekerzen. | 15-minütiger Zeitrahmen | – |
| **KDJ-Länge** | Lookback-Zeitraum für die RSV-Berechnung. | 30 | 10 → 60 Schritt 5 |
| **Glatt %K** | Auf die %K-Linie angewendete Glättung. | 3 | 1 → 10 Schritt 1 |
| **Glatt %D** | Auf die %D-Linie angewendete Glättung. | 6 | 1 → 15 Schritt 1 |
| **Stop-Loss (Pips)** | Abstand für den Schutzanschlag. | 100 | 0 → 300 Schritt 10 |
| **Gewinnmitnahme (Pips)** | Distanz für den schützenden Take-Profit. | 200 | 0 → 400 Schritt 10 |
| **Stop-Loss aktivieren** | Schalten Sie für das Stop-Loss-Bein um. | Aktiviert | – |
| **Take-Profit aktivieren** | Wechseln Sie zum Take-Profit-Teil. | Aktiviert | – |
| **Grundvolumen** | Minimales Volumen vor Risikoanpassung. | 0,1 | – |
| **Maximales Risiko** | Anteil des pro Trade zugewiesenen Eigenkapitals. | 0,4 | 0,0 → 1,0 Schritt 0,1 |
| **Verringerungsfaktor** | Volumenreduktion nach Verluststrähnen. | 0,3 | 0,0 → 5,0 Schritt 0,5 |
| **Hebelwirkung** | Im Margin-Modell verwendeter Kontohebel. | 100 | 10 → 500 Schritt 10 |

## Nutzungshinweise
1. Konfigurieren Sie die gewünschte Sicherheit und Verbindung in StockSharp Designer, Shell oder Runner.
2. Passen Sie den Kerzentyp an den in MetaTrader verwendeten Zeitrahmen an.
3. Legen Sie Stop-Loss-/Take-Profit-Einstellungen über die booleschen Schalter fest, um das `whichmethod`-Verhalten zu reproduzieren:
   - Deaktivieren Sie beide Beine für „kein SL, kein TP“.
   - Aktivieren Sie nur die Take-Profit- oder Stop-Loss-Komponente, um die Teilschutzmodi widerzuspiegeln.
4. Optimieren Sie optional `Base Volume`, `Maximum Risk`, `Decrease Factor` und `Leverage`, um Ihre Broker-Konfiguration widerzuspiegeln.
5. Starten Sie die Strategie. Der Chart-Helfer zeichnet automatisch Kerzen, den KDJ-Indikator und ausgeführte Trades zur Überprüfung auf.

## Unterschiede im Vergleich zur MQL-Version
- Der benutzerdefinierte `kdj.mq4`-Indikator wird durch den integrierten `Stochastic`-Indikator von StockSharp ersetzt, der so konfiguriert ist, dass er identische Puffer bereitstellt, sodass keine externen Dateien erforderlich sind.
- Bei der Positionsgröße werden Portfolio-Eigenkapital, Kontraktgröße und Leverage verwendet, die durch die Wertpapierdefinition StockSharp bereitgestellt werden. Broker mit unterschiedlichen Vertragsmultiplikatoren können `Base Volume` oder `Maximum Risk` entsprechend anpassen.
- Schutzexits basieren auf `StartProtection`, das bei Auslösung Marktaufträge übermittelt und den Ausführungspreis protokolliert. Dies bietet das gleiche funktionale Verhalten wie die Parameter `OrderSend` + Stop/Take in MetaTrader, bleibt aber idiomatisch für StockSharp.
- Die Risikominderung nach aufeinanderfolgenden Verlusten wird durch ausgeführte Trades verfolgt, anstatt die gesamte Trade-Historie bei jedem Tick zu scannen, wodurch die Leistung verbessert wird, während die Ergebnisse identisch bleiben.

## Testen
Die Strategie wurde validiert, indem die generierten Ein-/Ausstiegspunkte mit der ursprünglichen MQL-Logik anhand von EURUSD-Beispieldaten verglichen wurden. Händler sollten trotzdem Walk-Forward-Tests oder Optimierungen in ihrer Zielumgebung durchführen, um zu bestätigen, dass sich der Port wie erwartet mit den Vertragsspezifikationen und dem Ausführungsmodell ihres Brokers verhält.
