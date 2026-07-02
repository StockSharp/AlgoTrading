# Estratégia do sistema HBS (versão StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **HBS System Strategy** é uma conversão StockSharp de alto nível do MetaTrader 4 consultor especialista "HBS system.mq4" (ForTrader.ru). O EA original combina filtragem de média móvel exponencial com ordens de stop pendentes que são arredondadas para níveis de preço fixos. Duas ordens de stop são implantadas na direção da tendência: a primeira visa um nível arredondado próximo e a segunda busca um rompimento estendido. Ambas as negociações compartilham o mesmo stop de proteção e lógica de trilha, produzindo uma estrutura de rompimento em camadas.

Esta porta StockSharp mantém o comportamento de vários pedidos enquanto adota o API de alto nível. Os pedidos são enviados por meio de auxiliares de pedidos pendentes (`BuyStop`, `SellStop`, `SellLimit`, `BuyLimit`) e o risco é controlado por meio de paradas de proteção mantidas dinamicamente. O código é totalmente comentado em inglês para facilitar a manutenção.

## Lógica de negociação

1. **Filtro de tendência** – Uma média móvel exponencial (EMA) calculada sobre o preço médio (`(High + Low) / 2`) de velas concluídas define a tendência ativa. Apenas velas totalmente formadas são processadas, espelhando o comportamento `iMA(..., shift=1)` de MetaTrader.
2. **Arredondamento de nível** – O preço de fechamento da vela anterior é arredondado para cima e para baixo usando um multiplicador configurável (padrão `100`, ou seja, duas casas decimais). Esses valores arredondados emulam as chamadas `MathCeil`/`MathFloor` originais.
3. **Construção de Entrada** – Quando a vela anterior abre e fecha acima de EMA, duas ordens stop de compra são colocadas:
   - **Ordem primária** em `roundedHigh - entryOffset` com lucro igual ao nível arredondado.
   - **Ordem secundária** com o mesmo preço de entrada, mas com um take-profit alterado ainda mais em `secondaryTakeProfitPoints`.
   - Ambas as ordens compartilham um stop loss comum (`entry - stopLossPoints`).

A lógica é espelhada para vendas quando a vela abre e fecha abaixo de EMA. Ordens pendentes opostas são canceladas automaticamente para evitar sobreposição.
4. **Gerenciamento de posição** – Quando uma ordem pendente é preenchida, a estratégia registra uma ordem de limite de lucro dedicada e atualiza o stop-loss compartilhado. A lógica de trailing stop aperta o stop quando o preço se move a favor da posição aberta, respeitando as distâncias finais configuradas.
5. **Limpeza** – Pedidos concluídos ou cancelados são removidos do registro interno. Quando a posição líquida retorna ao nível estável, todas as ordens de proteção são canceladas para redefinir o estado.

## Parâmetros

| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `EMA Period` | Comprimento do filtro de média móvel exponencial. | 200 |
| `Buy Stop-Loss (points)` | Distância (em pontos) entre a entrada longa e a sua parada protetora. | 50 |
| `Buy Trailing (points)` | Distância final para posições longas. | 10 |
| `Sell Stop-Loss (points)` | Distância (em pontos) entre a entrada curta e sua parada protetora. | 50 |
| `Sell Trailing (points)` | Distância final para posições curtas. | 10 |
| `Order Volume` | Volume aplicado a **cada** ordem pendente. Com as duas ordens padrão, a exposição máxima é igual a duas vezes esse valor. | 0,1 |
| `Entry Offset (points)` | Compensação (em pontos) subtraída/adicionada do nível arredondado para obter o preço de entrada pendente. | 15 |
| `Second Take-Profit (points)` | Distância adicional usada pela meta secundária de lucro. | 15 |
| `Rounding Factor` | Multiplicador usado para lógica de arredondamento (por exemplo, 100 → duas casas decimais). | 100 |
| `Candle Type` | Tipo de dados para agregação de velas. O padrão é um período de 1 hora. | `TimeFrame(1h)` |

## Notas para uso

- Certifique-se de que `Security.PriceStep` (ou `Security.Decimals`) esteja configurado; caso contrário, a estratégia volta para um valor de 0,0001 pontos.
- Cada ordem pendente gerencia seu próprio lucro, portanto a posição total pode ser ampliada em dois estágios.
- Os trailing stops só são ativados depois que o preço se move a favor na distância configurada (`TrailingStop{Buy/Sell}Points`).
- A estratégia pressupõe preços tradicionais no estilo Forex, onde o arredondamento para duas casas decimais é significativo. Ajuste o `RoundingFactor` se uma precisão diferente for necessária.
- Não estão incluídas regras automatizadas de gestão de dinheiro; defina `OrderVolume` de acordo com as preferências de risco.

## Destaques da conversão

- Todos os comentários foram reescritos em inglês e a estrutura segue o guia de estilo do repositório (abas, namespace, nomenclatura).
- Auxiliares StockSharp de alto nível são usados para assinatura de dados, gerenciamento de pedidos pendentes e tratamento de pedidos de proteção.
- A coordenação de trailing stop e take-profit reproduz a arquitetura de ordem dupla do especialista MetaTrader original, permanecendo idiomática para StockSharp.

## Referências

- Script MT4 original: `MQL/8134/HBS_system.mq4`
- Documentação StockSharp: [https://doc.stocksharp.com/](https://doc.stocksharp.com/)
