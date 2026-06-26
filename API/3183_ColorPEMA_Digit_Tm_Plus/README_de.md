# Exp Color PEMA Digit Tm Plus-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Exp Color PEMA Digit Tm Plus-Strategie** ist ein direkter Port des MetaTrader 5 Expert Advisors "Exp_ColorPEMA_Digit_Tm_Plus". Die Strategie rekonstruiert den ursprünglichen Pentuple Exponential Moving Average (PEMA)-Indikator und reproduziert jedes Handelserlaubnis-Flag aus dem EA. Orders werden auf der ausgewählten Kerzenserie nur dann ausgeführt, nachdem der Indikator einen Farbwechsel bestätigt und die optionale Wartezeit (`Signal Bar`) verstrichen ist.

Die StockSharp-Version behält die gleichen Geldverwaltungsoptionen, Stop-/Zielkontrollen und zeitbasiertes Exit, die in der MQL-Implementierung vorhanden waren. Jede Einstellung wird über `StrategyParam<T>` bereitgestellt, um die UI-Konfiguration und Optimierung zu unterstützen.

## Indikatorlogik
* Der Indikator speist eine Kaskade von acht exponentiellen gleitenden Durchschnitten mit der konfigurierten `PEMA Length` und dem `Applied Price`.
* Die letzte Linie wird auf die angeforderten `Rounding Digits` gerundet, genau wie im ursprünglichen Indikator.
* Die Steigung der gerundeten Linie erzeugt drei Zustände:
  * **Up (magenta)** – bullisher Druck, potenzielles Long-Setup.
  * **Flat (grau)** – neutral, keine Aktion.
  * **Down (dodger blue)** – bearisher Druck, potenzielles Short-Setup.
* Die Strategie speichert den Indikatorstatus jeder abgeschlossenen Kerze, sodass sie ältere Bars referenzieren kann, wenn `Signal Bar` größer als null ist.

## Handelsregeln
1. **Signalerkennung** – auf einer abgeschlossenen Kerze den Indikatorstatus auswerten, der `Signal Bar` Kerzen alt ist, und ihn mit dem vorherigen Status vergleichen.
2. **Long-Setup** – wenn der Status von irgendwas zu *Up* wechselt:
   * einen Long-Einstieg einreihen, wenn `Allow Long Entries` aktiviert ist;
   * einen Ausstieg aus bestehenden Shorts einreihen, wenn `Allow Short Exits` aktiviert ist.
3. **Short-Setup** – wenn der Status von irgendwas zu *Down* wechselt:
   * einen Short-Einstieg einreihen, wenn `Allow Short Entries` aktiviert ist;
   * einen Ausstieg aus bestehenden Longs einreihen, wenn `Allow Long Exits` aktiviert ist.
4. **Ausführungsschicht** – eingestellte Aktionen werden nur ausgeführt, wenn:
   * die Strategie online und der Handel erlaubt ist;
   * der an die Quellkerze gebundene Aktivierungszeitstempel erreicht wurde; und
   * Positionsdimensionierungsregeln ein nicht-null Volumen erlauben.
5. **Risikomanagement** –
   * optionale Stop-Loss- und Take-Profit-Niveaus werden aus dem Ausführungspreis unter Verwendung derselben Punktabstände wie in MetaTrader abgeleitet;
   * `Use Time Exit` schließt Positionen, die die konfigurierte `Holding Minutes`-Lebensdauer überschreiten;
   * Gegensignale können das Exposure sofort flattieren, wenn die jeweilige Exit-Erlaubnis aktiv ist.

## Parameter
| Name | Beschreibung |
| ---- | ----------- |
| Money Management | Basiswert für Positionsdimensionierungsregeln. |
| Money Mode | Wählt zwischen lot-basierter oder Prozent-des-Saldos/Freie-Margin-Modellen. |
| Stop Loss (points) | Abstand zum Stop Loss in Preispunkten. |
| Take Profit (points) | Abstand zum Take Profit in Preispunkten. |
| Allowed Deviation | Platzhalterparameter aus dem EA für Vollständigkeit erhalten. |
| Allow Long Entries / Allow Short Entries | Öffnen von Trades in jede Richtung aktivieren oder deaktivieren. |
| Allow Long Exits / Allow Short Exits | Schließen von Trades bei Gegensignalen aktivieren oder deaktivieren. |
| Use Time Exit | Aktiviert die zeitbasierte Flättenlogik. |
| Holding Minutes | Maximale Haltezeit einer Position, in Minuten. |
| Candle Type | Von der Strategie verarbeitete Kerzenserie. Standard H4. |
| PEMA Length | Länge für alle acht EMA-Stufen in der Pentuple EMA. |
| Applied Price | Quellpreis für die Indikatorberechnung. |
| Rounding Digits | Dezimalstellen für die Rundung der Indikatorausgabe. |
| Signal Bar | Anzahl abgeschlossener Bars, die vor der Signalauswertung gewartet werden. |

## Nutzungshinweise
* Die Strategie innerhalb eines StockSharp-Konnektors platzieren, der Zugang zum gewünschten Instrument und zur Kerzenserie bietet.
* Parameter so konfigurieren, dass sie dem zu replizierenden MetaTrader-Setup entsprechen.
* Backtests oder Live-Trading nach Bedarf ausführen; die Strategie reagiert nur auf vollständig geschlossene Kerzen.

## Konvertierungsstatus
* **C#-Version** – implementiert (`CS/ExpColorPemaDigitTmPlusStrategy.cs`).
* **Python-Version** – nicht erstellt (gemäß Anweisung).
