# Exp i-KlPrice Vol Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Diese Strategie ist eine C#-Konvertierung des MetaTrader-Experten **Exp_i-KlPrice_Vol.mq5**. Sie baut den KlPrice-Oszillator
neu auf, der den Abstand zwischen Preis und einem Volatilitätsband misst, multipliziert den Oszillator mit dem Kerzenvolumen
und verfolgt Farbübergänge, die durch adaptive Schwellenwerte erzeugt werden. Zwei unabhängige Positions-Slots werden für jede
Richtung emuliert, was das Dual-Magic-Verhalten des ursprünglichen Expert Advisors widerspiegelt.

## Indikatorlogik
- Preis wird mit dem ausgewählten `AppliedPrice`-Modus transformiert (Close, Open, Median, Demark usw.).
- Der transformierte Preis wird durch die in `PriceMaMethod` und `PriceMaLength` definierte Methode des gleitenden Durchschnitts
  geglättet.
- Die Kerzenspanne (`High - Low`) wird mit `RangeMaMethod`/`RangeMaLength` geglättet. Die Spanne fungiert als dynamische
  Bandbreite.
- Der Basis-KlPrice-Oszillator wird als `100 * (Price - (MA - RangeMA)) / (2 * RangeMA) - 50` berechnet.
- Der Oszillator wird mit der ausgewählten Volumensquelle (`AppliedVolume.Tick` oder `AppliedVolume.Real`) multipliziert.
- Ein Jurik-Glätter der Länge `SmoothingLength` wird sowohl auf den Oszillator als auch auf das Rohvolumen angewendet und
  erzeugt zwei adaptive Reihen.
- Adaptive Schwellenwerte werden ermittelt, indem das geglättete Volumen mit `HighLevel2`, `HighLevel1`, `LowLevel1` und
  `LowLevel2` multipliziert wird.
- Die aktuelle Oszillatorfarbe wird durch den Vergleich des geglätteten Oszillatorwerts mit den adaptiven Schwellenwerten
  bestimmt:
  - **4** – über `HighLevel2 * volume` (extremer bullischer Druck).
  - **3** – zwischen `HighLevel1 * volume` und dem Extremniveau.
  - **2** – zwischen den bullischen und bärischen Schwellenwerten.
  - **1** – zwischen dem unteren Schwellenwert und der Neutrallinie.
  - **0** – unter `LowLevel2 * volume` (extremer bärischer Druck).

## Handelsregeln
1. Die Farbe bei `SignalBar` (in der Regel die vorherige abgeschlossene Kerze) und die Farbe davor auswerten.
2. Long-Einträge:
   - Slot 1 öffnet, wenn die Farbe von **4** auf einen beliebigen Wert unter **4** wechselt und `AllowLongEntry` `true` ist.
   - Slot 2 öffnet, wenn die Farbe von **3** auf unter **3** wechselt.
3. Short-Einträge:
   - Slot 1 öffnet, wenn die Farbe von **0** auf über **0** steigt und `AllowShortEntry` `true` ist.
   - Slot 2 öffnet, wenn die Farbe von **1** auf über **1** steigt.
4. Long-Ausstiege erfolgen, wenn die frühere Farbe **0** oder **1** war und `AllowLongExit` aktiviert ist.
5. Short-Ausstiege erfolgen, wenn die frühere Farbe **4** oder **3** war und `AllowShortExit` aktiviert ist.
6. Jeder Slot verfolgt den Zeitpunkt des letzten Signals, um doppelte Orders auf derselben Kerze zu vermeiden. Schutz-Stops
   sind optional und werden durch `StartProtection` gehandhabt, wenn `StopLossPoints` oder `TakeProfitPoints` größer als null
   sind.

## Parameter
| Name | Typ | Standard | Beschreibung |
|------|-----|----------|--------------|
| `PrimaryVolume` | `decimal` | `0.1` | Volumen des ersten Long/Short-Slots. |
| `SecondaryVolume` | `decimal` | `0.2` | Volumen des zweiten Slots. |
| `StopLossPoints` | `int` | `1000` | Optionaler Schutz-Stop-Abstand in Preisschritten. |
| `TakeProfitPoints` | `int` | `2000` | Optionaler Take-Profit-Abstand in Preisschritten. |
| `AllowLongEntry` | `bool` | `true` | Öffnen von Long-Positionen aktivieren. |
| `AllowShortEntry` | `bool` | `true` | Öffnen von Short-Positionen aktivieren. |
| `AllowLongExit` | `bool` | `true` | Long-Positionen bei bärischen Farben schließen. |
| `AllowShortExit` | `bool` | `true` | Short-Positionen bei bullischen Farben schließen. |
| `CandleType` | `DataType` | `H8` | Kerzen-Zeitrahmen für Berechnungen. |
| `PriceMaMethod` | `SmoothMethod` | `Sma` | Typ des gleitenden Durchschnitts für den angewendeten Preis. |
| `PriceMaLength` | `int` | `100` | Länge des Preis-Glätters. |
| `PriceMaPhase` | `int` | `15` | Phasenparameter für Jurik-basierte Filter. |
| `RangeMaMethod` | `SmoothMethod` | `Jjma` | Typ des gleitenden Durchschnitts für die Kerzenspanne. |
| `RangeMaLength` | `int` | `20` | Länge des Spannen-Glätters. |
| `RangeMaPhase` | `int` | `100` | Phasenparameter für den Spannen-Glätter. |
| `SmoothingLength` | `int` | `20` | Jurik-Glättungslänge für Oszillator und Volumen. |
| `AppliedPrice` | `AppliedPrice` | `Close` | Preisquelle für Oszillatorberechnungen. |
| `VolumeType` | `AppliedVolume` | `Tick` | Volumensquelle, mit der der Oszillator multipliziert wird. |
| `HighLevel2` | `int` | `150` | Oberer Extremmultiplikator für den adaptiven Schwellenwert. |
| `HighLevel1` | `int` | `20` | Oberer moderater Multiplikator. |
| `LowLevel1` | `int` | `-20` | Unterer moderater Multiplikator. |
| `LowLevel2` | `int` | `-150` | Unterer Extremmultiplikator. |
| `SignalBar` | `int` | `1` | Historischer Offset für das Lesen von Farbübergängen. |

## Verwendungshinweise
- Hängen Sie die Strategie an ein Wertpapier, das sowohl Preis- als auch Volumesinformationen liefert; wenn nur Tick-Volumen
  verfügbar ist, wird der Tick-Zähler als Proxy verwendet.
- Die beiden Slot-Volumina können unabhängig abgestimmt werden, um die duale Geldverwaltungseinrichtung des Original-EA zu
  emulieren.
- Passen Sie `SignalBar` an, wenn Sie mit teilweise geformten Kerzen arbeiten oder historische Daten resynchronisieren.
- Die Glättungsmethoden unterstützen Jurik-Filter durch Reflexion, um das Verhalten der MQL `SmoothAlgorithms`-Bibliothek zu
  replizieren.
- Da `StartProtection` nur aufgerufen wird, wenn Stop- oder Target-Abstände positiv sind, lassen Sie sie auf null, um
  Schutzaufträge zu deaktivieren.
