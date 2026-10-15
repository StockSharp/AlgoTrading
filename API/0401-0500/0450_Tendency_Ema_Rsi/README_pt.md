# Estratégia Tendency EMA + RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia sobrepõe um cruzamento de EMA rápida/média sobre uma EMA de tendência
mais lenta e um filtro RSI. Operações compradas requerem que a EMA rápida cruze acima
da EMA média enquanto ambas permanecem acima da linha de tendência lenta e o candle
fecha bullish. Operações vendidas espelham essas regras. Extremos do RSI fecham posições,
e um recurso opcional de "fechar após X barras" trava os lucros se o preço se mover na
direção esperada rapidamente.

A abordagem visa participar apenas de entradas de pullback que se alinhem com a tendência
prevalente, usando o RSI para sair quando o momentum fica excessivamente esticado. Funciona
melhor em gráficos intradiários onde cruzamentos de EMA oferecem sinais oportunos e
múltiplas configurações ocorrem em cada sessão.

## Detalhes

- **Critérios de entrada**:
  - EMA rápida cruza acima da EMA média, ambas acima da EMA lenta, candle bullish.
  - EMA rápida cruza abaixo da EMA média, ambas abaixo da EMA lenta, candle bearish.
- **Comprado/Vendido**: Comprado habilitado, vendido opcional.
- **Critérios de saída**:
  - RSI > 70 fecha comprado; RSI < 30 fecha vendido.
  - Opcional: fechar após X barras se a operação for lucrativa.
- **Stops**: Nenhum incorporado.
- **Valores padrão**:
  - Comprimento RSI = 14.
  - Comprimentos EMA A/B/C = 9/21/50.
  - Fechar após X barras = desativado, X = 5.
- **Filtros**:
  - Categoria: Tendência + Momentum
  - Direção: Ambos (comprado por padrão)
  - Indicadores: EMA, RSI
  - Stops: Não
  - Complexidade: Moderado
  - Período: Curto
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
