# Estratégia de propagação cruzada de média móvel
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma versão StockSharp do consultor especialista MQL4 **"EA - Média Móvel"** (arquivo `EA - Moving Average.mq4`).
Ele negocia um único instrumento reagindo aos cruzamentos de médias móveis que são detectados na abertura de cada nova vela.

## Ideia Central

- Use uma média móvel exponencial rápida e lenta (EMA) calculada na série de velas selecionada.
- Aguarde até que uma nova vela esteja disponível e avalie os valores EMA das duas velas concluídas mais recentemente, replicando as chamadas `iMA(..., shift=1/2)` do código original.
- Abra uma **posição longa** quando o EMA rápida tiver cruzado acima do EMA lenta na vela anterior, enquanto a vela anterior ainda tinha o EMA rápida abaixo do EMA lenta.
- Abra uma **posição curta** quando o EMA rápida cruzou abaixo do EMA lenta na vela anterior, enquanto a vela anterior ainda tinha o EMA rápida acima do EMA lenta.
- Apenas uma posição pode ser aberta por vez. A estratégia ignora novos sinais até que todas as ordens sejam fechadas.

## Gerenciamento de ordens

- Antes de fazer um pedido, o spread atual é verificado. Se o melhor pedido e o melhor lance estiverem disponíveis, o spread é convertido em pontos de instrumento e comparado com `MaxSpreadPoints`. Os sinais que excedem o limite são ignorados, assim como a proteção `MarketInfo(..., MODE_SPREAD)` original.
- Após o envio de uma ordem de mercado, a estratégia reflete níveis de proteção em torno do preço de entrada:
  - O stop loss é colocado no valor EMA lenta da vela anterior mais/menos o `StopLossPoints` configurado.
  - O take-profit é definido à mesma distância do preço de entrada que o stop-loss, criando uma meta simétrica como na implementação MQL (`Ask + (Ask - StopLoss)` / `Bid - (StopLoss - Bid)`).
- Todas as distâncias de preços expressas em pontos são traduzidas em preços absolutos por meio do instrumento `PriceStep`, portanto, o comportamento corresponde à configuração baseada em pontos de MetaTrader.

## Notas de conversão

- O especialista original permite escolher diferentes modos de média móvel, mas seus padrões usam EMA (`MAMode = 1`). A versão StockSharp concentra-se em EMA para manter a implementação concisa; diferentes algoritmos de suavização podem ser adicionados, se necessário.
- O volume de negociação é fornecido por meio do parâmetro `TradeVolume` e mapeado para `Strategy.Volume` durante `OnStarted`.
- A estratégia depende exclusivamente de dados de velas fornecidos por meio de `CandleType`. Não há coleções de indicadores adicionais ou buffers históricos além do histórico EMA de dois valores necessário para detectar cruzamentos.

## Parâmetros

- `CandleType` – tipo de dados da vela e período de assinatura.
- `FastPeriod` – comprimento do EMA rápido (o padrão é 21).
- `SlowPeriod` – duração da lentidão EMA (o padrão é 84).
- `StopLossPoints` – distância de stop-loss nos pontos do instrumento em relação à lentidão EMA.
- `MaxSpreadPoints` – spread máximo permitido em pontos antes que um novo pedido seja negado.
- `TradeVolume` – tamanho do lote utilizado no envio de ordens de mercado.

## Dicas de uso

1. Selecione o símbolo e o período da vela antes de iniciar a estratégia para que os valores EMA correspondam ao gráfico pretendido em MetaTrader.
2. Forneça dados de nível 1 (melhor bid/ask) se desejar que o filtro de spread funcione em tempo real; caso contrário, a estratégia assume que o spread é aceitável.
3. Certifique-se de que a segurança tenha um `PriceStep` válido. Sem isso, a estratégia não poderá traduzir distâncias baseadas em pontos em preços absolutos e ignorará a colocação de ordens de proteção.
