# PriceChannel Signal v2 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
PriceChannel Signal v2 ist ein trendfolgendes Breakout-System, das auf einem modifizierten Donchian-Kanal basiert. Der ursprüngliche MQL5-Expertenberater achtet auf Übergänge im Kanaltrend, optionale Wiedereintrittsbedingungen, wenn der Preis wieder durch die Bänder drückt, und schützende Ausstiegsniveaus, die aus derselben Spanne abgeleitet werden. Der StockSharp-Port behält das ursprüngliche Verhalten bei: Er handelt jeweils eine einzelne Position, reagiert nur auf abgeschlossene Kerzen und kann auf ein Intraday-Fenster beschränkt werden.

## Handelslogik
1. Donchian Kanalhoch und -tief werden über die konfigurierten `ChannelPeriod` berechnet.
2. Der Rohbereich wird um zwei Multiplikatoren verschoben:
   * **Risikofaktor** – komprimiert die Eintrittsbänder in Richtung des Kanalmedians.
   * **Exit Level** – bildet ein zweites Paar innerer Bänder, die Ausgänge auslösen.
3. Ein Trendstatus wird beibehalten:
   * Wenn der Schlusskurs das obere Einstiegsband durchbricht, wird der Trend bullisch.
   * Wenn der Schlusskurs unter das untere Einstiegsband fällt, wird der Trend rückläufig.
   * Ansonsten wird der bisherige Trend beibehalten.
4. Aus diesem Zustand generierte Signale:
   * **Long-Einstieg** – Trend wechselt von bärisch zu bullisch.
   * **Kurzer Einstieg** – Trend wechselt von bullisch zu bärisch.
   * **Langer Wiedereinstieg** – optional, der Preis schließt wieder über dem oberen Band, während der Trend bereits bullisch ist.
   * **Kurzer Wiedereinstieg** – optional, der Preis schließt wieder unter dem unteren Band, während der Trend bereits rückläufig ist.
   * **Langer Ausstieg** – optional, der Preis schließt unterhalb des bullischen Ausstiegsbandes, nachdem er beim vorherigen Balken darüber lag.
   * **Short Exit** – optional, der Preis schließt über dem rückläufigen Ausstiegsband, nachdem er beim vorherigen Balken darunter lag.
5. Es ist nur eine Bestellung pro Balken und pro Richtung zulässig. Die Strategie weigert sich, eine neue Position zu eröffnen, wenn bereits eine andere aktiv ist.
6. Wenn der Intraday-Zeitfilter aktiviert ist, werden alle oben genannten Signale außerhalb des konfigurierten Fensters ignoriert.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `ChannelPeriod` | Donchian Lookback-Länge, die zur Berechnung des Preiskanals und der Ausstiegsbänder verwendet wird. |
| `RiskFactor` | Verschiebung der Eintrittsbänder (0–10). Niedrigere Werte verbreitern die Bänder, höhere Werte verengen sie. |
| `ExitLevel` | Verschiebung der Ausgangsbänder. Muss größer als `RiskFactor` sein, um innerhalb des Eingabebereichs zu bleiben. |
| `UseReEntry` | Ermöglicht Wiedereinstiegsgeschäfte, wenn der Preis wieder durch das aktive Band fällt. |
| `UseExitSignals` | Ermöglicht die Ausgangslogik basierend auf den inneren Schutzbändern. |
| `CandleType` | Zeitrahmen, der zum Erstellen von Kerzen und zum Ausführen der Indikatoren verwendet wird. |
| `UseTimeControl` | Schaltet das Intraday-Handelsfenster um. |
| `StartHour` / `StartMinute` | Inklusive Beginn des Handelsfensters bei aktiver Zeitsteuerung. |
| `EndHour` / `EndMinute` | Exklusives Ende des Handelsfensters bei aktiver Zeitsteuerung. |

## Ein- und Ausreiseregeln
* **Einstieg in eine Long-Position:** Der Trend schlägt in Richtung Aufwärtstrend um oder es kommt zu einer Wiedereintrittsbedingung, die aktuelle Position ist flach und der Balken befindet sich innerhalb des zulässigen Zeitfensters.
* **Enter Short:** Der Trend dreht sich zu einem Abwärtstrend um oder die Bedingung für einen Short-Wiedereintritt wird ausgelöst, die aktuelle Position ist flach und der Balken befindet sich innerhalb des zulässigen Zeitfensters.
* **Exit Long:** `UseExitSignals` ist aktiviert und der Schlusskurs fällt unter das Ausstiegsband, nachdem er beim vorherigen Balken darüber lag.
* **Exit Short:** `UseExitSignals` ist aktiviert und der Schlusskurs steigt über das Ausstiegsband, nachdem er beim vorherigen Balken darunter lag.

## Zusätzliche Hinweise
* Die Strategie arbeitet mit Marktaufträgen und baut keine Pyramidenpositionen auf.
* Indikatorwerte werden nur bei fertigen Kerzen verarbeitet, um ein Neuzeichnen innerhalb des Balkens zu vermeiden.
* Sofern nicht ausdrücklich angegeben, beträgt das Volumen standardmäßig 1 Vertrag.
* Die Zeitsteuerung folgt dem ursprünglichen EA-Verhalten: Die Endzeit ist exklusiv und ein Umbruch über Mitternacht wird unterstützt.
