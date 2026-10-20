# Bitcoin 1H/15M Breakout

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Handelsregeln

Abgeschlossene 15-Minuten-Kerzen handeln den Ausbruch aus der vorherigen vollständig abgeschlossenen Stunde. Kleine Kerzen werden zu UTC-Stunden zusammengefasst. Nur vier lückenlose Viertel ab exaktem Stundenbeginn ergeben einen gültigen Bereich; ein anfänglicher Teil oder eine Datenlücke liefert keinen.

Für eine kleine Kerze innerhalb Stunde H gelten ausschließlich Hoch/Tief der fertigen Stunde H−1, niemals die laufende Stunde. Ohne Position wird bei Schluss strikt über dem Hoch `Volume` gekauft bzw. unter dem Tief `Volume` verkauft. Beide Seiten sind erlaubt; bestehende Positionen werden durch Ausbrüche weder vergrößert noch gedreht. Aktive Orders blockieren neue.

Der native lokale Schutz verwendet `StopLossBuffer` als absoluten Preisabstand zu tatsächlichen Einstiegsausführungen, standardmäßig 50. Das Gewinnziel beträgt `StopLossBuffer * RiskRewardRatio`, standardmäßig 100 bei Verhältnis 2. Beide Ausstiege schließen die ganze Position per Marktorder; neue Stundenbereiche verschieben keine Schutzgrenze. `HigherTimeFrame` muss ein ganzzahliges Vielfaches des kleinen Intervalls sein.

Der native lokale Schutz verwendet Kerzen und abonnierte Preisaktualisierungen des Instruments. Abstände beziehen sich auf tatsächliche Einstiegsausführungen. Ausstiege sind Marktorders; der Ausführungspreis hängt vom verfügbaren Markt und Preisraster ab.

## Parameter

| Parameter | Standardwert |
|---|---|
| `CandleType` | `15m` |
| `HigherTimeFrame` | `1h` |
| `StopLossBuffer` | `50` |
| `RiskRewardRatio` | `2` |
