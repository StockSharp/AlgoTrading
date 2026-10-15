# Multi-Strategie-Kombinationsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Multi Strategy Combo Strategy** ist eine C#-Konvertierung des Expertenberaters MetaTrader 4 „Multi-Strategy iFSF“. Das Original EA kombiniert mehrere Indikatoren (MA, RSI, MACD, Stochastic, SAR) und umhüllt sie mit Trend-, Bollinger-Bereichs- und Rauschfiltern. Der StockSharp-Port bewahrt die gleiche Idee unter Verwendung von `SubscribeCandles().Bind(...)`-Streams und Indikatorklassen auf hoher Ebene. Jeder aktivierte Indikator erzeugt eine KAUF/VERKAUF-Abstimmung; Erst wenn alle Stimmen übereinstimmen, führt die Strategie einen Auftrag aus. Zusätzliche Filter emulieren die Kombinationsmodi des EA.

## Kernlogik
* **Konsens-Engine** – Die gleitenden Durchschnitte RSI, MACD, Stochastic und Parabolic SAR liefern jeweils ein diskretes Signal. Wenn alle aktivierten Indikatoren auf KAUFEN (oder VERKAUFEN) übereinstimmen, wird der Konsens bullisch (oder bärisch).
* **Kombinationsfaktor (1–3)** – Spiegelt die `Combo_Trader_Factor`-Logik von EA wider. Jeder Faktor mischt Konsens mit ADX-Trenderkennung und Bollinger-Bereichsbestätigung unterschiedlich:
  * *Faktor 1* bevorzugt Trendbedingungen. Bereichszustände basieren auf Bollinger Umkehrungen, wenn sie aktiviert sind.
  * *Faktor 2* erfordert eine stärkere Bestätigung: Trend- und Bereichsfilter müssen mit dem Konsens übereinstimmen.
  * *Faktor 3* ist die strengste Variante und erfordert eine Abstimmung zwischen allen aktiven Modulen.
* **Trenderkennung** – ADX in einem konfigurierbaren Zeitrahmen kennzeichnet den Markt als aufwärts/abwärts tendierend oder aufwärts/abwärts tendierend.
* **Bollinger-Filter** – Verwendet mittlere (2σ) und breite (3σ) Bänder. Long-Signale erfordern einen Absprung vom unteren Band, der durch die jüngsten überverkauften RSI-Werte bestätigt wird; Shorts spiegeln das Verhalten am oberen Band wider.
* **Rauschfilter** – ATR-basierte Prüfung, die neue Trades blockiert, wenn die Volatilität zu gering ist (Ersatz für das Damiani Volatmeter).
* **Auto-Close** – Wenn diese Option aktiviert ist, wird die Strategie sofort beendet, wenn der Konsens in die entgegengesetzte Richtung wechselt.

## Indikatoren und Signale
* **Gleitende Durchschnitte** – Drei konfigurierbare MAs (Methode + Länge). Die Modi 1–5 reproduzieren die ursprünglichen Crossover-Kombinationen (schnell vs. mittel, mittel vs. langsam, aggregierte Logik).
* **RSI** – Die Modi 1–4 decken Überkauft/Überverkauft, Momentum, kombiniert und Zonenprüfungen ab. Alle Schwellenwerte sind einstellbar.
* **MACD** – Vier Modi ahmen den EA nach: Trendsteigung, Histogrammkreuzung unter/über Null, kombinierte Bestätigung und Nulldurchgang der Signallinie.
* **Stochastic-Oszillator** – Entweder einfache Kreuzung von %K vs. %D oder Kreuzung mit hohen/niedrigen Schwellenwerten.
* **Parabolic SAR** – Optionale Richtungsabstimmung, die das Verhalten „Letztes Signal merken“ unterstützt, um mehrere Auslöser pro Trend zu vermeiden.

## Risikomanagement
* Optionale Stop-Loss-/Take-Profit-Offsets (absoluter Preisabstand), konfiguriert über `StopLossOffset` und `TakeProfitOffset`.
* Integrierte Trailing-Stop-Unterstützung durch den StockSharp `StartProtection`-Helfer.
* Der tägliche Positionsschutz folgt der Basismechanik `Strategy`; Es ist keine manuelle Chargenverwaltung erforderlich.

## Schlüsselparameter
* **Allgemein** – `ComboFactor`, `CandleType`.
* **Gleitende Durchschnitte** – `UseMa`, `MaMode`, individuelle Längen/Methoden, Kerzenzeitrahmen, Flag „Letztes merken“.
* **RSI** – `UseRsi`, `RsiMode`, `RsiPeriod`, überkaufte/überverkaufte Ebenen, Zonenschwellenwerte, Flag „Letztes merken“.
* **MACD** – `UseMacd`, `MacdMode`, schnell/langsam/Signallängen, Kerzenzeitrahmen, Flag „Letztes merken“.
* **Stochastic** – `UseStochastic`, Glättungsparameter, Schwellenwerte und Kerzenzeitrahmen.
* **SAR** – `UseSar`, Beschleunigungseinstellungen, Kerzenzeitrahmen.
* **Trendfilter** – `UseTrendDetection`, `AdxPeriod`, `AdxLevel`, Kerzenzeitrahmen.
* **Bollinger-Filter** – `UseBollingerFilter`, `BollingerPeriod`, mittlere/breite Abweichungen, RSI Bereichslänge.
* **Rauschfilter** – `UseNoiseFilter`, `NoiseAtrLength`, `NoiseThreshold`, Kerzenzeitrahmen.
* **Automatisches Schließen und Risiko** – `UseAutoClose`, `AllowOppositeAfterClose`, `StopLossOffset`, `TakeProfitOffset`, `UseTrailingStop`.

Alle Parameter werden als `StrategyParam<T>` angezeigt, um Optimierung, Validierung und UI-Gruppierung zu unterstützen.

## Unterschiede zum MT4 EA
* Es werden nur StockSharp integrierte Indikatoren verwendet. Die ursprüngliche Option zwischen ZeroLag und der klassischen MACD wird durch die native MACD-Implementierung ersetzt.
* Alle gleitenden Durchschnitte und Oszillatoren basieren auf Kerzenschlusskursen. Preistyp- und Shift-Offsets von MT4 (z. B. `FastMa_Price`, `FastMa_Shift`) sind nicht verfügbar.
* Der Damiani-Rauschfilter wird mit ATR angenähert; Das Verhalten kann über `NoiseThreshold` angepasst werden.
* Die Geldverwaltung und Auftragswiederholungen werden von StockSharp abgewickelt (keine manuellen `OrderSend`-Schleifen). Die Strategie funktioniert mit aggregierten Positionen (`BuyMarket`/`SellMarket`).
* Das Kommentarfeld und die Diagrammobjekte von EA werden weggelassen. Stattdessen ist die Protokollierung über `LogInfo` verfügbar.

## Nutzung
1. Fügen Sie die Klasse `MultiStrategyComboStrategy` zu Ihrer StockSharp-Lösung hinzu und kompilieren Sie sie.
2. Instanziieren Sie die Strategie, legen Sie `Security`, `Portfolio` und das gewünschte `Volume` fest.
3. Konfigurieren Sie Zeitrahmen für jeden Indikator, wenn eine Bestätigung mehrerer Zeitrahmen erforderlich ist (Standardeinstellungen folgen den Eingaben von EA).
4. Passen Sie optional Stop/Take-Offsets, Nachlaufverhalten und Filterschwellenwerte an.
5. Starten Sie die Strategie. Trades werden bei geschlossenen Kerzen ausgelöst, wenn alle aktivierten Module entsprechend dem ausgewählten Kombinationsfaktor übereinstimmen.

## Konvertierungshinweise
* Die Strategie basiert ausschließlich auf High-Level-Abonnement-APIs (`SubscribeCandles().Bind(...)`) – es werden keine manuellen Indikatorpuffer verwendet.
* Tabulatoren werden zum Einrücken gemäß den Repository-Richtlinien verwendet.
* Ausführliche Inline-Kommentare verdeutlichen, wie EA-Konzepte auf StockSharp-Code abgebildet werden.
