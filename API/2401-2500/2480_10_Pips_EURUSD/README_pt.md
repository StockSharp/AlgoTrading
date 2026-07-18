# Estratégia de 10 Pips EURUSD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de 10 Pips EURUSD** é um sistema de rompimento que reproduz a lógica do Expert Advisor original do MetaTrader. Ela observa a vela completada mais recente e coloca ordens stop acima e abaixo desse intervalo. As ordens são dimensionadas em pips, ajustadas ao tamanho de tick do instrumento atual, e opcionalmente gerenciadas por um trailing stop. A implementação usa assinaturas de velas de alto nível do StockSharp junto com atualizações do livro de ordens para manter o comportamento próximo à versão MQL enquanto permanece neutro com o broker.

## Lógica da estratégia
1. Assinar o tipo de vela selecionado e aguardar até que uma nova barra se torne ativa.
2. Capturar a máxima e mínima da vela anterior quando essa barra termina. As ordens pendentes são canceladas neste momento porque o EA original as limita a uma barra.
3. No primeiro tick da nova barra verificar que:
   - A abertura atual está dentro do intervalo da vela anterior (filtragem de gaps).
   - O preço atual está pelo menos três unidades pip afastado de ambos os extremos (um proxy para o nível de stop do broker).
4. Calcular o spread atual usando o melhor bid/ask. Se não houver dados de nível 1, a estratégia recorre ao tamanho do pip.
5. Colocar duas ordens stop pendentes:
   - **Buy Stop**: ativação em `máxima anterior + 2 × spread` com stop loss abaixo do preço de entrada em `StopLossPips` e, se o trailing estiver desabilitado, take profit em `máxima anterior + 2 × spread + TakeProfitPips`.
   - **Sell Stop**: ativação em `mínima anterior − spread` com níveis de saída simétricos.
6. Assim que a vela se completa, ou ambas as ordens são preenchidas/canceladas, o processo se repete para a próxima barra.

### Gerenciamento de posição
- Enquanto uma posição está aberta, a estratégia monitora o melhor bid/ask em cada atualização do livro de ordens.
- Se o trailing estiver desabilitado, a posição fecha quando o preço toca o stop ou take-profit fixo.
- Se o trailing estiver habilitado:
  - Para operações compradas, o trailing stop é ativado assim que o preço avança `TrailingStopPips`. O stop é definido em `bid − TrailingStopPips` e se move cada vez que o preço melhora pelo menos `TrailingStepPips`.
  - Para operações vendidas, a lógica espelha o lado comprado usando o preço ask.
- As saídas manuais reiniciam todos os níveis de proteção e mantêm qualquer ordem stop pendente do lado oposto ativa até que a vela termine, reproduzindo o comportamento straddle do EA.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `Volume` | `0.01` | Volume da ordem em lotes (ou unidades de contrato para símbolos não FX). |
| `StopLossPips` | `50` | Distância entre a entrada e o stop de proteção, expressa em pips. |
| `TakeProfitPips` | `150` | Distância ao take-profit em pips, usada apenas quando o trailing está desabilitado. |
| `UseTrailing` | `false` | Habilita a lógica do trailing stop. |
| `TrailingStopPips` | `50` | Distância inicial para o trailing stop, medida em pips. |
| `TrailingStepPips` | `25` | Ganho mínimo (em pips) necessário para mover um trailing stop ativo. |
| `CandleType` | `período de 15 minutos` | Série de velas usada para detectar os níveis de rompimento. |

## Notas e recomendações
- O tamanho do pip é derivado automaticamente de `Security.PriceStep` e emula o ajuste de dígitos MQL, por isso a estratégia se adapta a símbolos FX de 3 e 5 dígitos.
- Todas as distâncias são recalculadas em unidades de preço antes de colocar ordens, o que mantém a estratégia compatível com ativos não FX, desde que o tamanho do tick esteja definido.
- O fallback do nível de stop mínimo (três unidades pip) imita o comportamento do EA original quando o broker não reporta um nível de stop.
- Como as ordens pendentes expiram no final de cada vela, você deve executar a estratégia no período desejado sem gaps no fluxo de velas recebido.
- O gerenciamento de risco é crucial. Considere testar com spreads realistas e modelos de comissão antes de negociar com capital real.
