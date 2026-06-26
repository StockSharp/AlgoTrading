# Day Trading PAMXA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Beschreibung
Die **Day Trading PAMXA**-Strategie reproduziert den MetaTrader 5-Expertenberater, der Bill Williams' Awesome-Oscillator-Momentum-Umkehrungen mit einem stochastischen Filter kombiniert. Der StockSharp-Port behält das originale Multi-Timeframe-Design:

- Die Hauptentscheidungsschleife läuft auf dem **Signal Candles**-Zeitrahmen (Standard 1 Stunde).
- Der Awesome Oscillator wird auf einem separaten **AO Candles**-Zeitrahmen (Standard 1 Tag) ausgewertet, um Momentum auf höherem Zeitrahmen zu erhalten.
- Der stochastische Oszillator verwendet seinen eigenen **Stochastic Candles**-Zeitrahmen (Standard 1 Stunde), sodass %K/%D-Niveaus mit den Originaleinstellungen übereinstimmen.

Die Strategie hält maximal eine Position gleichzeitig. Wenn ein bullisches Setup erscheint, werden zuerst aktive Shorts gedeckt bevor Long eingegangen wird, und umgekehrt für bärische Setups.

## Einstiegslogik
1. Die neuesten abgeschlossenen Werte des Awesome Oscillators auf dem AO-Zeitrahmen berechnen.
2. Die neuesten abgeschlossenen %K- und %D-Werte des stochastischen Oszillators auf dem Stochastic-Zeitrahmen berechnen.
3. Bei jeder abgeschlossenen Signal-Kerze:
   - **Bullisches Setup**: ausgelöst, wenn der vorherige AO-Balken unter null war und der letzte Balken über null schloss (Momentum-Umkehr), während entweder %K oder %D unter dem `Stochastic Level Down`-Schwellenwert liegt (überverkaufte Bedingung). Jeder offene Short wird gedeckt und ein neuer Long wird eröffnet, wenn keine Position verbleibt.
   - **Bärisches Setup**: ausgelöst, wenn der vorherige AO-Balken über null war und der letzte Balken unter null schloss, während entweder %K oder %D über dem `Stochastic Level Up`-Schwellenwert liegt (überkaufte Bedingung). Jeder offene Long wird geschlossen und, wenn flat, wird eine neue Short-Position eröffnet.

## Ausstieg und Risikomanagement
- Ein pip-basierter **Stop-Loss** und **Take-Profit** werden beim Einstieg angehängt. Wenn das Tief der Kerze (für Longs) oder das Hoch (für Shorts) den Stop-Level durchbricht, wird die Position sofort liquidiert. Dieselbe Logik gilt für das Gewinnziel.
- Ein optionaler **Trailing Stop** aktiviert sich, sobald sich der Preis um `Trailing Stop + Trailing Step` Pips zugunsten der Position bewegt hat. Für Longs folgt der Stop dem höchsten Hoch minus der Trailing-Distanz; für Shorts folgt er dem niedrigsten Tief plus der Trailing-Distanz. Die Trailing-Anpassung erfolgt nur, wenn die Bewegung den Trailing-Schritt übersteigt.
- Das Geldmanagement kann in zwei Modi betrieben werden:
  - `FixedVolume`: verwendet den Parameter `Order Volume` direkt.
  - `RiskPercent`: berechnet das Volumen so, dass der konfigurierte Prozentsatz des Portfolio-Werts verloren gehen würde, wenn der Stop-Loss getroffen wird.
- Die Strategie pyramidisiert nie – sobald eine Position existiert, wird das nächste entgegengesetzte Signal sie abflachen, bevor ein neuer Einstieg in Betracht gezogen wird.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `Stop Loss` | Stop-Loss-Abstand in Pips. Null deaktiviert den Schutz-Stop.
| `Take Profit` | Take-Profit-Abstand in Pips. Null deaktiviert das Gewinnziel.
| `Trailing Stop` | Aktivierungsdistanz des Trailing Stops in Pips. Null deaktiviert den Trailing.
| `Trailing Step` | Zusätzliche Pips erforderlich, bevor der Trailing Stop vorrückt. Muss positiv sein, wenn Trailing aktiviert ist.
| `Money Mode` | Wählt zwischen `FixedVolume`- und `RiskPercent`-Dimensionierung.
| `Money Value` | Als Lotgröße bei festem Volumen oder als Risikoprozentsatz bei risikobasierter Dimensionierung interpretiert.
| `Order Volume` | Basisvolumen, das verwendet wird, wenn `Money Mode` `FixedVolume` ist.
| `Stochastic %K` | Berechnungslänge für den stochastischen %K.
| `Stochastic %D` | Glättungslänge für die stochastische %D-Linie.
| `Stochastic Slow` | Abschließender Glättungsfaktor des stochastischen Oszillators.
| `Level Up` | Oberer stochastischer Schwellenwert, der Short-Einstiege ermöglicht.
| `Level Down` | Unterer stochastischer Schwellenwert, der Long-Einstiege ermöglicht.
| `Signal Candles` | Zeitrahmen, der die Haupthandelsschleife antreibt.
| `Stochastic Candles` | Zeitrahmen, der den stochastischen Oszillator speist.
| `AO Candles` | Zeitrahmen, der den Awesome Oscillator speist.
| `AO Fast` / `AO Slow` | Perioden für die internen gleitenden Durchschnitte des Awesome Oscillators.

## Implementierungshinweise
- Die Pip-Wert-Berechnung emuliert die MetaTrader-Logik: Wenn das Instrument 3 oder 5 Dezimalstellen verwendet, entspricht ein Pip zehn Preisschritten; andernfalls entspricht er einem Preisschritt.
- Der stochastische Oszillator von StockSharp stellt keine dedizierte "Preisfeld"-Auswahl bereit; der Port verwendet die Standard-Close-basierte Berechnung, während konfigurierbare Glättungsparameter beibehalten werden.
- Trailing-Stop-Handling wird als virtuelle Prüfung auf Kerzenhochs/-tiefs implementiert. Dies repliziert die serverseitigen Stop-Anpassungen in MetaTrader ohne explizite Stop-Orders zu registrieren.
- Der Code abonniert alle erforderlichen Kerzenzeitrahmen über `GetWorkingSecurities`.
- Englische Inline-Kommentare dokumentieren die wichtigsten Kontrollfluss-Entscheidungen.

## Nutzungstipps
- Richten Sie den `Signal Candles`-Zeitrahmen auf den Zeitrahmen aus, auf dem Sie backtesten oder handeln möchten. Behalten Sie `Stochastic Candles` und `AO Candles` gleich den originalen Standards bei, wenn Sie den MQL5-Experten genau nachahmen möchten.
- Wenn Sie zu `RiskPercent`-Dimensionierung wechseln, stellen Sie sicher, dass die Stop-Loss-Distanz ungleich null ist; andernfalls fällt die Strategie auf `Order Volume` zurück.
- Die Standard-Trailing-Konfiguration spiegelt den originalen EA wider (25-Pip-Trailing-Stop mit 5-Pip-Schritt). Setzen Sie `Trailing Stop` auf null, wenn Sie einen statischen Stop-Loss bevorzugen.
