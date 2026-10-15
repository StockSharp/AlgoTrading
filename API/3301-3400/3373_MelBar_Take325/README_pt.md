# Estratégia MelBar Take325
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia MelBar Take325 é uma conversão direta do sistema Expert Advisor Studio "MelBar™Take325%™ 5.5Y NZD-USD". Ele negocia em ambas as direções no NZD/USD usando uma combinação de quebras de volume de ticks, um filtro de oscilação baseado em uma média móvel simples de 12 períodos e um filtro de saída RSI de 14 períodos. A porta StockSharp mantém os parâmetros de risco originais de um stop loss de 16 pips e um take-profit de 45 pips, expressos em distâncias de pips do preço de entrada.

A estratégia começa aguardando um aumento no volume de ticks, definido como um rompimento acima do limite de volume configurado. Quando o volume se expande, ele verifica se a média móvel simples formou um ponto de inflexão local duas barras antes. Um máximo local em SMA abre uma negociação longa, enquanto um mínimo local abre uma negociação curta. Apenas uma direção pode ser tomada por vez, e sinais conflitantes são ignorados para evitar inversões na mesma barra.

As posições abertas são gerenciadas ativamente. Os níveis de stop-loss e take-profit são aplicados sempre que uma vela fecha, tornando o comportamento semelhante à versão MetaTrader. Além disso, o RSI de 14 períodos é usado para forçar saídas: as negociações longas fecham quando RSI cruza para baixo o nível configurado (padrão 80) e as negociações curtas fecham quando RSI cruza para cima o nível simétrico (padrão 20). A máxima/mínima da vela processada é comparada com o preço de entrada para acionar saídas de stop-loss e take-profit.

## Detalhes

- **Critérios de entrada**:
  - **Filtro de volume**: o volume do tick duas barras atrás deve estar abaixo do limite enquanto a barra anterior o excede.
  - **Longo**: SMA (comprimento 12) tem um pico local há duas barras (`SMA[t-3] < SMA[t-2]` e `SMA[t-2] > SMA[t-1]`).
  - **Curto**: SMA tem um vale local (`SMA[t-3] > SMA[t-2]` e `SMA[t-2] < SMA[t-1]`).
- **Critérios de saída**:
  - **Stop-loss**: 16 pips a partir da entrada, avaliado no fechamento da vela.
  - **Take-profit**: 45 pips desde a entrada, avaliados no fechamento da vela.
  - **Saída RSI longa**: RSI cruza para baixo até 80 (`RSI[t-3] > 80` e `RSI[t-2] < 80`).
  - **Saída RSI curta**: RSI cruza para cima até 20 (`RSI[t-3] < 20` e `RSI[t-2] > 20`).
- **Parâmetros padrão**:
  - Volume de entrada = 0,1 lote.
  - Limite de volume = 1.000 unidades de volume de tick.
  - SMA período = 12.
  - RSI período = 14.
  - RSI nível = 80 (a saída curta usa 100 - nível).
  - Prazo da vela = 30 minutos.
- **Mercado**: Projetado para NZD/USD, mas pode ser aplicado a outros pares de FX.
- **Estilo**: Quebra de impulso com saídas de reversão à média.
- **Stops**: Stop-loss e take-profit fixos; nenhuma parada final no código original.
- **Complexidade**: Moderada; combina vários filtros, mas sem escala de posição.
- **Risco**: Médio, pois o stop é mais apertado que o take-profit, mas ambos são distâncias fixas.
