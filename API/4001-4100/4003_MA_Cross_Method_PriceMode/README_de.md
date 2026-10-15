# MA Cross Method PriceMode-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **MA Cross Method PriceMode**-Strategie ist eine direkte StockSharp-Portierung des MetaTrader 4-Experten „MA_cross_Method_PriceMode“. Es kombiniert zwei konfigurierbare gleitende Durchschnitte und reagiert immer dann, wenn der schnelle Durchschnitt den langsamen Durchschnitt kreuzt. Beide Zeilen stellen die ursprünglichen MetaTrader-Eingaben dar: Zeitraum, Glättungsmethode (SMA, EMA, SMMA, LWMA), angewandter Preis (Schlusskurs, Eröffnungskurs, Höchstkurs, Tiefstkurs, Medianwert, typisch, gewichtet) und horizontale Verschiebung. Die Strategie funktioniert mit jedem Instrument, das regelmäßig zeitbasierte Kerzen bereitstellt.

## Indikatoren
- **Fast Moving Average** – konfigurierbare Länge, Methode und Preisquelle. Der Verschiebungsparameter MetaTrader wird reproduziert, indem die abgeschlossenen Indikatorwerte gepuffert und der Wert `FirstShift` Balken zurückgelesen werden.
- **Langsamer gleitender Durchschnitt** – konfigurierbare Länge, Methode und Preisquelle mit derselben Shift-Emulation über Pufferung.

## Handelslogik
1. Die Strategie abonniert den ausgewählten Kerzentyp und verarbeitet nur fertige Kerzen, um ein Neulackieren innerhalb des Balkens zu vermeiden.
2. Für jeden geschlossenen Balken werden beide gleitenden Durchschnitte mit den jeweils angewendeten Preisen gefüttert.
3. Wenn beide Durchschnittswerte Endwerte ergeben, wertet die Strategie zwei Bedingungen aus:
   - **Bullish Cross** – der schnelle MA lag unter oder gleich dem langsamen MA des vorherigen Balkens und bewegt sich auf dem aktuellen Balken darüber.
   - **Bearish Cross** – der schnelle MA lag über oder gleich dem langsamen MA des vorherigen Balkens und bewegt sich unter diesem Wert des aktuellen Balkens.
4. Bei einem bullischen Cross kauft die Strategie `OrderVolume` Kontrakte. Wenn eine Short-Position offen ist, wird die Ordergröße automatisch erhöht, um sowohl die Short-Position abzudecken als auch das neue Long-Engagement aufzubauen.
5. Bei einem rückläufigen Cross verkauft die Strategie `OrderVolume` Kontrakte. Wenn eine Long-Position offen ist, wird die Ordergröße erhöht, um sie zu schließen, bevor die Short-Position eingerichtet wird.
6. `StartProtection()` wird aufgerufen, damit bei Bedarf StockSharp Schutzmodule hinzugefügt werden können (z. B. Stop-Loss- oder Break-Even-Assistenten).

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `FirstPeriod` | Periode des schnellen gleitenden Durchschnitts. | `3` |
| `SecondPeriod` | Periode des langsamen gleitenden Durchschnitts. | `13` |
| `FirstMethod` | Glättungsmethode für den schnellen gleitenden Durchschnitt (`Simple`, `Exponential`, `Smoothed`, `LinearWeighted`). | `Simple` |
| `SecondMethod` | Glättungsmethode für den langsamen gleitenden Durchschnitt. | `LinearWeighted` |
| `FirstPriceMode` | Angewendeter Preis für den schnell gleitenden Durchschnitt (`Close`, `Open`, `High`, `Low`, `Median`, `Typical`, `Weighted`). | `Close` |
| `SecondPriceMode` | Angewandter Preis für den langsam gleitenden Durchschnitt. | `Median` |
| `FirstShift` | Horizontale Verschiebung (in Balken), angewendet auf den sich schnell bewegenden Durchschnitt. | `0` |
| `SecondShift` | Horizontale Verschiebung (in Balken), angewendet auf den langsam gleitenden Durchschnitt. | `0` |
| `OrderVolume` | Basisauftragsvolumen, das für neue Positionen verwendet wird. | `0.1` |
| `CandleType` | Von der Strategie verarbeiteter Kerzentyp/Zeitrahmen. | 5-Minuten-Kerzen |

## Unterschiede im Vergleich zur MQL-Version
- Die MetaTrader-Auftragsiteration (`OrdersTotal`, `OrderSelect`, `OrderClose`) wird durch die direkte Verwendung der Eigenschaft StockSharp `Strategy.Position` und Marktaufträge ersetzt, deren Größe bei Bedarf so dimensioniert ist, dass das Risiko umgekehrt wird.
- Das Flag „Neuer Balken“ MetaTrader ist nicht erforderlich: `ProcessCandle` wird genau einmal pro fertiger Kerze ausgeführt, wodurch das gleiche Verhalten einmal pro Balken ohne Abfragen auf Tick-Ebene sichergestellt wird.
- Die Handhabung der MA-Verschiebung wird mit kompakten Puffern implementiert, die die letzten `shift + 2`-Werte für jeden Durchschnitt enthalten. Dies spiegelt die Indikatorverschiebung wider, ohne sich auf verbotene Indikator-Rückverweise (`GetValue`) zu verlassen.
- Die Strategie ist maklerunabhängig; Risikomanagement-Helfer können über `StartProtection()` anstelle der festen MetaTrader Stopp-/Limit-Argumente angehängt werden.

## Nutzungshinweise
- Wählen Sie eine Kerzendauer, die dem ursprünglichen Zeitrahmen entspricht (z. B. M5 oder H1). Benutzerdefinierte Zeitrahmen können durch Bearbeiten von `CandleType` in den Strategieparametern bereitgestellt werden.
- Das Setzen von `FirstShift` oder `SecondShift` auf einen positiven Wert verzögert den effektiven Crossover um entsprechend viele abgeschlossene Balken, genau wie die horizontale Verschiebungseingabe in MetaTrader.
- Der Preismodus `Weighted` reproduziert die `(High + Low + 2 * Close) / 4`-Formel von MetaTrader. Der Median- und der typische Modus folgen den Standarddefinitionen `(High + Low) / 2` und `(High + Low + Close) / 3`.
- Da es sich bei jeder Order um eine Marktorder handelt, stellen Sie sicher, dass die Kontokonfiguration das angeforderte Volumen und die angeforderte Slippage toleriert.
