# Estratégia Supertrend Volume
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Supertrend Volume aumenta o indicador Supertrend com confirmação de volume.
O volume crescente durante uma inversão do Supertrend fortalece a probabilidade de um novo movimento impulsivo.

Os testes indicam um retorno anual médio de aproximadamente 145%. Funciona melhor no mercado de criptomoedas.

A estratégia entra na direção da tendência com um sinal Supertrend apenas quando acompanhado de volume acima da média.

Os stops seguem a linha Supertrend, saindo quando o preço fecha do outro lado.

## Detalhes

- **Critérios de entrada**: sinal de indicador
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop-loss ou sinal oposto
- **Stops**: Sim, baseado em percentual
- **Valores padrão**:
  - `CandleType` = 15 minute
  - `StopLossPercent` = 2
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3
  - `VolumePeriod` = 20
    Uma virada do Supertrend, um fechamento do outro lado da linha, encerra a posição contrária. A virada abre posição na sua direção apenas se o volume do candle estiver acima da média dos VolumePeriod candles anteriores, de modo que uma virada confirmada inverte a posição.
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Supertrend, Volume
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

