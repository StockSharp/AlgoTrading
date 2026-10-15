# OCO Schwebende Aufträge-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **OCO Schwebende Aufträge-Strategie** repliziert das Verhalten des MetaTrader4 Expert Advisors `OCO_EA.mq4` innerhalb der StockSharp High-Level-API. Der Algorithmus ermöglicht es einem Trader, bis zu vier unabhängige Preisauslöser zu aktivieren (Buy Limit, Buy Stop, Sell Limit, Sell Stop). Wann immer das live beste Geld oder Brief den konfigurierten Preisniveau berührt, sendet die Strategie sofort eine Marktorder und storniert optional alle anderen ausstehenden Auslöser auf klassische "One-Cancels-the-Others" (OCO)-Weise.

Die Strategie basiert ausschließlich auf Level-1-Marktdaten – keine historischen Indikatoren sind erforderlich. Sie ist für diskretionäre oder halbautomatisierte Handelsabläufe gedacht, bei denen Trader manuell Preisniveaus definieren und möchten, dass die Plattform ausführt, sobald das Niveau erreicht wird, während sie auch schützende Ausstiegsorders anhängt.

## Handelslogik
1. Der Trader setzt eine beliebige Kombination der vier Auslöserpreise und schaltet den Parameter **Armed** auf `true`.
2. Die Strategie abonniert Level-1-Updates und hält das neueste beste Geld und Brief im Speicher.
3. Bei jedem Update werden die gespeicherten Preise mit den konfigurierten Schwellen verglichen:
   - Wenn der beste Brief *kleiner oder gleich* dem **Buy Limit**-Preis ist, wird eine Markt-Kauforder mit dem konfigurierten Volumen gesendet.
   - Wenn der beste Brief *größer oder gleich* dem **Buy Stop**-Preis ist, wird eine Markt-Kauforder gesendet.
   - Wenn das beste Geld *größer oder gleich* dem **Sell Limit**-Preis ist, wird eine Markt-Verkaufsorder gesendet.
   - Wenn das beste Geld *kleiner oder gleich* dem **Sell Stop**-Preis ist, wird eine Markt-Verkaufsorder gesendet.
4. Nach jedem ausgeführten Auslöser wird das entsprechende Niveau gelöscht (auf null zurückgesetzt). Wenn **Use OCO Link** aktiviert ist, werden alle anderen Niveaus sofort gelöscht, was das ursprüngliche MT4-Verhalten widerspiegelt. Wenn der OCO-Link deaktiviert ist, bleiben andere Niveaus aktiv, bis sie auslösen oder manuell gelöscht werden.
5. Wenn alle Auslöserpreise null sind, deaktiviert sich die Strategie automatisch, indem sie **Armed** auf `false` zurückschaltet.

Alle Ausführungen werden mit `BuyMarket`- und `SellMarket`-Aufrufen durchgeführt, um sofortige Füllungen zu gewährleisten, die das in der StockSharp-Umgebung konfigurierte Börsenrouting respektieren. Informative Protokolleinträge werden für jeden Auslöser erstellt, um die Überwachung zu vereinfachen.

## Parameter
- **Order volume** – Volumen, das mit jeder Marktorder gesendet wird. Der Wert muss positiv sein.
- **Buy limit price** – Ask-Preisschwelle, die einen limitartigen Long-Einstieg aktiviert. Auf `0` setzen zum Deaktivieren.
- **Buy stop price** – Ask-Preisschwelle, die einen stopartigen Long-Einstieg aktiviert. Auf `0` setzen zum Deaktivieren.
- **Sell limit price** – Bid-Preisschwelle, die einen limitartigen Short-Einstieg aktiviert. Auf `0` setzen zum Deaktivieren.
- **Sell stop price** – Bid-Preisschwelle, die einen stopartigen Short-Einstieg aktiviert. Auf `0` setzen zum Deaktivieren.
- **Stop loss (pips)** – Abstand in Instrument-Punkten für den Schutz-Stop. In Preis umgerechnet durch Multiplikation mit `Security.PriceStep` (Fallback `1`, wenn das Instrument keine Tick-Größe meldet).
- **Take profit (pips)** – Abstand in Instrument-Punkten für das Gewinnziel. Dieselbe Konvertierungslogik wie für den Stop Loss wird verwendet.
- **Use OCO link** – wenn `true`, löscht die erste gefüllte Order die verbleibenden Preisniveaus und deaktiviert die Strategie. Wenn `false`, bleiben verbleibende Niveaus aktiv, bis sie einzeln auslösen.
- **Armed** – Sicherheitsschalter, der die Handelslogik aktiviert oder deaktiviert. Die Strategie setzt ihn automatisch auf `false` zurück, wenn keine aktiven Auslöser-Niveaus verbleiben.

## Risikomanagement
`StartProtection` wird während `OnStarted` aktiviert und hängt absolute Preis-Stop-Loss- und Take-Profit-Offsets an jede offene Position an. Die Offsets werden aus den Parametern **Stop loss (pips)** und **Take profit (pips)** unter Verwendung der Instrument-Tick-Größe abgeleitet. Schutzorders werden immer als Marktorders gesendet, um die Ausstiegsausführung auch dann zu garantieren, wenn das zugrunde liegende Instrument illiquide ist.

Da die Strategie rein ereignisgesteuert ist, unterhält sie keine ausstehenden Limit-Orders an der Börse; sie reagiert auf Marktdaten und sendet Marktorders, genau wie die ursprüngliche MQL-Version, die sofortige Orders ausgab und sie dann modifizierte, um Stop-Loss- und Take-Profit-Abstände anzuwenden.

## Verwendungstipps
1. Das Wertpapier, Portfolio und die Verbindung innerhalb von StockSharp wie üblich konfigurieren.
2. **Order volume** auf die gewünschte Lot-Größe setzen.
3. Einen beliebigen Teilsatz der Auslöserpreise eingeben und **Armed** auf `true` umschalten. Auf `0` belassene Werte werden ignoriert.
4. Optional **Use OCO link** deaktivieren, um verbleibende Auslöser nach der ersten Füllung aktiv zu halten.
5. Das Protokoll auf Nachrichten überwachen, die jeden Auslöser und den automatischen Rücksetzzustand bestätigen.

Denken Sie daran, dass die Strategie den vom Broker bereitgestellten Preisschritt verwendet. Wenn das Handelsinstrument in Bruchpips quotiert oder unkonventionelle Tick-Größen verwendet, passen Sie die pip-basierten Abstände entsprechend an, bevor Sie die Strategie aktivieren.

## Unterschiede zum originalen MQL-Skript
- Die Strategie verwendet den StockSharp `StartProtection`-Helfer anstatt Orders manuell zu modifizieren, um Stop-Loss- und Take-Profit-Niveaus anzuwenden.
- Level-1-Datenabonnements werden über High-Level-Bindings statt manuellem Polling der Werte `Bid`, `Ask`, `High` und `Low` verarbeitet.
- Parameter werden über `StrategyParam<T>` exponiert, damit sie direkt in der StockSharp-UI angepasst und optimiert werden können.
- Protokollierung ersetzt die MT4 `Comment`- und `PlaySound`-Benachrichtigungen und bietet Ausführungstransparenz innerhalb der StockSharp-Protokolle.
