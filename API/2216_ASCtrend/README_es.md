# Estrategia ASCtrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia usa el indicador Williams %R para detectar reversiones rápidas similares al enfoque ASCtrend. Vende cuando el indicador sube desde un nivel de sobreventa a uno de sobrecompra y compra cuando ocurre lo contrario.

## Detalles

- **Criterios de entrada**:
  - Vender cuando Williams %R cruza desde sobreventa (por debajo de `x2`) a sobrecompra (por encima de `x1`).
  - Comprar cuando Williams %R cruza desde sobrecompra (por encima de `x1`) a sobreventa (por debajo de `x2`).
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - La señal inversa cierra e invierte la posición.
- **Stops**: No.
- **Valores predeterminados**:
  - `Risk` = 4
  - `CandleType` = 1 hora
- **Filtros**:
  - Categoría: Reversión
  - Dirección: Ambos
  - Indicadores: Williams %R
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
