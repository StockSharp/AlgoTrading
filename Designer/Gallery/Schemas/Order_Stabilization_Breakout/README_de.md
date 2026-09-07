# Strategiediagramm für Limitorder-Ausbrüche nach Stabilisierung
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Diagramm überwacht abgeschlossene Fünf-Minuten-Kerzen von BTCUSDT@BNBFT auf einen Übergang von einem unter der Hälfte des ATR(14) stabilisierten Kerzenkörper zu einem über dieses Niveau wachsenden Körper. Es platziert eine Limitorder am Schlusskurs des Signals, lässt der Order drei spätere Kerzen bis zum Abschluss und verdoppelt bei einer ausgeführten Umkehr die Basismenge.

![schema](schema.svg)

## Strategieübersicht

- Abgeschlossene Fünf-Minuten-Kerzen liefern Open und Close, während vollständig gebildete ATR(14)-Werte das aktuelle Volatilitätsmaß bestimmen.
- Body wird als `abs(Close - Open)` berechnet, seine Stabilisierungsgrenze als `ATR * Stabilization Factor`. Der Standardfaktor ist `0.5`.
- Das erste vollständig gebildete Body/ATR-Paar initialisiert die gespeicherten Vorwerte, ohne ein Signal zu erzeugen. Jedes spätere Paar vergleicht den vorherigen und den aktuellen Körper mit der jeweils passenden Grenze.
- Ein Setup erfordert `Previous Body < Previous ATR * 0.5` und `Current Body > Current ATR * 0.5`. Gleichheit an einer der Grenzen erfüllt die Bedingung nicht.
- Eine gemeinsame Pending-Order-Sperre erlaubt nur eine aktive Limitorder. Getrennte Laufzeitfreigaben für Kauf und Verkauf verhindern, dass eine Seite ihren Drei-Kerzen-Zähler vor dessen Abschluss erneut verwendet.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Wenn eine qualifizierte Expansionskerze bullisch ist (`Close > Open`), der vorzeichenbehaftete Zustand neutral oder short ist, keine Order wartet und die Kauflaufzeit freigegeben ist, wird ein Kauflimit am aktuellen Close gesendet.
- **Short-Einstieg**: Wenn eine qualifizierte Expansionskerze bärisch ist (`Close < Open`), der vorzeichenbehaftete Zustand neutral oder long ist, keine Order wartet und die Verkaufslaufzeit freigegeben ist, wird ein Verkaufslimit am aktuellen Close gesendet.
- **Ordermenge**: Die Menge ist `Base Volume * (1 + abs(state))`. Ein Einstieg aus dem neutralen Zustand verwendet eine Basiseinheit; eine zugelassene Umkehr aus `-1` oder `1` verwendet zwei Basiseinheiten.
- **Ausstieg**: Es gibt keinen preisbasierten Schutz. Das Engagement ändert sich nur, wenn ein entgegengesetztes Limit ausgeführt wird; ein nicht ausgeführtes Limit erhält nach drei strikt späteren abgeschlossenen Kerzen eine adressierte Stornierungsanforderung.

## Parameter

| Parameter | Standardwert | Beschreibung |
|---|---|---|
| Security | BTCUSDT@BNBFT | Instrument der abgeschlossenen Fünf-Minuten-Kerzen. Strategy Security muss denselben Wert haben, da Orders, Stornierungen und Ausführungen Strategy Security und Strategy Portfolio verwenden. |
| Candle Series | 00:05:00 | Abgeschlossene Fünf-Minuten-Kerzen für ATR, Körperberechnung, Signale, Laufzeitzählung und Chart. |
| ATR Length | 14 | Mittelungslänge des Average True Range. Entscheidungen beginnen erst, wenn der ATR vollständig gebildet ist. |
| Stabilization Factor | 0.5 | Multiplikator, der getrennt auf den vorherigen und aktuellen ATR angewendet wird, um die Körpergrenzen zu bilden. |
| Lifetime N | 3 | Anzahl strikt späterer abgeschlossener Kerzen bis zur Stornierungsanforderung für ein nicht ausgeführtes Limit. |
| Base Volume | 1 | Menge für einen Einstieg aus dem neutralen Zustand; die Aktionsformel verdoppelt sie für eine Umkehr. |

## Diagrammdetails

- Die Security-[Variable](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) konfiguriert nur das Abonnement abgeschlossener [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html). Jede abgeschlossene Kerze liefert Open, Close und die vollständige Kerze für den ATR; Order- und Trade-Blöcke verwenden Strategy Security und Strategy Portfolio, daher muss Strategy Security mit Security übereinstimmen.
- Der [Indicator](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)-Block gibt nur vollständig gebildete ATR(14)-Werte aus. Formel- und Speicherblöcke halten aktuellen Body, aktuelle Grenze, vorherigen Body und vorherige Grenze innerhalb einer Kerzenentscheidung synchron.
- Ein Initialisierungsspeicher unterdrückt die erste vollständig gebildete Entscheidung und speichert ihr Paar. Spätere Entscheidungen prüfen die beiden strikten Grenzrelationen, bevor sie die Speicher für Vorwerte weiterschalten.
- Die aktuelle Kerze erreicht beide Laufzeitzähler, bevor der Signalzweig ausgeführt wird. Wird ein Zähler erst nach diesem Eingang gestartet, gibt er bei der dritten später abgeschlossenen Kerze frei; die Signalkerze zählt daher nie zu ihrer eigenen Laufzeit.
- Ein angenommenes Setup schließt die Zählerfreigabe seiner Seite und setzt die gemeinsame Pending-Sperre, bevor [Order registering](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/orders/register.html) ausgelöst wird. Die Pending-Sperre wird erst gelöscht, wenn die Order einen Endzustand meldet; der Zähler der Seite bleibt bis zu seiner Drei-Kerzen-Freigabe gesperrt, auch wenn die Order früher ausgeführt wird.
- Jeder Registrierungsblock speichert seine Order-Referenz für eine adressierte Stornierung. Die Laufzeitfreigabe leitet die passende gespeicherte Referenz an die Stornierung weiter und öffnet nur die Zählerfreigabe dieser Seite erneut.
- MyTrade-Ereignisse setzen den vorzeichenbehafteten Zustand anhand tatsächlicher Ausführungen: Ein Verkauf setzt `-1`, ein Kauf setzt `1`, und der Speicher startet bei `0` für neutral. Der Chart erhält Kerzen, ATR, Body, Stabilisierungsgrenze, gesendete Orders und Strategieausführungen.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, setzen Sie Strategy Security auf BTCUSDT@BNBFT, führen Sie sie mit Fünf-Minuten-Historie aus und prüfen Sie Limit-Ausführungen und Stornierungen, bevor Sie Faktor, Laufzeit oder Menge für eine andere Handelsumgebung anpassen.
