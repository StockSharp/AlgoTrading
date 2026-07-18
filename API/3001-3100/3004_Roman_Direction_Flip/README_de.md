# Roman Richtungswechsel
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie recreiert den ursprünglichen MQL Expert Advisor, der als `roman.mq5` veröffentlicht wurde. Sie hält immer eine Position offen und wechselt die Trade-Richtung nur, nachdem der vorherige Trade geschlossen wurde. Solange die Position profitabel bleibt, wiederholt sie dieselbe Richtung; nach einem Stop-Loss wechselt die Strategie auf die entgegengesetzte Seite. Die StockSharp-Version arbeitet mit Level-1-Daten und verwendet beste Geld-/Briefkurse, um die pip-basierten Ausstiege von MetaTrader zu emulieren.

## Strategielogik
1. **Anfangsrichtung** – beim Start bestimmt der Parameter `StartWithBuy`, ob die erste Order ein Kauf oder Verkauf ist. Die Entscheidung wird in `_nextTradeBuy` gespeichert, damit sie zwischen Deals erhalten bleibt.
2. **Markteintritt** – wenn die Strategie flat ist und keine ausstehenden Orders vorhanden sind, reicht sie eine Marktorder in der vordefinierten Richtung ein. Für Kauforders wird der aktuelle beste Ask als Referenz-Eintrittspreis gespeichert, für Verkaufsorders wird der aktuelle beste Bid verwendet. Dies spiegelt die MetaTrader-Implementierung wider, wo Käufe zum Ask und Verkäufe zum Bid ausgeführt werden.
3. **Überwachung der offenen Position** – nachdem die Order gefüllt ist, hört die Strategie auf Level-1-Updates. Jedes Update liefert den neuesten Bid/Ask, damit der Algorithmus den unrealisierten Gewinn in Preisschritten (Pips) berechnen kann. Der `PriceStep` des Wertpapiers wird als Nenner verwendet, mit einem Fallback von `1`, wenn der Schritt unbekannt ist.
4. **Take-Profit-Regel** – wenn der unrealisierte Gewinn `TakeProfitSteps` erreicht oder überschreitet, wird die Position mit `ClosePosition()` geschlossen. Das Flag `_nextTradeBuy` behält denselben Wert bei, damit die nächste Order der soeben erfolgreichen Richtung folgt.
5. **Stop-Loss-Regel** – wenn der unrealisierte Verlust `StopLossSteps` erreicht oder überschreitet, wird die Position geschlossen und `_nextTradeBuy` umgeschaltet. Der folgende Trade tritt daher in der entgegengesetzten Richtung ein, entsprechend dem ursprünglichen EA-Verhalten, wo der boolesche `bs` bei einem Verlust wechselt.
6. **Order-Drosselung** – `_orderPending` verhindert, dass der Algorithmus mehrere Orders einreicht, während eine vorherige Anfrage noch verarbeitet wird. Das Flag wird in `OnPositionChanged` zurückgesetzt, nachdem die Positionsgröße aktualisiert wurde.

Diese einfache Sequenz hält die Strategie jederzeit investiert und wechselt die Richtung nur nach einem verlorenen Trade. Das System ähnelt daher einem trendfolge-artigen Schalter: nach einem Stop-Loss nimmt es an, dass sich der Trend geändert hat, und folgt der neuen Seite.

## Parameter
- `OrderVolume` *(decimal, Standard = 0.1)* – Menge, die mit jeder Marktorder gesendet wird. Auf die Kontraktgröße setzen, die für Live-Trading oder Simulationen benötigt wird.
- `TakeProfitSteps` *(int, Standard = 46)* – positive Anzahl von Preisschritten, die zum Auslösen des Take-Profits erforderlich sind. Schritte entsprechen `Security.PriceStep`, also auf einem Symbol mit 0.01 Tick-Größe entspricht der Standard 0.46 Preiseinheiten.
- `StopLossSteps` *(int, Standard = 31)* – maximale adverse Preisbewegung (in Schritten), bevor die Position geschlossen und die Richtung gewechselt wird.
- `StartWithBuy` *(bool, Standard = true)* – bestimmt, ob der erste Trade long (`true`) oder short (`false`) ist. Nachfolgende Trades hängen von den Ergebnissen vorheriger Positionen ab.

Jeder Parameter wird über `StrategyParam<T>` exponiert, unterstützt Optimierung (außer dem booleschen Schalter) und ist in der UI dank `SetDisplay`-Metadaten sichtbar.

## Daten- und Ausführungsdetails
- Abonniert `SubscribeLevel1()`, um beste Geld-/Briefkurse zu erhalten. Keine Kerzen- oder Indikatordaten erforderlich.
- Verwendet `BuyMarket`/`SellMarket` für Einstiege und `ClosePosition()` für Ausstiege, um die Logik nah an der MQL-Version zu halten, die auf sofortige Marktorders angewiesen war.
- Speichert den zuletzt bekannten Bid/Ask lokal, um die `_Point`-basierte Gewinnberechnung von MetaTrader zu imitieren.

## Risikomanagement
- Feste Take-Profit und Stop-Loss in Preisschritten garantieren, dass jeder Trade vordefinierte Ausstiegslevels hat.
- Der Richtungswechsel nach einem Verlust kann zu schnellem Wechseln in choppy Markets führen, daher sollte die Positionsgröße (`OrderVolume`) entsprechend der Risikobereitschaft des Kontos kalibriert werden.
- Da die Strategie fast immer eine Position hält, ist sie empfindlich gegenüber Overnight-Gaps und plötzlichen Kurssprüngen; externe Schutzmaßnahmen in Betracht ziehen wenn das ein Problem ist.

## Standardwerte
- `OrderVolume` = 0.1
- `TakeProfitSteps` = 46
- `StopLossSteps` = 31
- `StartWithBuy` = true

## Filter
- **Kategorie**: Trendfolge / Richtungsschalter
- **Richtung**: Beide (Long & Short)
- **Indikatoren**: Keine
- **Stops**: Ja (fixer Schritt-Take-Profit und Stop-Loss)
- **Komplexität**: Grundlegend
- **Zeitrahmen**: Tick / Level1-Kurse
- **Saisonalität**: Nein
- **Neuronale Netze**: Nein
- **Divergenz**: Nein
- **Risikolevel**: Hoch (immer im Markt)

## Hinweise
- Der ursprüngliche EA speicherte die nächste Richtung in einem Boolean namens `bs`. Der StockSharp-Port behält dieselbe Idee über `_nextTradeBuy` bei und fügt Order-Drosselung hinzu, um doppelte Einreichungen zu vermeiden.
- Die Preisschrittgranularität ist wichtig: wenn Ihr Instrument Bruchpips verwendet, passen Sie die Standardwerte so an, dass Gewinn-/Verlustbeträge die gewünschten Geldbeträge widerspiegeln.
