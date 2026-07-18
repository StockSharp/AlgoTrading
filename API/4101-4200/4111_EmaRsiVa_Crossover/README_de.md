# EMA RSI Volatilitätsadaptive Crossover-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine direkte Portierung des MetaTrader Expert Advisors **EA_MARSI_1-02**. Es handelt sich um Crossovers zwischen zwei Kopien von
Der benutzerdefinierte *EMA_RSI_VA*-Indikator von Integer, ein volatilitätsadaptiver gleitender Durchschnitt, der vom Relative Strength Index (RSI) gesteuert wird.
Immer wenn die langsame Linie die schnelle Linie kreuzt, kehrt der Motor die Nettoposition um und reproduziert so den ursprünglichen „Flip-on-Crossover“.
Verhalten unter Beachtung der Best Practices für die Auftragsabwicklung von StockSharp.

## Anzeigemechanik

Das Originalpaket MQL wird mit einem benutzerdefinierten Indikator namens `EMA_RSI_VA` geliefert. Es berechnet einen preisgeglätteten EMA, dessen Effektivwert
Die Länge wird durch den Abstand von RSI von seinem neutralen Wert moduliert. Der StockSharp-Port führt das ein
`EmaRsiVolatilityAdaptiveIndicator`-Klasse, die die Formel genau repliziert:

1. Berechnen Sie RSI für die ausgewählte `AppliedPrice`-Quelle mit dem Zeitraum `RSIPeriod`.
2. Messen Sie den RSI-Abstand von 50 (`|RSI - 50| + 1`), der als Volatilitäts-Proxy dient.
3. Leiten Sie einen adaptiven Multiplikator ab
`multi = (5 + 100 / RSIPeriod) / (0.06 + 0.92 * dist + 0.02 * dist^2)`.
4. Multiplizieren Sie den konfigurierten Zeitraum EMA mit diesem Multiplikator, um eine dynamische Länge `pdsx` zu erhalten.
5. Wenden Sie die Standardrekursion EMA mit dem Glättungsfaktor `2 / (pdsx + 1)` an und verwenden Sie dabei den angewendeten Preis der Kerze als Eingabe.

Große RSI-Auslenkungen verkürzen das Glättungsfenster und sorgen dafür, dass die Leitung schneller reagiert. Ein flacher RSI verlängert das Fenster und dämpft
Lärm. Sowohl die langsame als auch die schnelle Zeile stellen den gesamten Satz der von `StockSharp.Messages.AppliedPrice` unterstützten Preismodi bereit.

## Handelsregeln

- **Signalerkennung**
  - *Verkaufen / Leerverkauf*: vorheriges langsames < vorheriges schnelles **und** aktuelles langsames ≥ aktuelles schnelles.
  - *Kauf / Long*: vorheriges langsames > vorheriges schnelles **und** aktuelles langsames ≤ aktuelles schnelles.
- **Ausführung**
  - Die Strategie analysiert nur fertige Kerzen aus der konfigurierten Kerzenserie.
  - Wenn ein Signal auftritt, wird eine Marktorder übermittelt, die sowohl das bestehende Engagement schließt als auch die neue Richtung eröffnet.
  - Umtauschlimits werden durch `Security.MinVolume`, `Security.VolumeStep` und `Security.MaxVolume` eingehalten.
- **Umbuchungen**
  - Aufträge werden saldiert, sodass ein einzelner `SellMarket`- oder `BuyMarket`-Aufruf die Position über der Nulllinie annimmt und mit der übereinstimmt
MQL-Verhalten, bei dem ein entgegengesetztes Signal den Handel sofort umkehrt.

## Risikomanagement

- `TakeProfitPoints` und `StopLossPoints` replizieren die TP/SL-Felder des Expert Advisors (ausgedrückt in Preispunkten). Wenn entweder
Der Wert ist ungleich Null. Die Strategie startet den Schutzmanager von StockSharp mit absoluten Preisversätzen und `useMarketOrders = true`.
um die ursprüngliche `OrderSend` Stopp-/Limit-Änderungsschleife widerzuspiegeln.
- `UseBalanceMultiplier` implementiert den `use_Multpl`-Schalter. Bei Aktivierung wird das effektive Auftragsvolumen erhöht
`Volume * PortfolioEquity / MaxDrawdown` mit einer defensiven Klammer, um Einschränkungen auszutauschen.
- Der Aufruf der Basisklasse `StartProtection()` wird weiterhin ausgeführt, sodass externe Risikomodule Trailing- oder Break-Even-Anhänge anhängen können
Logik, falls erforderlich.

## Parameter

| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `Volume` | `0.1` | Basis-Market-Order-Größe vor Anwendung eines Saldomultiplikators. |
| `TakeProfitPoints` | `0` | Take-Profit-Distanz in Instrumentenpunkten; `0` deaktiviert die Take-Profit-Komponente. |
| `StopLossPoints` | `0` | Stop-Loss-Distanz in Instrumentenpunkten; `0` deaktiviert den Schutzstopp. |
| `UseBalanceMultiplier` | `false` | Ermöglicht die Balance-proportionale Positionsgröße, identisch mit `use_Multpl` im EA. |
| `MaxDrawdown` | `10000` | Nenner für den Saldomultiplikator; entspricht dem `Max_drawdown` von EA. |
| `SlowRsiPeriod` | `310` | RSI-Lookback für die langsame EMA_RSI_VA-Zeile. |
| `SlowEmaPeriod` | `40` | Basislänge von EMA für die langsame Zeile vor der Adaption von RSI. |
| `SlowAppliedPrice` | `Close` | Der Preismodus wird an den langsamen Indikator weitergeleitet. |
| `FastRsiPeriod` | `200` | RSI-Lookback für die schnelle EMA_RSI_VA-Zeile. |
| `FastEmaPeriod` | `50` | Basislänge EMA für die schnelle Linie vor der RSI-Anpassung. |
| `FastAppliedPrice` | `Close` | Preismodus wird an den Schnellindikator weitergeleitet. |
| `CandleType` | `TimeFrame(1m)` | Für Berechnungen verwendete Kerzenreihe. |

## Hinweise zur Implementierung

- Der Port wird mit StockSharps High-Level-API (`SubscribeCandles().Bind(...)`) geschrieben, um manuelle Indikatorschleifen zu vermeiden.
- Es werden nur abgeschlossene Kerzen verarbeitet, die mit `CopyBuffer(..., 1, 2, ...)`-Aufrufen in der MQL-Quelle übereinstimmen.
- Die Volumennormalisierung verwendet `Security.MinVolume`, `Security.VolumeStep` und `Security.MaxVolume` und verhindert so ungültige Bestellungen
echter Austausch.
- Auf eine Python-Version wird wie gewünscht bewusst verzichtet; Das Verzeichnis enthält nur die C#-Implementierung und Dokumentation.

Das resultierende Verhalten spiegelt die Quelle EA wider und legt gleichzeitig StockSharp-freundliche Parameter und geeignete Risikokontrollen offen
Designer, Runner oder jeder benutzerdefinierte Host, der auf StockSharp API basiert.
