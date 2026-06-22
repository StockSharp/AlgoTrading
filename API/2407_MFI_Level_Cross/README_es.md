# Estrategia de Cruce de Nivel MFI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza el oscilador Money Flow Index (MFI) para identificar condiciones de sobrecompra y sobreventa. Cuando el MFI cruza niveles de umbral predefinidos, la estrategia entra o revierte posiciones. Puede operar en la dirección del cruce o en la dirección opuesta, dependiendo del modo de tendencia seleccionado.

La configuración predeterminada monitorea velas de cuatro horas y evalúa el MFI de 14 períodos. La estrategia abre una posición larga cuando el MFI cae por debajo del umbral inferior y una posición corta cuando sube por encima del umbral superior. Cuando se establece en modo "Against", la lógica de entrada se invierte para operar contra la dirección del indicador.

La gestión de riesgos se maneja a través de parámetros integrados de stop-loss y take-profit expresados como porcentajes del precio de entrada.

## Detalles

- **Criterios de entrada**:
  - **Trend Mode: Direct**:
    - **Largo**: MFI anterior > nivel bajo y MFI actual ≤ nivel bajo.
    - **Corto**: MFI anterior < nivel alto y MFI actual ≥ nivel alto.
  - **Trend Mode: Against**:
    - **Largo**: MFI anterior < nivel alto y MFI actual ≥ nivel alto.
    - **Corto**: MFI anterior > nivel bajo y MFI actual ≤ nivel bajo.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: La posición se revierte cuando aparece la señal opuesta o se cierra por el módulo de protección.
- **Stops**: Stop-loss y take-profit expresados en porcentaje del precio de entrada.
- **Valores predeterminados**:
  - `Candle Type` = velas de 4 horas.
  - `MFI Period` = 14.
  - `Low Level` = 40.
  - `High Level` = 60.
  - `Stop Loss %` = 1.
  - `Take Profit %` = 2.
- **Filtros**:
  - Categoría: Oscilador
  - Dirección: Configurable
  - Indicadores: Money Flow Index
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

## Notas

Esta implementación se basa en la API de alto nivel de StockSharp. Se suscribe a datos de velas, vincula el indicador MFI directamente y ejecuta órdenes de mercado cuando se cumplen las condiciones de cruce. La protección de posición se inicializa una vez al inicio para gestionar el riesgo automáticamente.
