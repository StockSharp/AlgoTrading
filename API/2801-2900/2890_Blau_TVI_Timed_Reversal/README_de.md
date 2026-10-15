# Blau TVI Zeitgesteuerte Umkehr-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Zusammenfassung
- Konvertiert vom MetaTrader 5-Experten **Exp_BlauTVI_Tm.mq5** in `MQL/21014`.
- Reimplementiert die Blau Tick Volume Index (TVI)-Logik mit drei konfigurierbaren Glättungsstufen.
- Generiert Umkehr-Trades, wenn der geglättete TVI die Steigung wechselt, und beschränkt Aufträge optional auf eine benutzerdefinierte Handelssitzung.
- Unterstützt optionale Stop-Loss- und Take-Profit-Schutz in Preispunkten.

## Blau Tick Volume Index-Logik
Der ursprüngliche Experte verwendet den benutzerdefinierten `BlauTVI`-Indikator, der Auf- und Abwärtsbewegungen des Tick-Volumens zählt und das Ergebnis mehrfach glättet. Der C#-Port behält dieselbe Idee bei:

1. **Rohe Auf-/Abwärts-Tick-Zählung**
   - `UpTicks = (Volume + (Close - Open) / PriceStep) / 2`
   - `DownTicks = Volume - UpTicks`
   - Das Kerzenvolumen wird als Ersatz für das Tick-Volumen verwendet, da der StockSharp-Feed keine Tick-Zählungen für aggregierte Kerzen bereitstellt.
2. **Stufe 1-Glättung** – `UpTicks` und `DownTicks` werden mit dem ausgewählten gleitenden Durchschnittstyp (`EMA`, `SMA`, `SMMA`, `WMA`, `JMA`) und Länge `Length1` geglättet.
3. **Stufe 2-Glättung** – die Ausgaben der Stufe 1 werden erneut mit Länge `Length2` geglättet.
4. **TVI-Berechnung** – `TVI = 100 * (Up2 - Down2) / (Up2 + Down2)`.
5. **Stufe 3-Glättung** – der TVI wird noch einmal mit Länge `Length3` geglättet, um Rauschen zu reduzieren.

Die Strategie speichert eine kurze rollende Historie der finalen TVI-Werte, um den `SignalBar`-Versatz zu replizieren, der vom ursprünglichen EA verwendet wird (`CopyBuffer` mit Versatz `SignalBar`).

## Handelsregeln
- **Signalsteigungserkennung**
  - Wenn der vorherige TVI-Wert (`SignalBar + 1`) kleiner als der ältere Wert (`SignalBar + 2`) ist, gilt der TVI als aufwärts drehend. Wenn der neueste verfügbare Wert ebenfalls größer als der vorherige ist, wird ein bullisches Umkehrsignal erzeugt.
  - Wenn der vorherige TVI-Wert größer als der ältere Wert ist, dreht der TVI abwärts. Wenn der neueste Wert unter dem vorherigen liegt, wird ein bärisches Umkehrsignal erzeugt.
- **Positionsverwaltung**
  - Long-Einstiege erfordern `EnableBuyOpen = true`, das obige bullische Signal und eine nicht-positive aktuelle Position. Die Strategie schließt alle bestehenden Short-Positionen vor dem Kauf, indem sie die absolute Short-Größe zum konfigurierten `Volume` hinzufügt.
  - Short-Einstiege erfordern `EnableSellOpen = true`, das bärische Signal und eine nicht-negative Position.
  - Long-Ausstiege werden ausgelöst, wenn die TVI-Steigung bärisch wird und `EnableBuyClose = true`.
  - Short-Ausstiege werden ausgelöst, wenn die TVI-Steigung bullisch wird und `EnableSellClose = true`.
- **Zeitfilter**
  - Wenn `EnableTimeFilter = true`, öffnet die Strategie nur innerhalb des Fensters [`StartHour`:`StartMinute`, `EndHour`:`EndMinute`] neue Positionen. Overnight-Sitzungen werden unterstützt (Start > Ende). Positionen werden zwangsweise geschlossen, sobald die Zeit die Sitzung verlässt.
- **Schutz**
  - `StopLossPoints` und `TakeProfitPoints` werden durch Multiplikation mit dem `PriceStep` des Instruments in absolute Preisabstände umgerechnet und an `StartProtection` übergeben. Auf null setzen, um zu deaktivieren.

## Parameter
| Parameter | Beschreibung |
|-----------|--------------|
| `Volume` | Ordergröße für jeden Einstieg (zusätzliche Kontrakte werden hinzugefügt, um die entgegengesetzte Exposition abzudecken). |
| `CandleType` | Kerzendatentyp/-zeitrahmen für alle Berechnungen (Standard: 4-Stunden-Zeitrahmen). |
| `MaType` | Gleitender Durchschnittstyp für alle Glättungsstufen (EMA, SMA, SMMA, WMA, JMA). |
| `Length1`, `Length2`, `Length3` | Glättungslängen für jede Stufe der Blau TVI-Berechnung. |
| `SignalBar` | Versatz für die TVI-Werte in der Signalgenerierung (1 entspricht der vorherigen geschlossenen Kerze wie die MQL-Version). |
| `EnableBuyOpen`, `EnableSellOpen` | Öffnen von Long-/Short-Positionen bei Signalen erlauben. |
| `EnableBuyClose`, `EnableSellClose` | Schließen bestehender Long-/Short-Positionen bei Steigungsumkehr erlauben. |
| `EnableTimeFilter` | Umschalter für das Handelssitzungsfenster. |
| `StartHour`, `StartMinute`, `EndHour`, `EndMinute` | Sitzungsgrenzen in Börsenzeitzone. Unterstützt Intraday- und Overnight-Bereiche. |
| `StopLossPoints`, `TakeProfitPoints` | Feste Schutzabstände in Preispunkten (0 deaktiviert jeden Schutz). |

## Implementierungshinweise
- Die StockSharp-Umgebung stellt keine Tick-Zählungen für aggregierte Kerzen bereit, daher wird das Kerzenvolumen statt des Tick-Volumens verwendet. Dies hält das Verhalten nah am ursprünglichen Indikator, während es mit verfügbaren Daten kompatibel bleibt.
- Die Strategie verfolgt nur eine kompakte TVI-Historie (wenige neueste Werte), um den `SignalBar`-Versatz zu replizieren, ohne die Repository-Richtlinie zu verletzen, die große benutzerdefinierte Sammlungen abrät.
- `StartProtection` wird nur initialisiert, wenn ein gültiger `PriceStep` verfügbar ist; andernfalls wird auf Schutz ohne feste Ziele zurückgegriffen.
- Alle Kommentare wurden auf Englisch umgeschrieben, um den Repository-Regeln zu entsprechen, und Tabulatoren werden für Einrückungen gemäß `AGENTS.md` verwendet.

## Verwendungshinweise
1. Mit dem Standard-H4-Zeitrahmen und EMA-Glättung beginnen, die den ursprünglichen Experteneinstellungen entsprechen.
2. `SignalBar` auf 0 anpassen, wenn Sie es vorziehen, auf der letzten geschlossenen Kerze zu agieren, anstatt einen Balken zu warten, aber beachten, dass dies von der MQL-Logik abweicht.
3. Bei Instruments mit unregelmäßigen Handelszeiten den Zeitfilter konfigurieren, um Signale während illiquider Perioden zu vermeiden.
4. Mit Portfolio-Level-Geldmanagement kombinieren, wenn dynamische Dimensionierung benötigt wird; `Volume` ist designbedingt statisch und spiegelt den Fixed-Lot-Ansatz des Quell-EAs wider.
