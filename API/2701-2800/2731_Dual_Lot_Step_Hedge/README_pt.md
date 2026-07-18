# Estratégia de Hedge de Duplo Lote por Passos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral

A **Estratégia de Hedge de Duplo Lote por Passos** é um port em C# dos consultores especialistas do MetaTrader 5 *"x1 lot from high to low"* e *"x1 lot from low to high"* (pasta `MQL/19543`). Os robôs originais abrem imediatamente uma cesta hedgeada de posições de compra e venda, ciclam o volume da ordem após cada nova entrada e fecham toda a cesta assim que um alvo de lucro fixo é atingido. Esta implementação reproduz esse comportamento sobre a API de alto nível do StockSharp enquanto expõe parâmetros limpos e gerenciamento detalhado de estado.

Dois modos de operação estão disponíveis:

- **HighToLow** – começa com o multiplicador de lote máximo, abre a primeira cesta hedgeada com o maior volume e depois diminui para o próximo passo de lote após as primeiras entradas.
- **LowToHigh** – começa com o passo de lote mínimo, aumenta o tamanho do lote após cada nova entrada até que o multiplicador configurado seja atingido, e então continua negociando nesse tamanho.

A estratégia mantém ambas as pernas de compra e venda ativas simultaneamente, gerencia os níveis de stop-loss e take-profit por perna, e monitora o patrimônio do portfólio para impor um alvo de lucro abrangente da cesta.

## Lógica de Trading

1. Quando não existem posições, a estratégia abre **ambas** uma ordem de mercado comprada e vendida usando o tamanho de lote atual.
2. Se exatamente uma perna está ativa (por exemplo, o lado oposto foi parado), a perna faltante é reaberta a mercado com o tamanho de lote atual.
3. Após cada entrada bem-sucedida, o tamanho do lote é atualizado dependendo do modo selecionado (`HighToLow` ou `LowToHigh`).
4. As saídas de proteção por perna são avaliadas em cada tick de negociação entrante:
   - Uma perna comprada é fechada se o preço atingir seu stop-loss (`StopLossPips` abaixo da entrada comprada média) ou seu take-profit (`TakeProfitPips` acima da entrada média).
   - Uma perna vendida é fechada se o preço atingir seu stop-loss (`StopLossPips` acima da entrada vendida média) ou seu take-profit (`TakeProfitPips` abaixo da entrada média).
5. Uma vez que o ganho de patrimônio do portfólio ultrapassa `MinProfit`, a estratégia fecha todas as posições restantes e redefine o estado do lote para o tamanho inicial do modo.
6. A lógica de segurança fecha a cesta e redefine tudo se mais de uma posição de compra ou venda for inesperadamente detectada.

Todas as ordens são submetidas através dos helpers de alto nível `BuyMarket` e `SellMarket`. A estratégia rastreia preenchimentos com `OnOwnTradeReceived`, mantém exposição agregada por perna e previne ordens duplicadas enquanto entradas ou saídas ainda estão pendentes.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-----------|
| `LotMultiplier` | Multiplicador de lote máximo expresso em passos de volume mínimos (padrão `10`). |
| `StopLossPips` | Distância de stop-loss em pips para cada perna (padrão `50`). Definir como `0` para desabilitar. |
| `TakeProfitPips` | Distância de take-profit em pips para cada perna (padrão `150`). Definir como `0` para desabilitar. |
| `MinProfit` | Alvo de lucro da cesta em moeda da conta. Uma vez que o ganho de patrimônio ultrapassa esse valor, todas as posições são fechadas (padrão `27`). |
| `ScalingMode` | Comportamento do passo de lote. `HighToLow` reflete o EA "x1 lot from high to low", `LowToHigh` reflete "x1 lot from low to high". |

A estratégia deriva automaticamente o passo de volume mínimo de `Security.VolumeStep` e calcula o valor de pip usando o passo de preço do instrumento (com o ajuste forex tradicional de 4/5 dígitos).

## Redefinição e Ciclagem de Volume

- **HighToLow** – abre a primeira cesta com o maior volume (`VolumeStep * LotMultiplier`). Após qualquer entrada, o volume interno é reduzido em um passo. Quando o alvo de lucro da cesta é atingido, o volume é redefinido para `0` para que o próximo ciclo comece do máximo novamente.
- **LowToHigh** – começa do passo de lote mínimo. Após cada entrada, o lote é aumentado em um passo até que o teto do multiplicador seja atingido. Quando o alvo de lucro da cesta é atingido, o volume é redefinido para o passo mínimo.

## Notas de Uso

- A estratégia subscreve trades de tick (`DataType.Ticks`) porque os bots MetaTrader originais são executados em eventos de tick. Configure o provedor de histórico ou o conector ao vivo de acordo.
- As verificações de stop-loss e take-profit ocorrem dentro do algoritmo, portanto nenhuma ordem de proteção adicional é registrada no exchange.
- Como ambas as pernas são abertas a mercado, a estratégia funciona melhor em brokers que suportam posições hedgeadas e spreads pequenos. Em venues de netting, ainda funcionará, mas as pernas se compensarão efetivamente até que uma delas seja fechada pela lógica interna.
- Os parâmetros padrão copiam as configurações MQL originais. Ajuste-os cuidadosamente: fazer hedge de altos volumes pode gerar drawdowns significativos antes que o alvo de lucro da cesta seja atingido.

## Mapeamento para a Lógica MQL Original

| Variável MetaTrader | Propriedade C# / Comportamento |
|---------------------|-------------------------------|
| `InpLots` | `LotMultiplier` com tratamento automático de passo de volume. |
| `InpStopLoss` & `InpTakeProfit` | `StopLossPips` e `TakeProfitPips` com conversão de pip baseada em `PriceStep`. |
| `InpMinProfit` | `MinProfit` e a verificação de patrimônio do portfólio. |
| `LotCheck` | Helper `LotCheck` que impõe o passo mínimo e o volume máximo. |
| `CalculatePositions` | Rastreamento interno de exposição comprada/vendida através de `OnOwnTradeReceived`. |
| `CloseAllPositions()` | Método `CloseAllPositions` com coordenação de ordem pendente e redefinição de estado. |

## Considerações de Gestão de Risco

A estratégia mantém intencionalmente posições compradas e vendidas abertas, o que causa exposição contínua a custos de spread e taxas de swap. Antes de executar com capital real:

- Validar o comportamento no emulador do StockSharp ou em trading em papel.
- Garantir que seu broker suporta hedging; caso contrário, as pernas compradas/vendidas serão netas imediatamente.
- Ajustar os valores de stop-loss, take-profit e alvo de lucro à volatilidade do instrumento.
- Monitorar o uso de margem, porque pernas compradas/vendidas simultâneas dobram a exposição nominal.

## Arquivos

- `CS/DualLotStepHedgeStrategy.cs` – implementação de estratégia StockSharp com comentários inline extensos.
- `README_ru.md` – tradução em russo com instruções detalhadas.
- `README_zh.md` – tradução em chinês com instruções detalhadas.
