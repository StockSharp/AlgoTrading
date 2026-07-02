# Estratégia de escadas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia de escadas** reproduz o comportamento do especialista MetaTrader original. Ele começa colocando ordens de stop simétricas em torno do preço de venda atual e, em seguida, reconstrói continuamente a grade em torno do preenchimento mais recente. Os lucros são acumulados em etapas de preços (pips) sem ponderação por volume, exatamente como no script de origem. Quando uma meta de lucro é atingida, a estratégia liquida todas as posições por ordem de mercado, remove quaisquer stops pendentes e redefine a grade.

## Lógica de negociação

1. Quando nenhuma posição estiver aberta, coloque um stop de compra e um stop de venda a uma distância de `ChannelSteps / 2` passos de preço acima e abaixo do preço de venda atual.
2. Depois que a primeira ordem stop for preenchida, rearme a grade em torno do preço executado:
   - Se houver menos de duas ordens stop ativas, cancele as obsoletas.
   - Contanto que o preço de oferta atual permaneça dentro da metade da distância do canal da última entrada, coloque um novo stop de compra e um novo stop de venda a `ChannelSteps` de distância do preenchimento mais recente.
   - Quando `AddLots` estiver ativado, aumente o volume do pedido pendente no lote base após cada preenchimento.
3. Mantenha duas listas contínuas com todas as entradas longas e curtas para reproduzir a cesta coberta usada pela versão MT4.
4. Calcule o lucro não realizado da cesta em cada vela acabada usando a melhor oferta para posições compradas e a melhor oferta para posições vendidas. As distâncias são normalizadas pela etapa de preço do instrumento, refletindo o cálculo do ponto original.
5. Acione uma liquidação total quando um dos limites for excedido:
   - `ProfitSteps` – lucro produzido apenas pelo símbolo atual.
   - `CommonProfitSteps` – lucro em toda a cesta.
6. A liquidação envia ordens de mercado para fechar cada exposição longa e curta separadamente. As ordens stop pendentes são canceladas quando o cesto fica estável.

> **Nota**: O especialista original anexou níveis de stop loss ao registrar ordens pendentes. StockSharp não suporta níveis de proteção por ordem através do API de alto nível, portanto a porta fecha negociações exclusivamente através da lógica baseada no lucro descrita acima.

## Parâmetros

| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `ChannelSteps` | Distância (em passos de preço mínimo) entre as ordens stop simétricas. | `1000` |
| `ProfitSteps` | Limite de lucro (em etapas) necessário para fechar a cesta local. | `1500` |
| `CommonProfitSteps` | Limite de lucro global (em etapas) que força uma liquidação total. | `1000` |
| `AddLots` | Quando ativado, aumenta o volume do próximo pedido pendente no lote base após cada preenchimento. | `true` |
| `BaseVolume` | Volume usado para o primeiro par de ordens stop. | `0.1m` |
| `CandleType` | Prazo usado para assinaturas de velas e gerenciamento comercial. | `1 minute` |

## Notas de implementação

- Usa o StockSharp API de alto nível com `SubscribeCandles()` e `Bind()` para processar apenas velas concluídas.
- Rastreia entradas individuais dentro de `OnOwnTradeReceived` para que o cálculo do lucro possa imitar a lógica de hedge da versão MQL.
- Os limites de lucro operam em distâncias puras de preços, sem multiplicar pelo volume executado, correspondendo à forma como o especialista MT4 somou os pips.
- Todas as ordens de parada são criadas por meio de `BuyStop` e `SellStop`, enquanto as saídas são executadas com ordens de mercado para manter a lógica portátil entre provedores de dados.
