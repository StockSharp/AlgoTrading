# Estratégia de Fechamento de Múltiplos Pares
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia de Fechamento de Múltiplos Pares** espelha o script original do MetaTrader que supervisiona uma cesta de pares de moedas e liquida todas as posições abertas assim que o lucro flutuante combinado atinge um alvo ou a perda acumulada excede um limite de segurança. A conversão aproveita a API de alto nível do StockSharp para rastrear lucros, impor um tempo mínimo de manutenção e fechar posições em vários instrumentos em uma ação.

## Lógica

1. Resolver os instrumentos observados do parâmetro `WatchedSymbols` separado por vírgulas. Se a lista estiver vazia, o `Security` principal é usado.
2. Inscrever-se no tipo de candle selecionado (padrão: período de 1 minuto) para cada instrumento. Cada candle terminado aciona uma avaliação de lucro.
3. Para cada instrumento a estratégia armazena:
   - O último lucro calculado (`Positions[i].PnL`).
   - O timestamp quando uma posição se tornou diferente de zero pela primeira vez para respeitar o requisito `MinAgeSeconds`.
4. Após cada atualização, o lucro líquido em todos os símbolos observados é calculado:
   - Se `ProfitTarget` for atingido, todas as posições com mais idade que o mínimo são achatadas usando ordens `BuyMarket` / `SellMarket`.
   - Se o lucro líquido cair abaixo de `-MaxLoss`, a mesma lógica de liquidação é aplicada como stop protetor.
5. Registros detalhados resumem o lucro por instrumento e o resultado atual da cesta após cada avaliação.

## Parâmetros

| Parâmetro | Descrição | Padrão |
|-----------|-----------|--------|
| `WatchedSymbols` | Lista de identificadores de instrumentos separada por vírgulas para supervisionar. Quando vazia, a estratégia recorre ao `Security` atribuído. | `"GBPUSD,USDCAD,USDCHF,USDSEK"` |
| `ProfitTarget` | Lucro líquido (em moeda do portfólio) necessário para acionar um fechamento global de todas as posições observadas. | `60` |
| `MaxLoss` | Perda máxima aceitável (em moeda do portfólio) antes de a estratégia fechar forçosamente a cesta. | `60` |
| `Slippage` | Parâmetro de compatibilidade que reflete o slippage permitido do script original. Ordens de mercado são usadas para saídas, portanto o valor é informativo. | `10` |
| `MinAgeSeconds` | Tempo de vida mínimo de uma posição antes de a estratégia poder fechá-la. | `60` |
| `CandleType` | Tipo de candle usado para supervisão periódica (padrão: candles de 1 minuto). | `1 minute` |

## Notas

- A estratégia depende de `Positions[i].PnL` fornecido pelo StockSharp para medir o lucro flutuante. Ela não busca histórico de trades nem calcula preços manualmente.
- Posições abertas antes do início da estratégia herdam o tempo de início como seu primeiro timestamp visto. Elas serão fechadas apenas após o intervalo `MinAgeSeconds` decorrido desde o início da estratégia.
- As saídas são executadas com ordens de mercado para maximizar a probabilidade de liquidação imediata. `Slippage` é registrado por paridade com a versão MQL, mas não é aplicado aos cálculos de preço.
- A saída do registro replica a janela "Comment" do MetaTrader, imprimindo o lucro de cada símbolo seguido do total geral da cesta.

## Requisitos

- Atribuir um `SecurityProvider` válido ou garantir que os identificadores solicitados estejam disponíveis através do conector.
- Fornecer configuração de volume suficiente por instrumento para que as ordens de mercado possam achatar completamente a posição.
