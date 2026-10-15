# Estrategia de Correo de Estado y Alerta al Cerrar Orden
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia monitorea la cuenta e informa sobre eventos importantes:

- Envía una notificación de estado diaria en un minuto especificado.
- Informa sobre cada orden cerrada con información básica de la operación.

Está basada en el experto MQL *StatusMailandAlertOnOrderClose.mq4* y muestra cómo gestionar notificaciones en StockSharp.

## Parámetros

| Nombre | Descripción |
|--------|-------------|
| `SendReportEmail` | Habilitar notificación de estado diaria. |
| `StatusEmailMinute` | Minuto de la hora para enviar el mensaje de estado. |
| `SendClosedEmail` | Habilitar notificaciones cuando se cierran órdenes. |
| `StartBalance` | Saldo inicial de la cuenta usado para el cálculo de beneficios. |
| `CandleType` | Marco temporal usado para verificar el reloj. Normalmente configurado a 1 minuto. |

## Lógica

1. Suscribirse a velas del marco temporal elegido.
2. Cuando finaliza una vela, comprobar si es el minuto especificado y enviar un mensaje de informe.
3. En cada nueva operación, notificar si se ha cerrado una orden.

Estos mensajes se registran mediante `AddInfo`, pero pueden reemplazarse por cualquier mecanismo de notificación deseado.
