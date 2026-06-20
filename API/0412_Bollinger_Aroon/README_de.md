# Bollinger Aroon-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Bollinger Aroon-Strategie sucht nach Rücksetzern innerhalb eines starken Aufwärtstrends.
Wenn der Preis unter das untere Bollinger Band fällt, der Aroon Up-Wert jedoch
erhöht bleibt, geht das System davon aus, dass der Trend intakt ist, und sucht
nach einer Rückkehr zum Mittelwert. Es handelt nur Long und versucht, den
Rückprall nach einem temporären Rücksetzer zu nutzen.

Das Setup wird ausgelöst, nachdem eine abgeschlossene Kerze unterhalb des unteren
Bandes schließt, während *Aroon Up* den Bestätigungslevel überschreitet. Die Position
bleibt offen, bis der Aroon-Wert unter einen Stop-Schwellenwert fällt oder der Preis
bis zum oberen Band steigt. Die Bandbreite passt sich der Volatilität an, wodurch
die Strategie sowohl in ruhigen als auch in aktiven Märkten handeln kann.

Backtests an wichtigen Krypto-Paaren zeigen, dass der Ansatz während starker Trends
mit gelegentlichen Ausschüttelern hervorragend abschneidet. Da Einstiege sowohl
eine Volatilitätserweiterung als auch einen anhaltenden Aroon Up-Wert erfordern,
werden Fehlsignale im Vergleich zu einer einfachen Bollinger-Umkehr reduziert.

## Details
- **Daten**: Kurskerzen.
- **Einstiegskriterien**:
  - **Long**: Schlusskurs unter unterem Band UND `Aroon Up` > Bestätigungslevel.
  - **Short**: nicht verwendet.
- **Ausstiegskriterien**:
  - Schlusskurs berührt oberes Band ODER `Aroon Up` < Stop-Level.
- **Stops**: Indikatorbasiert; kein fester Stop standardmäßig.
- **Standardwerte**:
  - `BBLength` = 20
  - `BBMultiplier` = 2.0
  - `AroonLength` = 288
  - `AroonConfirmation` = 90
  - `AroonStop` = 70
- **Filter**:
  - Kategorie: Mean Reversion innerhalb des Trends
  - Richtung: Nur Long
  - Indikatoren: Bollinger Bands, Aroon
  - Komplexität: Moderat
  - Risikolevel: Mittel
