# Estratégia de Reversão de Diferença RVI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia opera com base na diferença suavizada entre o Relative Vigor Index (RVI) e sua linha de sinal.
Ela detecta pontos onde essa diferença para de cair e começa a subir para entrar comprado, e vice-versa para posições vendidas.

## Detalhes

- **Critérios de entrada**: Reversão de inclinação da diferença RVI suavizada
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Sinal oposto
- **Stops**: Não
- **Valores padrão**:
  - `RviLength` = 12
  - `SmoothingLength` = 13
  - `CandleType` = velas de 6 horas
- **Filtros**:
  - Categoria: Oscilador
  - Direção: Ambos
  - Indicadores: RVI, SMA, EMA
  - Stops: Não
  - Complexidade: Básico
  - Período: 6H
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
