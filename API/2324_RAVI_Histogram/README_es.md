# Estrategia de Histograma RAVI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia convierte el experto MetaTrader RAVI Histogram a StockSharp. Mide la fuerza de la tendencia como la diferencia porcentual entre una EMA rápida y una lenta. El resultado se compara con niveles superior e inferior para decidir cuándo operar.

Cuando el valor RAVI sube por encima del nivel superior, el mercado se considera alcista. Las posiciones cortas se cierran y, si está habilitado, se abre una posición larga. Cuando el valor cae por debajo del nivel inferior, la estrategia cierra los largos y puede abrir un corto. Por defecto opera con velas de cuatro horas.

## Detalles

- **Criterios de entrada**:
  - **Largo**: RAVI cruza hacia arriba a través de `UpLevel`.
  - **Corto**: RAVI cruza hacia abajo a través de `DownLevel`.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - La señal RAVI opuesta cierra las posiciones existentes.
- **Stops**: Ninguno.
- **Filtros**: Ninguno.
- **Marco temporal**: velas de 4 horas por defecto.
- **Parámetros**:
  - `FastLength` y `SlowLength` – periodos de EMA para el cálculo RAVI.
  - `UpLevel` y `DownLevel` – umbrales que definen las zonas de tendencia.
  - `BuyOpen`, `SellOpen`, `BuyClose`, `SellClose` – habilitan o deshabilitan operaciones en cada dirección.
