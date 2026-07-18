# App-Preis-Level-Cross-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
- Konvertierung des MetaTrader 4 Expertenberaters **BT_v4** (`MQL/8543/BT_v4.mq4`).
- Neu implementiert mit der übergeordneten StockSharp-Strategie API (Kerzenabonnements, indikatorfreie Verarbeitung, integrierte Schutzmaßnahmen).
- Konzentriert sich auf die Reaktion auf den Schlusskurs, der ein benutzerdefiniertes horizontales Niveau (`AppPrice`) überschreitet.

## Handelslogik
1. Jede fertige Kerze aktualisiert einen internen Puffer mit dem neuesten Schlusskurs.
2. Wenn sich der Schlusskurs **über** `AppPrice` bewegt, während der vorherige Schlusskurs **auf oder unter** dem Niveau lag, ist die Strategie
   - Wird nur gehandelt, wenn `BuyOnly = true` (spiegelt die ursprüngliche EA-Standardeinstellung wider).
   - Storniert alle ausstehenden Aufträge, gleicht einen bestehenden Short mit demselben Marktauftragsvolumen aus und baut eine Long-Position der berechneten Losgröße auf.
3. Wenn der Schlusskurs **unter** `AppPrice` fällt, während der vorherige Schlusskurs **auf oder über** dem Niveau lag, gilt die Strategie
   - Wird nur gehandelt, wenn `BuyOnly = false` (Nur-Verkaufsmodus von EA).
   - Storniert ausstehende Aufträge, gleicht bestehende Long-Positionen aus und richtet eine Short-Position mit der berechneten Losgröße ein.
4. Signale werden ausschließlich anhand abgeschlossener Kerzen ausgewertet; Teilweise geformte Kerzen werden wie im MQL-Skript ignoriert.

## Positionsgrößen
- `EnableMoneyManagement = false` → `FixedVolume` verwenden (entspricht der Eingabe MQL `Lots`).
- `EnableMoneyManagement = true` → Berechnen Sie das Los mit der Originalformel:

\[
\text{lot} = \text{round}_{\text{LotPrecision}} \left( \frac{\text{LotBalancePercent}}{100} \times \frac{\text{Balance}}{\text{divisor}} \right)
\]

  - `divisor = 1000` für Ein-Dezimal-Lots und `100` für Zwei-Dezimal-Lots (gleiche Regel wie `LotPrec` in MQL).
  - Das Ergebnis wird auf [`MinLot`, `MaxLot`] beschränkt und dann mit den Sicherheitseinschränkungen `VolumeStep`, `VolumeMin` und `VolumeMax` abgeglichen.
  - Wenn keine Portfoliobilanzdaten verfügbar sind, greift die Strategie auf `FixedVolume` zurück.

## Risikomanagement
- `StopLossPoints` und `TakeProfitPoints` werden in Instrumentenpreispunkten (Ticks) gemessen.
- Wenn einer der Werte positiv ist, wird `StartProtection` mit den durch `Security.PriceStep` konvertierten Offsets aktiviert.
- Wenn Sie einen Abstand auf `0` festlegen, wird dieser bestimmte Schutzzweig deaktiviert, was dem ursprünglichen Verhalten von EA entspricht.

## Parameter
| Name | Beschreibung | Standard |
| ---- | ----------- | ------- |
| `AppPrice` | Niveau, das Trades auslöst, wenn der Schlusskurs dieses Niveau überschreitet. | `0` |
| `BuyOnly` | `true` = Nur-Long-Modus (ursprüngliche Standardeinstellung), `false` = Nur-Short-Modus. | `true` |
| `FixedVolume` | Losgröße, wenn MM deaktiviert ist. | `0.1` |
| `EnableMoneyManagement` | Ermöglicht die prozentuale Größenanpassung. | `false` |
| `LotBalancePercent` | Prozentsatz des Guthabens, der verwendet wird, wenn MM aktiviert ist. | `10` |
| `MinLot` / `MaxLot` | Grenzen für die berechnete Losgröße. | `0.1` / `5` |
| `LotPrecision` | Anzahl der Dezimalstellen zum Runden der berechneten Menge. | `1` |
| `StopLossPoints` | Stop-Loss-Distanz in Preispunkten (0 = deaktiviert). | `140` |
| `TakeProfitPoints` | Take-Profit-Distanz in Preispunkten (0 = deaktiviert). | `180` |
| `CandleType` | Kerzenzeitrahmen, der für die Kreuzerkennung verwendet wird. | `1 Minute` |

## Implementierungshinweise
- Verwendet `SubscribeCandles(...).Bind(...)`, sodass Indikatoren nicht erforderlich sind; Die Schlusskurse kommen direkt im Callback.
- Market-Orders (`BuyMarket`/`SellMarket`) sind so dimensioniert, dass sie die entgegengesetzte Position abflachen, bevor eine neue eröffnet wird, was der EA-Logik widerspiegelt, entgegengesetzte Orders vor dem Eintritt zu schließen.
- `CancelActiveOrders()` wird vor jeder Marktorder aufgerufen, um unbeabsichtigte ausstehende Orders zu vermeiden.
- Parameter wie `Magic`, `Slippage` und Farbeinstellungen aus der Datei MQL werden weggelassen, da sie in StockSharp kein direktes Äquivalent haben.
- Stellen Sie sicher, dass die `Security`-Metadaten (`PriceStep`, `VolumeStep`, `VolumeMin`, `VolumeMax`) ausgefüllt sind, damit Preis-/Volumenanpassungen den Brokerregeln entsprechen.

## Nutzungstipps
- Stellen Sie `AppPrice` auf die horizontale Ebene ein, die Sie überwachen möchten (z. B. psychologischer Preis, täglicher Pivot usw.).
- Schalten Sie `BuyOnly` aus, um den ursprünglichen „Nur-Verkauf“-Modus zu reproduzieren. Lassen Sie es eingeschaltet, um das bereitgestellte standardmäßige Long-Only-Verhalten auszuführen.
- Stellen Sie beim Aktivieren der Geldverwaltung sicher, dass die Portfolioverbindung Saldoaktualisierungen bereitstellt. andernfalls kehrt die Strategie zum festen Volumen zurück.
- Pro Anfrage wird kein Python-Port bereitgestellt. Es wird nur die C#-Strategie generiert.
