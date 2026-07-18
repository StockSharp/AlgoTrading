# Bandas de Bollinger com Stop Trailing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Entra comprado quando o preço fecha acima da banda superior das Bandas de Bollinger.
Sai quando o preço cai abaixo da banda inferior ou um stop trailing baseado em ATR é acionado.

## Detalhes

- **Critérios de entrada**: Fechamento acima da banda superior.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Fechamento abaixo da banda inferior ou acionamento do stop trailing.
- **Stops**: Stop trailing.
- **Valores padrão**:
  - `BbLength` = 20
  - `BbDeviation` = 2m
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2m
  - `MaType` = MovingAverageTypeEnum.Simple
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Somente comprado
  - Indicadores: Bollinger Bands, ATR
  - Stops: Sim
  - Complexidade: Iniciante
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
