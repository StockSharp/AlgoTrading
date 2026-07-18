# Macd Pattern Trader Trigger-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Macd Pattern Trader Trigger Strategy portiert den MetaTrader 4-Expertenberater `MacdPatternTraderv05cb` zur High-Level-Strategie API von StockSharp. Das System handelt mit reinen MACD-Histogrammmustern und sucht nach einer Double-Top-Struktur unterhalb der Nulllinie, um Shorts zu eröffnen, und nach einem spiegelbildlichen Double-Bottom über der Nulllinie, um Long-Positionen zu eröffnen. Das Handelsmanagement spiegelt das ursprüngliche EA wider: Jeder Eintrag wird am Markt mit einem konfigurierbaren festen Stop-Loss und Take-Profit, gemessen in Instrumentenpunkten, eingereicht.

## Strategielogik
### Indikatorstrom
* Ein einzelnes Kerzenabonnement steuert die Logik (Standard: 15-Minuten-Kerzen). Jede fertige Kerze speist einen `MovingAverageConvergenceDivergence`-Indikator, der mit den ungewöhnlichen MT4-Parametern `(fast = 13, slow = 5, signal = 1)` konfiguriert ist, die von der Quelle EA verwendet werden.
* Es wird nur die Hauptzeile MACD verwendet. Die Strategie puffert die letzten drei abgeschlossenen Werte, um `iMACD(..., MODE_MAIN, shift=1..3)` von MetaTrader zu emulieren.

### Bullisches Setup (Long-Einstiege)
1. **Scharfschaltbedingung** – die Linie MACD muss über `Bullish Trigger` steigen (Standard: `0.0015`). Dies bereitet die Strategie für die Suche nach der Pullback-Sequenz vor. Jeder Rückgang unter Null löscht den Zustand sofort.
2. **Rückzugsfenster** – Sobald der MACD aktiviert ist, muss er unter `Bullish Reset` (Standardwert `0.0005`) zurückfallen. Dies markiert den potenziellen Rückzugsbereich. Das Fenster bleibt aktiv, bis ein gültiges Muster bestätigt wird oder MACD negativ wird.
3. **Musterbestätigung** – während das Fenster aktiv ist, müssen die letzten drei gepufferten MACD-Messwerte Folgendes erfüllen:
   * `macd_curr > macd_last` (die Dynamik nimmt wieder zu),
   * `macd_last < macd_last3` (der vorherige Balken hat den Swing niedrig gesetzt),
   * `macd_curr > Bullish Reset` und `macd_last < Bullish Reset` (Preis erholt sich von der flachen Rückzugszone).
4. **Ausführung** – bei Bestätigung wird die Strategie zum Marktpreis gekauft. Wenn eine Short-Position besteht, umfasst die Ordergröße automatisch das Volumen, das zur Abflachung vor dem Aufbau des Long-Exposure erforderlich ist.

### Bärisches Setup (kurze Einstiege)
1. **Scharfschaltbedingung** – die Zeile MACD muss unter `-Bearish Trigger` fallen (Standard: `-0.0015`). Jede Bewegung über Null löscht alle rückläufigen Zustände.
2. **Rückzugsfenster** – Sobald der MACD scharf ist, muss er über `-Bearish Reset` zurückspringen (Standard: `-0.0005`).
3. **Musterbestätigung** – während das Fenster geöffnet ist, müssen die gepufferten Werte Folgendes erfüllen:
   * `macd_curr < macd_last`,
   * `macd_last > macd_last3`,
   * `macd_curr < -Bearish Reset` und `macd_last > -Bearish Reset`.
4. **Ausführung** – ein Marktverkaufsauftrag wird übermittelt. Wenn eine Long-Position besteht, wird deren Volumen in die Order einbezogen, sodass das Konto um die konfigurierte Handelsgröße netto short wird.

### Risikomanagement
* **Fester Stop-Loss / Take-Profit** – Abstände werden in Punkten (Preisschritten) angegeben. Die Strategie multipliziert sie mit dem `PriceStep` des Instruments und ruft `StartProtection` auf, um das ursprüngliche SL/TP-Verhalten zu reproduzieren. Wenn Sie einen Abstand auf `0` festlegen, wird die entsprechende Ebene deaktiviert.
* **Ein Signal pro Fenster** – nach der Auftragserteilung werden die Scharfschalt- und Fensterflags gelöscht, um wiederholte Eingaben aus demselben MACD-Muster zu vermeiden.

## Parameter
* **Handelsvolumen** – Market-Order-Volumen. Gegensätzliche Positionen werden automatisch geschlossen, bevor der neue Handel eröffnet wird.
* **Schneller EMA / Langsamer EMA / Signal EMA** – MACD Längen. Die Standardeinstellungen replizieren den ursprünglichen Ratgeber, können jedoch optimiert werden.
* **Bullish Trigger/Reset** – positive Schwellenwerte von MACD (in Indikatoreinheiten), die das Long-Setup aktivieren und seine Pullback-Zone definieren.
* **Bearish Trigger/Reset** – absolute MACD Schwellenwerte für das Short-Setup. Der Trigger wird zur Laufzeit mit negativem Vorzeichen angewendet.
* **Stop Loss / Take Profit** – Abstände in Punkten (Preisschritte). Ein Wert von `0` deaktiviert den entsprechenden Schutz.
* **Kerzentyp** – Zeitrahmen, der für die MACD-Berechnung und Handelsentscheidungen verwendet wird.

## Hinweise zur Implementierung
* Die StockSharp-Hochebene API wird durchgehend verwendet: `SubscribeCandles` speist den Indikator und `StartProtection` spiegelt das MT4-Handelsmanagement wider.
* Der MACD-Verlaufspuffer stellt sicher, dass die Entscheidungslogik auf den drei vorherigen abgeschlossenen Balken basiert und mit den `shift=1..3`-Aufrufen von MetaTrader übereinstimmt.
* Im Paket API gibt es keine Python-Version dieser Strategie, sondern nur die C#-Implementierung.
