# TCP-Pivot-Reversal-Limit-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die TCP-Pivot-Limit-Strategie ist eine Konvertierung des klassischen MetaTrader 4-Experten **gpfTCPivotLimit.mq4**. Der ursprüngliche Experte berechnet tägliche Pivot-Levels und sucht mithilfe stündlicher Kerzen nach falschen Ausbrüchen um diese Levels. Sobald ein Ausbruch fehlschlägt, geht die Strategie sofort einen Umkehrhandel ein, der auf die gegenüberliegenden Pivot-Levels abzielt. Diese Implementierung reproduziert dieselbe Logik unter Verwendung der übergeordneten Strategie StockSharp API.

Die Strategie basiert auf stündlichen Kerzen und hält zu jedem Zeitpunkt nur eine einzige offene Position. An jedem neuen Handelstag wird das Pivot-Raster anhand der Höchst-, Tiefst- und Schlusswerte des Vortages neu berechnet. Diese Ebenen leiten die Einstiegsauslöser, Stop-Loss, Take-Profit und optionales Trailing-Management.

## Handelslogik

1. **Pivot-Berechnung**
   - Bei der ersten Kerze jedes neuen Handelstages aggregiert die Strategie die Höchst-, Tiefst- und Schlusskurse des Vortages, um die klassischen Pivot-Level für Parketthändler zu berechnen (Pivot, R1–R3, S1–S3).
   - Jedes Mal, wenn neue Ebenen generiert werden, wird ein Protokolleintrag erstellt, damit Sie verfolgen können, wie sich das Raster entwickelt.

2. **Eintrittsbedingungen**
   - Bei jeder abgeschlossenen stündlichen Kerze prüft die Strategie die letzten beiden abgeschlossenen Kerzen.
   - Eine *Short*-Position wird eröffnet, wenn die Kerze vor zwei Perioden über ein Widerstandsniveau gestiegen ist (oder bei/über diesem Niveau geschlossen hat), während sie darunter geöffnet hat, und die letzte Kerze wieder unter diesem Niveau geschlossen hat. Dies weist auf einen gescheiterten Ausbruch hin und lässt auf eine Umkehr nach unten schließen.
   - Eine *Long*-Position wird symmetrisch eröffnet, wenn der Markt unter ein Unterstützungsniveau fällt, die folgende Kerze jedoch wieder darüber schließt.
   - Es kann jeweils nur eine Position aktiv sein. Das Bestellvolumen wird durch den Parameter `OrderVolume` definiert.

3. **Exit-Management**
   - Jeder Eintrag verwendet die Stop-Loss- und Take-Profit-Level, die durch die ausgewählte `TargetMode`-Voreinstellung definiert sind. Die Voreinstellungen spiegeln die `TgtProfit`-Optionen des ursprünglichen Expert Advisors wider und kombinieren verschiedene Pivot-Ebenen:
     | Modus | Kurzer Eintrag | Kurzer Stopp | Kurzes Ziel | Langer Eintrag | Langer Stopp | Langes Ziel |
     |------|-------------|------------|--------------|------------|-----------|-------------|
     | 1    | R1          | R2         | S1           | S1         | S2        | R1          |
     | 2    | R1          | R2         | S2           | S1         | S2        | R2          |
     | 3    | R2          | R3         | S1           | S2         | S3        | R1          |
     | 4    | R2          | R3         | S2           | S2         | S3        | R2          |
     | 5    | R2          | R3         | S3           | S2         | S3        | R3          |
   - Wenn `IntradayTrading` aktiviert ist, wird jede offene Position beim Kerzenschluss um 23:00 Uhr geschlossen, um ein Halten über Nacht zu vermeiden.
   - Ein optionaler Trailing Stop in Punkten (Vielfache des Instrumentpreisschritts) emuliert das MetaTrader-Verhalten. Das Trailing wird erst aktiviert, nachdem die Bewegung um die konfigurierte Distanz fortgeschritten ist, und schließt den Handel, wenn der Preis um denselben Betrag zurückgeht.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `OrderVolume` | Volumen, das sowohl für Kauf- als auch Verkaufsmarktaufträge verwendet wird. |
| `TargetMode` | Ganzzahl von 1 bis 5, die auswählt, welche Widerstands-/Unterstützungskombination für Einstiege, Stopps und Ziele verwendet wird. |
| `TrailingPoints` | Trailing-Stop-Distanz, gemessen in Preispunkten. Auf Null setzen, um das Nachziehen zu deaktivieren. |
| `IntradayTrading` | Bei `true` werden die Positionen um 23:00 Uhr geschlossen, um den Intraday-Handel fortzusetzen. |
| `CandleType` | Kerzendatentyp. Der Standardwert ist ein einstündiger Zeitrahmen, der dem ursprünglichen Experten entspricht. |

## Notizen

- Die Strategie erwartet einen kontinuierlichen Strom stündlicher Kerzen. Die Anwendung auf andere Zeitrahmen ändert das Verhalten und sollte erneut getestet werden.
- Stop-Loss- und Take-Profit-Niveaus werden anhand von Candle-Extremen bewertet, sodass Lücken zwischen den Niveaus zu Ausstiegen zu schlechteren Preisen führen können, genau wie in der MetaTrader-Version.
- Das Trailing-Management wird bei Kerzenschlüssen und -tiefs/-hochs durchgeführt und entspricht weitgehend der ursprünglichen Tick-basierten Logik, bleibt aber in der StockSharp-Umgebung effizient.
