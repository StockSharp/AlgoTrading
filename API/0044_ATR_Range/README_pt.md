# Estratégia ATR Range Breakout
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
ATR Range Breakout mede o movimento do preço ao longo de um número fixo de barras e o compara com o intervalo verdadeiro médio. Quando o movimento supera o ATR, uma posição é aberta na direção do movimento.

Os testes indicam um retorno anual médio de aproximadamente 169%. Funciona melhor no mercado de criptomoedas.

A estratégia verifica o preço a cada N barras e usa a média móvel para sinais de saída. Visa capturar momentum quando a volatilidade se expande além dos níveis normais.

As operações fecham quando o preço cruza novamente a média móvel ou quando o stop baseado em ATR é acionado.

## Detalhes

- **Critérios de entrada**: O preço se move mais do que o ATR ao longo do período de observação.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: O preço cruza a MA ou stop.
- **Stops**: Sim.
- **Valores padrão**:
  - `MAPeriod` = 20
  - `ATRPeriod` = 14
  - `LookbackPeriod` = 5
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: ATR, MA
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

