# GPF TCPivotLimit-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **GPF TCPivotLimit-Strategie** erstellt den MetaTrader 4 Expert Advisor `gpfTCPivotLimit.mq4` innerhalb des StockSharp-Frameworks neu. Das System handelt auf **stündlichen Kerzen** und reagiert auf Umkehrungen um klassische **tägliche Pivot-Levels**. An jedem neuen Handelstag berechnet die Strategie den Pivot, drei Widerstandsniveaus (R1–R3) und drei Unterstützungsniveaus (S1–S3) aus dem Hoch, Tief und Schlusskurs des Vortages. Sobald der nächste Tag beginnt, wertet es die letzten beiden abgeschlossenen Stundenkerzen aus, um zu entscheiden, ob der Preis eine Widerstands- oder Unterstützungszone abgelehnt hat, und eröffnet eine Marktorder in die entgegengesetzte Richtung.

## Handelslogik

1. **Pivot-Berechnung** – Wenn eine neue tägliche Sitzung beginnt, speichert die Strategie die Höchst-, Tiefst- und Schlusskurse des Vortages und berechnet dann:
   - `Pivot = (High + Low + Close) / 3`
   - `R1 = 2 × Pivot − Low`, `S1 = 2 × Pivot − High`
   - `R2 = Pivot + (High − Low)`, `S2 = Pivot − (High − Low)`
   - `R3 = High + 2 × (Pivot − Low)`, `S3 = Low − 2 × (High − Pivot)`
2. **Eingabebestätigung** – zu Beginn des neuen Tages werden die letzten beiden geschlossenen Stundenkerzen (`t-2` und `t-1`) überprüft.
   - Ein **Short** wird eröffnet, wenn die Kerze `t-2` über dem ausgewählten Widerstand (hoch über oder nahe dem Niveau) liegt, sich darunter öffnet und die Kerze `t-1` wieder unter dem Niveau schließt.
   - Ein **Long** wird eröffnet, wenn die Kerze `t-2` unter die ausgewählte Unterstützung fällt (Tief unter oder Schluss auf dem Niveau), darüber öffnet und die Kerze `t-1` wieder über dem Niveau schließt.
3. **Zielvorgaben** – der ursprüngliche Expert Advisor stellt fünf Gewinn-/Stopp-Layouts vor. Die folgende Tabelle zeigt die genaue Zuordnung, die in diesem Port erhalten bleibt.

| `TargetMode` | Langer Abzug | Langer Stopp | Langes Ziel | Kurzer Auslöser | Kurzer Stopp | Kurzes Ziel |
|-------------:|--------------|-----------|-------------|---------------|------------|--------------|
| 1 | `S1` | `S2` | `R1` | `R1` | `R2` | `S1` |
| 2 | `S1` | `S2` | `R2` | `R1` | `R2` | `S2` |
| 3 | `S2` | `S3` | `R1` | `R2` | `R3` | `S1` |
| 4 | `S2` | `S3` | `R2` | `R2` | `R3` | `S2` |
| 5 | `S2` | `S3` | `R3` | `R2` | `R3` | `S3` |

4. **Risikomanagement** – schützende Stop-Loss- und Take-Profit-Prüfungen werden bei jeder abgeschlossenen Kerze durchgeführt. Die optionale Trailing-Stop-Logik emuliert das MT4-Verhalten: Sobald der nicht realisierte Gewinn die konfigurierte Distanz überschreitet, wird der Stop zugunsten des Handels verschoben. Ein optionaler Ausstieg am Ende des Tages flacht die Position um 23:00 Uhr Bahnsteigzeit ab.

5. **Lautstärkeanpassung** – der MetaTrader-Eingang `isFloatLots` wird durch den `UseDynamicVolume`-Schalter gespiegelt. Wenn diese Option aktiviert ist, wird die Positionsgröße nach aufeinanderfolgenden Verlustgeschäften mithilfe der Eingaben `DrawdownFactor` und `RiskPercentage` reduziert.

## Parameter

| Name | Beschreibung | Standard |
|------|-------------|---------|
| `BaseVolume` | Mit jeder Marktorder übermitteltes Basisvolumen vor Risikoanpassungen. | `1` |
| `UseDynamicVolume` | Reduziert die Handelsgröße nach mehr als einem aufeinanderfolgenden Verlust. | `false` |
| `RiskPercentage` | Referenz-Risiko-pro-Trade-Verhältnis zur Skalierung des Basisvolumens (MetaTrader `MaxR`). | `0.02` |
| `DrawdownFactor` | Divisor wird angewendet, wenn das Volumen nach einer Pechsträhne verkleinert wird (MetaTrader `DcF`). | `3` |
| `TargetMode` | Wählt die oben aufgeführte Widerstands-/Unterstützungskombination aus (MetaTrader `TgtProfit`). | `1` |
| `TrailingPoints` | Trailing-Stop-Distanz, ausgedrückt in Instrumentenpunkten. Zum Deaktivieren auf `0` setzen. | `30` |
| `CloseAtSessionEnd` | Bei `true` werden alle Positionen zum Kerzenschluss um 23:00 Uhr geschlossen. | `false` |
| `LogSignals` | Druckt Pivot-Werte, Ein- und Ausgänge im Strategieprotokoll. | `false` |
| `CandleType` | Für die Analyse verwendeter Kerzendatentyp (standardmäßig 1-Stunden-Kerzen). | `TimeFrameCandleMessage(1h)` |

## Notizen

- Die Strategie gibt **Marktaufträge** genau wie das Original EA aus und platziert keine ausstehenden Aufträge.
- Stop-Loss- und Take-Profit-Ereignisse werden mit Marktausstiegen ausgeführt, um mit allen StockSharp-Konnektoren kompatibel zu bleiben.
- Nachlaufentfernungen hängen vom Instrument `PriceStep` ab. Fehlt die Stufe, wird der Nachlaufmechanismus automatisch deaktiviert.
- Das E-Mail-Benachrichtigungsflag der MT4-Version wird durch `LogSignals` dargestellt und erzeugt Protokollnachrichten anstelle von E-Mails.
