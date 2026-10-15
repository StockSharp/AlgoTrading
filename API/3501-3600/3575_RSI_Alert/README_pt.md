# RSI Estratégia de alerta
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **RSI Estratégia de Alerta** reproduz o comportamento do consultor especialista MetaTrader 5 "RSI Alerta" dentro da estrutura StockSharp. O bot original observou leituras do Índice de Força Relativa (RSI) que cruzaram níveis profundamente sobrevendidos (≤20) ou sobrecomprados (≥80) e enviaram imediatamente notificações de alerta ao abrir posições de mercado. A versão convertida mantém esta filosofia orientada a eventos: ela escuta velas concluídas, avalia o RSI e inverte automaticamente a posição enviando ordens de mercado quando os limites configurados são atingidos.

## Lógica de negociação
1. Assine a série de velas configurada (padrão: período de 1 minuto) e insira os preços de fechamento em um indicador `RelativeStrengthIndex`.
2. Ignore as velas incompletas e espere até que o indicador RSI esteja totalmente formado. Isso reflete o especialista MQL, que avaliou as condições apenas uma vez por nova barra.
3. Gere sinais de negociação:
   - **Sinal de compra** – RSI ≤ `OversoldLevel`. A estratégia fecha qualquer exposição curta e abre uma posição longa com o volume configurado.
   - **Sinal de venda** – RSI ≥ `OverboughtLevel`. A estratégia fecha qualquer exposição longa e abre uma posição curta com o volume configurado.
4. Os pedidos são sempre feitos com `BuyMarket`/`SellMarket`, portanto, não há níveis de pedidos pendentes, stop-loss ou take-profit. A implementação MetaTrader permitia entradas SL/TP opcionais, mas por padrão dependia de gerenciamento manual. A porta StockSharp concentra-se na conversão de alerta para negociação e deixa o gerenciamento de risco para módulos externos (por exemplo, `StartProtection()` ou controles em nível de portfólio).

A estratégia permanece estável entre os sinais. Quando um gatilho oposto aparece, ele inverte a posição adicionando volume suficiente para nivelar a exposição existente antes de entrar na nova direção, exatamente como o EA original fez ao disparar alertas consecutivos.

## Parâmetros
| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `OrderVolume` | 0,01 | Tamanho da negociação para ordens de mercado. Ao reverter, a estratégia adiciona o valor necessário para cobrir a posição existente antes de entrar novamente. |
| `RsiPeriod` | 30 | RSI período médio. Deve ser um número inteiro positivo. |
| `OverboughtLevel` | 80 | Limite de RSI que emite um sinal de venda. Pode ser otimizado para ajustar a agressividade. |
| `OversoldLevel` | 20 | Limite de RSI que emite um sinal de compra. |
| `CandleType` | 1 minuto `TimeFrameCandle` | Fonte de dados Candle usada para cálculo de RSI. Altere-o para analisar prazos maiores. |

Todos os parâmetros são expostos por meio de `StrategyParam<T>` para que apareçam no designer StockSharp, possam ser salvos em predefinições XML e suportem cenários de otimização.

## Notas de implementação
- O StockSharp API de alto nível é usado em todo: as velas são obtidas por meio de `SubscribeCandles()` e o RSI é atualizado por meio de `subscription.Bind(indicator, callback)`. Não é necessário nenhum manuseio manual de buffer ou cópia histórica.
- A propriedade base `Strategy.Volume` é sincronizada com o parâmetro `OrderVolume` para que a reversão de posição funcione corretamente mesmo que o usuário altere o tamanho do lote em tempo de execução.
- Os comentários embutidos e a documentação XML são escritos em inglês para atender aos requisitos do projeto.
- A saída do gráfico é opcional, mas suportada: quando a estratégia é executada dentro do designer, ela traça as velas de preço, as negociações executadas e os valores do indicador RSI.

## Dicas de uso
- Combine a estratégia com módulos externos de stop-loss/take-profit se for necessário controle automatizado de risco.
- Otimize os limites RSI ao se adaptar a mercados com diferentes regimes de volatilidade.
- Aumente o intervalo de tempo da vela para configurações de swing ou mantenha a série padrão de 1 minuto para alertas de estilo scalping, como no script original.
