# Estrategia Avellaneda-Stoikov de Khaled Tamim
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementa el modelo de creación de mercado Avellaneda-Stoikov. La estrategia calcula cotizaciones de compra y venta a partir de los dos últimos cierres y coloca órdenes de mercado cuando el precio se desvía más allá de los márgenes configurables.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `close < bidQuote - M`
  - **Corto**: `close > askQuote + M`
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Señal opuesta.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `Gamma` = 2
  - `Sigma` = 8
  - `T` = 0.0833
  - `K` = 5
  - `M` = 0.5
  - `Fee` = 0
- **Filtros**:
  - Categoría: Creación de mercado
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
