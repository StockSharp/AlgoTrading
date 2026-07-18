# Vorlage M5-Umschläge Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Konvertiert vom MetaTrader 4 Expert Advisor „Template_M5_Envelopes.mq4“. Die Strategie verfolgt einen linear gewichteten gleitenden Durchschnitt (LWMA)-Umschlag bei Fünf-Minuten-Kerzen und Waffen-Breakout-Stop-Orders, wann immer sich der Preis weit genug vom Kanal entfernt. Ausstehende Aufträge werden dynamisch neu bewertet, um dem Markt zu folgen, und gefüllte Positionen werden durch konfigurierbare Stop-Loss-, Take-Profit- und Trailing-Stop-Logik geschützt.

## Handelslogik

1. Mit dem konfigurierten `EnvelopePeriod` wird ein LWMA basierend auf dem mittleren Kerzenpreis berechnet. Obere und untere Hüllkurvenbänder werden durch Anwendung des Prozentsatzes `EnvelopeDeviation` abgeleitet.
2. Jede fertige Fünf-Minuten-Kerze speichert neben dem Höchst- und Tiefstwert auch ihre Hüllkurvenwerte. Signale werden erst ausgewertet, wenn ein vollständiger Satz „vorheriger“ Werte verfügbar ist, der mit der MetaTrader-Implementierung, die auf `iEnvelopes(..., shift = 1)` verwiesen hat, und dem vorherigen Balken übereinstimmt.
3. Ein **Kauf**-Setup erscheint, wenn:
   * Das vorherige Kerzentief liegt mindestens `DistancePoints` unter dem vorherigen unteren Umschlag und
   * Der aktuelle Gebotspreis bleibt mindestens `DistancePoints` unter dem gleichen Umschlagswert.
4. Ein **Verkaufs-Setup** spiegelt die Logik mit dem vorherigen Hoch und der oberen Hüllkurve wider.
5. Wenn ein Setup aktiv ist, ist nur eine Stop-Order zulässig (die ursprüngliche EA beschränkte sich auch auf einen einzelnen Markt oder eine ausstehende Order). Die Order wird zum aktuellen Brief-/Gebotskurs plus der `EntryOffsetPoints`-Distanz platziert.
6. Während die ausstehende Bestellung aktiv bleibt, überwacht die Strategie den Markt. Wenn die Differenz zwischen dem Orderpreis und dem aktuellen Geld-/Briefkurs `EntryOffsetPoints + SlippagePoints` überschreitet, wird die Order storniert und sofort zum neuen Referenzpreis neu registriert, wobei der angehängte Stop-Loss und Take-Profit an den gewünschten Offsets ausgerichtet bleiben.
7. Wenn der aktuelle Spread `MaxSpreadPoints` überschreitet, werden alle ausstehenden Einträge storniert, um einen Handel bei ungünstigen Liquiditätsbedingungen zu vermeiden.

## Auftragsverwaltung

* Bei der Aktivierung der Einstiegsorder zeichnet die Strategie den Ausführungspreis auf und registriert schützende Stop- und Take-Profit-Orders mit einem Offset von `StopLossPoints` bzw. `TakeProfitPoints`. Wenn einer der Werte Null ist, wird der entsprechende Schutz übersprungen.
* Das Trailing-Stop-Modul (aktiviert mit `UseTrailingStop`) verfolgt den besten Geld-/Briefkurs. Immer wenn sich der Preis um mehr als `TrailingStopPoints` zugunsten der offenen Position bewegt, wird die Stop-Order mit `ReRegisterOrder` näher an den Markt angepasst. Lange Stopps verlaufen nur nach oben, während kurze Stopps nur nach unten verlaufen.
* Wenn die Position vollständig geschlossen ist, werden alle Schutzbefehle aufgehoben und der interne Status zurückgesetzt. Es werden keine neuen Einstiegsaufträge berücksichtigt, bis die Position wieder stabil ist.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `MaxSpreadPoints` | Maximal zulässiger Spread, bevor ausstehende Aufträge storniert werden. |
| `TakeProfitPoints` | Auf besetzte Positionen angewendete Take-Profit-Distanz. |
| `StopLossPoints` | Stop-Loss-Distanz, angewendet auf ausstehende und gefüllte Positionen. |
| `EntryOffsetPoints` | Offset (in Punkten) vom Geld-/Briefkurs, an dem Stop-Einträge platziert werden. |
| `UseTrailingStop` | Ermöglicht die Trailing-Stop-Verwaltung für offene Positionen. |
| `TrailingStopPoints` | Vom Trailing Stop eingehaltener Abstand (in Punkten). |
| `FixedVolume` | Mit jedem Einstiegsauftrag übermitteltes Handelsvolumen. |
| `EnvelopePeriod` | Länge des LWMA, der als Hüllkurvenbasis verwendet wird. |
| `EnvelopeDeviation` | Breite des Umschlags in Prozent. |
| `DistancePoints` | Mindestabstand zwischen Preis und Hüllkurve, der für ein Signal erforderlich ist. |
| `SlippagePoints` | Zusätzliche Toleranz (in Punkten) zum Neupreisschwellenwert hinzugefügt. |
| `CandleType` | Zeitrahmen zur Berechnung des LWMA-Umschlags (Standard M5). |

## Notizen

* Die Strategie abonniert sowohl Kerzen als auch Level-1-Kurse. Wenn keine Geld-/Briefdaten verfügbar sind, werden die Einstiegsbedingungen nicht ausgelöst, da die Spread- und Trailing-Stop-Berechnungen davon abhängen.
* Schutz-Stopp- und Take-Profit-Orders werden immer dann mit dem aktuellen Volumen neu erstellt, wenn die Trailing-Logik den Stop-Loss-Preis anpasst.
* Alle Kommentare im Code sind in Englisch verfasst und Tabulatoren werden zum Einrücken verwendet, um den Projektkonventionen zu entsprechen.
