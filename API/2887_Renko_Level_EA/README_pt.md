# Estratégia Renko Level EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- Convertida do consultor especializado MetaTrader **Renko Level EA.mq5**.
- Emula o indicador original mantendo um nível Renko superior e inferior derivado do parâmetro `BrickSize`.
- Avalia velas finalizadas fornecidas por `CandleType` (padrão: período de 1 minuto) e reage quando a grade Renko se desloca.
- Não usa stops ou alvos fixos; cada saída ocorre por meio de um sinal oposto.

## Lógica de trading
1. Na primeira vela finalizada o preço de fechamento é arredondado para a grade Renko para inicializar os níveis superior e inferior.
2. Para cada vela subsequente:
   - Se o fechamento permanecer entre os limites atuais, a grade permanece inalterada.
   - Um fechamento acima do nível superior eleva o bloco Renko para cima para o próximo valor da grade.
   - Um fechamento abaixo do nível inferior empurra o bloco para baixo.
3. Uma mudança no nível Renko superior é interpretada como um rompimento direcional.
   - Nível superior crescente → sinal altista (a menos que `ReverseSignals` esteja habilitado).
   - Nível superior decrescente → sinal baixista.
4. Os sinais podem opcionalmente ser invertidos (`ReverseSignals`) ou piramidados (`AllowIncrease`) para corresponder ao comportamento do EA original.

## Gestão de ordens
- Antes de entrar comprado, qualquer posição vendida é fechada; o oposto acontece antes de entrar vendido.
- Quando `AllowIncrease = false`, a estratégia abre um novo trade apenas se não existir nenhuma posição nessa direção.
- Quando `AllowIncrease = true`, ordens adicionais de tamanho `OrderVolume` são permitidas mesmo se uma posição já estiver aberta.
- Não há stop-loss ou take-profit dedicados; os reversais de posição servem como mecanismo de saída.
- `StartProtection()` é invocado uma vez para manter as salvaguardas de risco alinhadas com o framework base.

## Parâmetros
| Nome | Descrição | Padrão | Otimizável |
| --- | --- | --- | --- |
| `BrickSize` | Tamanho do bloco Renko medido como múltiplos de `Security.PriceStep`. Define o quanto o preço deve se mover para deslocar a grade. | `30` | Sim (10 → 100 passo 10) |
| `OrderVolume` | Volume enviado com cada ordem a mercado. | `1` | Não |
| `ReverseSignals` | Inverte as ações altistas e baixistas. Espelha a entrada *Reverse* do EA. | `false` | Não |
| `AllowIncrease` | Permite adicionar a uma posição existente em vez de esperar por uma posição plana. Espelha o indicador *Increase* do EA. | `false` | Não |
| `CandleType` | Fonte de velas usada para os cálculos. Padrão para velas de período de 1 minuto, mas qualquer série suportada pode ser fornecida. | `TimeFrameCandleMessage(1m)` | Não |

## Notas práticas
- `BrickSize` se adapta automaticamente ao instrumento negociado porque multiplica o `PriceStep` definido pela bolsa.
- A decisão é baseada puramente em preços de fechamento; movimentos intrabarra importam apenas quando formam o fechamento final.
- Combinar `ReverseSignals` e `AllowIncrease` permite testar variantes tanto contratendência quanto de piramidação do EA.
- Funciona em qualquer mercado onde a lógica de rompimento estilo Renko é relevante, incluindo forex, futuros e instrumentos cripto.

## Classificação
- **Regime**: Seguidor de tendência (rompimento Renko).
- **Direção**: Comprado/Vendido.
- **Complexidade**: Moderado (rastreamento de nível personalizado, ajuste mínimo).
- **Stops**: Nenhum; saídas em sinais inversos.
- **Período**: Configurável via `CandleType`.
- **Indicadores**: Projeção de nível Renko personalizada.
