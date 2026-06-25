# Trend RDS
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Trend RDS sucht nach klaren direktionalen Sequenzen in der Kursbewegung. Wenn drei abgeschlossene Kerzen strikt höhere Tiefs bilden, behandelt die Strategie die Struktur als bullisches Trendsegment. Drei strikt niedrigere Hochs markieren eine bärische Konfiguration. Eine Schutzregel blockiert Einstiege, wenn dieselben drei Bars gleichzeitig sowohl höhere Tiefs als auch niedrigere Hochs erzeugen, was üblicherweise ein sich zusammenziehendes Dreieck anstatt einer direktionalen Bewegung anzeigt. Die Strategie kann optional die Richtung über den `Reverse`-Parameter umkehren.

Der Handel ist auf ein konfigurierbares Zeitfenster begrenzt (Standard 09:00–12:00). Wenn das Fenster offen ist und ein gültiges Muster erscheint, schließt die Strategie jede entgegengesetzte Exposure, eröffnet eine neue Marktposition beim Kerzenschluss und platziert Stop-Loss- und Take-Profit-Orders gemessen in Pips. Der Pip-Abstand wird aus dem Preisschritt des Instruments abgeleitet und spiegelt die ursprüngliche MetaTrader-Logik wider. Ein optionaler Trailing-Stop bewegt den Schutz-Stop vorwärts, sobald der Preis um den Trailing-Abstand plus den Trailing-Schritt vorgerückt ist. Trailing-Anpassungen werden nur ausgewertet, während das Sitzungsfenster aktiv ist.

Die Positionsgröße wird bei jedem Einstieg neu berechnet. Die Strategie weist einen Bruchteil des Portfolio-Eigenkapitals zu, der durch `RiskPercent` definiert wird, und dividiert ihn durch das monetäre Risiko, das durch den gewählten Stop-Abstand dargestellt wird. Dies erzeugt eine dynamische Größenbestimmung, die sowohl mit der Kontogröße als auch mit der Stop-Breite skaliert und dabei den Mindestwert `Volume` respektiert. Das Setzen eines risikobezogenen Parameters auf null deaktiviert diese Funktion und ermöglicht bei Bedarf Einstiege mit fester Größe oder ohne Schutz.

## Details
- **Einstiegskriterien**: Drei aufeinanderfolgende Kerzen mit höheren Tiefs lösen Longs aus (oder Shorts, wenn `Reverse` wahr ist). Drei aufeinanderfolgende niedrigere Hochs lösen Shorts aus (oder Longs im Umkehrmodus). Signale werden ignoriert, wenn dieselben drei Bars beide Bedingungen gleichzeitig erfüllen.
- **Long/Short**: Beide Richtungen mit einem optionalen Umkehrschalter.
- **Ausstiegskriterien**: Marktausstiege, wenn die verfolgten Stop-Loss-, Take-Profit- oder Trailing-Stop-Niveaus durchbrochen werden.
- **Stops**: Fester Stop-Loss und Take-Profit in Pips mit einem inkrementellen Trailing-Stop (erfordert, dass beide Trailing-Parameter positiv sind).
- **Zeitfenster**: Handelt nur zwischen `StartTime` und `EndTime` (Standard 09:00–12:00 Börsenzeit).
- **Positionsgrößenbestimmung**: Risikobasierte Größenbestimmung unter Verwendung von `RiskPercent` des Portfolio-Eigenkapitals relativ zum aktuellen Stop-Abstand (greift auf `Volume` zurück, wenn die Größenbestimmung nicht berechnet werden kann).
- **Standardwerte**:
  - `StopLossPips` = 30
  - `TakeProfitPips` = 65
  - `TrailingStopPips` = 0
  - `TrailingStepPips` = 5
  - `RiskPercent` = 3
  - `StartTime` = 09:00
  - `EndTime` = 12:00
  - `Reverse` = false
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Kursbewegung (Hochs/Tiefs)
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
