# Estratégia de grade de limite pendente (conversão MQL/8147)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Pending Limit Grid Strategy** reproduz o comportamento do especialista MetaTrader
armazenado em `MQL/8147`. A estratégia constrói uma grade simétrica de ordens de limite pendentes
em torno dos preços atuais de compra/venda. Mantém a grade ativa enquanto o lucro flutua
permanece dentro de uma meta de lucro configurada e limite de rebaixamento. Quando um dos
os limites são violados, todas as ordens são canceladas, as posições abertas são achatadas e
a grade é reconstruída usando o novo patrimônio líquido como linha de base.

## Lógica de negociação

1. Assine os dados de nível um para rastrear os melhores preços de compra e venda.
2. Capture o patrimônio da conta na primeira vez que os dados em tempo real forem recebidos e armazene-os como
a linha de base da sessão.
3. Coloque `LevelsPerSide` limites de venda acima do mercado e o mesmo número de compras
limites abaixo do mercado. A distância entre os níveis da grade é controlada por
`GridStepPoints` convertido para a etapa de preço do instrumento.
4. Manter as ordens pendentes sem reemitir novas quando forem atendidas. O
a grade é recriada somente após uma reinicialização completa.
5. Monitore continuamente o PnL flutuante:
   - Se o lucro atingir `ProfitTargetCurrency`, feche toda a exposição e reinicie.
   - Se o rebaixamento exceder `MaxDrawdownCurrency`, nivele o livro e reinicie.
6. Após cada reinicialização, o patrimônio da linha de base é capturado novamente e a grade é reconstruída
usando o instantâneo de oferta/venda mais recente.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `ProfitTargetCurrency` | Lucro líquido (na moeda da conta) que desencadeia uma redefinição completa da grade. |
| `MaxDrawdownCurrency` | Perda flutuante máxima tolerada antes que toda a exposição seja encerrada. |
| `GridStepPoints` | Distância entre níveis de grade consecutivos expressos em pontos de corretagem. |
| `LevelsPerSide` | Número de ordens pendentes criadas acima e abaixo do mercado. |
| `OrderVolume` | Volume atribuído a cada ordem limite pendente. |

## Gestão de risco

A estratégia não anexa paradas ou metas por pedido. Em vez disso, supervisiona o
lucros e perdas agregados. O auxiliar `RequestFlatten` cancela pedidos pendentes e
usa ordens de mercado (via `ClosePosition`) para remover qualquer exposição aberta. Depois do
nivelamento for concluído, o estado da rede e a equidade da linha de base serão redefinidos antes de colocar
novas encomendas.

## Notas

- Os preços são normalizados através de `Security.ShrinkPrice` para respeitar o câmbio
etapa de preço.
- O valor MetaTrader "Ponto" é emulado pela análise do instrumento `PriceStep`
para corresponder às cotações de quatro e cinco dígitos.
- A estratégia evita o reenvio de ordens de grade depois de colocadas, imitando o
especialista original que dependia de variáveis de sinalização para manter cada nível único até
ocorre uma reinicialização manual ou automática.
