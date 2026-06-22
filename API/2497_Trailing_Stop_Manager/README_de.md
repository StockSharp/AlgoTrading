# Trailing Stop Manager-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie recreiert den Trailing-Stop-Controller des MetaTrader Expert Advisors `MQL/17263/TrailingStop.mq5`. Sie konzentriert sich auf die Automatisierung des Stop-Loss-Managements, nachdem eine Position bereits eröffnet wurde.

## Ursprüngliche Idee
- **Quelle**: Vladimir Karputovs TrailingStop Expert für Hedging-Konten.
- **Konzept**: Beim ersten Tick öffnete der EA sowohl Long- als auch Short-Positionen, dann wurden die Stop-Loss-Levels unabhängig für jede Seite mit pip-basierten Abständen nachgezogen.
- **Ziel**: Demonstrieren, wie Stops mit einer konfigurierbaren Aktivierungsdistanz und einem Aktualisierungsschritt nachgezogen werden.

## StockSharp-Anpassung
- **Netting-Kompatibilität**: StockSharp-Strategien operieren auf der Netto-Position, daher verwaltet dieser Port eine Richtung auf einmal. Um beide Seiten gleichzeitig zu verfolgen, starten Sie zwei Strategieinstanzen.
- **Tick-basierte Updates**: Die Strategie abonniert Trade-Ticks (`DataType.Ticks`), um die tick-gesteuerten Anpassungen von MetaTrader zu spiegeln.
- **Pip-Konvertierung**: Multipliziert die konfigurierten Pip-Werte mit `Security.PriceStep` (fällt auf 1 zurück, wenn die Börse keinen Step bereitstellt), um Eingaben in absolute Preisoffsets umzuwandeln.
- **Optionaler Auto-Einstieg**: Ein Parameter ermöglicht es, beim Start eine sofortige Market Order zu senden, was für schnelle Demonstrationen oder manuelle Tests praktisch ist.

## Handelslogik
1. **Start-up**
   - Liest den Instrument-Preisschritt und abonniert Tick-Daten.
   - Schickt optional eine Market Order gemäß dem `Initial Direction`-Parameter.
2. **Einstiegsverfolgung**
   - Jeder eigene Trade setzt den Trailing-Zustand zurück und speichert den tatsächlichen Ausführungspreis als neue Referenz.
3. **Aktivierung**
   - Bei Long-Positionen aktiviert sich der Trailing-Motor erst, wenn der Preis `Trailing Stop (pips)` vom Einstieg vorrückt. Bei Shorts ist ein gleichwertiger Rückgang erforderlich.
4. **Stop-Anpassung**
   - Nach der Aktivierung entspricht das Stop-Level dem aktuellen Tick-Preis minus/plus der Aktivierungsdistanz.
   - Der Stop wird nur bewegt, wenn der neueste Tick ihn mindestens `Trailing Step (pips)` vorwärts schiebt.
   - Ein Null-Schritt bedeutet, dass der Stop bei jedem günstigen Tick aktualisiert wird.
5. **Ausstieg**
   - Wenn der Preis zum Trailing-Level zurückkehrt oder darüber hinausgeht, schließt die Strategie die verbleibende Position mit einer Market Order.

## Parameter
| Name | Beschreibung |
| --- | --- |
| **Trailing Stop (pips)** | Aktivierungsdistanz in Pips. Muss größer als null sein. |
| **Trailing Step (pips)** | Minimale günstige Bewegung in Pips, bevor der Stop erneut vorgerückt wird. Kann null sein. |
| **Initial Direction** | Optionale Market Order, die während `OnStarted` platziert wird (`None`, `Long`, `Short`). |

## Weitere Hinweise
- Der ursprüngliche Expert verwendete Bid/Ask-Werte. Diese C#-Version verwendet den letzten Trade-Preis als gute Annäherung, die für die meisten liquiden Instrumente ausreichend ist.
- Es ist keine Take-Profit- oder Neueinstiegslogik enthalten. Sie können diese Komponente mit einer anderen Signalstrategie kombinieren oder sie nach dem manuellen Öffnen einer Position starten.
- Wenn der Broker Fraktions-Pip-Schritte bereitstellt, stellen Sie sicher, dass `Security.PriceStep` diese widerspiegelt; andernfalls passen Sie die Pip-Werte an die tatsächliche Tick-Größe an.
- Es gibt keine automatisierten Tests für dieses Modul, also validieren Sie auf einem Demo-Feed, bevor Sie echtes Kapital einsetzen.
