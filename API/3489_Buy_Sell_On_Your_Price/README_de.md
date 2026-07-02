# BuySellOnYourPrice-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
- Konvertiert den MetaTrader-Expertenberater **BuySellonYourPrice.mq5** (ID 35391) in den StockSharp-High-Level-API.
- Sendet beim Start genau eine Order, entsprechend der ursprünglichen Logik, die keine aktiven Orders oder Positionen erfordert.
- Unterstützt Markt-, Limit- und Stop-Einträge mit optionalen Stop-Loss- und Take-Profit-Levels, ausgedrückt als absolute Preise.
- Konfiguriert automatisch StockSharp Schutzaufträge, wenn gültige Stop-Loss-/Take-Profit-Abstände aus den bereitgestellten Preisniveaus abgeleitet werden können.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `Mode` | Zu übermittelnder Auftragstyp (Keine, Kaufen, Verkaufen, BuyLimit, SellLimit, BuyStop, SellStop). | `None` |
| `OrderVolume` | Volumen für die generierte Bestellung. | `1` |
| `EntryPrice` | Preis für ausstehende Orders; wird bei Marktaufträgen ignoriert. | `0` |
| `StopLossPrice` | Absolutes Preisniveau für den Stop-Loss. | `0` |
| `TakeProfitPrice` | Absolutes Preisniveau für den Take-Profit. | `0` |

## Handelslogik
1. Wenn die Strategie startet, prüft sie Folgendes:
   - Es wurde ein gültiger `Mode` ausgewählt, der sich von `None` unterscheidet.
   - `OrderVolume` ist positiv.
   - Es gibt keine aktuelle Position und keine aktiven Aufträge. Wenn eines davon vorhanden ist, wird die Bestellung nicht gesendet (dasselbe wie beim Einchecken von `OrdersTotal()==0` und `PositionsTotal()==0` bei MQL).
2. Der Eintrittspreis steht fest:
   - Marktmodi verwenden den besten Geld-/Briefkurs und greifen auf den letzten Preis oder `EntryPrice` zurück, wenn noch keine Marktdaten verfügbar sind.
   - Ausstehende Modi erfordern `EntryPrice > 0`.
3. Schutzabstände werden aus den vorgegebenen Stop-Loss- und Take-Profit-Werten abgeleitet. Nur gültige, positive Abstände werden an `StartProtection` übergeben, um die EA-Parameter zu emulieren.
4. Der ausgewählte Auftragstyp wird genau einmal gesendet (`BuyMarket`, `SellLimit`, `BuyStop` usw.) und es werden Informationsprotokolle erstellt, die die Aktion widerspiegeln.

## Unterschiede zum Original EA
- Die Protokollierung erfolgt über `AddInfoLog` statt über `Print`.
- Schutzaufträge werden über `StartProtection` registriert, wenn sowohl der Einstiegspreis als auch der Stop-Loss/Take-Profit die Berechnung einer positiven Distanz zulassen.
- Die Marktpreisauflösung verwendet aktuelle Level1-Daten (`BestBid`, `BestAsk`, `LastPrice`) und verschiebt die Auftragsübermittlung, wenn noch kein Angebot verfügbar ist.

## Nutzungshinweise
- Weisen Sie vor Beginn der Strategie das gewünschte Wertpapier zu und stellen Sie sicher, dass Level1-Daten für Marktaufträge verfügbar sind.
- Legen Sie `EntryPrice`, `StopLossPrice` und `TakeProfitPrice` in absoluten Zahlen fest, wenn Sie ausstehende Orders verwenden.
- Belassen Sie `Mode` auf `None`, um den Handel zu deaktivieren, ohne die Strategie aus der Umgebung zu entfernen.
