# CAi Standardabweichungs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine StockSharp-Portierung des ursprünglichen MQL5-Experten **Exp_i-CAi_StDev**. Sie kombiniert einen gleitenden Durchschnitt mit Standardabweichungsbändern, um Ausbrüche und anschließende Umkehrungen zu erkennen.

## Strategielogik

1. Einen einfachen gleitenden Durchschnitt (SMA) über den angegebenen Zeitraum berechnen.
2. Die Standardabweichung der Schlusskurse über denselben Zeitraum berechnen.
3. Zwei Bandsets um den SMA aufbauen:
   - **Eintrittsbänder**: SMA ± `OpenMultiplier` × StdDev.
   - **Ausstiegsbänder**: SMA ± `CloseMultiplier` × StdDev.
4. Eine Long-Position eröffnen, wenn der Kurs über das obere Eintrittsband schließt.
5. Eine Short-Position eröffnen, wenn der Kurs unter das untere Eintrittsband schließt.
6. Eine bestehende Long-Position schließen, wenn der Kurs unter das obere Ausstiegsband fällt.
7. Eine bestehende Short-Position schließen, wenn der Kurs über das untere Ausstiegsband steigt.

## Parameter

| Name | Beschreibung | Standard |
| --- | --- | --- |
| `MaLength` | Länge der Berechnung für gleitenden Durchschnitt und Standardabweichung | 12 |
| `StdDevPeriod` | Periode für den Standardabweichungsindikator | 9 |
| `OpenMultiplier` | Multiplikator für Eintrittsbänder | 2.5 |
| `CloseMultiplier` | Multiplikator für Ausstiegsbänder | 1.5 |
| `CandleType` | Von der Strategie verwendeter Kerzentyp | 5-Minuten-Kerzen |

## Hinweise

- Die Strategie verwendet die High-Level-API mit `Bind`, um Indikatorwerte zu empfangen.
- Nur abgeschlossene Kerzen werden verarbeitet, um vorzeitige Signale zu vermeiden.
- Alle Kommentare im Quellcode sind auf Englisch.
