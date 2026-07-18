# Bago EA Klassische Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine originalgetreue StockSharp-Portierung des MetaTrader-Experten von `MQL/7656/Bago_ea.mq4`. Dabei bleibt die ursprüngliche Trendfolge-Philosophie erhalten: Einstiege werden nur ausgelöst, wenn exponentielle gleitende Durchschnitte und RSI die neutrale Zone in die gleiche Richtung durchbrechen, während der Vegas-Tunnel als räumlicher Filter und als Anker für schrittweises Nachlaufen fungiert.

## Handelslogik im Detail

1. **Indikatorstapel**
   - Schnelle und langsame EMAs (`FastPeriod`/`SlowPeriod`, gemeinsame Methode `MaMethod`, angewendeter Preis `MaAppliedPrice`).
   - Vegas-Tunnel-EMAs mit den festen Perioden 144 und 169 verwenden dieselben Einstellungen, um die Tunnelhüllkurven zu emulieren.
   - RSI (`RsiPeriod`, `RsiAppliedPrice`) mit der klassischen 50-Ebene als Bestätigungsfilter.
   - Kerzendaten stammen von `CandleType`; Derselbe Kerzen-Feed versorgt alle Indikatoren über die `Bind`-Pipeline auf hoher Ebene.
2. **Zustandsübergreifende Maschine**
   - EMA- und RSI-Überschreitungen über/unter ihren Schwellenwerten setzen boolesche Flags und Balkenzähler. Jeder Zustand läuft nach `CrossEffectiveBars` abgeschlossenen Kerzen ab oder wenn das entgegengesetzte Kreuz erscheint, genau wie die Timer aus der MQL-Version.
   - Zusätzliche Tunnel-Flags verfolgen, wann der Schlusskurs von einer Seite des Vegas-Tunnels zur anderen springt, sodass die nachfolgende Logik weiß, welches Regime anzuwenden ist.
3. **Sitzungstor**
   - Der Handel ist nur während ausgewählter Marktsitzungen gestattet: London (07–16), New York (12–21) und Tokio (00–08 plus die 23:00-Uhr-Bar). Diese Fenster replizieren die `extern bool`-Schalter im ursprünglichen EA.
4. **Eingabefilter**
   - **Long**: erfordert sowohl EMA-up- als auch RSI-up-Flags und entweder einen bullischen Schlusskurs über dem Tunnel um mindestens `TunnelBandWidthPips`, aber nicht weiter als `TunnelSafeZonePips`, oder einen Retracement-Schlusskurs unterhalb des Tunnels um `TunnelBandWidthPips`, der einen Absprung signalisiert.
   - **Kurz**: gespiegelte Bedingungen mit EMA-down/RSI-down und symmetrischen Tunnelprüfungen.
   - Wenn eine Umkehrposition offen ist, wird sie von der Strategie zum Marktwert geschlossen, bevor die neue Richtung eingeschlagen wird, wobei `OrderClose` von MetaTrader nachgeahmt wird.
5. **Positions- und Exit-Management**
   - Der anfängliche Stop-Loss wird `StopLossPips` vom Einstieg entfernt platziert. Immer wenn der Preis um den Vegas-Tunnel herum einbricht, wird die Haltestelle mithilfe eines zusätzlichen Puffers `StopLossToFiboPips` verschoben, um den „Fibo“-Offsets des Experten zu entsprechen.
   - Nachfolgende Schritte entsprechen den TP-Ebenen aus dem EA. Wenn sich der Preis vom Tunnel entfernt, parkt die Strategie den Stopp zunächst in der Nähe des Tunnels ±(`TrailingStepX` + `StopLossToFiboPips`) und wechselt dann allmählich zu einem reinen Preisverfolgungs-Trailing von `TrailingStopPips`.
   - Teilausstiege (`PartialClose1Volume`, `PartialClose2Volume`) werden ausgeführt, sobald die Verschiebung `TrailingStep1Pips` und `TrailingStep2Pips` erreicht. Das verbleibende Volumen wird durch den Trailing Stop verwaltet, bis der dritte Schritt (`TrailingStep3Pips`) erreicht wird.
   - Jede entgegengesetzte EMA/RSI-Kreuzung schließt sofort die volle Position.
6. **Auftragsabwicklung**
   - Stop-Orders werden explizit über `SellStop`/`BuyStop`-Aufrufe verwaltet. Jedes Mal, wenn der Stopp verschoben werden muss, wird die vorherige Order storniert und eine neue übermittelt; Dies spiegelt die wiederholten `OrderModify`-Aufrufe vom MQL-Code wider.
   - Alle Pip-Berechnungen basieren auf dem Instrument `PriceStep` und passen sich automatisch an drei- oder fünfstellige Kurse an, indem der Schritt mit zehn multipliziert wird, genau wie die Punktumrechnung von MetaTrader.

## Parameter

| Parameter | Typ | Standard | Beschreibung |
|-----------|------|---------|-------------|
| `TradeVolume` | dezimal | 3 | Gesamtvolumen bei einem neuen Signal geöffnet. |
| `StopLossPips` | dezimal | 30 | Anfänglicher Schutzstoppabstand in Pips. |
| `StopLossToFiboPips` | dezimal | 20 | Zusätzlicher Puffer beim Bewegen von Haltestellen rund um die Vegas-Tunnelbänder. |
| `TrailingStopPips` | dezimal | 30 | Entfernung des harten Trailing-Stops, wenn der Preis den Tunnel verlässt. |
| `TrailingStep1Pips` | dezimal | 55 | Erste Gewinnschicht, abgeleitet vom TP1-Level von EA. |
| `TrailingStep2Pips` | dezimal | 89 | Zweite Gewinnschicht (TP2). |
| `TrailingStep3Pips` | dezimal | 144 | Dritte Gewinnschicht (TP3) vor dem Wechsel zum reinen Trailing. |
| `PartialClose1Volume` | dezimal | 1 | Volumen, das geschlossen werden soll, wenn `TrailingStep1Pips` erreicht ist. |
| `PartialClose2Volume` | dezimal | 1 | Volumen, das geschlossen werden soll, wenn `TrailingStep2Pips` erreicht ist. |
| `CrossEffectiveBars` | int | 2 | Anzahl der abgeschlossenen Kerzen, während die Kreuzflaggen gültig bleiben. |
| `TunnelBandWidthPips` | dezimal | 5 | Neutrale Zone rund um den Tunnel, in der neue Trades vermieden werden. |
| `TunnelSafeZonePips` | dezimal | 120 | Maximaler Abstand vom Tunnel, der noch einen Ausbruchseintritt ermöglicht. |
| `EnableLondonSession` | bool | wahr | Ermöglichen Sie den Handel zwischen 07:00 und 16:00 Uhr Börsenzeit. |
| `EnableNewYorkSession` | bool | wahr | Ermöglichen Sie den Handel zwischen 12:00 und 21:00 Uhr Börsenzeit. |
| `EnableTokyoSession` | bool | falsch | Ermöglichen Sie den Handel zwischen 00:00 und 08:00 Uhr und der 23:00-Uhr-Kerze. |
| `FastPeriod` | int | 5 | Schnelle Länge von EMA. |
| `SlowPeriod` | int | 12 | Langsame Länge von EMA. |
| `MaShift` | int | 0 | Horizontale Verschiebung der gleitenden Durchschnitte. |
| `MaMethod` | `MovingAverageType` | Exponentiell | Berechnungsmodus EMA (zum Experimentieren konfigurierbar gehalten). |
| `MaAppliedPrice` | `AppliedPriceType` | Schließen | Kerzenpreis wird an die EMAs weitergeleitet. |
| `RsiPeriod` | int | 21 | RSI Mittelungszeitraum. |
| `RsiAppliedPrice` | `AppliedPriceType` | Schließen | Kerzenpreis wird an RSI weitergeleitet. |
| `CandleType` | `DataType` | H1-Zeitrahmen | Kerzenserien, die die Strategie vorantreiben. |

## Implementierungshinweise

- Die Strategie läuft vollständig auf dem High-Level-Kerzenabonnement API und behält Indikatorwerte in rollierenden Listen bei, um die Balkenindizierung (`Close[1]`, `Close[2]`) aus dem ursprünglichen Skript zu imitieren.
- Timer und Tunnelflags reproduzieren die Finite-State-Maschine, die bestimmt, ob ein Kreuz noch „frisch“ ist.
- `StartProtection()` wird auf `OnStarted` aufgerufen, sodass die integrierten Risikokontrollen von StockSharp die offene Position genau wie der harte Stop-Loss von MetaTrader überwachen.
- Inline-Kommentare sind auf Englisch verfasst und beschreiben jeden Schritt der Konvertierung, um die Wartung zu erleichtern.
