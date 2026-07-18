# Estratégia Rnd Trade
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- Conversão do consultor especialista do MetaTrader 5 `RndTrade.mq5` para a API de estratégia de alto nível do StockSharp.
- Fecha qualquer posição existente em um intervalo de tempo fixo e imediatamente abre uma nova posição de mercado em uma direção selecionada aleatoriamente.
- Usa assinaturas de velas baseadas em tempo como substituto determinístico para os callbacks de temporizador originais.

## Parâmetros
| Nome | Tipo | Padrão | Descrição |
| ---- | ---- | ------- | ----------- |
| `IntervalMinutes` | `int` | `60` | Número de minutos entre o fechamento da posição atual e a abertura de uma nova posição aleatória. Deve ser maior que zero. |
| `Volume` | `decimal` | `1` | Tamanho da posição usado para entradas a mercado. Derivado da classe base `Strategy`. |

## Assinaturas de dados
- Assina velas de período cujo comprimento corresponde a `IntervalMinutes` (p. ex., `60` → velas de 60 minutos).
- O evento de fechamento de vela (`CandleStates.Finished`) é usado para acionar a lógica exatamente uma vez por intervalo.

## Lógica de trading
1. Aguardar a conclusão de cada vela de intervalo.
2. Pular o processamento até que a estratégia esteja formada, online e o trading seja permitido.
3. Fechar qualquer posição aberta criada durante o intervalo anterior.
4. Gerar um valor aleatório para decidir entre uma entrada comprada ou vendida.
5. Enviar uma ordem a mercado (`BuyMarket` ou `SellMarket`) com o volume configurado na direção selecionada.

## Notas de implementação
- Baseia-se em `SubscribeCandles().Bind(ProcessCandle)` para evitar o polling manual de valores de indicadores ou coleções.
- Chama `StartProtection()` durante a inicialização para que o módulo de risco integrado esteja ativo, mesmo que nenhum stop-loss ou take-profit explícito seja configurado.
- Usa `Random` da biblioteca padrão para imitar o comportamento `MathRand()` encontrado na estratégia MQL original.
- O código contém comentários em inglês que explicam como cada etapa de conversão se mapeia para as características do StockSharp.

## Diferenças em relação à estratégia MQL original
- Eventos de temporizador (`OnTimer`) são emulados por meio de assinaturas de velas em vez da API de temporizador do MetaTrader.
- O fechamento de posição é tratado com `ClosePosition()` em vez de iterar sobre listas de posições e chamar `PositionClose` para cada ticket.
- A versão StockSharp depende da propriedade integrada `Volume` para o dimensionamento de posições em vez da consulta do lote mínimo do símbolo.
- As regras de preenchimento de ordens e as configurações de deslizamento são gerenciadas pelo corretor ou simulador conectado, portanto não são configuradas explicitamente na estratégia.

## Uso
1. Anexar a estratégia a um portfólio e instrumento dentro do ambiente StockSharp.
2. Configurar `IntervalMinutes` e `Volume` de acordo com a frequência de trading e o tamanho desejados.
3. Iniciar a estratégia. Ela aplanará e reabrirá posições automaticamente em cada intervalo sem nenhuma entrada adicional.
4. Nenhuma implementação em Python está disponível no momento; apenas a versão C# está disponível.
