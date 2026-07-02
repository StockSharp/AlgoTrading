# Einfachste DeMarker-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die einfachste DeMarker-Strategie reproduziert die Logik des ursprünglichen MetaTrader-Expertenberaters. Es verfolgt den DeMarker-Oszillator, um zu erkennen, wann die Preisdynamik überkaufte oder überverkaufte Zonen verlässt. Wenn der Oszillator wieder in den neutralen Bereich zurückkehrt, eröffnet die Strategie eine Position in Richtung der erwarteten Umkehr und steuert gleichzeitig das Risiko über konfigurierbare Stop-Loss- und Take-Profit-Abstände.

## Kernlogik
1. Abonnieren Sie Kerzen des ausgewählten Zeitrahmens und berechnen Sie den DeMarker-Indikator mit dem konfigurierten Zeitraum.
2. Markieren Sie den Marktzustand als **überkauft**, wenn der vorherige DeMarker-Wert über dem überkauften Schwellenwert liegt, und als **überverkauft**, wenn er unter dem überverkauften Schwellenwert liegt.
3. Erzeugen Sie Signale, wenn der aktuelle DeMarker-Wert wieder in den neutralen Bereich zurückkehrt:
   - Verkaufen Sie, wenn der Oszillator unter das überkaufte Niveau fällt, nachdem er zuvor darüber gelegen hat.
   - Kaufen Sie, wenn der Oszillator über den überverkauften Wert steigt, nachdem er zuvor darunter lag.
4. Platzieren Sie jeweils nur eine Position. Wenn `Trade On Bar Open` aktiviert ist, wird die Bestellung verzögert, bis der nächste Balken geöffnet wird. andernfalls wird die Position sofort beim aktuellen Schlusskurs des Balkens eingegeben.
5. Wenden Sie Stop-Loss- und Take-Profit-Orders mithilfe des integrierten Schutzdienstes an, um die festen Abstände der MQL-Version nachzuahmen.

## Parameter
- **Volumen** – Bestellgröße in Losen/Verträgen.
- **DeMarker-Periode** – Periode des DeMarker-Oszillators.
- **Überkauft-Level** – oberer DeMarker-Schwellenwert, der überkaufte Bedingungen definiert.
- **Überverkauft-Level** – unterer DeMarker-Schwellenwert, der überverkaufte Bedingungen definiert.
- **Trade On Bar Open** – wenn aktiviert, werden Eingaben bei der nächsten Baröffnung und nicht sofort ausgeführt.
- **Stop-Loss-Punkte** – schützende Stop-Loss-Distanz, ausgedrückt in Preispunkten.
- **Take Profit Points** – Gewinnzieldistanz ausgedrückt in Preispunkten.
- **Kerzentyp** – Kerzentyp (Zeitrahmen), der für Indikatorberechnungen verwendet wird.

## Money-Management
- Stop-Loss- und Take-Profit-Orders werden automatisch über `StartProtection` registriert, wobei die Distanzen in Preispunkte umgewandelt werden.
- Es kann jeweils nur eine Position aktiv sein. Neue Signale werden ignoriert, solange eine Position besteht.

## Diagrammelemente
- Preiskerzen für das ausgewählte Abonnement.
- Die DeMarker-Indikatorkurve.
- Eigene Handelsmarkierungen zur visuellen Validierung von Ein- und Ausgängen.

## Notizen
- Verwenden Sie Instrumente mit ausreichend hoher Liquidität, um die Qualität der Stop-Loss- und Take-Profit-Ausführung sicherzustellen.
- Das Flag `Trade On Bar Open` entspricht in etwa dem ursprünglichen Verhalten des Expert Advisors, der auf einen neuen Balken wartet, bevor er die Bestellung sendet.
