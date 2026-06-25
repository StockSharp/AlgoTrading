# Deep-Drawdown-MA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Deep-Drawdown-MA-Strategie ist eine direkte Konvertierung des MetaTrader 5-Expertenberaters "Deep Drawdown MA (barabashkakvn's edition)" in die StockSharp High-Level-API. Die Strategie handelt gleitende Durchschnittskrenzungen und wendet dabei einen Break-even-Mechanismus an, der darauf ausgelegt ist, Trades zu schützen, die in einen Drawdown geraten sind. Die StockSharp-Version behält die konfigurierbaren gleitenden Durchschnittsparameter, die Möglichkeit, die Anzahl aggregierter Einstiege zu begrenzen, und die Option bei, verlierende Trades sofort bei einer Signalumkehr zu liquidieren.

## Handelslogik
- **Indikatoren**: Zwei gleitende Durchschnitte mit individuellen Perioden, Preisquellen und historischen Versätzen. Beide Durchschnitte teilen dieselbe Glättungsmethode (SMA, EMA, SMMA oder LWMA).
- **Einsstiegsbedingungen**:
  - **Long**: Der verschobene schnelle Durchschnitt steigt über den verschobenen langsamen Durchschnitt. Die Strategie fügt das konfigurierte Ordervolumen hinzu (und deckt jede Short-Exposition ab), wenn der letzte Einstieg nicht Long war und die maximale Positionsgrenze nicht überschritten wird.
  - **Short**: Der verschobene schnelle Durchschnitt fällt unter den verschobenen langsamen Durchschnitt. Die Strategie verkauft das konfigurierte Volumen (und deckt jede Long-Exposition ab), wenn der vorherige Einstieg nicht Short war und die maximale Positionsgrenze es erlaubt.
- **Ausstiegsbedingungen**:
  - **Longs**: Wenn der schnelle Durchschnitt zurück unter den langsamen Durchschnitt kreuzt, wird die Position entweder sofort geschlossen (`CloseLosses = true`) oder für einen Break-even-Ausstieg markiert. Während eines Break-even-Ausstiegs wartet die Strategie, bis der Kerzenschlusskurs zum durchschnittlichen Einstiegspreis zurückkehrt, bevor sie flattert.
  - **Shorts**: Gespiegeltes Verhalten – bei einem bullischen Kreuz wird die Position entweder sofort geschlossen oder mit einem Break-even-Ziel bewaffnet, das ausgelöst wird, sobald der Preis zur durchschnittlichen Einstiegsposition zurückfällt.
- **Positionsverfolgung**: Durchschnittlicher Einstandspreis und die zuletzt eröffnete Richtung werden aus eigenen Trades rekonstruiert, damit die High-Level-API das MQL-Verhalten reproduzieren kann.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `OrderVolume` | Ordergröße für jede Marktoperation. | 0.1 |
| `MaxPositions` | Maximale Anzahl aggregierter Lots pro Richtung (Nettoexposition). | 5 |
| `CloseLosses` | Verlierende Trades sofort bei einer Umkehr schließen statt auf Break-even zu warten. | false |
| `FastMaPeriod` / `SlowMaPeriod` | Länge der schnellen und langsamen gleitenden Durchschnitte. | 10 / 30 |
| `FastMaShift` / `SlowMaShift` | Historischer Versatz für jeden gleitenden Durchschnitt (emuliert das MT5 shift-Argument). | 3 / 0 |
| `FastPriceType` / `SlowPriceType` | Preisquelle für jeden gleitenden Durchschnitt (Close, Open, High, Low, Median, Typical, Weighted). | Close |
| `MaMethod` | Gemeinsame Glättungsmethode für beide Durchschnitte (SMA, EMA, SMMA, LWMA). | SMA |
| `CandleType` | Kerzenserie für Berechnungen. | 15-Minuten-Kerzen |

## Konvertierungshinweise
- Der ursprüngliche MetaTrader-Roboter konnte gleichzeitig abgesicherte Long- und Short-Positionen halten. StockSharp-Strategien arbeiten mit Nettopositionen; daher erzwingt die konvertierte Version aggregierte Exposition unter Beibehaltung der maximalen Positionsanzahl.
- Break-even-Schutz wird mit internen Flags anstatt MT5-Ordermodifikationen implementiert. Die Strategie überwacht Kerzenschlüsse und tritt beim rekonstruierten durchschnittlichen Einstandspreis aus.
- Gleitende Durchschnitts-"Versatz"-Parameter werden durch Führen einer kurzen Warteschlange aktueller Indikatorwerte reproduziert, was das MT5-`shift`-Argument widerspiegelt, ohne Low-Level-Indikatorbuffer aufzurufen.

## Verwendung
1. Die Strategie an das gewünschte Wertpapier anhängen und `OrderVolume`, Kerzentyp und gleitende Durchschnittsparameter entsprechend dem Zielmarkt einstellen.
2. Trading aktivieren, sobald die Strategie läuft und das Kerzenabonnement aktiv ist.
3. Die Break-even-Flags in den Logs überwachen: Trades werden automatisch geflattert, sobald der Preis zur gemittelten Einstiegsposition zurückkehrt.

## Risikomanagement
- `CloseLosses = true` verwenden, um bei Umkehr der Durchschnitte eine schnelle Liquidation verlierender Trades zu erzwingen.
- `MaxPositions` anpassen, um die aggregierte Exposition nach aufeinanderfolgenden alternierenden Einstiegen zu begrenzen.
- Die Strategie mit kontoseitigen Risikokontrollen in StockSharp kombinieren (z. B. `StartProtection`) für zusätzliche Sicherheitsmechanismen.

## Dateien
- `CS/DeepDrawdownMaStrategy.cs` – C#-Implementierung mit der StockSharp High-Level-API.
- `README.md`, `README_ru.md`, `README_zh.md` – Mehrsprachige Dokumentation des Strategie-Verhaltens und der Parameter.
