# Parabolic SAR Flip-Alert-Strategie (4164)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Diese Strategie reproduziert den MetaTrader-Expertenberater **pSAR_alert2** innerhalb des StockSharp-Frameworks. Es überwacht den Indikator Parabolic SAR für das ausgewählte Instrument und den ausgewählten Zeitrahmen. Immer wenn der SAR-Wert von über dem Schlusskurs auf darunter (oder umgekehrt) fällt, generiert die Strategie eine Informationswarnung. Optional kann es Marktaufträge in Richtung des Flips erteilen, um den Alarm in einen automatischen Einstieg umzuwandeln.

## Handelslogik

1. Abonnieren Sie die konfigurierte Kerzenserie und berechnen Sie den Indikator Parabolic SAR mit den bereitgestellten Beschleunigungseinstellungen.
2. Warten Sie, bis jede Kerze fertig ist, um das ursprüngliche EA-Timing zu emulieren.
3. Vergleichen Sie den Indikatorwert mit dem Kerzenschluss:
   - Bisher SAR über dem Schlusskurs und aktuell SAR unter dem Schlusskurs → **bullischer Flip**.
   - Vorheriger SAR unter dem Schlusskurs und aktueller SAR über dem Schlusskurs → **bärischer Flip**.
4. Protokollieren Sie für jeden Schlag eine detaillierte Warnung. Wenn der automatische Handel aktiviert ist, glätten Sie jegliches entgegengesetzte Risiko und eröffnen mithilfe von Marktaufträgen eine neue Position in Richtung des Signals.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `Candle Type` | Zeitrahmen, der zum Erstellen von Kerzen und zum Auswerten des Indikators Parabolic SAR verwendet wird. |
| `SAR Step` | Anfänglicher Beschleunigungsfaktor, der an Parabolic SAR übergeben wird. |
| `SAR Max` | Maximaler Beschleunigungsfaktor des Parabolic SAR. |
| `Enable Auto Trading` | Bei `true` werden Marktaufträge bei jeder Benachrichtigung gesendet; Bei `false` werden nur Protokolle generiert. |
| `Trade Volume` | Die Auftragsgröße wird angewendet, wenn der automatische Handel aktiviert ist. |

## Konvertierungshinweise

- Das ursprüngliche MetaTrader-Skript stützte sich auf `Sleep`, um die Ausführung zu drosseln. StockSharp ist ereignisgesteuert, sodass die Strategie sofort und ohne manuelle Verzögerungen auf neue Kerzen reagiert.
- Warnungen werden über `AddInfoLog` erzeugt, wobei das ursprüngliche Verhalten von Popup-Benachrichtigungen beibehalten wird, ohne dass zusätzliche UI-Komponenten erforderlich sind.
- Um die Alarmlogik in automatisierte Arbeitsabläufe zu integrieren, steht optionaler automatischer Handel zur Verfügung. Deaktivieren Sie den Parameter `Enable Auto Trading`, um dem genauen Verhalten von MetaTrader zu entsprechen.
- Auf die Python-Implementierung wird wie gewünscht bewusst verzichtet.
