# Laufzeitstruktur-Strategie für Rohstoffe
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie handelt die Steigung von Rohstoff-Futures-Kurven. Sie kauft Kontrakte in Backwardation und verkauft jene in Contango, in der Erwartung einer Mean Reversion der Laufzeitstruktur.

Jeden Monat werden Futures nach Carry eingestuft: Long-Positionen in der stärksten Backwardation, Short in der steilsten Contango. Positionen werden vor Fälligkeit gerollt.

## Details

- **Daten**: Preise naher und aufgeschobener Futures.
- **Einstieg**: Long in Rohstoffe mit höchstem Carry, Short in solche mit niedrigstem Carry.
- **Ausstieg**: Rollen bei Kontraktfälligkeit oder wenn der Carry das Vorzeichen wechselt.
- **Instrumente**: Rohstoff-Futures.
- **Risiko**: Gleichgewichtete Dollarbeträge mit Stop bei nachteiliger Carry-Änderung.

