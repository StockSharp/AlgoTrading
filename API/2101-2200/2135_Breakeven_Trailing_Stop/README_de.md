# Breakeven-Trailing-Stop-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die zeigt, wie man den Stop-Loss auf Breakeven verschiebt und ihn dann mit dem Preisanstieg verfolgt.
Die Strategie eröffnet eine Long-Position und verwaltet sie in zwei Phasen:
1. Nachdem der Preis `BreakevenPlus` Punkte gewonnen hat, wird der Stop auf `BreakevenStep` Punkte über den Einstiegspreis verschoben.
2. Wenn der Preis mit `TrailingPlus` Punkten Gewinn über dem Stop weitergeht, verfolgt der Stop den Preis in einem Abstand von `TrailingStep` Punkten.

Die Logik ist symmetrisch für Short-Positionen, wenn eine manuell eröffnet wird.

## Details

- **Einstiegskriterien**: Eröffnet eine Long-Position auf der ersten abgeschlossenen Kerze.
- **Long/Short**: Beide (Beispiel verwendet Long).
- **Ausstiegskriterien**: Preis kreuzt den Trailing Stop.
- **Stops**: Breakeven und Trailing Stop.
- **Standardwerte**:
  - `BreakevenPlus` = 5
  - `BreakevenStep` = 2
  - `TrailingPlus` = 3
  - `TrailingStep` = 1
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filter**:
  - Kategorie: Stop-Verwaltung
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Breakeven, Trailing
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
