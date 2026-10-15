# RM Stochastic Bandstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **RM Stochastic Band-Strategie** ist eine hochrangige StockSharp-Portierung des MetaTrader-Expertenberaters *EA RM Stochastic Band* von Ronny Maheza. Die Strategie beobachtet drei stochastische Oszillatoren, die auf unterschiedlichen Zeitrahmen (Basis, Mitte und Hoch) berechnet werden, und eröffnet Geschäfte nur, wenn alle drei überverkaufte oder überkaufte Bedingungen bestätigen. Beim Einstieg werden die Ausstiegsniveaus aus der durchschnittlichen wahren Spanne (ATR) abgeleitet, die im höheren Zeitrahmen gemessen wird, und replizieren die ATR-basierten Stop-Loss- und Take-Profit-Niveaus des ursprünglichen Expertenberaters. Zu den weiteren Ausführungsfiltern gehören ein konfigurierbarer Mindestportfoliowert als Margin-Proxy und eine Spread-Steuerung, die ihre Toleranz abhängig vom beobachteten Spread anpasst.

## Kernlogik

1. **Stochastische Bestätigung mit mehreren Zeitrahmen**
   - Der primäre Ausführungszeitrahmen (Standard M1) generiert das Handelssignal.
   - Bestätigungszeitrahmen (Standard M5 und M15) müssen mit der Signalrichtung übereinstimmen.
   - Ein Trade wird nur eröffnet, wenn die stochastischen %K-Werte in allen drei Zeitrahmen gleichzeitig unter dem überverkauften Niveau (Long-Setup) oder über dem überkauften Niveau (Short-Setup) liegen.

2. **Volatilitätsbasierte Exits mit ATR**
   - ATR wird im höchsten Zeitrahmen berechnet (Standard M15).
   - Stop-Loss = `entry price ± ATR * StopLossMultiplier`.
   - Take-Profit = `entry price ± ATR * TakeProfitMultiplier`.
   - Die Preise werden anhand der Basiszeitrahmenkerzen überwacht. Wenn eine Kerze eines der beiden Niveaus berührt, wird die Position zum Marktwert geschlossen.

3. **Ausführungs- und Sicherheitsfilter**
   - Aufträge werden übersprungen, wenn der beobachtete Spread (BestAsk – BestBid) den adaptiven Schwellenwert überschreitet. Wenn der Spread höher als das Standardlimit ist, wird das lockerere Cent-Kontolimit angewendet, was die Logik der Quelle EA widerspiegelt.
   - Der Handel ist gesperrt, solange der Portfoliowert unter `MinMargin` liegt.
   - Es kann jeweils nur eine Position offen sein und es wird kein neuer Handel initiiert, wenn aktive Aufträge vorliegen.

## Indikatoren und Abonnements

| Indikator | Zeitrahmen | Zweck |
|-----------|-----------|---------|
| Stochastic Oszillator | Basiszeitrahmen (Standard 1 Minute) | Erzeugt ein Primärsignal (nur %K wird verwendet). |
| Stochastic Oszillator | Mittlerer Zeitrahmen (Standard 5 Minuten) | Bestätigt die primäre Signalrichtung. |
| Stochastic Oszillator | Hoher Zeitrahmen (Standard 15 Minuten) | Bietet langfristige Bestätigung. |
| Durchschnittliche wahre Reichweite | Hoher Zeitrahmen (Standard 15 Minuten) | Definiert volatilitätsbereinigte Stop-Loss- und Take-Profit-Abstände. |

Daten der Ebene 1 werden abonniert, um die besten Geld- und Briefkurse für die Spread-Bewertung zu erfassen.

## Teilnahmebedingungen

- **Lange Einrichtung**: Alle drei stochastischen %K-Werte liegen unter `OversoldLevel`. Bei Auslösung kauft die Strategie mit einem Marktvolumen von `OrderVolume` und speichert ATR-basierte Ausstiegsniveaus.
- **Kurzer Aufbau**: Alle drei stochastischen %K-Werte liegen über `OverboughtLevel`. Ein Marktverkauf wird mit der gleichen Volumenabwicklung durchgeführt.

## Ausgangsregeln

- **Stop-Loss**: Bei Long-Positionen beenden Sie den Kurs, wenn das Kerzentief `entry - ATR * StopLossMultiplier` erreicht. Bei Short-Positionen steigen Sie aus, wenn das Kerzenhoch `entry + ATR * StopLossMultiplier` erreicht.
- **Take-Profit**: Bei Long-Positionen steigen Sie aus, wenn das Kerzenhoch `entry + ATR * TakeProfitMultiplier` erreicht. Bei Short-Positionen steigen Sie aus, wenn das Kerzentief `entry - ATR * TakeProfitMultiplier` erreicht.
- Nach einem Ausgang werden die internen Stopp- und Zielplatzhalter gelöscht, sodass das nächste Signal die neuen Pegel neu berechnen kann.

## Parameter

| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `OrderVolume` | Volumen jeder Marktorder. | 0,1 |
| `StochasticLength` | %K Lookback-Zeitraum. | 5 |
| `StochasticSmoothing` | Glättung auf %K angewendet. | 3 |
| `StochasticSignalLength` | %D Länge. | 3 |
| `AtrPeriod` | ATR Zeitraum im höchsten Zeitrahmen. | 14 |
| `StopLossMultiplier` | ATR Multiplikator für den Stop-Loss. | 1.5 |
| `TakeProfitMultiplier` | ATR Multiplikator für den Take-Profit. | 3,0 |
| `MinMargin` | Für den Handel erforderlicher Mindestportfoliowert. | 100 |
| `MaxSpreadStandard` | Spread-Obergrenze für Standardkonten. | 3 |
| `MaxSpreadCent` | Spread-Cap wird verwendet, wenn der aktuelle Spread bereits das Standard-Cap überschreitet. | 10 |
| `OversoldLevel` | Überverkaufter Schwellenwert für stochastischen %K. | 20 |
| `OverboughtLevel` | Überkaufter Schwellenwert für stochastischen %K. | 80 |
| `BaseCandleType` | Primärer Zeitrahmen (Standard-1-Minuten-Kerzen). | 1 Minute |
| `MidCandleType` | Bestätigungszeitrahmen (Standard 5-Minuten-Kerzen). | 5 Minuten |
| `HighCandleType` | Bestätigung + ATR Zeitrahmen (standardmäßige 15-Minuten-Kerzen). | 15 Minuten |

Alle Parameter unterstützen gegebenenfalls Optimierungsbereiche, die mit den MetaTrader-Eingaben identisch sind.

## Implementierungshinweise

- Die Strategie verwendet `SubscribeCandles(...).BindEx(...)`, um Indikatorwerte ausschließlich über das übergeordnete API zu erhalten, wie in den Projektrichtlinien vorgeschrieben.
- Der Spread wird anhand von Live-Updates der Stufe 1 berechnet. Ohne Geld-/Briefdaten bleibt der Handel deaktiviert, wodurch ein sicherer Betrieb bei Datenfeeds gewährleistet wird, die keine Kurse bereitstellen.
- Positionen werden ausschließlich über Marktaufträge verwaltet, was dem ursprünglichen EA entspricht, das auf Markteintritten mit vorberechneten Stop-Loss- und Take-Profit-Niveaus beruhte.
- Es gibt keine Breakeven- oder Trailing-Logik, da die MQL-Quelle diese Funktionen trotz zugehöriger Eingabeparameter nicht implementiert hat.

## Nutzungstipps

1. Hängen Sie die Strategie an das gewünschte Wertpapier an und stellen Sie sicher, dass Level-1-Daten (Bid/Ask) für eine ordnungsgemäße Spread-Filterung verfügbar sind.
2. Passen Sie die stochastischen Schwellenwerte und ATR-Multiplikatoren an, um sie an das Volatilitätsprofil des Zielinstruments anzupassen.
3. Erwägen Sie bei der Optimierung das Testen verschiedener Zeitrahmenkombinationen, wenn der Markt, auf dem Sie handeln, andere dominante Zyklen aufweist als die ursprüngliche M1/M5/M15-Struktur.
