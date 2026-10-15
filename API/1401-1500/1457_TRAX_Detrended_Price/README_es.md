# Estrategia de Precio Destendenciado TRAX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que utiliza los osciladores TRAX y DPO para operar reversiones de tendencia.

## Detalles
- **Criterios de entrada**: DPO cruzando TRAX con signo TRAX y filtro SMA.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Señales de cruce opuestas.
- **Stops**: Ninguno.
- **Valores predeterminados**: Longitud TRAX 12, longitud DPO 19, longitud SMA de confirmación 3.
- **Filtros**: Signo TRAX y SMA de confirmación.
