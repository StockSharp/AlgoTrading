# MACD Simple Reshetov Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie reproduziert das Verhalten von Yury Reshetovs "MACDSimple"-MetaTrader-Expertenberaters im StockSharp-Framework. Sie arbeitet mit einem einzigen Wertpapier und bewertet klassische MACD-Signale, die durch zwei Offset-Parameter modifiziert werden. Der Algorithmus verarbeitet nur abgeschlossene Kerzen, stellt sicher, dass alle Handelsentscheidungen auf bestätigten Daten getroffen werden, und vermeidet Intrabar-Rauschen.

## Indikatoren und Berechnungen
- **MACD (Moving Average Convergence Divergence)** – die MACD-Linie und Signallinie werden mit benutzerdefinierten Perioden berechnet:
  - Schnelle EMA-Periode = `SignalPeriod + DF`
  - Langsame EMA-Periode = `SignalPeriod + DS + DF`
  - Signallinienperiode = `SignalPeriod`
Die Offsets `DF` und `DS` folgen den ursprünglichen Experteineingaben und ermöglichen dem Trader, die MACD-Komponenten zu strecken oder zu komprimieren, während ihre Beziehung intakt bleibt.

## Parameter
| Name | Beschreibung | Standard |
| ---- | ------------ | -------- |
| `Volume` | Für jeden Markteinstieg verwendete Ordergröße. | 2 |
| `DF` | Zum schnellen MACD-EMA-Länge hinzugefügter Offset. Muss null oder positiv sein. | 1 |
| `DS` | Zusätzlicher Offset auf die langsame MACD-EMA-Länge angewendet. Muss null oder positiv sein. | 2 |
| `SignalPeriod` | Basisperiode, aus der die schnellen und langsamen EMA-Längen abgeleitet werden. | 10 |
| `CandleType` | Zeitrahmen der für Analyse und Handel verwendeten Kerzen. | 30-Minuten-Zeitrahmen |

## Handelslogik
### Positionshandling
1. Bei jeder fertigen Kerze aktualisiert die Strategie den MACD-Indikator und ignoriert die Bar, wenn der Indikator noch nicht vollständig geformt ist.
2. Wenn eine **Long**-Position offen ist und die MACD-Linie unter null fällt, schließt die Strategie die gesamte Long-Position zum Marktpreis.
3. Wenn eine **Short**-Position offen ist und die MACD-Linie über null steigt, schließt die Strategie die gesamte Short-Position zum Marktpreis.
4. Nach dem Schließen einer Position in einer gegebenen Bar hört der Algorithmus auf, diese Bar zu verarbeiten, und spiegelt das Verhalten des ursprünglichen Expertenberaters wider.

### Einstiegsregeln
1. Der Algorithmus überprüft, dass sowohl die MACD-Linie als auch die Signallinie dasselbe Vorzeichen haben (beide positiv oder beide negativ). Gemischte Vorzeichen produzieren keine Trades.
2. Wenn beide Linien **positiv** sind, wird eine Long-Position eröffnet, wenn die MACD-Linie über der Signallinie liegt.
3. Wenn beide Linien **negativ** sind, wird eine Short-Position eröffnet, wenn die MACD-Linie unter der Signallinie liegt.
4. Marktorders haben die mit dem `Volume`-Parameter konfigurierte Größe. Es kann jeweils nur eine Position existieren.

### Ausstiegsregeln
- Ausstiege werden ausschließlich durch die MACD-Linie getrieben, die das Null-Niveau gegen die offene Position kreuzt, wie im Positionshandling-Abschnitt beschrieben. Standardmäßig sind keine Teilausstiege, Stop-Losses oder Take-Profits implementiert.

## Zusätzliche Hinweise
- Die Strategie handelt nur wenn `IsFormedAndOnlineAndAllowTrading()` erfüllt ist, um sicherzustellen, dass Live-Daten verfügbar und der Handel aktiviert sind, bevor neue Positionen eingegangen werden.
- Es ist kein automatisches Risikomanagement eingebaut. Benutzer können bei Bedarf benutzerdefinierte Schutzmaßnahmen wie `StartProtection()` hinzufügen oder die Strategie mit Portfolio-Risikokontrollen kombinieren.
- Da die MACD-Parameter aus einer einzigen Basisperiode plus Offsets abgeleitet werden, beeinflusst das Anpassen von `SignalPeriod`, `DF` oder `DS` alle Komponenten gleichzeitig und bewahrt den relativen Abstand des ursprünglichen Expertenberaters.

## Implementierungsdetails
- Die Indikatorbindung verwendet StockSharp's High-Level `SubscribeCandles().Bind()` API und hält die Implementierung prägnant und ereignisgesteuert.
- Die Konvertierung folgt dem in `AGENTS.md` beschriebenen Regelwerk: Tabs werden für Einrückungen verwendet, Indikatorwerte werden direkt aus dem Binding-Callback konsumiert, und Handelsfunktionen `BuyMarket`/`SellMarket` verwalten Ein- und Ausstiege.
- Die Strategiestruktur ist bereit für Erweiterungen (z.B. Hinzufügen von Filtern oder Risikostatik) und bleibt dabei der ursprünglichen MetaTrader-Expertenlogik treu.
