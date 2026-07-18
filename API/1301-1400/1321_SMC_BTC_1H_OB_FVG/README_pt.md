# Estratégia SMC para BTC em 1H com OB e FVG
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada em Smart Money Concepts para Bitcoin em velas de 1 hora. O sistema entra comprado após uma quebra de estrutura de alta quando o preço retorna ao bloco de ordens detectado ou à lacuna de valor justo. O stop-loss usa um multiplicador ATR e o take-profit é calculado a partir de uma relação risco/recompensa.

## Detalhes

- **Critérios de entrada**: Após BOS de alta, comprar se o preço tocar o bloco de ordens ou a lacuna de valor justo dentro de `ZoneTimeout` barras.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Take-profit e stop-loss fixos.
- **Stops**: Fixo usando ATR.
- **Valores padrão**:
  - `UseOrderBlock` = true
  - `UseFvg` = true
  - `AtrFactor` = 6
  - `RiskRewardRatio` = 2.5
  - `ZoneTimeout` = 10
  - `CandleType` = TimeSpan.FromHours(1)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Comprado
  - Indicadores: ATR
  - Stops: Fixo
  - Complexidade: Simples
  - Período: Intradiário (1H)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
