# Estratégia e-TurboFx Steps
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
A estratégia **e-TurboFx** é um sistema de reversão por exaustão de momentum originalmente escrito para MetaTrader 5. Ela monitora as velas concluídas mais recentes e procura sequências onde os corpos das velas continuam se expandindo na mesma direção. Uma série crescente de velas baixistas indica capitulação e, portanto, uma possível configuração de compra, enquanto uma série crescente de velas altistas anuncia uma possível oportunidade de venda. O port do StockSharp usa a API de alto nível com assinaturas de velas e proteção automatizada de posição.

## Lógica de Trading
- Inspecionar as últimas `DepthAnalysis` velas concluídas do `CandleType` selecionado.
- Contar quantas velas consecutivas fecharam abaixo de sua abertura (baixistas) e quantas fecharam acima de sua abertura (altistas).
- Rastrear a progressão do tamanho do corpo: cada nova vela na sequência deve ter um corpo absoluto maior que a anterior. Quando essa condição falhar, a sequência é redefinida.
- **Entrada comprada:** `DepthAnalysis` velas baixistas consecutivas com corpos estritamente em expansão acionam uma compra a mercado, desde que nenhuma posição esteja aberta no momento.
- **Entrada vendida:** `DepthAnalysis` velas altistas consecutivas com corpos estritamente em expansão acionam uma venda a mercado, igualmente apenas quando a posição está zerada.
- Enquanto uma posição está ativa, a estratégia pausa a detecção de sinais para evitar o empilhamento de trades. O gerenciamento de risco é delegado ao bloco de proteção integrado configurado no início.

## Gerenciamento de Posição
- `StartProtection` registra automaticamente ordens de stop-loss e take-profit usando distâncias medidas em passos de preço (ticks do exchange). Definir uma distância como zero desabilita a ordem de proteção correspondente.
- A estratégia mantém apenas uma posição aberta. Quando um novo sinal aparece depois que o trade anterior é fechado, as sequências de velas são reconstruídas do zero com base em dados de mercado frescos.
- As entradas a mercado usam o parâmetro `TradeVolume`. Alterar o parâmetro na UI atualiza imediatamente o volume da estratégia.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-----------|--------|
| `DepthAnalysis` | Número de velas concluídas recentes usadas para validar o padrão de expansão. Valores mais altos exigem sequências mais longas antes de operar. | `3` |
| `TakeProfitSteps` | Distância do take-profit em passos de preço do exchange (ticks). `0` desabilita o take-profit. | `120` |
| `StopLossSteps` | Distância do stop-loss em passos de preço do exchange (ticks). `0` desabilita o stop-loss. | `70` |
| `TradeVolume` | Volume de ordem enviado com cada entrada a mercado. | `0.1` |
| `CandleType` | Tipo de dados de vela (período) assinado para a análise. | Período de `1 hora` |

Todos os parâmetros numéricos têm metadados de otimização para que possam ser incluídos nas otimizações do StockSharp, se desejado.

## Notas e Recomendações
- O consultor especializado MQL5 original recalculava os dados de velas a cada tick; a implementação do StockSharp alcança o mesmo comportamento com eventos de velas concluídas e contadores internos.
- Como a estratégia depende de comparações de corpo de velas, ela é sensível ao período selecionado. Períodos mais curtos produzirão mais sinais, mas podem exigir stops mais apertados.
- Certifique-se de que o instrumento conectado exponha um `PriceStep` válido para que as distâncias de stop-loss e take-profit definidas em passos sejam traduzidas corretamente para preços.
- Antes de operar ao vivo, valide o comportamento no Designer/Backtester para confirmar que as distâncias de stop e alvo se alinhem com o instrumento escolhido.
