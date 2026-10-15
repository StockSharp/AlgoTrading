# Estratégia Exp TEMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia Exp TEMA** é uma versão StockSharp do MetaTrader consultor especialista `Exp_TEMA.mq5`. O sistema original verifica vários pares de divisas e monitora a inclinação da média móvel exponencial tripla (TEMA). Sempre que a inclinação muda de sinal, o especialista entra em uma nova posição de acompanhamento de tendência ou sai da posição oposta. Esta conversão C# mantém a mesma lógica do indicador enquanto se concentra em um único título atribuído à estratégia em StockSharp.

## Lógica de negociação

A estratégia opera em velas finalizadas produzidas pelo parâmetro `CandleType` selecionado. Um TEMA com comprimento `TemaPeriod` configurável é calculado em cada fechamento de vela. Três leituras consecutivas de TEMA são comparadas para reproduzir o esquema de detecção de inclinação do especialista MQL5:

1. Seja `tema[0]` o valor da vela mais recente, `tema[1]` o anterior e `tema[2]` o valor duas velas atrás.
2. A inclinação de curto prazo é `d1 = tema[1] - tema[2]`, enquanto a inclinação mais antiga é `d2 = tema[2] - tema[3]`.
3. Uma **entrada de alta** é acionada quando a inclinação aumenta (`d2 < 0` e `d1 > 0`). Qualquer posição curta é fechada primeiro e, em seguida, uma ordem longa de `Volume + |Position|` lotes é colocada.
4. Uma **entrada de baixa** é acionada quando a inclinação diminui (`d2 > 0` e `d1 < 0`). Qualquer posição longa é achatada primeiro e, em seguida, uma ordem curta de `Volume + |Position|` lotes é enviada.
5. As saídas de proteção imitam os sinalizadores de parada originais: se a inclinação atual se tornar negativa, a posição longa é fechada, enquanto uma inclinação positiva fecha qualquer posição curta.

Isso reproduz o mesmo tempo de sinal da fonte EA sem usar o acesso histórico ao buffer, permanecendo dentro do StockSharp API de alto nível.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `TemaPeriod` | 15 | Comprimento da média móvel exponencial tripla. |
| `TradeVolume` | 1 | Volume básico do pedido. O tamanho executado torna-se `TradeVolume + |Posição|` ao inverter. |
| `StopLossPoints` | 1000 | Distância de stop-loss expressa em etapas de preço. Passado para `StartProtection` se positivo. |
| `TakeProfitPoints` | 2000 | Distância de lucro expressa em etapas de preço. Passado para `StartProtection` se positivo. |
| `CandleType` | Velas de 15 minutos | Tipo de vela que alimenta o indicador. Escolha um prazo que corresponda ao gráfico usado pelo especialista original. |

Todos os parâmetros são criados com `StrategyParam<T>` para que possam ser otimizados dentro do Designer.

## Diferenças do especialista MQL5

- A versão MQL gerencia até doze símbolos simultaneamente. As estratégias StockSharp estão vinculadas a um `Security` específico, portanto esta porta negocia o instrumento que é atribuído quando a estratégia é lançada. Execute várias instâncias de estratégia se for necessária cobertura multissímbolo.
- O gerenciamento de pedidos depende de `BuyMarket`/`SellMarket` e `StartProtection`, que mapeiam as ordens de mercado originais, paradas e metas para o API de alto nível de StockSharp.
- O acesso ao indicador é realizado através de `SubscribeCandles().Bind(...)`, evitando a cópia manual do buffer e mantendo a conformidade com as diretrizes do repositório.

## Dicas de uso

1. Anexe a estratégia à segurança desejada e defina o `CandleType` que corresponda ao seu prazo analítico.
2. Ajuste as distâncias de stop e take-profit nas etapas de preço de acordo com a volatilidade do instrumento.
3. Opcional: execute a otimização em `TemaPeriod`, `StopLossPoints` e `TakeProfitPoints` para replicar as varreduras de parâmetros executadas em MetaTrader.
4. Monitore a área incluída do gráfico para visualizar velas, a linha TEMA e as negociações executadas.
