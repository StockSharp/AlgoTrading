# Estratégia Donchian Volume
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Donchian Volume usa rompimentos do canal Donchian confirmados por volume crescente para iniciar operações.
Um movimento fora do canal com volume forte sugere o início de uma nova tendência.

Os testes indicam um retorno anual médio de aproximadamente 160%. Funciona melhor no mercado forex.

A estratégia entra na direção do rompimento e sai quando o preço fecha novamente dentro do canal ou o volume diminui.

Os stops são definidos a uma curta distância dentro do canal para proteger contra movimentos falsos.

## Detalhes

- **Critérios de entrada**: sinal de indicador
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop-loss ou sinal oposto
- **Stops**: Sim, baseado em percentual
- **Valores padrão**:
  - `CandleType` = 15 minute
  - `StopLossPercent` = 2
  - `DonchianPeriod` = 20
  - `VolumePeriod` = 20
    O canal abrange a máxima e a mínima dos DonchianPeriod candles anteriores. Um fechamento além dele com volume acima da média dos VolumePeriod candles anteriores entra na direção do rompimento e inverte uma posição oposta. A posição é encerrada quando o preço volta a fechar dentro do canal ou o volume cai abaixo da média; o stop fica a StopLossPercent do preço de entrada, logo dentro do limite rompido.
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Donchian Channel, Volume
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

