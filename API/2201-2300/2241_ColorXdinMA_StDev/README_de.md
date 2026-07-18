# ColorXdinMA mit Standardabweichung-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist ein StockSharp-Port des MQL5-Experten **Exp_ColorXdinMA_StDev**.
Sie kombiniert zwei gleitende Durchschnitte zu einer einzigen Linie namens `XdinMA` und verfolgt deren
Änderung im Laufe der Zeit. Die Differenz zwischen dem aktuellen und dem vorherigen `XdinMA`-Wert
wird mit einem Vielfachen seiner jüngsten Standardabweichung verglichen. Wenn die
Änderung den positiven Schwellenwert überschreitet, wird eine Long-Position eröffnet, während ein Rückgang
unter den negativen Schwellenwert eine Short-Position eröffnet.

## Funktionsweise

1. Zwei einfache gleitende Durchschnitte werden berechnet:
   - **Main MA** – Periode definiert durch `MainLength`.
   - **Plus MA** – Periode definiert durch `PlusLength`.
2. Die benutzerdefinierte Linie `XdinMA = 2 * MainMA - PlusMA` wird erstellt.
3. Die Änderung von `XdinMA` zwischen aufeinanderfolgenden Kerzen wird an einen Standardabweichungsindikator mit der Länge `StdPeriod` übergeben.
4. Wenn die Änderung größer als `K1 * StdDev` ist, wird eine Kauforder platziert. Wenn sie kleiner als `-K1 * StdDev` ist, wird eine Verkaufsorder platziert. Vorhandene entgegengesetzte Positionen werden vor dem Öffnen einer neuen geschlossen.

## Parameter

| Parameter   | Beschreibung                                       |
|-------------|----------------------------------------------------|
| `MainLength`| Periode für den primären gleitenden Durchschnitt.  |
| `PlusLength`| Periode für den sekundären gleitenden Durchschnitt.|
| `StdPeriod` | Anzahl der Balken für die Standardabweichung.      |
| `K1`        | Multiplikator für den Abweichungsschwellenwert.    |
| `K2`        | Reserviert für zukünftige Erweiterung des zweiten Filters.|

Alle Parameter werden über `StrategyParam` bereitgestellt, sodass sie optimiert oder
über die Benutzeroberfläche geändert werden können.

## Hinweise

- Es werden nur abgeschlossene Kerzen verarbeitet.
- Die Strategie verwendet Marktorders und implementiert keine Stop-Loss- oder
  Take-Profit-Logik.
- Die Chart-Darstellung enthält beide gleitenden Durchschnitte und ausgeführte Trades für die visuelle
  Analyse.
