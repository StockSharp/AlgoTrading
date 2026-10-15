# MelBar Take325-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die MelBar Take325-Strategie ist eine direkte Umsetzung des Expert Advisor Studio-Systems „MelBar™Take325%™ 5.5Y NZD-USD“. Der Handel mit NZD/USD in beide Richtungen erfolgt mithilfe einer Kombination aus Ausbrüchen des Tick-Volumens, einem Swing-Filter basierend auf einem einfachen gleitenden Durchschnitt über 12 Perioden und einem Exit-Filter über 14 Perioden RSI. Der StockSharp-Port behält die ursprünglichen Risikoparameter eines 16-Pip-Stop-Loss und eines 45-Pip-Take-Profit bei, ausgedrückt in Pip-Abständen vom Einstiegspreis.

Die Strategie beginnt damit, auf einen Anstieg des Tick-Volumens zu warten, das als Ausbruch über den konfigurierten Volumenschwellenwert definiert ist. Wenn das Volumen zunimmt, wird geprüft, ob der einfache gleitende Durchschnitt zwei Balken zuvor einen lokalen Wendepunkt gebildet hat. Ein lokales Maximum im SMA eröffnet einen Long-Trade, während ein lokales Minimum einen Short-Trade eröffnet. Es kann jeweils nur eine Richtung eingeschlagen werden und widersprüchliche Signale werden ignoriert, um ein Umkippen auf derselben Leiste zu vermeiden.

Offene Positionen werden aktiv verwaltet. Stop-Loss- und Take-Profit-Level werden jedes Mal erzwungen, wenn eine Kerze schließt, wodurch das Verhalten der MetaTrader-Version ähnelt. Darüber hinaus wird der 14-Perioden-RSI verwendet, um Ausstiege zu erzwingen: Long-Trades werden geschlossen, wenn RSI das konfigurierte Niveau nach unten durchläuft (Standard 80), und Short-Trades werden geschlossen, wenn RSI das symmetrische Niveau nach oben kreuzt (Standard 20). Das Hoch/Tief der verarbeiteten Kerze wird mit dem Einstiegspreis verglichen, um Stop-Loss- und Take-Profit-Ausstiege auszulösen.

## Einzelheiten

- **Eintrittskriterien**:
  - **Volumenfilter**: Das Tick-Volumen vor zwei Balken muss unter dem Schwellenwert liegen, während der vorherige Balken ihn überschreitet.
  - **Lang**: SMA (Länge 12) hat einen lokalen Höhepunkt vor zwei Balken (`SMA[t-3] < SMA[t-2]` und `SMA[t-2] > SMA[t-1]`).
  - **Kurzfassung**: SMA hat einen lokalen Tiefpunkt (`SMA[t-3] > SMA[t-2]` und `SMA[t-2] < SMA[t-1]`).
- **Ausstiegskriterien**:
  - **Stop-Loss**: 16 Pips ab Einstieg, bewertet bei Kerzenschluss.
  - **Take-Profit**: 45 Pips ab Einstieg, bewertet bei Kerzenschluss.
  - **Langer RSI-Ausgang**: RSI kreuzt nach unten durch 80 (`RSI[t-3] > 80` und `RSI[t-2] < 80`).
  - **Kurzer RSI-Ausgang**: RSI kreuzt nach oben durch 20 (`RSI[t-3] < 20` und `RSI[t-2] > 20`).
- **Standardparameter**:
  - Eintrittsvolumen = 0,1 Lots.
  - Volumenschwelle = 1000 Tick-Volumeneinheiten.
  - SMA Zeitraum = 12.
  - RSI Zeitraum = 14.
  - RSI Level = 80 (kurzer Exit verwendet 100 - Level).
  - Kerzenzeitrahmen = 30 Minuten.
- **Markt**: Konzipiert für NZD/USD, kann aber auch auf andere FX-Paare angewendet werden.
- **Stil**: Momentum-Ausbruch mit Mean-Reversion-Ausstiegen.
- **Stops**: Fester Stop-Loss und Take-Profit; kein Trailing Stop im Originalcode.
- **Komplexität**: Moderat; kombiniert mehrere Filter, aber keine Positionsskalierung.
- **Risiko**: Mittel, da der Stop enger als der Take-Profit ist, aber beide feste Distanzen haben.
