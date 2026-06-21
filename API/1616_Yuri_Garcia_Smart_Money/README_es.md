# Estrategia de Dinero Inteligente de Yuri Garcia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia de concepto de dinero inteligente busca reacciones de precio dentro de zonas de alto volumen y áreas de soporte/resistencia de cuatro horas. Confirma las entradas con el delta acumulado y retrocesos de sombras, con el objetivo de seguir el flujo de órdenes institucionales.

Las pruebas indican un retorno anual promedio de aproximadamente el 42%. Funciona mejor en BTC y los principales índices.

El sistema calcula el stop loss y el take profit basados en ATR con una relación riesgo/recompensa configurable. Las operaciones se permiten largas, cortas o ambas, y las posiciones se abren solo cuando el precio está dentro de la zona, ocurre un retroceso de sombra y el delta respalda el movimiento.

## Detalles

- **Criterios de entrada**:
  - **Largo**: Precio dentro de la zona con buffer, retroceso alcista de sombra, delta acumulado en ascenso.
  - **Corto**: Precio dentro de la zona, retroceso bajista de sombra, delta acumulado en descenso.
- **Largo/Corto**: Configurable (ambos, solo compra o solo venta).
- **Criterios de salida**:
  - Stop loss o take profit basados en ATR.
- **Stops**: Sí, basados en ATR.
- **Filtros**:
  - Zona HTF, confirmación de delta acumulado, retroceso de sombra.
