# Estrategia SuperTrend + EMA Rebound
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El sistema opera en la dirección del SuperTrend y busca retrocesos hacia una media
móvil exponencial. Se abre una posición cuando la línea del SuperTrend cambia de
dirección o cuando el precio rebota desde la EMA mientras permanece en el sesgo
predominante del SuperTrend. Esta combinación intenta capturar el primer tramo de un
nuevo movimiento y las posteriores correcciones dentro de una tendencia establecida.

Un take profit basado en porcentaje puede activarse mediante el módulo de protección
integrado configurando el tipo de take profit a "%". Los valores predeterminados
favorecen las operaciones largas, pero las entradas cortas también pueden activarse.
Debido a que la estrategia depende de los cambios de dirección, es más efectiva en
mercados tendenciales donde el SuperTrend reacciona rápidamente a los cambios de
momentum.

## Detalles

- **Criterios de entrada**:
  - SuperTrend cambia a tendencia alcista, o el precio rebota por encima de la EMA durante una tendencia alcista.
  - SuperTrend cambia a tendencia bajista, o el precio rebota por debajo de la EMA durante una tendencia bajista.
- **Largo/Corto**: Largo habilitado por defecto, corto opcional.
- **Criterios de salida**:
  - Cambio de dirección opuesto del SuperTrend.
  - Take profit opcional gestionado por el módulo de protección.
- **Stops**: Take profit porcentual mediante protección; sin stop loss incluido.
- **Valores predeterminados**:
  - Período ATR = 10, factor ATR = 3.0.
  - Longitud EMA = 20, TP = 1.5%.
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos (largo por defecto)
  - Indicadores: SuperTrend, EMA
  - Stops: TP opcional
  - Complejidad: Moderado
  - Marco temporal: Corto/medio
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
