# BHS System Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Das BHS System ist ein Ausbruchsansatz, der den ursprünglichen MetaTrader 5 Expert Advisor in die StockSharp High-Level-API konvertiert. Die Strategie beobachtet die Beziehung zwischen Preis und einem Kaufman Adaptive Moving Average (AMA). Wenn der aktuelle Balken über dem AMA schließt, bereitet sich das System auf einen bullischen Ausbruch vor; wenn der Schluss unter dem AMA liegt, bereitet es sich auf eine bärische Expansion vor. Anstatt sofort einzusteigen, wartet der Algorithmus, bis der Preis vordefinierte „runde Zahl"-Niveaus berührt, und sendet Stop-Orders an diesen Niveaus. Dies hält das Verhalten der portierten Strategie identisch mit der MQL-Version, bei der ausstehende Orders immer an gerundeten Preisgrenzen ausgerichtet waren.

## Handelslogik

1. Bei jeder abgeschlossenen Kerze berechnet die Strategie das nächste höhere und nächste niedrigere Rundzahl-Preisniveau. Die Rundung verwendet den benutzerdefinierte Schritt (in Punkten) und den Instrumentenpreisschritt, um exakte börsenkonforme Auslösepreise zu erzeugen.
2. Der vorherige AMA-Wert (um einen Balken verschoben, wie in der ursprünglichen MQL-Implementierung) wird mit dem aktuellen Kerzenschluss verglichen.
3. Wenn keine offene Position und keine aktive Einstiegsorder existiert:
   - Wenn Schluss > AMA, wird ein Buy-Stop am gerundeten Deckel-Niveau platziert.
   - Wenn Schluss < AMA, wird ein Sell-Stop am gerundeten Boden-Niveau platziert.
4. Ausstehende Orders verfallen automatisch nach der konfigurierten Stundenanzahl. Dies spiegelt das Lebensdauer-Feld der MT5-Order-Anfrage wider.
5. Wenn eine Einstiegsorder ausgeführt wird, wird die entgegengesetzte ausstehende Order storniert und eine schützende Stop-Order mit der ausgewählten Stop-Loss-Distanz registriert. Das System überwacht dann die Preisbewegung und bewegt den Stop gemäß den Trailing-Parametern.
6. Trailing-Stops werden nur angepasst, wenn der Preis mindestens die Trailing-Distanz plus den Trailing-Schritt vorgerückt ist. Dies vermeidet ständige Modifikationen und spiegelt die diskrete Trailing-Logik im MT5-Code wider.

## Risikomanagement

- **Initialer Stop-Loss:** Separate punktbasierte Abstände für Long- und Short-Trades werden in absolute Preisoffsets konvertiert und sofort nach dem Einstieg für schützende Stop-Orders verwendet.
- **Trailing-Stop:** Long- und Short-Positionen haben unabhängige Trailing-Abstände. Stops werden nur aktualisiert, wenn der neue Stop mindestens um den Trailing-Schritt verbessert, um Mikro-Anpassungen in ruhigen Märkten zu verhindern.
- **Order-Ablauf:** Beide Einstiegsorders speichern ihre Erstellungszeit. Wenn die Order nach der angegebenen Stundenanzahl noch aktiv ist, wird sie storniert, um abgelaufenes ausstehende Engagement zu vermeiden.

## Parameter

- `OrderVolume` – Losgröße, die sowohl für Einstiege als auch für Schutzorders verwendet wird.
- `StopLossBuyPoints` / `StopLossSellPoints` – Stop-Loss-Abstand in Punkten für Long- bzw. Short-Positionen.
- `TrailingStopBuyPoints` / `TrailingStopSellPoints` – Trailing-Stop-Abstand für Long- und Short-Positionen, ausgedrückt in Punkten.
- `TrailingStepPoints` – zusätzliche Lücke (in Punkten), die erforderlich ist, bevor der Trailing-Stop wieder verbessert werden kann.
- `RoundStepPoints` – Anzahl der Punkte, die beim Erstellen gerundeter Auslöseniveaus verwendet werden.
- `ExpirationHours` – Lebensdauer einer ausstehenden Einstiegsorder. Bei null verfallen Orders niemals automatisch.
- `AmaLength`, `AmaFastPeriod`, `AmaSlowPeriod` – Parameter des Kaufman Adaptive Moving Average, der als Richtungsfilter verwendet wird.
- `CandleType` – Datentyp/Zeitrahmen der Kerzen, die die Strategie antreiben.

## Implementierungshinweise

- Die Strategie verwendet den `KaufmanAdaptiveMovingAverage`-Indikator von StockSharp und einen dateiweiten Namespace, der mit den Repository-Richtlinien übereinstimmt.
- Alle Handelsvorgänge beruhen auf High-Level-API-Helfern (`BuyStop`, `SellStop`, `CancelOrder`) und keine Indikatorwerte werden durch `GetValue`-Aufrufe abgerufen.
- Chart-Unterstützung ist aktiviert: Das Abonnement zeichnet Kerzen, die AMA-Linie und eigene Trades, wenn ein Chart-Kontext verfügbar ist.
- Die Schutzlogik ist in einer einzigen Stop-Order-Referenz konsolidiert, sodass der Trailing-Mechanismus den ursprünglichen Stop wiederverwendet, anstatt zusätzliche Orders zu spawnen.
- Die Konvertierung hält Kommentare auf Englisch und bewahrt das Verhalten der ursprünglichen MQL-Trailing-Routine durch Verwendung derselben Schwellenwertprüfungen.
