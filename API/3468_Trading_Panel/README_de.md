# Trading-Panel-Strategie (ID 3468)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **TradingPanelStrategy** ist ein manueller Ordereingabe-Assistent, der vom MQL5 Expert Advisor *EA_TradingPanel* konvertiert wurde. Es stellt programmatische Methoden zur Verfügung, die das ursprüngliche Diagrammfeld nachbilden: Eine einzige Aktion kann mehrere Marktaufträge übermitteln, automatisch Stop-Loss- und Take-Profit-Abstände in Pips anhängen und optional ein benutzerdefiniertes Wertpapier zum Handel auswählen. Die Standardwerte spiegeln die Quelle EA wider (ein Trade, 2-Pip-Stopp, 10-Pip-Take, 0,01 Volumen).

Im Gegensatz zum grafischen Panel konzentriert sich dieser StockSharp-Port auf automatisierungsfreundliche Einstiegspunkte. Aufrufer (z. B. eine benutzerdefinierte Benutzeroberfläche oder ein Skript) können bei Bedarf `PlaceBuyOrders()` oder `PlaceSellOrders()` auslösen, während die Strategie für die Volumennormalisierung, Preisrundung und die Platzierung von Schutzaufträgen sorgt.

## Parameter
| Name | Beschreibung | Notizen |
| ---- | ----------- | ----- |
| `TradeCount` | Anzahl der pro Aktion gesendeten Marktaufträge. | Stellt mindestens Null sicher. Standardwert `1`. |
| `StopLossPips` | Stop-Loss-Distanz in Pips. | Null deaktiviert die Stopp-Erstellung. Standardwert `2`. |
| `TakeProfitPips` | Take-Profit-Distanz in Pips. | Null deaktiviert die Zielerstellung. Standardwert `10`. |
| `VolumePerTrade` | Volumen für jede einzelne Marktorder. | Gerundet über `Security.VolumeStep`. Standard `0.01`. |
| `TargetSecurity` | Optionale Überschreibung für das gehandelte Instrument. | Fällt auf `Strategy.Security` zurück, wenn null. |

Alle Parameter werden über `StrategyParam<T>` verfügbar gemacht, sodass sie die Optimierung und Laufzeitneukonfiguration über die StockSharp-Benutzeroberfläche unterstützen.

## Ausführungsablauf
1. Lösen Sie die aktive Sicherheit auf (`TargetSecurity` oder `Strategy.Security`).
2. Leiten Sie die Pip-Größe aus Instrumentenmetadaten ab: `PriceStep` multipliziert mit 10, wenn das Instrument 3+ Dezimalstellen hat, identisch mit der MQL-Logik, die für Symbole mit 3 oder 5 Ziffern multipliziert.
3. Erhalten Sie den neuesten Referenzpreis (bester Geld-/Briefkurs, Rückfall auf den letzten Handel) und runden Sie ihn mit `Security.ShrinkPrice`.
4. Berechnen Sie das gewünschte Volumen: `TradeCount × VolumePerTrade`, richten Sie es an den Wechselkurslimits (`MinVolume`, `MaxVolume`, `VolumeStep`) aus und passen Sie es an eine entgegengesetzte offene Position an, sodass eine Aktion sowohl abflachen als auch umkehren kann.
5. Senden Sie eine Marktorder über `BuyMarket` oder `SellMarket`.
6. Erstellen Sie Schutzaufträge (Stop und Limit) mithilfe der Pip-Offsets, wiederum normalisiert auf die Tick-Größe der Börse.
7. Stornieren Sie veraltete Schutzaufträge, wenn die Position umkippt oder die Strategie stoppt.

## Logik der Schutzanordnung
- Bei langen Einträgen wird ein `SellStop` für den Stop-Loss und ein `SellLimit` für den Take-Profit gesetzt.
- Bei Short-Einträgen wird ein `BuyStop` für den Stop-Loss und ein `BuyLimit` für den Take-Profit gesetzt.
- Jede Schutzanordnung deckt das neu angeforderte Panel-Volumen ab (derselbe Betrag wie eine einzelne Aktion am ursprünglichen MQL-Panel).
- Bestellungen werden automatisch in `OnStopped`, `OnReseted` und immer dann storniert, wenn die Gegenseite ausgelöst wird.

## Nutzungshinweise
- Weisen Sie `Strategy.Security` in der Hostanwendung zu oder stellen Sie ein `TargetSecurity` bereit, bevor Sie die Panel-Methoden aufrufen. andernfalls werden keine Trades eingereicht.
- Rufen Sie `PlaceBuyOrders()` auf, um die Schaltfläche „KAUFEN“ von MQL und `PlaceSellOrders()` für die Schaltfläche „VERKAUFEN“ zu replizieren.
- Die Preise basieren auf Live-Marktdaten. Wenn weder das beste Geld/Briefgeschäft noch der letzte Handel verfügbar ist, protokolliert die Strategie einen Fehler und überspringt die Auftragsübermittlung.
- Der Helfer ruft `StartProtection()` in `OnStarted` auf, um nach Neustarts vor veralteten Positionen zu schützen.
- Wenn die Metadaten des Instruments `PriceStep` nicht enthalten, beträgt die Pip-Größe standardmäßig `0.0001` (ein Pip für die meisten FX-Symbole); Legen Sie `PriceStep` explizit fest, wenn Ihr Broker alternative Inkremente verwendet.

## Unterschiede im Vergleich zum MQL-Panel
- Es gibt keine eingebettete grafische Benutzeroberfläche. Von Integratoren wird erwartet, dass sie ihre eigene Schnittstelle erstellen oder die öffentlichen Methoden über externe Logik auslösen.
- Schutzanordnungen werden pro Aktion und nicht pro einzelnem MT5-Ticket zusammengefasst. Das resultierende Nettorisiko entspricht dem MT5-Verhalten, während die StockSharp-Implementierung prägnant bleibt.
- Die Volumen- und Preisvalidierung folgt den StockSharp-Konventionen (`Security.ShrinkPrice`, `VolumeStep`, `MinVolume`, `MaxVolume`). Dies vermeidet abgelehnte Bestellungen für Veranstaltungsorte mit strengen Abstufungen.
- Die Ausführungsprotokollierung wird über `LogInfo` und `LogError` bereitgestellt, um die Überwachung in StockSharp-Terminals zu unterstützen.

## Erste Schritte
1. Instanziieren Sie die Strategie, weisen Sie Portfolio und Sicherheit zu (oder legen Sie `TargetSecurity` fest).
2. Starten Sie die Strategie, damit `StartProtection()` die internen Sicherheitsmaßnahmen aktiviert.
3. Rufen Sie `PlaceBuyOrders()` oder `PlaceSellOrders()` basierend auf Benutzereingaben oder automatisierten Auslösern auf.
4. Überwachen Sie das Protokoll auf Bestätigungsmeldungen und verwalten Sie bei Bedarf zusätzliche UI-Logik.

Diese manuelle Trading-Panel-Konvertierung bietet eine leichte und dennoch originalgetreue Reproduktion des ursprünglichen MT5-Expertenberaters, angepasst an das High-Level-Strategie-Framework von StockSharp.
