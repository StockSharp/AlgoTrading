# Close-Agent-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Close Agent Strategy ist ein Risikomanagement-Assistent, der den CloseAgent-Expertenberater MQL widerspiegelt. Die Strategie eröffnet keine neuen Trades. Stattdessen überwacht es bestehende Positionen und schließt sie, wenn der Preis über die Bollinger-Bänder hinausgeht, während der Relative-Stärke-Index (RSI) extreme Zonen erreicht. Das Tool kann nach manuell oder durch andere automatisierte Strategien erstellten Positionen Ausschau halten und optional alles auflösen, sobald ein globales Gewinnziel erreicht ist.

## Indikatoren und Daten
- **Kerzen:** konfigurierbarer Zeitrahmen (Standard: 5 Minuten), der zur Berechnung von Indikatoren verwendet wird.
- **Bollinger Bänder (Länge 21, Breite 2):** erkennt Preisausschläge über das obere Band oder unter das untere Band.
- **Relative Strength Index (Länge 13):** bestätigt, ob der Markt überkauft (>70) oder überverkauft (<30) ist.
- **Level1-Daten:** erfasst die neuesten Geld- und Briefkurse, um die Ausstiegsbedingungen so genau wie möglich zu bewerten.

## Parameter
- **Schließmodus (`CloseMode`):** wählt aus, welche Positionen zum Schließen berechtigt sind.
  - `Manual` – nur Positionen ohne diese Strategiekennung (manuelle Trades oder andere Bots).
  - `Auto` – nur von dieser Strategieinstanz eröffnete Positionen.
  - `Both` – Überwachen Sie jede Position auf dem Strategiesymbol.
- **Kerzentyp (`CandleType`):** Zeitrahmen, der zur Berechnung der Bollinger-Bänder und RSI verwendet wird.
- **Betriebsmodus (`OperationMode`):**
  - `LiveBar` – die zuletzt entstehende Kerze verwenden; reagiert schneller, verwendet jedoch möglicherweise unfertige Daten.
  - `NewBar` – wartet auf das Schließen einer Kerze, bevor ein Signal generiert wird (sicherer, aber langsamer).
- **Alle Ziele schließen (`CloseAllTarget`):** Wenn der variable Gewinn (`PnL`) diesen absoluten Wert erreicht, wird jede überwachte Position sofort geschlossen.
- **Benachrichtigungen aktivieren (`EnableAlerts`):** Wenn der Wert „true“ ist, wird jedes Mal eine Nachricht protokolliert, wenn ein Exit ausgelöst wird, einschließlich der Schätzung des realisierten Gewinns.

## Handelslogik
1. Abonniert Level1-Kurse und die konfigurierte Kerzenserie. Bollinger-Bänder und RSI werden für jede eingehende Kerze aktualisiert.
2. Behält einen kompakten Verlaufspuffer bei, damit die Strategie auf die zuletzt geschlossene Kerze verweisen kann, wenn `OperationMode` auf `NewBar` gesetzt ist.
3. Überprüft, ob das globale Gewinnziel erreicht wird. Wenn `CloseAllTarget` > 0 und `PnL` den Schwellenwert überschreitet, werden alle zulässigen Positionen zu Marktpreisen reduziert.
4. Für jede überwachte Position auf dem Strategiesymbol:
   - **Long-Positionen:** geschlossen, wenn das beste Gebot über dem oberen Bollinger-Band liegt, RSI über 70 liegt und der Preis über dem Einstiegsdurchschnittspreis bleibt.
   - **Short-Positionen:** geschlossen, wenn der beste Brief unter dem unteren Bollinger-Band liegt, RSI unter 30 liegt und der Preis unter dem Einstiegsdurchschnittspreis bleibt.
5. Verwendet Geld-/Briefkurse, sofern verfügbar; Andernfalls wird auf den zuletzt verarbeiteten Kerzenschluss zurückgegriffen, um verpasste Ausgänge zu vermeiden.

## Nutzungshinweise
- Die Strategie ist als Schutzschicht konzipiert und geht davon aus, dass Positionen möglicherweise extern eröffnet werden.
- Da die Logik nur bei Marktaustritten wirkt, sollte die Strategie parallel zu anderen Handelssystemen laufen, um das Risikorisiko zu steuern.
- Benachrichtigungen werden im Designer-Protokoll angezeigt, wenn `EnableAlerts` aktiv ist und mit den ursprünglichen MQL-Benachrichtigungen übereinstimmt.
