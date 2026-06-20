# Estratégia de Rebote na Linha de Tendência
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Os mercados frequentemente respeitam as linhas de tendência traçadas através de máximas ou mínimas anteriores de swing. Esta estratégia ajusta automaticamente linhas de regressão à ação de preço recente e procura velas que ricocheteiem nessas linhas na direção da tendência dominante.

Os testes indicam um retorno anual médio de aproximadamente 124%. Funciona melhor no mercado de câmbio.

Velas recentes são armazenadas para calcular linhas de suporte e resistência inclinadas para cima ou para baixo. Quando o preço se aproxima de uma linha de tendência e uma vela confirma o rebote enquanto permanece no lado correto de uma média móvel, o sistema abre uma operação. O stop é definido usando um percentual do preço e a saída ocorre no cruzamento da média móvel.

Ao operar apenas na direção predominante e aguardar uma reação clara no suporte ou resistência, o método tenta capturar movimentos de continuação sem perseguir rompimentos.

## Detalhes

- **Critérios de entrada**: O preço toca a linha de tendência calculada e a vela fecha na direção da tendência acima/abaixo da MA.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Preço cruzando a média móvel ou stop-loss.
- **Stops**: Sim, baseados em percentual.
- **Valores padrão**:
  - `TrendlinePeriod` = 20
  - `MAPeriod` = 20
  - `BounceThresholdPercent` = 0.5
  - `CandleType` = 5 minute
  - `StopLossPercent` = 2
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: MA, Trendlines
  - Stops: Sim
  - Complexidade: Avançado
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

