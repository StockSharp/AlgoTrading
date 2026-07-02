# Franks 4-Stunden-Limit-Orders-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Franks 4 Hour Limit Orders Strategy** portiert den MetaTrader 4 Expert Advisor von `MQL/7684/Franks_4hour_limit_orders.mq4` zum StockSharp High-Level API. Der ursprüngliche EA kombiniert die Triple-Screen-Ideen von Alexander Elder: Er bewertet die Dynamik auf einem Vier-Stunden-Chart unter Verwendung des MACD-Histogramms (OsMA) zusammen mit dem Force Index und platziert dann konträre Limit-Orders um die vorherigen Candle-Extreme. Die StockSharp-Implementierung behält diese Multi-Indikator-Logik bei, befolgt aber gleichzeitig die Repository-Richtlinien (Tabs, API auf hoher Ebene, keine benutzerdefinierten Sammlungen) und fügt der Übersichtlichkeit halber ausführliche Inline-Kommentare auf Englisch hinzu.

## Handelslogik
1. **Datenquelle** – Die Strategie abonniert einen konfigurierbaren Kerzentyp, der standardmäßig Vier-Stunden-Kerzen verwendet. Alle Berechnungen werden nur für abgeschlossene Kerzen durchgeführt, um dem Verhalten des MT4-Experten zu entsprechen.
2. **Indikatoren** – Es werden zwei verwaltete Indikatoren verwendet:
   - `MovingAverageConvergenceDivergenceSignal(12, 26, 9)` stellt sowohl die MACD-Leitung als auch die Signalleitung bereit. Ihre Differenz stellt das im EA verwendete OsMA-Histogramm wieder her.
   - `ForceIndex(24)` misst die Kraft der vorherigen Kerze. Es werden nur endgültige Indikatorwerte berücksichtigt.
3. **Historischer Kontext** – Der EA erfordert zwei abgeschlossene Kerzen, um die Steigung des Indikators zu bestimmen. Der Port speichert die vorherigen OsMA-Werte, den vorherigen Force-Index-Wert und das vorherige Kerzenhoch/-tief, um diese Anforderung widerzuspiegeln.
4. **Verkaufs-Setup** – Wenn das OsMA-Histogramm ansteigt (`OsMA[1] > OsMA[2]`) und der vorherige Force-Index-Wert negativ ist, plant der Roboter eine konträre Verkaufs-Limit-Order:
   - Der Basispreis ist das vorherige Kerzenhoch plus einen Punkt.
   - Es wird ein Sicherheitspuffer von 16 Pips (konfigurierbar) gegenüber dem aktuellen Gebot erzwungen. Der Zielpreis wird zum Maximum zwischen dem Grundpreis und `Bid + buffer`.
   - Stop-Loss- und Take-Profit-Preise werden anhand der konfigurierten Pip-Abstände (standardmäßig 35 Pips und 150 Pips) an der Preisstufe des Instruments ausgerichtet.
5. **Kauf-Setup** – Wenn das OsMA-Histogramm abnimmt (`OsMA[1] < OsMA[2]`) und der vorherige Force-Index positiv ist, bereitet die Strategie eine Kauf-Limit-Order unterhalb des Marktes vor:
   - Der Basispreis ist das vorherige Kerzentief minus einen Punkt.
   - Der Algorithmus erzwingt den gleichen 16-Pip-Puffer relativ zum aktuellen Brief und wählt das Minimum zwischen dem Basispreis und `Ask - buffer`.
6. **Aufrechterhaltung der ausstehenden Order** – Wenn sich die OsMA-Steigung vor der Ausführung in die entgegengesetzte Richtung dreht, wird die entsprechende ausstehende Order storniert. Wenn eine Seite gefüllt ist, wird die gegenüberliegende ausstehende Bestellung entfernt, um eine Doppelbelichtung zu vermeiden.
7. **Positionsverwaltung** – Bei der Ausführung wird der Füllpreis gespeichert und die vorberechneten Stop-Loss- und Take-Profit-Level werden aktiviert. Die Strategie implementiert außerdem einen Pip-basierten Trailing Stop (standardmäßig 30 Pips), der den Schutzstopp nur dann in die günstige Richtung verschiebt, wenn der Preis über den Einstiegspunkt plus die Trailing-Distanz hinaus steigt.
8. **Exits** – Schutzaufträge werden bei jeder abgeschlossenen Kerze überwacht. Eine Long-Position wird geschlossen, wenn das Kerzentief den Stopp berührt oder das Kerzenhoch das Ziel erreicht. Bei Short-Positionen gelten die gespiegelten Regeln.

## Parameter
| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `OrderVolume` | 1 | Festes Volumen für ausstehende Limit-Orders. |
| `StopLossPips` | 35 | Abstand in Pips zwischen dem Einstiegspreis und dem Schutzstopp. |
| `TakeProfitPips` | 150 | Abstand (in Pips) zwischen dem Einstiegspreis und dem Take-Profit-Niveau. |
| `TrailingStopPips` | 30 | Abstand (in Pips) für den Trailing Stop, der Gewinne sichert, sobald sich der Preis weit genug bewegt. |
| `EntryBufferPips` | 16 | Mindestlücke in Pips zwischen dem aktuellen Marktpreis und der ausstehenden Order. |
| `PipSize` | 0,0001 | Pip-Größe, die für Preisumrechnungen verwendet wird; Der Standardwert ist 0,0001, kann aber mit exotischen Symbolen ausgerichtet werden. |
| `CandleType` | 4h Zeitrahmen | Von der Strategie verarbeitete Kerzenserie. |

## Dateien
- `CS/Franks4HourLimitOrdersStrategy.cs` – Haupt-C#-Implementierung mit detaillierten englischen Kommentaren.
- `README.md` – Diese englische Beschreibung des Algorithmus.
- `README_ru.md` – Russische Dokumentation.
- `README_zh.md` – Chinesische Dokumentation.

## Implementierungshinweise
- Die Strategie basiert ausschließlich auf den übergeordneten API (`SubscribeCandles`, Indikatorbindungen und praktischen Bestellhilfen).
- Alle Preisberechnungen orientieren sich an der Preisstufe des Instruments, um ungültige Niveaus zu vermeiden.
- Zustandsvariablen speichern nur die notwendigen historischen Daten und entsprechen damit der Repository-Regel, die benutzerdefinierte Sammlungen verbietet.
- Stop-Loss-, Take-Profit- und Trailing-Stop-Management werden innerhalb der Kerzenverarbeitungsroutine durchgeführt, um das MT4-Trailing-Verhalten zu emulieren.
