# Spread-Informer-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Sammelt detaillierte Statistiken zur Geld-Brief-Spanne des ausgewählten Instruments und benachrichtigt, wenn die Spanne ein konfigurierbares Limit überschreitet. Die Strategie überwacht kontinuierlich Level-1-Updates, verfolgt die maximale, minimale und durchschnittliche Streuung in Punkten und protokolliert eine Zusammenfassung, sobald sie stoppt. Es ist nützlich, um die Liquiditätsbedingungen zu untersuchen, bevor latenzempfindliche Systeme ausgeführt werden, oder um Handelsfenster im Strategietester zu optimieren.

## Einzelheiten

- **Datenquelle**: Beste Geld- und Briefkurse der Ebene 1.
- **Erfasste Statistiken**:
  - Start- und Endzeitstempel des Beobachtungszeitraums.
  - Maximale Ausbreitung und Zeitpunkt des Auftretens.
  - Mindestausbreitung und Zeitpunkt des Auftretens.
  - Durchschnittliche Streuung, berechnet über alle beobachteten Level1-Proben.
- **Warnungen**:
  - Optionaler Alarm, wenn der Spread (in Punkten) über den konfigurierten Schwellenwert `MaxSpreadPoints` steigt.
  - Die Benachrichtigungshäufigkeit ist auf `AlertIntervalSeconds` begrenzt, um Spam im Protokoll zu vermeiden.
  - Warnungen werden nur ausgelöst, wenn der Spread den Schwellenwert von unten überschreitet.
- **Protokollierung**:
  - Echtzeitwarnungen werden über `LogInfo` geschrieben.
  - Die endgültige Statistikzusammenfassung wird während `OnStopped` ausgegeben.
- **Standardwerte**:
  - `MaxSpreadPoints` = 0 (Benachrichtigungen deaktiviert).
  - `AlertIntervalSeconds` = 0 (keine Drosselung).

## Parameter

| Name | Beschreibung | Standard | Notizen |
| --- | --- | --- | --- |
| `MaxSpreadPoints` | Maximal zulässiger Spread in Punkten. Auf 0 setzen, um Warnungen zu deaktivieren. | 0 | Punkte werden anhand der Instrumentenpreisstufe berechnet. |
| `AlertIntervalSeconds` | Mindestzeit zwischen aufeinanderfolgenden Warnungen. | 0 | Verhindert doppelte Warnungen, wenn die Spanne groß bleibt. |

## Nutzungshinweise

1. Hängen Sie die Strategie an ein Instrument an und stellen Sie sicher, dass Level1-Daten verfügbar sind.
2. Konfigurieren Sie `MaxSpreadPoints` entsprechend der akzeptablen Spanne für das Instrument.
3. Erhöhen Sie optional `AlertIntervalSeconds`, um wiederholte Benachrichtigungen in volatilen Zeiten zu unterdrücken.
4. Stoppen Sie die Strategie, um die protokollierten Statistiken in der Terminalausgabe zu überprüfen.
