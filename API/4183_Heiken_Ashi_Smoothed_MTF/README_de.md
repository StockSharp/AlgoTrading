# Heiken Ashi hat die MTF-Strategie geglättet
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Heiken Ashi Smoothed MTF-Strategie ist eine Portierung des Expertenberaters „HASNEWJ“ MetaTrader. Es baut den benutzerdefinierten geglätteten Heiken-Ashi-Indikator auf sechs Zeitrahmen (M1, M5, M15, M30, H1, H4) neu auf und wartet auf die Trendausrichtung über die höheren Rahmen hinweg. Ein Handel wird eröffnet, wenn der untere M5-Strom einen erneuten Rückgang zeigt, während die längerfristigen geglätteten Kerzen stark bullisch oder bärisch bleiben. Die manuelle Stop-Loss- und Take-Profit-Logik reproduziert das Verhalten des ursprünglichen EA, einschließlich der Möglichkeit, den Stop nach einem Verlusthandel leicht zu erweitern.

## Indikatoren und Daten
- **Geglättete Heiken Ashi-Kerzen** auf M1, M5, M15, M30, H1 und H4.
  - Der erste Glättungsdurchgang wendet eine konfigurierbare Methode/Länge des gleitenden Durchschnitts auf die rohen OHLC-Werte an.
  - Der zweite Durchgang glättet den zwischenzeitlichen Eröffnungs-/Schlusskurs von Heiken Ashi mit einem weiteren konfigurierbaren gleitenden Durchschnitt.
- **Richtungszähler**, die verfolgen, wie viele einminütige Aktualisierungen in jedem Zeitrahmen bullisch oder bärisch geblieben sind.
- **Roher Schlusskurs** aus der M1-Serie für Risikomanagementprüfungen.

## Eingabelogik
1. Aktualisieren Sie die geglättete Heiken Ashi-Richtung für jeden Zeitrahmen, wenn eine Kerze endet.
2. Bei jeder abgeschlossenen M1-Kerze werden die bullischen/bärischen Zähler abhängig von der letzten Richtung jedes Zeitrahmens erhöht oder zurückgesetzt.
3. **Kaufbedingungen:**
   - M5 geglättet. Heiken Ashi ist bullisch und der bullische Zähler liegt unter `MaxM5TrendLength` (Standard 10 Aktualisierungen).
   - M15 geglättet. Heiken Ashi ist bullisch und sein bullischer Zähler liegt über `MinM15TrendLength` (Standard 200 Aktualisierungen).
   - Die geglätteten M30-, H1- und H4-Heiken-Ashi-Kerzen sind ebenfalls bullisch.
   - Derzeit ist keine Long-Position offen (Short-Positionen sind zulässig und werden umgedreht).
4. **Verkaufsbedingungen:**
   - M5 geglättet Heiken Ashi ist bärisch und der bärische Zähler liegt unter `MaxM5TrendLength`.
   - M15 geglättet Heiken Ashi ist bärisch und sein bärischer Zähler liegt über `MinM15TrendLength`.
   - Die geglätteten Kerzen M30, H1 und H4 sind bärisch.
   - Derzeit ist keine Short-Position offen (Long-Engagement ist geschlossen oder umgekehrt).
5. Das Market-Order-Volumen entspricht `TradeVolume` plus dem absoluten Wert des entgegengesetzten Engagements, um sicherzustellen, dass Flips den vorherigen Trade schließen.

## Risikomanagement
- Ein manueller Stop-Loss und Take-Profit werden für jede fertige M1-Kerze mit `Security.PriceStep` ausgewertet.
- Der Take-Profit schließt die Position, sobald sich der Preis um `TakeProfitPoints` Schritte zugunsten des Handels bewegt.
- Der Stop-Loss schließt die Position, sobald sich der Preis um `StopLossPoints` Schritte gegen den Handel bewegt.
- Nach einem verlustbringenden Trade erweitert der nächste Eintrag den Stop-Loss um `ExtraStopLossPoints` Schritte und imitiert damit die „Fail“-Flagge von EA.
- Das Handelsvolumen ist durch `TradeVolume` festgelegt; Es wird keine Pyramiden- oder Skalierungslogik angewendet, die über die Umkehrung der bestehenden Exposition hinausgeht.

## Parameter
| Name | Beschreibung | Standard |
| ---- | ----------- | ------- |
| `TradeVolume` | Basisauftragsvolumen, das für Einträge verwendet wird | `0.1` |
| `TakeProfitPoints` | Take-Profit-Distanz in Preisschritten | `20` |
| `StopLossPoints` | Stop-Loss-Distanz in Preisschritten | `500` |
| `ExtraStopLossPoints` | Nach einem Verlusthandel werden zusätzliche Stoppschritte angewendet | `5` |
| `FirstMaPeriod` | Länge des ersten gleitenden gleitenden Durchschnitts | `6` |
| `FirstMaMethod` | Methode des ersten Glättungs-MA (`Simple`, `Exponential`, `Smoothed`, `LinearWeighted`) | `Smoothed` |
| `SecondMaPeriod` | Länge des zweiten gleitenden gleitenden Durchschnitts | `2` |
| `SecondMaMethod` | Methode des zweiten Glättungs-MA | `LinearWeighted` |
| `MaxM5TrendLength` | Maximal zulässige Anzahl von M5-Updates, bevor ein Pullback-Eintrag abgebrochen wird | `10` |
| `MinM15TrendLength` | Mindestanzahl an M15-Updates erforderlich, um den höheren Trend zu bestätigen | `200` |
| `M1CandleType` | Datentyp für den Basis-Kerzenstrom von einer Minute | `TimeFrame(00:01:00)` |
| `M5CandleType` | Datentyp für den fünfminütigen Bestätigungsstream | `TimeFrame(00:05:00)` |
| `M15CandleType` | Datentyp für den 15-minütigen Bestätigungsstream | `TimeFrame(00:15:00)` |
| `M30CandleType` | Datentyp für den 30-minütigen Bestätigungsstream | `TimeFrame(00:30:00)` |
| `H1CandleType` | Datentyp für den stündlichen Bestätigungsstream | `TimeFrame(01:00:00)` |
| `H4CandleType` | Datentyp für den vierstündigen Bestätigungsstream | `TimeFrame(04:00:00)` |

## Nutzungshinweise
- Die Richtungszähler werden einmal pro fertiger M1-Kerze aktualisiert, was den tickbasierten Zählern von MetaTrader nahe kommt, während die Implementierung kerzengesteuert bleibt.
- Stellen Sie sicher, dass `Security.PriceStep` konfiguriert ist. andernfalls fällt die Strategie bei der Berechnung der Stopp- und Zielniveaus auf einen Schritt von 0,0001 zurück.
- Beide Glättungsdurchgänge basieren auf gleitenden Durchschnitten. Durch das Experimentieren mit verschiedenen Kombinationen von Methoden und Zeiträumen kann das System an Instrumente mit unterschiedlichen Volatilitätsprofilen angepasst werden.
