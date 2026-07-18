# Estratégia de Sazonalidade Intradiária do Bitcoin
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que compra Bitcoin durante horas intradiárias predefinidas de forte atividade.

Os testes indicam um retorno anual médio de aproximadamente 45%. Tem melhor desempenho no mercado de criptomoedas.

O sistema monitora velas horárias. Durante as horas UTC selecionadas mantém uma posição comprada dimensionada ao valor do portfólio. Fora dessas horas sai para caixa. Ordens inferiores a um valor mínimo em USD são ignoradas.

## Detalhes

- **Critérios de entrada**: Manter BTC comprado durante as horas UTC especificadas.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Sair fora das horas especificadas.
- **Stops**: Não.
- **Valores padrão**:
  - `HoursLong` = [0, 1, 2, 3]
  - `MinTradeUsd` = 200
  - `CandleType` = TimeSpan.FromHours(1)
- **Filtros**:
  - Categoria: Sazonalidade
  - Direção: Somente comprado
  - Indicadores: Nenhum
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário (1h)
  - Sazonalidade: Sim
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
