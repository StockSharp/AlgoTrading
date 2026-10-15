# Gewichteter Signalkorb mit ablaufenden Limits
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Das Diagramm verbindet eine RSI-Zonenstimme und eine EMA-Lagestimme zu einem Wert von −3 bis +3. Ein Schwellenübergang bei flacher Position registriert ein Limit am fertigen Schluss; Combination<Order> gibt genau diese Order an zwölf Kerzen N values und Order cancellation, Ausführungen starten Position protection mit 1,2%/0,8%.

![schema](schema.svg)

## Strategieübersicht

- RSI unter 30 liefert +2, über 70 liefert −2, die Mittelzone null.
- Close über EMA(20) liefert +1, darunter −1; Formula summiert beide gewichteten Stimmen.
- Aktuelle und Previous-value-Vergleiche erkennen einen neuen Übergang über +1 oder unter −1, jeweils nur bei Position == 0.
- Kauf und Verkauf teilen fertigen Close und Volumen eins; ihre Order-Ausgänge laufen in Combination, MyTrade in Position protection.
- N values zählt nach Registrierung zwölf fertige Kerzen und Order cancellation zieht die aktuelle unausgeführte Order.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Der Wert steigt von unter +1 auf mindestens +1 bei flacher Position. Order registering setzt ein Kauf-Limit am fertigen Close.
- **Short-Einstieg**: Der Wert fällt von über −1 auf höchstens −1 bei flacher Position. Order registering setzt ein Verkauf-Limit am fertigen Close.
- **Ausstieg**: Ein ausgeführter Einstieg wird bei +1,2% und −0,8% geschützt. Ein offenes Limit geht als Orderobjekt nach zwölf von N values gezählten Kerzen an Order cancellation.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Candle Time Frame | 00:05:00 | Fertiges Intervall; fünf Minuten passen den C#-Standard 60 Minuten an den Monats-Replay an. |
| RSI Length | 14 | RSI-Periode; C# nutzt standardmäßig 21, das Diagramm 14. |
| EMA Length | 20 | EMA-Periode; C# nutzt standardmäßig 50, das Diagramm 20. |
| RSI Weight | 2 | Gewicht der überverkauften bzw. überkauften RSI-Stimme. |
| Trend Weight | 1 | Gewicht der Close-Lage gegenüber EMA. |
| Replay Signal Threshold | 1 | Replay-Grenze; Entwurfswert 2 bleibt dokumentierte Alternative. |
| Cancel After N Candles | 12 | Fertige Kerzen vor dem Stornoversuch einer offenen Order. |
| Take Profit, % | 1.2 | Prozentgewinn für Position protection. |
| Stop Loss, % | 0.8 | Prozentverlust für Position protection. |
| Order Volume | 1 | Volumen jedes Kauf- oder Verkaufslimits. |

## Diagrammdetails

- Der ausführbare C#-Standard ist 60 Minuten, RSI(21), EMA(50), Schwelle-2-Verhalten und vier Kerzen Cooldown; zusätzlich werden Kerzenrichtung und mittlere RSI-Zonen bewertet. Das kompakte Diagramm nutzt 5 Minuten, 14/20, zwei Stimmen und keinen eigenen Cooldown.
- Der geprüfte Entwurf sah Schwelle 2 vor. Mit nur zwei Stimmen entstanden im März-Replay keine Orders, weil überverkaufter RSI meist mit Kurs unter EMA zusammenfiel und sich die Stimmen aufhoben. Der sichtbare Replay-Standard ist daher 1 und bleibt zum Zurückstellen auf 2 exponiert.
- Das benachbarte README beschreibt acht Muster, Pending-Abstand, Ablauf und Schutz des ursprünglichen Experts. Der aktuelle C#-Code besitzt drei Scorefamilien und Market-Einstiege, ohne Ablauf- oder Schutzblöcke.
- Das Close-Limit ersetzt den Market-Einstieg bewusst, damit Combination, N values und Order cancellation einen echten Zyklus zeigen. Preisrundung ist mangels Preisschritt im Replay deaktiviert.
- Anders als Position <= 0 / >= 0 in C# steigt das Diagramm nur flach ein und dreht nicht. Der prozentuale Schutz 1,2/0,8 ist ein Galeriebeispiel, keine C#-Logik.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
