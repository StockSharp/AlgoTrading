# Estratégia Elli Ichimoku ADX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia é um port em C# do especialista MetaTrader 5 "Elli" (edição de barabashkakvn). Combina a estrutura do Ichimoku Kinko Hyo com um filtro de rompimento do Average Directional Index (+DI). As operações são abertas somente quando um forte impulso direcional é confirmado simultaneamente pelo alinhamento das linhas de Ichimoku e um aumento repentino no índice de direção positiva.

A implementação do StockSharp mantém o comportamento original de trabalhar com dois fluxos de velas: a análise de Ichimoku é realizada em um período superior (padrão 1 hora) enquanto o ADX é avaliado em uma série mais rápida (padrão 1 minuto). As ordens são inseridas com um stop protetor fixo e alvo medidos em passos de preço, idênticos ao consultor especialista original.

## Indicadores e dados
- **Ichimoku** (Tenkan 19, Kijun 60, Senkou Span B 120 por padrão).
- **Average Directional Index (ADX)**, apenas a linha +DI é usada como no código-fonte.
- Áreas de gráfico opcionais exibem a série de velas, a nuvem de Ichimoku e a linha ADX.

Duas subscrições de velas independentes são criadas:
1. `IchimokuCandleType` (padrão 1 hora) – impulsiona os cálculos de Ichimoku e gera decisões de negociação.
2. `AdxCandleType` (padrão 1 minuto) – alimenta o indicador ADX e fornece valores +DI atuais/anteriores.

## Parâmetros
| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `TakeProfitPoints` | 60 | Distância de take profit em passos de preço. Definir como 0 para desabilitar. |
| `StopLossPoints` | 30 | Distância de stop loss em passos de preço. Definir como 0 para desabilitar. |
| `TenkanPeriod` | 19 | Comprimento da linha Tenkan-sen (linha de conversão) do Ichimoku. |
| `KijunPeriod` | 60 | Comprimento da linha Kijun-sen (linha base) do Ichimoku. |
| `SenkouSpanBPeriod` | 120 | Comprimento da linha Senkou Span B do Ichimoku. |
| `AdxPeriod` | 10 | Período para o indicador ADX. |
| `PlusDiHighThreshold` | 13 | Limiar que o valor atual +DI deve exceder. |
| `PlusDiLowThreshold` | 6 | Limiar abaixo do qual o valor anterior +DI deve permanecer. |
| `BaselineDistanceThreshold` | 20 | Separação mínima entre Tenkan/Kijun (em passos de preço) necessária para confirmar momentum. |
| `IchimokuCandleType` | velas de 1 hora | Série de velas usada para a avaliação de Ichimoku. |
| `AdxCandleType` | velas de 1 minuto | Série de velas usada para o cálculo de ADX. |

## Lógica de negociação
1. Aguardar uma vela de Ichimoku terminada.
2. Garantir que ADX tenha pelo menos dois valores terminados e a última leitura produziu um rompimento +DI (`+DI anterior < PlusDiLowThreshold` e `+DI atual > PlusDiHighThreshold`).
3. Converter a separação Tenkan/Kijun em passos de preço e verificar se excede `BaselineDistanceThreshold`.
4. Todas as ordens são bloqueadas se uma posição aberta já existir.
5. **Comprar** quando:
   - Tenkan > Kijun.
   - Kijun > Senkou Span A.
   - Senkou Span A > Senkou Span B (nuvem altista).
   - Preço de fechamento > Kijun.
6. **Vender** quando o alinhamento inverso é observado (Tenkan < Kijun < Senkou Span A < Senkou Span B e o fechamento está abaixo de Kijun).
7. As saídas de posição dependem do stop protetor e do alvo configurados via `StartProtection`. Nenhuma saída discricionária é acionada; isso reflete o EA original que aguardava stops/alvos ou intervenção manual.

## Gestão de risco
`StartProtection` é chamado uma vez na inicialização. Se o stop ou o alvo for zero, a proteção respectiva é omitida. As ordens são enviadas com execução de mercado (`BuyMarket`/`SellMarket`), correspondendo à implementação MQL que usava ordens de mercado com SL/TP anexados.

## Notas de implementação
- Apenas o índice de direção positiva é usado para sinais longos e curtos, replicando a lógica do código MQL5 (o autor original comentou o ramo -DI).
- A estratégia não rastreia a linha Chikou explicitamente; em vez disso, o alinhamento da nuvem é validado comparando Senkou Span A e B.
- Campos internos armazenam os últimos dois valores +DI sem chamar `GetValue`, de acordo com as diretrizes da API de alto nível.
- Se ambos os parâmetros de vela forem idênticos, uma única subscrição é reutilizada para Ichimoku e ADX para reduzir o overhead.

## Dicas de uso
- Manter `AdxCandleType` mais rápido que `IchimokuCandleType` para emular a versão MT5 (p.ex., ADX M1 vs. Ichimoku H1).
- Aumentar `BaselineDistanceThreshold` em instrumentos de alta volatilidade para exigir maior separação Tenkan/Kijun.
- Como o especialista abre apenas uma posição de cada vez, combinar a estratégia com controles de risco no nível do portfólio ao negociar múltiplos símbolos.
