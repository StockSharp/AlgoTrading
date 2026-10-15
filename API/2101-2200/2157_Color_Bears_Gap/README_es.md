# Estrategia Color Bears Gap
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementa una estrategia basada en el indicador Color Bears Gap. El indicador compara dos brechas suavizadas entre el precio máximo y los valores suavizados de apertura/cierre. Cuando la diferencia cruza cero, se abren posiciones en la nueva dirección y se cierran las posiciones opuestas.

## Detalles
- **Criterios de entrada**: El indicador cruza por debajo de cero -> comprar; cruza por encima de cero -> vender.
- **Largo/Corto**: Configurable mediante parámetros.
- **Criterios de salida**: Cruce opuesto de la línea cero.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `Length1` = 12
  - `Length2` = 5
  - `BuyOpen` = true
  - `SellOpen` = true
  - `BuyClose` = true
  - `SellClose` = true
  - `CandleType` = marco temporal de 8 horas
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Ambos
  - Indicadores: Color Bears Gap
  - Stops: No
  - Complejidad: Moderado
  - Marco temporal: 8 horas
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
