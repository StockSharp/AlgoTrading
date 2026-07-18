# RRS Chaotische Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Das ursprüngliche **RRS Chaotic EA** würfelt kontinuierlich bei jedem Tick und wählt zufällige Symbole und Positionsgrößen aus, bevor es Marktaufträge versendet. Der StockSharp-Port hält den Geist des kontrollierten Chaos aufrecht, indem er Einträge von einem Kerzenstrom auf die konfigurierte Sicherheit steuert. Jede geschlossene Kerze löst eine neue zufällige Entscheidung für Richtung und Volumen aus und spiegelt gleichzeitig die Geldverwaltungsregeln des Fachberaters wider.

## Hauptmerkmale
- **Zufällige Einträge** – jede fertige Kerze generiert eine zufällige Ganzzahl von 0 bis 10. Die Werte `6` oder `9` eröffnen eine Long-Position, während `3` oder `8` eine Short-Position eröffnen, entsprechend der MT4-Logik.
- **Variables Volumen** – das gehandelte Volumen wird gleichmäßig zwischen den Parametern *MinVolume* und *MaxVolume* abgetastet und an der Volumenstufe des Wertpapiers ausgerichtet.
- **Spread-Filter** – neue Positionen werden immer dann blockiert, wenn der aktuelle Spread (in Punkten) *MaxSpreadPoints* überschreitet.
- **Take-Profit und Stop-Loss** – optionale punktbasierte Exits, die die Einstellungen des Experten auf Orderebene nachbilden.
- **Drawdown-Schutz** – nicht realisierte Verluste werden kontinuierlich entweder mit einem festen Barlimit oder einem Prozentsatz des Portfoliowerts verglichen. Bei Überschreitung des Limits werden aktive Orders storniert und die Position verflacht.

## Parameter
| Name | Beschreibung |
|------|-------------|
| `CandleType` | Kerzenserie, die zum Auslösen der Strategie verwendet wird (Standard-1-Minuten-Kerzen). |
| `MinVolume` / `MaxVolume` | Bereich für die Zufallslosgenerierung. |
| `TakeProfitPoints` | Take-Profit-Distanz in Preispunkten. Zum Deaktivieren auf `0` setzen. |
| `StopLossPoints` | Stop-Loss-Distanz in Preispunkten. Zum Deaktivieren auf `0` setzen. |
| `MaxOpenTrades` | Maximales Nettovolumen, gemessen in Volumenschritten, die gleichzeitig offen bleiben dürfen. |
| `MaxSpreadPoints` | Maximal zulässiger Spread, ausgedrückt in Preispunkten. |
| `SlippagePoints` | Informativer Slippage-Parameter (der Vollständigkeit halber beibehalten). |
| `RiskControlMode` | Wählt zwischen den Risikomodellen `FixedMoney` und `BalancePercentage` aus. |
| `RiskValue` | Je nach Modus entweder der zu riskierende Geldbetrag oder der Prozentsatz des Eigenkapitals. |
| `TradeComment` | Zur einfacheren Prüfung wird ein Tag an generierte Bestellungen angehängt. |

## Strategielogik
1. Abonnieren Sie die konfigurierte Kerzenserie und warten Sie auf fertige Kerzen.
2. Wenden Sie die Drawdown-Kontrolle an. Wenn der nicht realisierte Verlust den Schwellenwert überschreitet, stornieren Sie aktive Aufträge und schließen Sie die aktuelle Position.
3. Behalten Sie optionale Stop-Loss- und Take-Profit-Ziele bei, die die MT4-Auftragseinstellungen widerspiegeln.
4. Wenn der Handel erlaubt ist und der Spread akzeptabel ist, würfeln Sie eine Zufallszahl, um zu entscheiden, ob Sie eine Long- oder Short-Position eröffnen möchten.
5. Begrenzen Sie die Gesamtbelastung, indem Sie die Anzahl der Lautstärkeschritte auf `MaxOpenTrades` begrenzen.

## Unterschiede zur MQL4-Version
- Der ursprüngliche Experte handelte mit mehreren zufälligen Symbolen. StockSharp-Strategien basieren auf einem einzigen Wertpapier; Daher wird die Zufälligkeit nur auf Richtung und Größe angewendet.
- Schutzstopps werden über Marktaufträge bei Kerzenschließungen statt über native Stop-Loss-/Take-Profit-Auftragsparameter ausgeführt.
- Die Spread-Auswertung verwendet den aktuell besten Geld-/Briefkurs anstelle der MT4-Funktion `MarketInfo`.
- Alle generierten Aufträge enthalten den Text *TradeComment*, der einen ähnlichen Kontext wie die magischen MT4-Zahlen bietet.

## Nutzungshinweise
- Stellen Sie sicher, dass die verbundene Sicherheit gültige `PriceStep`-, `MinStep`- und `VolumeStep`-Werte für eine genaue Point-to-Price-Konvertierung bereitstellt.
- Der standardmäßige Kerzenzeitrahmen beträgt eine Minute, um die Zufälligkeit auf Tick-Ebene zu emulieren, ohne die Backtesting-Pipeline zu überlasten. Erhöhen Sie den Zeitrahmen, um die Handelshäufigkeit zu verringern.
- Die Risikokontrolle basiert auf dem nicht realisierten PnL, der aus der aggregierten Position abgeleitet wird. Gemischte Long/Short-Baskets, wie sie in der MT4-Version zu sehen sind, werden von StockSharp nicht unterstützt und daher saldiert.
