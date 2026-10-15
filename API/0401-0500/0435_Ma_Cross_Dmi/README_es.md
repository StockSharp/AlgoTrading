# Estrategia de MA Cross + DMI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Opera un cruce de medias móviles exponenciales rápida y lenta solo cuando el Directional Movement Index confirma la fortaleza de la tendencia. Al esperar que +DI o -DI domine mientras el ADX supera un nivel clave, el sistema filtra los cruces débiles.

Esta estrategia puede entrar en posiciones largas o cortas y sale en cruces opuestos. El filtrado por ADX ayuda al método a mantenerse fuera de los períodos de rango donde las medias móviles generan falsas señales frecuentemente.

## Detalles

- **Criterios de entrada**:
  - **Largo**: La EMA rápida cruza por encima de la EMA lenta, +DI > -DI y ADX por encima del nivel clave.
  - **Corto**: La EMA rápida cruza por debajo de la EMA lenta, -DI > +DI y ADX por encima del nivel clave.
- **Criterios de salida**:
  - Cruce opuesto o stop manual.
- **Indicadores**:
  - Dos EMAs (períodos 10 y 20)
  - Directional Movement Index (longitud 14, suavizado ADX 14)
- **Stops**: Ninguno por defecto; se puede usar StartProtection.
- **Valores predeterminados**:
  - `Ma1Length` = 10
  - `Ma2Length` = 20
  - `DmiLength` = 14
  - `AdxSmoothing` = 14
  - `KeyLevel` = 20
- **Filtros**:
  - Seguimiento de tendencia
  - Funciona en marcos temporales intradía a swing
  - Indicadores: EMA, DMI
  - Stops: Opcional
  - Complejidad: Básico
