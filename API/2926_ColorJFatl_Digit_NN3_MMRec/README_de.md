# ColorJFatl Digit NN3 MMRec-Strategie (StockSharp-Konvertierung)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist ein StockSharp High-Level-Port des MetaTrader 5-Experten *Exp_ColorJFatl_Digit_NN3_MMRec*. Der ursprüngliche Roboter verwendete einen benutzerdefinierten ColorJFatl_Digit-Indikator zusammen mit Money-Management-Recovery-Regeln. Die StockSharp-Version konzentriert sich auf die Kernsignalmaschine und drückt sie durch drei unabhängige Module aus, die auf verschiedenen Zeitrahmen arbeiten.

Jedes Modul wendet einen Jurik Moving Average (JMA) auf die ausgewählte Preisquelle an und überwacht die Neigung dieses Durchschnitts. Wenn die Neigung positiv wird, behandelt das Modul dies als bullisches Regime, schließt Short-Exposition und öffnet optional eine neue Long-Position. Wenn die Neigung negativ wird, führt das Modul die Spiegellogik für Short-Trades aus. Alle Module teilen dasselbe Portfolio und arbeiten daher immer mit der Nettoposition der Strategie.

## Handelslogik

1. Kerzen auf drei Zeitrahmen abonnieren (Standard: 1 Tag, 8 Stunden, 3 Stunden).
2. Für jede abgeschlossene Kerze:
   - Die Kerze zum konfigurierten angewandten Preis konvertieren (Schlusskurs, Eröffnungskurs, typischer Preis, DeMark-Preis usw.).
   - Den Wert durch einen Jurik Moving Average verarbeiten, um eine geglättete Reihe zu erhalten.
   - Den aktuellen JMA-Wert mit dem vorherigen vergleichen, um die Neigungsrichtung zu bestimmen. Eine positive Neigung ergibt einen "Up"-Zustand, eine negative Neigung ergibt einen "Down"-Zustand, eine flache Neigung behält den vorherigen Zustand bei.
   - Die Zustände gemäß der *SignalBar*-Verzögerung puffern, damit die Strategie bei Bedarf auf historischen Bars agieren kann (der ursprüngliche Experte unterstützte verzögerte Signale).
3. Immer wenn ein Modul einen Übergang erkennt:
   - **Übergang nach oben**: optional jede Short-Position schließen und eine Long-Position mit dem Modulvolumen öffnen.
   - **Übergang nach unten**: optional jede Long-Position schließen und eine Short-Position mit dem Modulvolumen öffnen.
4. Entgegengesetzte Signale von einem anderen Modul können die Position abhängig von den Aktivierungsflags flachstellen oder umkehren.

Stops und Gewinne sind nicht fest codiert; stattdessen setzt die Strategie auf entgegengesetzte Signale und die integrierten StockSharp-Schutzmaßnahmen (`StartProtection()`) für die Sicherheit.

## Parameter

Die Parameter sind pro Modul (A, B, C) gruppiert, um die MT5-Vorlage widerzuspiegeln. Jede Gruppe exponiert folgende Werte:

- **CandleType** – Zeitrahmen für eingehende Kerzen.
- **JmaLength** – Periode des Jurik Moving Average.
- **JmaPhase** – zur Dokumentation gespeichert; StockSharp's JMA bietet keine Phasenanpassung.
- **SignalBar** – Anzahl abgeschlossener Bars, die vor dem Handeln auf ein Signal gewartet werden (0 = sofort).
- **AppliedPrices** – Preistransformation als Eingabe für JMA (Schlusskurs, Eröffnungskurs, Median, typisch, gewichtet, einfach, Quartal, Trendfolge, DeMark).
- **AllowBuyOpen / AllowSellOpen** – Berechtigung, Positionen in der entsprechenden Richtung zu öffnen.
- **AllowBuyClose / AllowSellClose** – Berechtigung, bestehende Positionen zu schließen, wenn das Modul ein entgegengesetztes Signal ausgibt.
- **Volume** – Ordergröße, die das Modul beim Öffnen eines neuen Trades verwendet.

Da Module ein einzelnes Kontoportfolio teilen, kann nur jeweils eine Netto-Long- oder Netto-Short-Position existieren. Wenn ein Modul versucht, einen Trade zu öffnen, während das Portfolio bereits Exposition in dieselbe Richtung trägt, wird die Order übersprungen; wenn eine entgegengesetzte Richtung offen ist, wird sie vor dem neuen Trade geschlossen (vorbehaltlich der Berechtigungs-Flags).

## Verwendungshinweise

- Die Strategie abonniert alle konfigurierten Zeitrahmen über `GetWorkingSecurities()`, um sicherzustellen, dass die Simulations- oder Live-Umgebung die erforderlichen Kerzenreihen lädt.
- Signale operieren ausschließlich auf abgeschlossenen Kerzen, um Intrabar-Neuzeichnung zu verhindern.
- Der *AppliedPrices*-Enum reproduziert die Optionen des ursprünglichen Indikators, einschließlich zweier Trendfolge-Preisvarianten und des DeMark-Preises.
- Die Money-Management-Recovery-Logik aus der MQL-Version ist nicht reproduziert. Stattdessen kann das Risiko über StockSharp-Schutzmaßnahmen oder durch Anpassen der Modulvolumina verwaltet werden.
- Englische Kommentare im Code erläutern jeden Schritt der Konvertierung für einfachere Wartung und zukünftiges Python-Porting.

## Strategie erweitern

- Um Stop-Loss- oder Take-Profit-Regeln hinzuzufügen, den Standard-`StartProtection()`-Aufruf durch die gewünschte Konfiguration ersetzen.
- Zusätzliche Module können durch Klonen des `SignalModule`-Konfigurationsmusters erstellt werden.
- Für erweitertes Positionsmanagement (z. B. Verfolgen der Exposition pro Modul) können StockSharp-Child-Strategien oder virtuelle Portfolios auf dieser Basis hinzugefügt werden.
