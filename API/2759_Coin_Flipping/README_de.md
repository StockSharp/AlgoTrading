# Münzwurf-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die Münzwurf-Strategie ist ein wörtlicher Port des klassischen MetaTrader-Expertenberaters, der entscheidet, ob er kaufen oder verkaufen soll, indem er einen Münzwurf simuliert. Jede abgeschlossene Kerze löst eine neue Entscheidung aus, wenn die Strategie keine Position hält, sodass das System eine kontinuierliche Reihe unabhängiger Trades durchläuft. Die StockSharp-Konvertierung behält das Verhalten absichtlich einfach bei: Es wird nur eine Position gleichzeitig gehalten, und jeder Trade ist mit einem symmetrischen Take-Profit und Stop-Loss in Pips verbunden.

Obwohl die Kernidee absichtlich naiv ist, demonstriert das Beispiel, wie man auch sehr kleine Expert Advisors in die StockSharp High-Level-API übersetzen kann. Die Strategie ist als Lehrhilfe für die Verkabelung von Abonnements, Geldmanagement-Helfern und Schutzaufträgen nützlich.

## Handelslogik
1. Beim Start der Strategie wird der Zufallszahlengenerator mit dem aktuellen Umgebungs-Tick-Count initialisiert, was dem ursprünglichen `MathSrand(GetTickCount())`-Aufruf aus MQL entspricht.
2. Für jede abgeschlossene Kerze (der Standard-Zeitrahmen ist 1 Minute, aber jeder Kerzentyp kann angegeben werden) prüft die Strategie, ob der Handel erlaubt ist und ob keine Position aktuell offen ist.
3. Wenn keine Position offen ist, produziert der Generator entweder 0 oder 1. Ein Wert von 0 führt zu einem Markt-Kaufauftrag, während 1 einen Markt-Verkaufsauftrag auslöst. Das Volumen wird dynamisch basierend auf dem konfigurierten Risikoprozentsatz und dem Stop-Loss-Abstand berechnet.
4. Von `StartProtection` erstellte Schutzaufträge fügen jeder Position einen Stop-Loss und Take-Profit hinzu, sodass die Exit-Verwaltung automatisch bleibt.

Es werden keine anderen Filter verwendet: Jedes Mal wenn eine Position geschlossen wird, erstellt die nächste Kerze sofort einen neuen Trade.

## Positionsgröße
Die StockSharp-Version interpretiert die Losgröße-Formel neu, um mit Portfolio-Werten zu arbeiten. Der Risikobetrag wird als `Portfolio.CurrentValue * RiskPercent / 100` berechnet. Dieses Kapital wird durch den Stop-Loss-Abstand in Preiseinheiten (Pips konvertiert unter Verwendung des Sicherheitspreisschritts) geteilt, um die Anzahl der Kontrakte abzuleiten. Der Helfer rundet dann die Größe auf den nächsten zulässigen Volumenschritt und setzt Börsenlimits durch `MinVolume` und `MaxVolume` durch.

Dies bewahrt den Geist des Originalcodes — ein festes Prozentsatz des Eigenkapitals pro Trade riskieren — und stellt gleichzeitig sicher, dass die Auftragsgröße die StockSharp-Sicherheitsmetadaten respektiert.

## Parameter
| Parameter | Beschreibung | Standard | Hinweise |
| --- | --- | --- | --- |
| `RiskPercent` | Prozentsatz des Portfolios, der bei jedem Trade riskiert wird. | `2` | Erhöhung dieser Zahl amplifies das Volumen; Reduzierungen machen die Aufträge kleiner. |
| `TakeProfitPips` | Abstand zwischen Einstieg und Take-Profit-Level in Pips. | `20` | In absoluten Preis umgerechnet unter Verwendung des Instrument-Preisschritts und an `StartProtection` übergeben. |
| `StopLossPips` | Abstand zwischen Einstieg und Stop-Loss-Level in Pips. | `10` | Ebenfalls in Preiseinheiten umgerechnet; derselbe Wert wird für die Positionsgrößenbestimmung verwendet. |
| `CandleType` | Kerzenabonnement, das die Entscheidungsschleife plant. | `1-Minuten-Zeitrahmen` | Es kann jeder StockSharp-Kerzentyp angegeben werden; höhere Intervalle verlangsamen das Handelstempo. |

## Risikomanagement
`StartProtection` wird einmal während `OnStarted` mit den berechneten Take-Profit- und Stop-Loss-Abständen gestartet. StockSharp verwaltet dann die Schutzaufträge automatisch und spiegelt die `OrderSend`-Argumente im MQL-Skript wider. Da die Strategie nur handelt, wenn `Position == 0`, müssen vorhandene Aufträge nicht manuell storniert oder neu eingereicht werden; die Plattform storniert die Schutzaufträge, sobald die Position geschlossen ist.

## Implementierungshinweise
- Die Kerzenverarbeitung verwendet das High-Level `SubscribeCandles().Bind(...)`-Muster für Klarheit und Einfachheit.
- Log-Anweisungen beschreiben die gewählte Richtung und das Volumen, damit Backtests deutlich zeigen, wie sich der Pseudozufallsgenerator verhält.
- Die Volumen-Normalisierung berücksichtigt `VolumeStep`, `MinVolume` und `MaxVolume`, wodurch sichergestellt wird, dass generierte Größen der Instrumentenspezifikation entsprechen.
- Der Code hält alle Kommentare auf Englisch, wie erforderlich, und spiegelt die vom Repository verlangte Struktur wider.

## Verwendungshinweise
- Da die Handelsrichtung zufällig ist, wird keine langfristige Rentabilität erwartet. Die Strategie für Demonstrations- oder Testzwecke verwenden.
- Sicherstellen, dass das der Strategie zugewiesene Portfolio einen positiven `CurrentValue` hat, sonst gibt die Risikoberechnung null zurück und es werden keine Trades platziert.
- Den Kerzentyp anpassen, wenn man möchte, dass der Münzwurf seltener (z.B. stündliche Kerzen) oder häufiger (z.B. Tick-Kerzen) stattfindet.
- Bei der Optimierung können alternative Take-Profit- und Stop-Loss-Abstände erkundet oder der Risikoprozentsatz gesenkt werden, um Drawdowns handhabbar zu halten.
