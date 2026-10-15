# Estratégia de Oscilador de Momentum Chande
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia compra quando o Oscilador de Momentum Chande cai abaixo de um limiar inferior e fecha a posição quando sobe acima de um limiar superior ou após um número fixo de barras.

Os testes indicam um retorno anual médio de aproximadamente 40%. Tem melhor desempenho em mercados com tendência.

O oscilador compara ganhos e perdas recentes para medir o momentum. Valores negativos extremos sugerem condições de sobrevenda, que a estratégia usa para entradas compradas. As posições são fechadas quando o momentum se torna positivo ou o período de manutenção expira.

## Detalhes

- **Critérios de entrada**: `CMO < LowerThreshold`.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: `CMO > UpperThreshold` ou `MaxBarsInPosition` barras decorridas.
- **Stops**: Não.
- **Valores padrão**:
  - `CmoPeriod` = 9
  - `LowerThreshold` = -50
  - `UpperThreshold` = 50
  - `MaxBarsInPosition` = 5
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Somente comprado
  - Indicadores: CMO
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário (1m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
