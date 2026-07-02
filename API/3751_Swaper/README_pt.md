# Estratégia de troca (API 3751)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia Swaper** replica o consultor especialista MetaTrader "Swaper 1.1" usando a estratégia de alto nível de StockSharp API. O
O sistema original acumula ganhos de swap reequilibrando constantemente uma carteira sintética entre exposições longas e curtas. Isto
a conversão preserva a lógica do fluxo de dinheiro, reconstruindo o equilíbrio virtual do especialista, calculando um valor justo para o
instrumento subjacente e alinhando a posição aberta com esse valor alvo.

## Lógica principal

1. **Reconstrução de capital sintético.** A estratégia recria o acumulador MetaTrader `money` combinando o acumulador inicial
saldo (`BaseUnits * BeginPrice`), lucro realizado de pedidos atendidos e a parte não realizada da posição atual
dimensionado por `ContractMultiplier`.
2. **Denominador do valor justo.** O especialista MQL mantém uma variável `com` que aumenta ou diminui com o volume ativo. O StockSharp
port espelha esse comportamento por meio de `BaseUnits + ContractMultiplier * Position`.
3. **Cálculo do volume alvo.** O algoritmo avalia o máximo das duas últimas máximas do candle (ajustado pelo spread do mercado)
e o mínimo dos dois últimos mínimos para reproduzir o guard-rail MetaTrader. Um fator `Experts / (Experts + 1)` controla como
agressivamente a estratégia caminha em direção ao valor justo.
4. **Ajustes de posição.** Dependendo do valor `dt` calculado, a estratégia também
   - fecha posições quando o ajuste calculado é inferior a um décimo do lote, ou
   - vende volume adicional quando `dt < 0`, ou
   - compra volume adicional quando `dt >= 0`.
5. **Dimensionamento de lote com reconhecimento de margem.** O método auxiliar `GetTradableVolume` aproxima as verificações de `AccountFreeMargin()` comparando o
configurado `MarginPerLot` com o capital do portfólio disponível. Se o tamanho solicitado exceder a margem disponível, o lote
o valor é calculado até o décimo mais próximo.

Todo o loop é executado em velas finalizadas, substituindo a função original baseada em ticks, mantendo a lógica econômica
intacto.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `Experts` | `1` | Peso aplicado ao ajuste sintético de valor justo. |
| `BeginPrice` | `1.8014` | Preço inicial usado para reconstruir o saldo virtual. |
| `MagicNumber` | `777` | Identificador preservado para compatibilidade com a versão MetaTrader (registrado nos pedidos, se necessário). |
| `BaseUnits` | `1000` | Unidades de capital inicial utilizadas pelo denominador da equação do valor justo. |
| `ContractMultiplier` | `10` | Multiplicador que converte as diferenças de preço na moeda da conta. |
| `MarginPerLot` | `1000` | Capital aproximado necessário para suportar um lote; rege a lógica de redução de lote. |
| `FallbackSpreadSteps` | `1` | Spread em etapas de preço quando faltam cotações de nível um. |
| `CandleType` | `1 Hour` | Período primário que alimenta o ciclo de rebalanceamento. |

## Fluxo de trabalho de negociação

1. Assine a série de velas configurada e os dados de nível um.
2. Acompanhe as melhores cotações de compra/venda para obter um spread preciso. Se o feed estiver silencioso, volte para
`FallbackSpreadSteps * PriceStep`.
3. Recalcule o capital sintético e o denominador de cada vela acabada.
4. Calcule `dt` usando o caminho de preço alto. Quando `dt < 0`, mude para o ramo de preço baixo para emular a proteção original
lógica.
5. Use `AdjustShort` ou `AdjustLong` para diminuir ou expandir a posição. Quando o tamanho do alvo for menor que um décimo do lote,
feche a posição completamente para copiar o comportamento `closeby` de MetaTrader.
6. Atualize o PnL realizado dentro de `OnOwnTradeReceived` para que as iterações subsequentes usem o saldo mais recente.

## Diferenças versus a versão MQL4

- O loop `start()` acionado por ticks é substituído pelo processamento de velas, o que evita espera ocupada enquanto preserva a estratégia
intenção.
- O histórico de pedidos e a varredura de negociações abertas são aproximados por meio do fluxo de negociações da própria estratégia, em vez de `OrdersHistoryTotal()`
e `OrdersTotal()`.
- As verificações de margem usam `Portfolio.CurrentValue` com uma constante `MarginPerLot` configurável porque a margem específica do corretor
funções não estão disponíveis em StockSharp.
- O fechamento do par via `OrderCloseBy` é emulado simplesmente achatando a posição líquida, consistente com o modelo de compensação da maioria
Conectores StockSharp.

## Notas de uso

- Configure `MarginPerLot` de acordo com as especificações do contrato do conector para evitar que a estratégia solicite um
volume inviável.
- A estratégia espera que os dados das velas forneçam máximos e mínimos confiáveis; use um período que corresponda ao feed do corretor usado pelo
Versão MetaTrader se desejar um comportamento idêntico.
- Como as cotações de nível um podem chegar de forma assíncrona, a estratégia armazena o spread mais recente. Certifique-se de que as velas e o nível
uma assinatura é habilitada para replicação precisa.
