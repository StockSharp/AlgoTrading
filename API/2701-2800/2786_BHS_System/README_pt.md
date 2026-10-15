# Estratégia BHS System
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

O BHS System é uma abordagem de rompimento que converte o expert advisor original do MetaTrader 5 na API de alto nível do StockSharp. A estratégia observa a relação entre o preço e uma Média Móvel Adaptativa de Kaufman (AMA). Quando a barra atual fecha acima do AMA, o sistema se prepara para se juntar a um rompimento altista; quando o fechamento cai abaixo do AMA, se prepara para uma expansão baixista. Em vez de entrar imediatamente, o algoritmo aguarda o preço tocar níveis de "número redondo" predefinidos e envia ordens stop nesses níveis. Isso mantém o comportamento da estratégia portada idêntico à versão MQL, onde as ordens pendentes eram sempre alinhadas com fronteiras de preço arredondadas.

## Lógica de trading

1. Em cada vela concluída, a estratégia calcula os próximos níveis de preço redondo mais alto e mais baixo. O arredondamento usa o passo definido pelo usuário (em pontos) e o passo de preço do instrumento para produzir preços de ativação exatos compatíveis com o exchange.
2. O valor AMA anterior (deslocado um bar, como na implementação MQL original) é comparado com o fechamento da vela atual.
3. Se não há posição aberta e não há ordem de entrada ativa:
   - Quando fechamento > AMA, um buy stop é colocado no nível de teto arredondado.
   - Quando fechamento < AMA, um sell stop é colocado no nível de piso arredondado.
4. As ordens pendentes expiram automaticamente após o número configurado de horas. Isso espelha o campo de vida útil da solicitação de ordem MT5.
5. Quando uma ordem de entrada é executada, a ordem pendente oposta é cancelada e uma ordem stop de proteção é registrada usando a distância de stop-loss selecionada. O sistema então monitora o movimento do preço e move o stop de acordo com os parâmetros de trailing.
6. Os trailing stops são ajustados apenas quando o preço avançou pelo menos a distância de trailing mais o passo de trailing. Isso evita modificações constantes e espelha a lógica de trailing discreta no código MT5.

## Gestão de risco

- **Stop-loss inicial:** Distâncias separadas baseadas em pontos para operações compradas e vendidas são convertidas em offsets de preço absolutos e usadas para colocar ordens stop de proteção imediatamente após a entrada.
- **Trailing stop:** Posições compradas e vendidas têm distâncias de trailing independentes. Os stops são atualizados apenas quando o novo stop melhora pelo menos o passo de trailing, evitando micro-ajustes em mercados calmos.
- **Expiração de ordens:** Ambas as ordens de entrada armazenam seu tempo de criação. Se a ordem permanecer ativa após o número especificado de horas, ela é cancelada para evitar exposição pendente obsoleta.

## Parâmetros

- `OrderVolume` – tamanho de lote usado tanto para entradas quanto para ordens de proteção.
- `StopLossBuyPoints` / `StopLossSellPoints` – distância de stop-loss em pontos para posições compradas e vendidas respectivamente.
- `TrailingStopBuyPoints` / `TrailingStopSellPoints` – distância de trailing stop para posições compradas e vendidas expressa em pontos.
- `TrailingStepPoints` – lacuna adicional (em pontos) necessária antes que o trailing stop possa ser melhorado novamente.
- `RoundStepPoints` – número de pontos usados ao construir níveis de ativação arredondados.
- `ExpirationHours` – vida útil de uma ordem de entrada pendente. Quando definido como zero, as ordens nunca expiram automaticamente.
- `AmaLength`, `AmaFastPeriod`, `AmaSlowPeriod` – parâmetros da Média Móvel Adaptativa de Kaufman usada como filtro direcional.
- `CandleType` – tipo de dado/período de velas que impulsiona a estratégia.

## Notas de implementação

- A estratégia usa o indicador `KaufmanAdaptiveMovingAverage` do StockSharp e um namespace de escopo de arquivo consistente com as diretrizes do repositório.
- Todas as operações de trading dependem de auxiliares de API de alto nível (`BuyStop`, `SellStop`, `CancelOrder`) e nenhum valor de indicador é recuperado através de chamadas `GetValue`.
- O suporte a gráficos está habilitado: a assinatura desenha velas, a linha AMA e as operações próprias quando um contexto de gráfico está disponível.
- A lógica de proteção está consolidada em uma única referência de ordem stop, de modo que o mecanismo de trailing reutiliza o stop original em vez de gerar ordens adicionais.
- A conversão mantém comentários em inglês e preserva o comportamento da rotina de trailing MQL original usando as mesmas verificações de limite.
