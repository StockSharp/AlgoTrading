# Estratégia de Gestão de Ordens Trading Boxing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A Estratégia de Gestão de Ordens Trading Boxing recria o painel de gerenciamento manual de ordens do consultor especialista TradingBoxing original. Em vez de botões no gráfico, a versão StockSharp expõe parâmetros que podem ser alternados pela interface da estratégia ou automação. Cada interruptor executa imediatamente a ação solicitada e depois se redefine, fornecendo uma superfície de controle conveniente para entradas no mercado, colocação de ordens pendentes e limpeza de posições existentes.

A estratégia não depende de lógica de indicadores ou eventos de dados de mercado. Ela simplesmente coordena o envio e cancelamento de ordens para o instrumento e o portfólio atribuídos à instância da estratégia.

## Parâmetros
### Configuração de volume
- `BuyVolume` – quantidade usada quando a ação *Open Buy Market* é acionada. Deve ser positiva.
- `SellVolume` – quantidade usada quando a ação *Open Sell Market* é acionada. Deve ser positiva.
- `BuyStopVolume` – quantidade para novas ordens de stop de compra.
- `BuyLimitVolume` – quantidade para novas ordens de limite de compra.
- `SellStopVolume` – quantidade para novas ordens de stop de venda.
- `SellLimitVolume` – quantidade para novas ordens de limite de venda.

### Configuração de preço
- `BuyStopPrice` – preço de ativação para ordens de stop de compra.
- `BuyLimitPrice` – preço para ordens de limite de compra.
- `SellStopPrice` – preço de ativação para ordens de stop de venda.
- `SellLimitPrice` – preço para ordens de limite de venda.

### Interruptores de ação
Todos os parâmetros de ação são interruptores booleanos. Definir um interruptor como `true` realiza a tarefa correspondente e a estratégia o redefine para `false` no mesmo ciclo de processamento.

- `CloseBuyPositions` – fecha a exposição comprada atual (se `Position > 0`).
- `CloseSellPositions` – fecha a exposição vendida atual (se `Position < 0`).
- `DeleteBuyStops` – cancela as ordens de stop de compra rastreadas.
- `DeleteBuyLimits` – cancela as ordens de limite de compra rastreadas.
- `DeleteSellStops` – cancela as ordens de stop de venda rastreadas.
- `DeleteSellLimits` – cancela as ordens de limite de venda rastreadas.
- `OpenBuyMarket` – envia uma ordem de compra a mercado usando `BuyVolume`.
- `OpenSellMarket` – envia uma ordem de venda a mercado usando `SellVolume`.
- `PlaceBuyStop` – registra uma nova ordem de stop de compra em `BuyStopPrice` com `BuyStopVolume` e a armazena para cancelamento posterior.
- `PlaceBuyLimit` – registra uma nova ordem de limite de compra em `BuyLimitPrice` com `BuyLimitVolume` e a armazena para cancelamento posterior.
- `PlaceSellStop` – registra uma nova ordem de stop de venda em `SellStopPrice` com `SellStopVolume` e a armazena para cancelamento posterior.
- `PlaceSellLimit` – registra uma nova ordem de limite de venda em `SellLimitPrice` com `SellLimitVolume` e a armazena para cancelamento posterior.

## Detalhes de comportamento
- As ordens criadas através das ações de ordens pendentes são rastreadas internamente para que as ações de exclusão possam cancelá-las posteriormente. Ordens externas que não foram colocadas por esta estratégia não são afetadas.
- A estratégia verifica que está em execução e que tanto `Security` quanto `Portfolio` estão atribuídos antes de executar qualquer solicitação. Quando um requisito está faltando, ela registra um aviso e ignora o interruptor.
- A validação de volume e preço replica as salvaguardas do painel original: qualquer quantidade não positiva aciona um aviso e nenhuma ordem é enviada.
- As ações de fechamento operam sobre a posição líquida mantida pela estratégia. Se um vendido precisar ser coberto, a estratégia envia uma ordem de compra a mercado igual a `Math.Abs(Position)`; para uma posição comprada, envia uma ordem de venda a mercado do valor atual de `Position`.

## Notas de uso
1. Inicie a estratégia com um portfólio e um instrumento válidos.
2. Ajuste os parâmetros de volume e preço para corresponder ao instrumento que você negocia.
3. Acione ações manuais definindo o parâmetro booleano necessário como `true`. A propriedade reverte automaticamente para `false` após a conclusão da ação, então o próximo acionamento está pronto imediatamente.
4. Use os interruptores de exclusão para limpar ordens pendentes colocadas anteriormente sempre que o plano de operação mudar.

Como a estratégia é puramente orientada por eventos de entrada do usuário, não há necessidade de se inscrever em velas ou cotações. Ela age como um assistente de execução simples, espelhando a flexibilidade da interface TradingBoxing original dentro do ambiente StockSharp.
