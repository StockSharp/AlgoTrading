# 3874 Trendcapture-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **Trendcapture-Strategie** ist eine High-Level-StockSharp-Portierung des MetaTrader-Expertenberaters `MQL/7772/Trendcapture.mq4`. Der ursprüngliche EA beobachtet die Trendrichtung Parabolic SAR und wartet auf ein schwaches ADX-Umfeld, um neue Positionen einzunehmen. Nach jedem geschlossenen Trade entscheidet es abhängig vom realisierten Gewinn, ob die Handelsrichtung beibehalten oder umgekehrt wird, und sobald eine offene Position ein paar Punkte gewinnt, zieht es den Stop auf die Gewinnschwelle.

Dieser Port behält das Verhalten bei und verlässt sich dabei auf die Order-Helfer und Indikatorbindungen von StockSharp. Alle Signale werden auf abgeschlossene Kerzen eines konfigurierbaren Zeitrahmens verarbeitet.

## Handelslogik

1. **Indikator-Setup**
   - Parabolic SAR (`ParabolicSar`) mit konfigurierbarer Beschleunigungsstufe und Obergrenze.
   - Durchschnittlicher Richtungsindex (`AverageDirectionalIndex`) für den Haupttrendstärkewert.
2. **Eintragsauswahl**
   - Es kann jeweils nur eine Position offen sein.
   - Eine lange Eingabe ist zulässig, wenn:
     - Die gewünschte Richtung (abgeleitet aus dem letzten geschlossenen Trade) deutet auf einen Kauf hin.
     - Die aktuelle Kerze schließt über dem Wert SAR.
     - Die Hauptzeile von ADX befindet sich unter `20` und gibt das vom Originalcode geforderte Bereichsregime an.
   - Ein Short-Einstieg spiegelt die Regeln wider (gewünschte Richtung zeigt auf Verkauf, Schlusskurs unter SAR, ADX unter `20`).
3. **Exit-Management**
   - Bei jeder Ausführung übermittelt die Strategie Stop-Loss- und Take-Profit-Orders in Abständen von `StopLossPoints` und `TakeProfitPoints` (umgerechnet durch den Wertpapierpreisschritt).
   - Wenn der variable Gewinn `GuardPoints` erreicht, wird der aktive Stop zum Einstiegspreis erneut ausgegeben, um eine Break-Even-Untergrenze festzulegen.
   - Das Schließen von Trades löst eine Richtungsaktualisierung aus: Profitable Trades behalten die gleiche Tendenz bei, verlierende oder flache Trades kehren sie um und reproduzieren die `OrderProfit()`-Prüfung des Experten.

## Parameter

| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `CandleType` | Kerzendatentyp, der für Indikatorberechnungen verwendet wird. | 1-stündiger Zeitrahmen |
| `SarStep` | Anfänglicher Beschleunigungsfaktor von Parabolic SAR. | `0.02` |
| `SarMax` | Maximaler Beschleunigungsfaktor für Parabolic SAR. | `0.2` |
| `AdxPeriod` | Glättungszeitraum von ADX. | `14` |
| `TakeProfitPoints` | Take-Profit-Distanz, ausgedrückt in Preisschritten. | `180` |
| `StopLossPoints` | Stop-Loss-Distanz ausgedrückt in Preisschritten. | `50` |
| `GuardPoints` | Gewinnschwelle (in Preisschritten), die erforderlich ist, bevor der Stop auf die Gewinnschwelle verschoben wird. | `5` |
| `MaximumRisk` | Volumenskalierungsfaktor; `0.03` reproduziert die ursprüngliche Losgröße. | `0.03` |

## Nutzungshinweise

- Stellen Sie sicher, dass die ausgewählte Sicherheit `PriceStep` (oder mindestens `MinStep`) offenlegt, damit Punktabstände korrekt in Preiswerte umgewandelt werden.
- Die Basiseigenschaft `Volume` stellt die Losgröße dar, die verwendet wird, wenn `MaximumRisk` gleich `0.03` ist. Durch Erhöhen des Risikofaktors wird das übermittelte Volumen proportional skaliert.
- Da EA zum Marktwert handelt und sofort Schutzaufträge erteilt, sind keine ausstehenden Einträge im Buch, wenn die Strategie inaktiv ist.
- Der Break-Even-Guard hebt den Schutzstopp auf und gibt ihn erneut zum Einstiegspreis aus; Dies spiegelt den ursprünglichen Aufruf von `OrderModify` wider, der den Stop-Loss auf die Gewinnschwelle verschoben hat.

## Dateien

- `CS/TrendcaptureStrategy.cs` – High-Level-StockSharp-Implementierung des Trendcapture EA.
- `README_zh.md` – Chinesische Übersetzung dieses Dokuments.
- `README_ru.md` – Russische Übersetzung dieses Dokuments.
