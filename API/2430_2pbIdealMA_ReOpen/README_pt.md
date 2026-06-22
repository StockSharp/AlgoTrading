# Estratégia 2pb Ideal MA ReOpen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- Implementa o especialista MQL "Exp_2pbIdealMA_ReOpen" usando a API de alto nível do StockSharp.
- Opera um cruzamento contrário entre uma média móvel ideal simples e uma média móvel ideal de triplo estágio.
- Adiciona a posições vencedoras quando o preço avança um número configurável de ticks e opcionalmente fecha posições em sinais opostos.

## Indicadores
- **2pb Ideal 1 MA** – média móvel ideal simples com dois períodos de ponderação. Reage rapidamente e define o viés de curto prazo.
- **2pb Ideal 3 MA** – tripla cascata do mesmo filtro ideal (estágios X, Y, Z). Reage mais lentamente e representa a tendência de fundo.

## Lógica de negociação
1. Assinar a série de velas selecionada (padrão H4) e avaliar sinais apenas em velas fechadas.
2. Armazenar valores de filtro `SignalBarShift` barras atrás (padrão 1). Usar o par de valores nos deslocamentos `SignalBarShift` e `SignalBarShift + 1` para detectar cruzamentos.
3. **Entrada comprada** – quando o filtro rápido estava acima do filtro lento duas barras atrás e caiu abaixo uma barra atrás (cruzamento de baixa), abrir uma posição comprada se as entradas compradas estiverem habilitadas e nenhuma posição estiver aberta.
4. **Entrada vendida** – quando o filtro rápido estava abaixo do filtro lento duas barras atrás e subiu acima uma barra atrás (cruzamento de alta), abrir uma posição vendida se as entradas vendidas estiverem habilitadas e nenhuma posição estiver aberta.
5. **Reentradas** – enquanto uma posição for lucrativa, adicionar uma ordem de `PositionVolume` assim que o preço se mover `PriceStepTicks * Security.PriceStep` na direção da operação. O número de adições por direção é limitado por `MaxReEntries`.
6. **Saídas** – se o cruzamento oposto aparecer e o respectivo indicador de saída estiver habilitado, fechar a posição aberta antes de considerar novas entradas.
7. Aplicar stop loss e take profit opcionais usando as distâncias de ticks configuradas.

## Parâmetros
- `CandleType` – período da série de velas de trabalho.
- `PositionVolume` – volume base para entradas e reentradas (também atribuído a `Strategy.Volume`).
- `StopLossTicks` / `TakeProfitTicks` – distâncias de proteção expressas em ticks; convertidas em preço usando `Security.PriceStep`.
- `PriceStepTicks` – número de ticks necessários entre ordens de reentrada sucessivas.
- `MaxReEntries` – número máximo de negociações adicionais por direção.
- `EnableBuyEntries` / `EnableSellEntries` – permitir a abertura de posições compradas ou vendidas.
- `EnableBuyExits` / `EnableSellExits` – fechar posições existentes quando o sinal oposto aparecer.
- `SignalBarShift` – número de barras atrás usadas para avaliar o cruzamento (imita o `SignalBar` original).
- `Period1`, `Period2` – ponderações para a média móvel ideal simples.
- `PeriodX1`, `PeriodX2`, `PeriodY1`, `PeriodY2`, `PeriodZ1`, `PeriodZ2` – ponderações para cada estágio da média móvel ideal tripla.

## Gestão de risco
- As proteções de stop loss e take profit são ativadas através de `StartProtection` se as distâncias de ticks correspondentes forem maiores que zero.
- A estratégia não abre novos trades enquanto uma posição oposta ainda estiver aberta, espelhando o comportamento MQL.

## Notas
- Funciona com qualquer instrumento que forneça `Security.PriceStep`; a configuração padrão visa velas H4.
- Não é fornecida versão em Python, de acordo com a solicitação original.
