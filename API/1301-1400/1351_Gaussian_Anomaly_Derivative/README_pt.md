# Estratégia de Derivada de Anomalia Gaussiana
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Utiliza uma média móvel da anomalia de preço `1 - (high + low) / (2 * close)` e sua derivada suavizada.
Opera comprado quando a derivada excede seu limiar positivo e vendido quando cai abaixo do limiar negativo.

## Detalhes

- **Critérios de entrada**: a anomalia ou sua derivada cruza o limiar
- **Comprado/Vendido**: Configurável
- **Critérios de saída**: sinal oposto
- **Stops**: Não
- **Valores padrão**:
  - `UseSma` = true
  - `MaPeriod` = 3
  - `DerivativeMaPeriod` = 2
  - `ThresholdCoeff` = 1.0
  - `DerivativeThresholdCoeff` = 1.0
  - `StartBarCount` = 600
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: SMA, EMA
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
