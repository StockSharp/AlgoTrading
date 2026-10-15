# Estrategia LUBE
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia mide la "fricción" alrededor del precio de cierre actual escaneando velas anteriores. Un filtro FIR define la dirección de la tendencia.

- **Largo** cuando la fricción cae por debajo del nivel de disparo y la tendencia es alcista.
- **Corto** cuando la fricción cae por debajo del nivel de disparo y la tendencia es bajista.
- **Salida** cuando la fricción sube por encima del nivel medio o aparece la señal contraria.

## Detalles
- **Indicadores**: cálculo de fricción personalizado, filtro FIR.
- **Marco temporal**: velas de 30m por defecto.
- **Ambos lados**: sí, cortos opcionales.
