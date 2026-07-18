# Wajdyss MA Expertenstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Wajdyss MA Expert Strategy** ist eine C#-Portierung des MetaTrader 4 Expert Advisors „wajdyss MA expert v3“. Es vergleicht zwei gleitende Durchschnitte, die mit unabhängigen Zeiträumen, Berechnungsmodi, Verschiebungen und angewendeten Preisen konfiguriert sind. Ein bullischer Crossover des schnellen Durchschnitts über den langsamen Durchschnitt eröffnet ein Long-Engagement, während ein bärischer Crossover ein Short-Engagement eröffnet. Die Konvertierung reproduziert die ursprünglichen Money-Management-Regeln, optionale automatische Schließung gegensätzlicher Geschäfte und Liquidationsfilter am Ende des Tages/am Ende der Woche.

## Handelslogik
1. Abonnieren Sie die ausgewählten `CandleType` (standardmäßig 15-Minuten-Kerzen) und berechnen Sie die schnellen und langsamen gleitenden Durchschnitte mithilfe der ausgewählten `MovingAverageMethod`- und `PriceSource`-Einstellungen für jedes Bein.
2. Speichern Sie die Indikatorwerte für fertige Kerzen. Bewerten Sie ein bullisches Signal, wenn der schnelle Durchschnitt (mit seiner konfigurierten Verschiebung) über dem langsamen Durchschnitt des letzten geschlossenen Balkens liegt, während er vor zwei Balken unter diesem lag. Bewerten Sie ein bärisches Signal mit der umgekehrten Bedingung.
3. Erzwingen Sie eine Abklingzeit zwischen neuen Einträgen in die gleiche Richtung. Die Strategie muss nach dem letzten Trade dieser Seite mindestens eine volle Kerze des abonnierten Zeitrahmens warten und spiegelt damit den globalen Variablen-Timing-Guard der MT4-Version wider.
4. Wenn **AutoCloseOpposite** aktiviert ist, stornieren Sie Arbeitsaufträge und kehren Sie das Engagement in einer einzigen Marktorder um: Das neue Auftragsvolumen umfasst alle ausstehenden Positionen in die entgegengesetzte Richtung, sodass das Konto sofort umgedreht wird.
5. Wenden Sie Tages- und Freitagsabschlussfilter an. Nach dem konfigurierten `DailyCloseHour`/`DailyCloseMinute` oder `FridayCloseHour`/`FridayCloseMinute` werden alle Positionen abgeflacht und neue Trades bis zur nächsten Sitzung blockiert.

## Risiko- und Geldmanagement
- **TakeProfitPips**, **StopLossPips** und **TrailingStopPips** werden in ganzen Pips interpretiert. Die Implementierung wandelt sie unter Verwendung der Sicherheitsmetadaten in Preisschritte um und steuert die `StartProtection`-Engine von StockSharp mit Marktausgängen für Parität mit der ursprünglichen Trailing-Logik.
- **UseMoneyManagement** emuliert die MT4-Lotberechnung: `volume = (account_balance / BalanceReference) * InitialVolume`. Umtauschgrenzen werden durch Volumenschritt-, Mindest- und Höchstprüfungen eingehalten.
- Wenn die Geldverwaltung deaktiviert ist, verwenden Bestellungen direkt **InitialVolume**.

## Parameter
| Parameter | Typ | Standard | Beschreibung |
|-----------|------|---------|-------------|
| `FastPeriod` | `int` | `10` | Periode des schnellen gleitenden Durchschnitts. |
| `FastShift` | `int` | `0` | Balken zum Verschieben des schnellen Durchschnitts vor dem Vergleich der Crossover-Werte. |
| `FastMethod` | `MovingAverageMethod` | `Ema` | Gleitender Durchschnittsmodus für die schnelle Linie (`Sma`, `Ema`, `Smma`, `Lwma`). |
| `FastPriceType` | `PriceSource` | `Close` | Der Kerzenpreis floss in den sich schnell bewegenden Durchschnitt ein (`Close`, `Open`, `High`, `Low`, `Median`, `Typical`, `Weighted`). |
| `SlowPeriod` | `int` | `20` | Periode des langsamen gleitenden Durchschnitts. |
| `SlowShift` | `int` | `0` | Balken zur Verschiebung des langsamen Durchschnitts vor dem Vergleich. |
| `SlowMethod` | `MovingAverageMethod` | `Ema` | Moving-Average-Modus für die langsame Linie. |
| `SlowPriceType` | `PriceSource` | `Close` | Der Kerzenpreis trug zum langsamen Durchschnitt bei. |
| `TakeProfitPips` | `decimal` | `100` | Abstand zum Gewinnziel in Pips (zum Deaktivieren auf `0` setzen). |
| `StopLossPips` | `decimal` | `50` | Abstand zum Schutzstopp in Pips (zum Deaktivieren auf `0` einstellen). |
| `TrailingStopPips` | `decimal` | `0` | Trailing-Stop-Distanz in Pips (zum Deaktivieren auf `0` setzen). |
| `AutoCloseOpposite` | `bool` | `true` | Schließen Sie das gegnerische Engagement, bevor Sie einen neuen Trade in die andere Richtung eröffnen. |
| `InitialVolume` | `decimal` | `0.1` | Bestimmen Sie das Handelsvolumen, bevor Sie das Money-Management anwenden. |
| `UseMoneyManagement` | `bool` | `true` | Aktivieren Sie die ausgleichsbasierte Positionsgrößenbestimmung. |
| `BalanceReference` | `decimal` | `1000` | Divisor, der beim Skalieren des Volumens mit dem Kontostand verwendet wird. |
| `DailyCloseHour` | `int` | `23` | Stunde (0-23), nach der tägliche Positionen geschlossen werden. |
| `DailyCloseMinute` | `int` | `45` | Minutenkomponente des täglichen Abschlussfilters. |
| `FridayCloseHour` | `int` | `22` | Stunde (0-23), nach der der Freitagshandel endet. |
| `FridayCloseMinute` | `int` | `45` | Minutenkomponente des Freitagsschlussfilters. |
| `CandleType` | `DataType` | `15m` Zeitrahmen | Kerzenserien, die für Berechnungen und Abklingzeiten verwendet werden. |

## Notizen
- Die Strategie basiert ausschließlich auf dem übergeordneten StockSharp API: Kerzen werden über `SubscribeCandles` verarbeitet, Indikatorbindungen versorgen gleitende Durchschnitte und `StartProtection` verwaltet Stops/Take-Profit/Trailing-Orders.
- Beim Positionsflattening werden Marktaufträge verwendet, um die unmittelbaren Schließungen entgegengesetzter Tickets durch den MT4-Experten widerzuspiegeln.
- In diesem Ordner ist keine Python-Übersetzung enthalten; Es wird nur die C#-Implementierung bereitgestellt.
