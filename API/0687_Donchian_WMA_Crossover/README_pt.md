# Estratégia de Cruzamento Donchian WMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O cruzamento da mínima do canal Donchian acima de uma média móvel ponderada aciona entradas compradas apenas durante o ano calendário de 2025. As posições são fechadas quando um nível de take-profit é atingido, o cruzamento se inverte com uma WMA em queda, ou a data sai de 2025.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `DonchianLow` cruza acima de `WMA` e a data está dentro de 2025
- **Comprado/Vendido**: Somente comprado
- **Critérios de saída**:
  - Take profit via `TakeProfitPercent`
  - Cruzamento de `DonchianLow` abaixo de `WMA` enquanto `WMA` cai
  - Data fora de 2025
- **Stops**: Apenas take profit
- **Valores padrão**:
  - `DonchianLength` = 7
  - `WmaLength` = 62
  - `TakeProfitPercent` = 0.01m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Comprado
  - Indicadores: Canal Donchian, Média Móvel Ponderada
  - Stops: Sim
  - Complexidade: Iniciante
  - Período: Médio prazo
  - Sazonalidade: Apenas o ano de 2025
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
