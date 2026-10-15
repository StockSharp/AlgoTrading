# Estratégia Nevalyashka Flip
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Um port direto para StockSharp do especialista MetaTrader "Nevalyashka". A estratégia sempre alterna entre trades comprados e vendidos: começa com uma ordem de venda a mercado, aguarda o fechamento da posição por stop loss ou take profit, e então abre imediatamente uma ordem a mercado na direção oposta. Ordens protetoras são recriadas para cada entrada usando os mesmos offsets baseados em pips do código original.

## Lógica da estratégia

1. **Inicialização**
   - Detecta o passo de preço do instrumento e os decimais para derivar um tamanho de pip idêntico à versão MQL (pares de 3/5 dígitos são multiplicados por 10).
   - Multiplica o `MinVolume` da bolsa pelo parâmetro `LotMultiplier` para obter o tamanho da ordem e arredonda para o step de volume, se necessário.
2. **Tratamento de cotações**
   - Assina atualizações do livro de ordens para capturar os últimos preços bid/ask, espelhando a chamada `RefreshRates()` do especialista.
3. **Fluxo de ordens**
   - Coloca uma ordem de venda a mercado inicial assim que as melhores cotações bid/ask estão disponíveis.
   - Após o fechamento de uma posição, inverte o lado (compra após venda, venda após compra) e emite uma nova ordem a mercado com o mesmo volume.
   - Para cada entrada executada, a estratégia coloca ordens separadas de stop-loss e take-profit usando os parâmetros de distância em pips.

## Gestão de risco

- **Stop Loss**: Opcional. Quando `StopLossPips` é maior que zero, a estratégia envia uma ordem stop protetora (`SellStop` para posições compradas, `BuyStop` para posições vendidas) em `entrada ± StopLossPips * pip`.
- **Take Profit**: Opcional. Quando `TakeProfitPips` é maior que zero, a estratégia envia uma ordem limite protetora (`SellLimit` para posições compradas, `BuyLimit` para posições vendidas) em `entrada ± TakeProfitPips * pip`.
- Ambas as ordens protetoras são canceladas sempre que a posição está zerada para evitar ordens pendentes antes do próximo flip.

## Parâmetros

| Nome | Descrição | Padrão |
| ---- | ----------- | ------- |
| `LotMultiplier` | Multiplicador aplicado ao volume mínimo do instrumento. O resultado é arredondado para o step de volume da bolsa. | `1` |
| `StopLossPips` | Distância de stop-loss em pips. Definir como `0` para desabilitar o stop. | `50` |
| `TakeProfitPips` | Distância de take-profit em pips. Definir como `0` para desabilitar o alvo. | `50` |

## Notas operacionais

- A abordagem alterna continuamente a exposição e, portanto, se adapta a mercados de reversão à média onde um movimento completado provavelmente irá reverter.
- Funciona com qualquer símbolo que forneça cotações do topo do livro; os cálculos de pips se adaptam automaticamente com base na precisão do preço.
- O tratamento de slippage é delegado à bolsa — as ordens são enviadas a mercado sem verificações adicionais, assim como no especialista original.
- A estratégia não inclui filtros de horário de negociação, filtros de notícias ou trailing stops. Tal lógica pode ser adicionada estendendo `TryOpenNextPosition` ou `RegisterProtectionOrders`.
