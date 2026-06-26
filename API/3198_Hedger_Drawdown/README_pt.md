# Estratégia de Hedger Drawdown
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Port do StockSharp do consultor especialista de MetaTrader 5 **hedger.mq5** (MQL #23511). O sistema original abre uma cobertura protetora na direção oposta quando uma posição existente apresenta drawdown de um número específico de pips. Quando o preço retrocede em uma quantidade menor, a cobertura é fechada mesmo com prejuízo, permitindo que a operação original se recupere. Esta conversão reproduz o comportamento com a API de alto nível do StockSharp e adapta a mecânica ao modelo de posição líquida da plataforma.

## Lógica de trading

1. A estratégia monitora o fechamento de cada vela do período configurado.
2. Para cada posição comprada que não seja de cobertura, verifica se a distância entre o preço de entrada e o fechamento atual é maior ou igual a **DrawdownOpenPips**. Se não houver cobertura vendida ativa, abre uma com o mesmo volume.
3. Para cada posição vendida que não seja de cobertura, aplica a regra simétrica, abrindo uma cobertura comprada após a perda atingir o limiar de abertura.
4. As coberturas ativas são fechadas quando sua perda flutuante atinge **DrawdownClosePips**, espelhando a lógica do MetaTrader de liberar a proteção após uma recuperação parcial.
5. Quando a conta está sem posições e **StartWithLong** está habilitado, o algoritmo abre uma posição comprada inicial para iniciar o ciclo.

Como o StockSharp rastreia posições líquidas, a estratégia mantém registros internos de entradas compradas e vendidas (incluindo quais são coberturas). Cada ordem de mercado atualiza os registros para que as coberturas possam ser abertas e fechadas independentemente, mesmo que o broker consolide as posições.

## Parâmetros

| Parâmetro | Descrição |
| --- | --- |
| `DrawdownOpenPips` | Drawdown em pips que aciona a abertura da cobertura oposta. |
| `DrawdownClosePips` | Drawdown em pips que força o fechamento da cobertura. |
| `InitialVolume` | Volume da operação inicial ao iniciar o ciclo. |
| `StartWithLong` | Se habilitado, abre a posição comprada inicial quando a conta está sem posições. |
| `EnableVerboseLogging` | Escreve as ações de cobertura no registro da estratégia para depuração. |
| `CandleType` | Série de velas utilizada para monitorar os drawdowns. |

## Diferenças da versão MetaTrader

- O consultor especialista dependia de comentários nos tickets (`hedge_buy` / `hedge_sell`) para distinguir as posições de cobertura. A conversão armazena esse estado na memória porque o StockSharp usa netting.
- Verificações de margem e configurações de slippage são omitidas; o envio de ordens usa os helpers de alto nível `BuyMarket` / `SellMarket`.
- A estratégia expõe intervalos de otimização para os limiares de pips e o volume para que possam ser ajustados com os otimizadores do StockSharp.

## Notas de uso

1. Anexe a estratégia ao símbolo e portfólio desejados.
2. Ajuste os limiares de pips para corresponder à volatilidade do instrumento.
3. Habilite o registro detalhado ao validar a conversão — o registro registra cada criação e remoção de cobertura com estatísticas de pips.
4. Implante em períodos que forneçam fechamentos de velas significativos (por exemplo, M15 a H1) para evitar excesso de operações.
