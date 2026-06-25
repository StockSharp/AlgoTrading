# Preis-Impuls-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Preis-Impuls-Strategie analysiert rohe Level1-Kurse und reagiert auf plötzliche Änderungen zwischen dem besten Bid und dem besten Ask. Sie spiegelt den ursprünglichen MetaTrader 5-Expert Advisor wider, indem sie Preissprünge über eine konfigurierbare Anzahl von Ticks überwacht und den Markt betritt, wenn die Bewegung einen punktebasierten Schwellenwert überschreitet. Schutz-Stop-Loss- und Take-Profit-Abstände werden automatisch über den High-Level-Helfer `StartProtection` angewendet.

Der Ansatz ist marktneutral: Eine Long-Position wird eröffnet, wenn der Ask-Preis im Vergleich zu einer älteren Kursnotiz nach oben springt, während eine Short-Position eröffnet wird, wenn der Bid unter seinen vorherigen Wert einbricht. Eine konfigurierbare Abklingzeit verhindert, dass die Strategie unmittelbar nach einem Trade erneut einsteigt, genau wie die MQL-Implementierung, die ein angegebenes Schlafintervall abwartet.

## Funktionsweise

- Abonniert Level1-Daten und speichert gleitende Historien der besten Bid- und Ask-Preise.
- Berechnet die Preisdifferenz zwischen der neuesten Kursnotiz und der Kursnotiz, die `HistoryGap` Ticks zuvor eintraf (mit zusätzlichem Puffer, der durch `ExtraHistory` definiert wird).
- Eröffnet eine Long-Position, wenn der Ask-Preis um mehr als `ImpulsePoints * PriceStep` steigt und kein Long-Exposure besteht.
- Eröffnet eine Short-Position, wenn der Bid-Preis um mehr als denselben Schwellenwert fällt und kein Short-Exposure besteht.
- Wendet feste Take-Profit- und Stop-Loss-Niveaus in Preispunkten an und erzwingt eine `CooldownSeconds`-Pause zwischen Aufträgen.

## Parameter

- **OrderVolume** – Volumen, das mit jeder Market Order gesendet wird. Standardmäßig `0.1` Lots, um mit dem Quell-Robot übereinzustimmen, kann aber für andere Instrumente optimiert werden.
- **StopLossPoints** – Abstand vom Einstiegspreis zum Schutz-Stop, gemessen in Instrumentenpunkten. Ein Wert von `0` deaktiviert den Stop.
- **TakeProfitPoints** – Abstand zum Take-Profit-Ziel, ebenfalls in Punkten gemessen. Ein Wert von `0` deaktiviert das Ziel.
- **ImpulsePoints** – Mindest-Preisimpuls in Punkten, der zwischen der aktuellen Kursnotiz und der Kursnotiz `HistoryGap` Ticks zuvor überschritten werden muss, um einen Einstieg auszulösen.
- **HistoryGap** – Anzahl der Level1-Updates, die den aktuellen Preis von der Vergleichsbasis trennen. Höhere Werte erfordern größere Rückblicke, was Rauschen glättet, aber Einstiege verzögert.
- **ExtraHistory** – Zusätzliche Level1-Samples, die im gleitenden Puffer gehalten werden, um Kursstoßphasen zu absorbieren, wenn mehrere Ticks zwischen Callbacks ankommen. Hält die Logik konsistent mit der MT5-Implementierung, die das Historien-Array überabtastet.
- **CooldownSeconds** – Mindestwartezeit nach einem Trade, bevor ein weiterer Einstieg platziert werden kann. Stellt sicher, dass die Strategie den `InpSleep`-Parameter des MQL-Experten widerspiegelt und verhindert schnelles Hin- und Herschalten.

## Hinweise

- Die Punktabstandsparameter werden automatisch mit `Security.PriceStep` (oder `Security.MinPriceStep` als Fallback) konvertiert, sodass dieselbe Konfiguration sich an verschiedene Tick-Größen anpasst.
- Trading beginnt erst, wenn die Strategie online ist, die Historien-Puffer genügend Daten enthalten und die Impulsbedingung erfüllt ist.
- Da Entscheidungen auf Basis roher Kurs-Updates getroffen werden, funktioniert die Strategie am besten bei liquiden Instrumenten mit zuverlässigen Level1-Feeds.
- Es gibt keine Python-Version für diese Strategie. Nur die C#-Version wird bereitgestellt, entsprechend der Benutzeranfrage.
