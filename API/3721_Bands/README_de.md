# Bands-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie portiert den MetaTrader 5 Expert Advisor **Bands.mq5** auf den StockSharp High-Level API. Es wartet auf eine fertige Kerze
das die Bollinger-Bänder von außen zurück in den Kanal durchdringt und erst dann eine Position öffnet, wenn die Donchian-Kanal-Konf
Dies bedeutet, dass die Bandsteigung über eine konfigurierbare Anzahl von Balken hinweg stabil war. Die Vielfachen des durchschnittlichen wahren Bereichs (ATR) reproduzieren den Ori
Die anfänglichen Stop-Loss- und Take-Profit-Abstände werden ermittelt, während ein optionaler Regressions-Tracker den Bestimmungskoeffizienten der Equity-Kurve ausgibt
(R-Quadrat) alle 100 Trades, was die Diagnoseausgabe der MQL-Version widerspiegelt.

## Handelslogik
1. Abonnieren Sie einen einzelnen Kerzenstrom und berechnen Sie Bollinger-Bänder, einen Donchian-Kanal und ATR mit denselben Perioden wie das MetaT
Rader-Roboter.
2. Wenn keine Position offen ist, überprüfen Sie die **vorherige** abgeschlossene Kerze:
   - Geben Sie „Long“ ein, wenn die Kerze unterhalb des unteren Bollinger-Bandes eröffnet und darüber geschlossen hat und das untere Donchian-Band nicht gesunken ist
für mehr als `ConfirmationPeriod` Balken vorgesehen.
   - Geben Sie Short ein, wenn die Kerze über dem oberen Bollinger-Band geöffnet und darunter geschlossen hat und das obere Donchian-Band nicht gestiegen ist
de für mehr als `ConfirmationPeriod` Balken.
3. Wenn eine Position vorhanden ist, beenden Sie die Position, wenn entweder die nachfolgende Donchian-Grenze überschritten wird (unter Verwendung des vorherigen Schlusskurses) oder wenn die ATR-Basis
d Schutzniveaus werden intrabar verletzt.
4. Bei jedem ausgeführten Trade wird das aktuelle Portfolio-Eigenkapital gespeichert und nach jedem Block die lineare Regressions-R-Quadrat-Metrik ausgedruckt
100 Trades. Eine negative Steigung erzeugt ein negatives R-Quadrat, genau wie beim ursprünglichen Expertenberater.

## Risikomanagement
- Einstiegsaufträge werden immer am Markt mit dem benutzerdefinierten `TradeVolume` gesendet.
- Schutzniveaus werden im Code neu erstellt (anstatt ausstehende Aufträge zu verwenden), indem Kerzenhochs und -tiefs mit dem ATR mu verglichen werden
Tipps.
- Wenn der Stop-Loss oder Take-Profit ausgelöst wird, schließt die Strategie die gesamte Position mit einer Marktorder und setzt den Schutz zurück
auf Ebenen.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `TradeVolume` | Nettovolumen (in Lots) für jede Marktorder. |
| `CandleType` | Kerzendatentyp/Zeitrahmen, der für alle Indikatoren verwendet wird. |
| `BollingerPeriod` | Anzahl der von den Bollinger-Bändern verwendeten Kerzen. |
| `BollingerDeviation` | Auf die Bollinger-Bänder angewendeter Standardabweichungsmultiplikator. |
| `DonchianPeriod` | Länge des Kanals Donchian, der als Trendfilter verwendet wird. |
| `ConfirmationPeriod` | Mindestanzahl aufeinanderfolgender Balken, bei denen die Donchian-Steigung nicht abnehmend (lang) oder nicht ansteigend (kurz) bleiben muss. |
| `AtrPeriod` | Zeitraum der durchschnittlichen wahren Spanne, der für das Risikomanagement verwendet wird. |
| `StopAtrMultiplier` | ATR Vielfaches, das die Stop-Loss-Distanz definiert. |
| `TakeAtrMultiplier` | ATR Vielfaches, das die Take-Profit-Distanz definiert. |

## Notizen
- Die Donchian-Steigungsprüfung wird als rollierender Zähler implementiert, anstatt Indikatorpuffer zu kopieren, wodurch die StockSharp erhalten bleibt.
versioneffizient und entspricht gleichzeitig dem Verhalten des Originals EA.
- Alle Kommentare und Diagnosen werden gemäß den Projektrichtlinien auf Englisch bereitgestellt.
- Money-Management-Helfer aus dem MetaTrader-Code werden nicht reproduziert; Die StockSharp-Implementierung basiert auf der `TradeVolume`
Parameter für die Positionsgröße.
