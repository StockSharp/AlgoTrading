# Estratégia de energia das ondas EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia Wave Power EA** é uma versão C# do consultor especialista MQL4 "Wave Power EA1". O robô original constrói uma posição em
direção de um sinal estocástico ou MACD e, em seguida, adiciona ordens de mercado adicionais a cada número fixo de pips enquanto ajusta o
nível de lucro compartilhado. A versão StockSharp reproduz esse comportamento usando a estratégia de alto nível API, vinculação de indicador
e ajudantes de pedidos integrados. Todos os comentários permanecem em inglês, conforme necessário.

## Como funciona a estratégia

1. **Seleção de sinal** – a primeira negociação é aberta somente quando um dos filtros do indicador gera uma direção:
   - `Stochastic` – %K cruzando %D dentro de regiões de sobrevenda/sobrecompra.
   - `MacdSlope` – MACD linha subindo acima ou caindo abaixo de seu valor anterior.
   - `CciLevels` – CCI caindo abaixo de –120 ou subindo acima de +120.
   - `AwesomeBreakout` – Oscilador impressionante quebrando o histórico adaptativo baixo/alto que foi capturado durante a inicialização.
   - `RsiMa` – rápido SMA cruza lento SMA enquanto RSI confirma impulso (acima/abaixo de 50).
   - `SmaTrend` – um leque 15/20/25/50 SMA apontando na mesma direção com uma diferença mínima de inclinação.

2. **Expansão da grade** – após o preenchimento da primeira ordem de mercado, a estratégia lembra o preço de preenchimento. Sempre que o mercado se move
por `GridStepPips` em relação à posição atual e a contagem máxima de pedidos não for excedida, a estratégia envia um novo mercado
faça o pedido na *mesma* direção. Cada nova camada multiplica o volume pelo parâmetro `Multiplier`.

3. **Metas compartilhadas** – cada novo pedido recalcula um preço comum de take-profit e (opcionalmente) de stop-loss. Quando o número de
pedidos ativos se aproximam do limite `OrdersToProtect`, a distância de realização do lucro é substituída por `ReboundProfitPrimary`.
Depois que o limite é excedido, a distância muda para `ReboundProfitSecondary` para estimular uma recuperação mais rápida.

4. **Monitoramento de cesta** – a cada fechamento de vela, a estratégia converte o P&L aberto em pips por lote. Se o lucro de recuperação ou
os limites de proteção contra perdas são atingidos, toda a cesta é liquidada usando ordens de mercado. O mesmo acontece quando o mais velho
a negociação for anterior a `OrdersTimeAliveSeconds` ou quando a negociação na sexta-feira estiver desativada.

5. **Ciclo de vida** – quando a cesta estiver plana, todos os contadores internos serão zerados, permitindo que o próximo sinal inicie uma nova média
ciclo.

Em comparação com o EA original, esta porta evita intencionalmente a abertura de posições opostas (hedge) após um certo número de grades
camadas. Todas as entradas adicionais seguem a direção inicial. O resto das regras de gestão de dinheiro, lógica de proteção e
os filtros do indicador permanecem compatíveis com a implementação de referência MQL4.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `EntryLogic` | Modo indicador usado para o primeiro pedido. |
| `CandleType` | Prazo que alimenta todos os indicadores (padrão: 1 hora). |
| `InitialVolume` | Volume da primeira ordem em lotes/contratos. |
| `GridStepPips` | Distância mínima em pips entre as camadas da grade. |
| `MaxOrders` | Número máximo de pedidos simultâneos na cesta. |
| `TakeProfitPips` | Distância de lucro compartilhada em pips (0 desativa a meta). |
| `StopLossPips` | Distância de stop-loss compartilhada em pips (0 desativa o stop). |
| `Multiplier` | Multiplicador de volume aplicado a cada pedido adicional. |
| `SecureProfitProtection` | Ativa a lógica de lucro de recuperação. |
| `OrdersToProtect` | Número de pedidos necessários antes do início da proteção contra recuperação. |
| `ReboundProfitPrimary` | Lucro por lote (em pips) para a primeira etapa de proteção. |
| `ReboundProfitSecondary` | Lucro por lote (em pips) quando a contagem de pedidos protegidos for excedida. |
| `LossProtection` | Ativa a proteção contra perda flutuante. |
| `LossThreshold` | Perda por lote (em pips) que aciona a guarda quando o cesto está cheio. |
| `ReverseCondition` | Inverte sinais de compra/venda. |
| `TradeOnFriday` | Permite abertura de novos pedidos às sextas-feiras. |
| `OrdersTimeAliveSeconds` | Vida útil máxima do pedido mais recente em segundos (0 desativa o cronômetro). |
| `TrendSlopeThreshold` | Diferença mínima de inclinação SMA usada pela lógica `SmaTrend`. |

## Dicas de uso

1. Anexe a estratégia a um título com uma etapa de preço configurada para que a conversão do pip funcione corretamente.
2. Ajuste `GridStepPips`, `Multiplier` e `MaxOrders` de acordo com a volatilidade do instrumento e a política de margem da corretora.
3. Ative os bloqueios de proteção ao executar em uma conta real para evitar perdas descontroladas durante tendências prolongadas.
4. A estratégia depende de velas fechadas; escolha um período de tempo que reflita o ritmo de negociação desejado (o EA original usa M30
e H1, mas as velas H1 padrão funcionam bem).
5. Como a cobertura após a quinta camada não é implementada, considere reduzir `MaxOrders` se você precisar do original exato
comportamento.

## Arquivos

- `CS/WavePowerEAStrategy.cs` – StockSharp implementação da lógica de grade Wave Power EA.
- `README.md` / `README_ru.md` / `README_zh.md` – documentação em inglês, russo e chinês.

A versão Python é omitida intencionalmente de acordo com os requisitos da tarefa.
