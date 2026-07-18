# Estrategia de ruptura SR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Resumen
SR Breakout Strategy monitorea los niveles de soporte y resistencia derivados de los canales Donchian en dos períodos de tiempo (H1 y H4). Cuando una vela completa cierra por encima de la resistencia o por debajo del soporte, la estrategia escribe un mensaje de registro informativo. La implementación refleja la lógica de alertas del experto MQL4 original sin realizar ningún pedido.

## Cómo funciona
1. Se crean dos suscripciones de velas: una para el período de 1 hora y otra para el período de 4 horas.
2. Cada suscripción está vinculada a su propio indicador `DonchianChannels` con una longitud retrospectiva configurable (predeterminado `26`).
3. Una vez que se forma el indicador, la estrategia realiza un seguimiento del cierre de la vela anterior para cada período de tiempo.
4. En cada vela terminada, el cierre actual se compara con las bandas superior e inferior Donchian:
   - Si el cierre se mueve desde abajo hacia arriba de la banda superior, se registra un mensaje de "cruce por encima de la resistencia".
   - Si el cierre se mueve desde arriba hacia abajo de la banda inferior, se registra un mensaje de "cruce por debajo del soporte".
5. La lógica reproduce el comportamiento de notificación del script MQL4 utilizando entradas `LogInfo` como alertas.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `LookbackLength` | Número de velas utilizadas para calcular Donchian soporte/resistencia. | 26 |
| `Hour1CandleType` | Tipo de vela para el abono de una hora. | `TimeFrame(1h)` |
| `Hour4CandleType` | Tipo de vela para el abono de cuatro horas. | `TimeFrame(4h)` |

## Señales
- **Ruptura del H1**: registra cuando el cierre de la vela de una hora cruza por encima de la resistencia o por debajo del soporte.
- **Ruptura H4**: registra cuando el cierre de la vela de cuatro horas cruza por encima de la resistencia o por debajo del soporte.

## Notas
- La estrategia está destinada únicamente a alertar; no ejecuta operaciones.
- Ambas suscripciones de velas deben proporcionar datos máximos y mínimos para que el indicador Donchian funcione correctamente.
- Ajuste la duración retrospectiva o los tipos de velas para que coincidan con otras sesiones de negociación o instrumentos.
