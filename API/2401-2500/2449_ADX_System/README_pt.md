# Sistema ADX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Sistema ADX** opera usando o Average Directional Index e seus componentes DI. Abre uma posição quando o ADX sobe e uma das linhas direcionais cruza acima do ADX. As posições incluem níveis fixos de take-profit e stop-loss com um trailing stop para proteger o lucro.

## Detalhes

- **Critérios de entrada**
  - ADX está subindo (ADX anterior abaixo do atual).
  - Para trades **comprados**: +DI anterior abaixo do ADX anterior e +DI atual acima do ADX atual.
  - Para trades **vendidos**: -DI anterior abaixo do ADX anterior e -DI atual acima do ADX atual.
- **Critérios de saída**
  - Sinal oposto nas linhas ADX e DI.
  - O preço atinge o nível do trailing stop.
  - O preço atinge o take-profit ou stop-loss fixo.
- **Comprado/Vendido**: Ambas as direções.
- **Stops**: Stop-loss fixo, take-profit e trailing stop em unidades de preço absolutas.
- **Valores padrão**:
  - `AdxPeriod` = 14
  - `TakeProfit` = 15
  - `StopLoss` = 100
  - `TrailingStop` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: ADX, +DI, -DI
  - Stops: Sim
  - Complexidade: Iniciante
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
