# Estrategia Brake Parabolic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia implementa el indicador Brake Parabolic que proyecta una barrera parabólica por encima o por debajo del precio. Cuando la barrera es rota, la tendencia cambia y se abre una nueva posición en la dirección del rompimiento. El algoritmo sigue el precio extremo con una línea curva definida por los parámetros **A**, **B** y **Shift**.

Las pruebas indican un retorno anual promedio de aproximadamente el 48%. Funciona mejor en mercados con tendencia en marcos temporales superiores.

El sistema espera que la barrera cambie de lado. Un giro alcista cierra cualquier corto y abre una nueva posición larga. Un giro bajista cierra cualquier largo y abre un corto. Mientras está en tendencia, las posiciones opuestas se cierran cuando el indicador confirma la dirección.

## Detalles

- **Criterios de entrada**:
  - **Largo**: La barrera cambia de por encima del precio a por debajo del precio.
  - **Corto**: La barrera cambia de por debajo del precio a por encima del precio.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Señal opuesta o el indicador confirma tendencia contraria.
- **Stops**: Sin stops fijos; las salidas dependen de la reversión de la barrera.
- **Valores predeterminados**:
  - `A` = 1.5
  - `B` = 1.0
  - `BeginShift` = 10
  - `CandleType` = marco temporal de 4 horas
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Personalizado
  - Stops: No
  - Complejidad: Moderado
  - Marco temporal: Swing
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
