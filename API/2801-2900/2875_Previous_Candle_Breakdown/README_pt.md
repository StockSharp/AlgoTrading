# Estratégia de Rompimento da Vela Anterior
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia de Rompimento da Vela Anterior** observa a máxima e a mínima da vela mais recentemente fechada de um período definido pelo usuário (padrão: 4 horas). Sempre que a vela em tempo real perfura além desses níveis de referência por uma margem configurável, a estratégia abre operações de rompimento. Um filtro de tendência de média móvel opcional mantém as operações alinhadas com a direção prevalecente, enquanto a lógica de saída em camadas (stop loss fixo, take profit e trailing stop baseado em pips) gerencia o risco após a entrada.

## Características principais

- Usa uma vela de período superior como âncora de rompimento. Todos os sinais se originam da máxima ou mínima da última vela de referência concluída.
- Suporta quatro tipos de média móvel (SMA, EMA, Smoothed, WMA) com deslocamentos independentes para as linhas rápida e lenta. Quando ambos os períodos são diferentes de zero, o filtro exige que a MA rápida esteja acima/abaixo da MA lenta antes de aceitar operações compradas/vendidas.
- Converte distâncias baseadas em pips (margem, stop loss, take profit, trailing stop e passo) em unidades de preço usando as configurações do instrumento. Para instrumentos de 3 ou 5 casas decimais, o pip equivale a 10 passos de preço, espelhando a lógica MQL original.
- Permite o dimensionamento de posição por volume fixo ou arriscando um percentual do patrimônio da conta relativo à distância do stop loss.
- Limita o número máximo de entradas por direção e opcionalmente fecha todas as posições abertas quando o lucro flutuante atinge uma quantia em dinheiro especificada.
- A lógica do trailing stop emula o consultor especialista MQL5: depois que o preço avança além da margem de trailing mais o passo, o nível de stop avança em passos discretos.

## Parâmetros

| Nome | Descrição |
|------|-----------|
| `CandleType` | Período usado para construir a referência da vela anterior (padrão: 4 horas). |
| `IndentPips` | Distância em pips adicionada acima da máxima ou abaixo da mínima antes de acionar entradas. |
| `FastPeriod` / `SlowPeriod` | Comprimentos das médias móveis. Definir qualquer um como 0 para desabilitar o filtro de tendência. |
| `FastShift` / `SlowShift` | Deslocamento horizontal (em barras) aplicado a cada média móvel antes da comparação. |
| `MaType` | Método de cálculo da média móvel (Simple, Exponential, Smoothed, Weighted). |
| `StopLossPips` | Distância em pips para o stop de proteção inicial. Definir como 0 para desabilitar. |
| `TakeProfitPips` | Distância em pips para ordens de take profit. Definir como 0 para desabilitar. |
| `TrailingStopPips` | Distância do trailing stop. Requer `TrailingStepPips` > 0. |
| `TrailingStepPips` | Melhoria mínima de pips antes do trailing stop ser atualizado. |
| `OrderVolume` | Volume de operação fixo. Deixar em 0 para dimensionar posições por percentual de risco. |
| `RiskPercent` | Percentual do patrimônio do portfólio a arriscar por operação quando `OrderVolume` é 0. Requer stop loss diferente de zero. |
| `MaxPositions` | Número máximo de entradas permitidas por direção. |
| `ProfitClose` | Fecha todas as posições abertas quando o lucro flutuante atinge este valor (moeda base). |

## Lógica de operação

1. Rastrear a vela concluída mais recente do `CandleType` e armazenar sua máxima/mínima.
2. Em cada atualização da vela atual:
   - Aplicar o filtro de média móvel se habilitado. Sem histórico de MA suficiente, a estratégia aguarda.
   - Calcular os níveis de rompimento: máxima anterior + margem e mínima anterior − margem.
   - Quando a máxima da vela atual cruza o nível superior, abrir uma posição comprada (sujeito a filtros, contagem máxima de posições e bloqueio de entrada por vela).
   - Quando a mínima da vela atual cruza o nível inferior, abrir uma posição vendida usando as mesmas verificações.
3. Após a entrada, a estratégia anexa os níveis de stop loss e take profit (se configurados) e os mantém em memória. Quando o preço toca qualquer limite, a posição é fechada por ordem a mercado.
4. A ativação do trailing stop replica o consultor especialista MQL: o preço deve exceder a margem de trailing mais o passo de trailing antes de mover o stop. Atualizações subsequentes requerem outra melhoria completa de `TrailingStepPips`.
5. O lucro flutuante é recalculado a cada tick a partir do preço de entrada médio. Se atingir `ProfitClose`, toda a exposição aberta é liquidada imediatamente.
6. Para o dimensionamento baseado em risco, a estratégia converte a distância do stop em pips para moeda usando o `PriceStep` e `StepPrice` do instrumento. O volume resultante respeita `MaxPositions` para escalonamento.

## Notas

- Defina `TrailingStopPips` como 0 para desabilitar o trailing. Se você habilitar o trailing, certifique-se de que `TrailingStepPips` também seja positivo; caso contrário, não ocorrerão atualizações de trailing.
- A estratégia armazena registros de hora de entrada por vela para evitar múltiplas entradas na mesma barra de referência, correspondendo ao comportamento original do EA.
- Para instrumentos sem metadados `PriceStep`/`StepPrice`, o dimensionamento baseado em risco não pode ser calculado e as operações serão ignoradas, a menos que `OrderVolume` seja especificado.
- Todos os comentários no código são escritos em inglês para se alinhar com as diretrizes do projeto.

## Arquivos

- `CS/PreviousCandleBreakdownStrategy.cs` – Implementação em C# da estratégia.

A tradução para Python não está disponível para esta estratégia.
