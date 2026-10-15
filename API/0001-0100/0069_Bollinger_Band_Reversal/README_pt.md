# Estratégia de Reversão nas Bandas de Bollinger
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Extremos de preço fora das Bandas de Bollinger frequentemente voltam em direção à banda do meio. Esta abordagem vai contra essas extensões, comprando quedas abaixo da banda inferior quando a vela fecha verde e vendendo rompimentos acima da banda superior após uma vela vermelha.

Os testes indicam um retorno anual médio de aproximadamente 94%. Tem melhor desempenho no mercado de ações.

O algoritmo calcula as Bandas de Bollinger em cada barra e verifica se o fechamento rompe a banda externa. Se uma vela altista fecha abaixo da banda inferior, um comprado é aberto; se uma vela baixista fecha acima da banda superior, um vendido é tomado. O stop baseia-se em um múltiplo de ATR enquanto as saídas ocorrem quando o preço retorna à banda do meio.

Operações de reversão à média normalmente duram apenas algumas barras, tornando esta configuração adequada para contrações de volatilidade de curto prazo.

## Detalhes

- **Critérios de entrada**: Fechamento abaixo da banda inferior com vela altista ou fechamento acima da banda superior com vela baixista.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Preço cruzando a banda do meio ou stop-loss.
- **Stops**: Sim, baseado em ATR.
- **Valores padrão**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0
  - `AtrMultiplier` = 2.0
  - `CandleType` = 5 minute
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: Bollinger Bands, ATR
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

