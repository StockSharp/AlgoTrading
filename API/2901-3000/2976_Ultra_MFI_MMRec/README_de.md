# Ultra MFI Geldmanagement-Neuberechnung Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die **Ultra MFI MMRec Strategie** ist ein direkter Port des MetaTrader 5 Expert Advisors `Exp_UltraMFI_MMRec`. Sie kombiniert einen mehrfach geglätteten Money Flow Index (MFI) Oszillator mit reihungsbasiertem Geldmanagement. Zwei interne Zähler akkumulieren, wie viele Glättungsschichten nach oben oder unten zeigen. Kreuzungen zwischen diesen Zählern generieren Handelssignale, während jüngste Handelsergebnisse bestimmen, ob die nächste Position die normale oder reduzierte Positionsgröße verwendet.

## Handelslogik
1. **Basisindikator** – ein Money Flow Index mit konfigurierbarer Länge wird auf dem ausgewählten Kerzentyp berechnet.
2. **Stufenglättung** – der MFI-Wert wird durch eine Leiter von gleitenden Durchschnitten geleitet. Jeder Schritt erhöht die Glättungslänge um einen festen Zuwachs. Unterstützte Glättungsmethoden sind Simple, Exponential, Smoothed, Linear Weighted und Jurik Moving Averages (andere MT5-spezifische Modi sind in StockSharp nicht verfügbar).
3. **Richtungszähler** – für jeden Balken vergleicht die Strategie den aktuellen und vorherigen Ausgang jedes Glättungsschritts. Wenn der Schritt steigt, erhöht sich der bullische Zähler, andernfalls der bärische. Beide Zähler werden nochmals durch einen abschließenden gleitenden Durchschnitt geglättet.
4. **Signalverschiebung** – Handelsregeln operieren auf fertigen Balken. Ein konfigurierbarer `SignalShift` teilt der Strategie mit, wie viele geschlossene Kerzen beim Vergleich der Zähler zurückgeschaut werden sollen, was das MT5-Verhalten mit `SignalBar=1` nachahmt.
5. **Einstiege und Ausstiege** –
   * Wenn der vorherige Balken stärkere Bullen zeigte (`bulls > bears`) und der neueste Balken einen Kreuzung zu `bulls < bears` zeigt, öffnet die Strategie eine Long-Position. Dieselbe Bedingung schließt auch jede offene Short-Position.
   * Wenn der vorherige Balken stärkere Bären zeigte und der neueste Balken auf `bulls > bears` wechselt, öffnet die Strategie eine Short-Position und schließt jede offene Long-Position.
   * Optionaler Stop-Loss und Take-Profit (prozentbasiert) können über `StartProtection` verwaltet werden.
6. **Geldmanagement** – die nächste Ordergröße hängt von den jüngsten Handelsergebnissen pro Richtung ab. Nach dem Schließen jeder Position wird der realisierte PnL inspiziert:
   * Die Strategie speichert die jüngsten `BuyTotalTrigger` Kauftrades und zählt, wie viele Verluste waren. Wenn die Anzahl `BuyLossTrigger` erreicht, verwendet die nächste Kauforder `ReducedVolume`, andernfalls `NormalVolume`.
   * Dieselbe Logik wird unabhängig für Verkaufstrades mit `SellTotalTrigger` und `SellLossTrigger` angewendet.

## Parameter
- **CandleType** – Instrument-Datentyp (Zeitrahmen) für die Signalerzeugung.
- **MfiPeriod** – Länge des Money Flow Index Oszillators.
- **StepSmoothing / FinalSmoothing** – Moving-Average-Typ für die Stufenschritte und die abschließenden Zähler.
- **StartLength / StepSize / StepsTotal** – Geometrie der Glättungsleiter (erste Länge, Zuwachs, Anzahl der Schritte).
- **FinalSmoothingLength** – Länge der Zähler-Glättungsphase.
- **SignalShift** – Anzahl abgeschlossener Balken, die beim Auswerten von Signalen zurückgeschaut werden.
- **NormalVolume / ReducedVolume** – Handelsgröße für normale Bedingungen und nach einer Verluststrähne.
- **BuyTotalTrigger / BuyLossTrigger** – Verlaufstiefe und Verlustschwelle zum Umschalten des nächsten Long-Trades auf reduzierte Größe.
- **SellTotalTrigger / SellLossTrigger** – analoge Einstellungen für Short-Trades.
- **AllowLongEntries / AllowShortEntries / AllowLongExits / AllowShortExits** – Einstiege und Ausstiege für jede Richtung aktivieren oder deaktivieren.
- **TakeProfitPercent / StopLossPercent** – optionale prozentbasierte Schutzniveaus.

## Verwendungshinweise
- Die Stufenglättung erfordert genügend historische Kerzen, um jeden gleitenden Durchschnitt zu füllen. Warten Sie, bis die Strategie vollständig ausgebildet ist, bevor Sie den Signalen vertrauen.
- Da StockSharp keine MT5-spezifischen Glätter wie JurX, Parabolic, VIDYA oder AMA bereitstellt, werden die nächstliegenden unterstützten Alternativen verwendet. Jurik-Glättung ist ein guter Standard, der das ursprüngliche Gefühl des UltraMFI-Indikators reproduziert.
- Das Geldmanagement basiert auf realisiertem PnL. Stellen Sie sicher, dass Ihre Backtests die Orderausführung beinhalten, damit der realisierte PnL nach jedem Positionsschluss aktualisiert wird.
- Dieser Port hält das Verhalten bei, neue Positionen nur einzugehen, wenn die aktuelle Position flach ist. Wenn ein Umkehrsignal erscheint, während die entgegengesetzte Position gehalten wird, verlässt die Strategie zuerst den bestehenden Trade und wird beim nächsten geeigneten Balken einsteigen, sobald sie flach ist.
