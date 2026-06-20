# VIX Trigger
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
VIX Trigger reage a mudanças no Índice de Volatilidade. Um VIX em alta sinaliza medo e possíveis reversões no instrumento subjacente. A estratégia compara a direção do VIX com o preço em relação a uma média móvel.

Os testes indicam um retorno anual médio de aproximadamente 148%. Funciona melhor no mercado forex.

Quando o VIX aumenta e o preço está abaixo da média móvel, compra esperando uma recuperação. Por outro lado, o VIX em alta com o preço acima da média convida a uma posição vendida.

As posições fecham quando o VIX cai ou o percentual de stop-loss é atingido.

## Detalhes

- **Critérios de entrada**: VIX em alta enquanto preço relativo à MA aciona compras ou vendas.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: VIX cai ou stop.
- **Stops**: Sim.
- **Valores padrão**:
  - `MAPeriod` = 20
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Contrarian
  - Direção: Ambos
  - Indicadores: VIX, MA
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

